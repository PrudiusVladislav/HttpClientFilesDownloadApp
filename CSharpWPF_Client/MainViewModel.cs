using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shapes;
using CSharpWPF_Client.Infrastructure;
using Microsoft.WindowsAPICodePack.Dialogs;
using CSharpWPF_Client.ClientLogic;
using Path = System.IO.Path;

namespace CSharpWPF_Client;

public class MainViewModel : ObservableObject
{
    private string _downloadFileName;
    private string _pauseResumeButtonText;
    private string _savePath;
    private FileDownloadClient _currentDownloadClient;
    private readonly Client _client = new Client();
    
    public ObservableCollection<FileDownloadClient> DownloadedFiles { get; set; } = new();
    public ObservableCollection<FileDownloadClient> CanceledDownloads { get; set; } = new();
    public ObservableCollection<FileDownloadClient> CurrentDownloads { get; set; } = new();
    
    
    public ICommand DownloadCommand { get; set; }
    public ICommand PauseResumeCommand { get; set; }
    public ICommand StopDownloadCommand { get; set; }
    public ICommand SelectSavePathCommand { get; set; }
    public ICommand RenameFileCommand { get; set; }
    public ICommand DeleteFileCommand { get; set; }
    public ICommand RelocateFileCommand { get; set; }
    
    public string DownloadFileName
    {
        get => _downloadFileName;
        set
        {
            _downloadFileName = value;
            OnPropertyChanged();
        }
    }
    public string PauseResumeButtonText
    {
        get => _pauseResumeButtonText;
        private set
        {
            _pauseResumeButtonText = value;
            OnPropertyChanged();
        }
    }
    public string SavePath
    {
        get => _savePath;
        set
        {
            _savePath = value;
            OnPropertyChanged();
        }
    }

    public MainViewModel()
    {
        PauseResumeButtonText = "Pause";
        
        DownloadCommand = new RelayCommand((action) =>
        {
            ExecuteDownloadCommand();
        }, o => !(string.IsNullOrWhiteSpace(DownloadFileName) || string.IsNullOrWhiteSpace(SavePath) || !Directory.Exists(SavePath) || CurrentDownloads.Count > 0));
        SelectSavePathCommand = new RelayCommand((action) =>
        {
            ExecuteSelectSavePathCommand();
        }, o => CurrentDownloads.Count == 0);
        PauseResumeCommand = new RelayCommand((action) =>
        {
            ExecutePauseResumeCommand();
        }, o => CurrentDownloads.Count > 0);
        StopDownloadCommand = new RelayCommand((action) =>
        {
            ExecuteStopDownloadCommand();
        }, o => CurrentDownloads.Count > 0);
        RenameFileCommand = new RelayCommand(ExecuteRenameFileCommand, o => true);
    }
    
    
    private async void ExecuteDownloadCommand()
    {
        var fullSavePath = System.IO.Path.Combine(SavePath, DownloadFileName);
        if (File.Exists(fullSavePath))
        {
            var counter = 1;
            var fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(DownloadFileName);
            var fileExtension = System.IO.Path.GetExtension(DownloadFileName);
                
            while (File.Exists(fullSavePath))
            {
                fullSavePath = System.IO.Path.Combine(SavePath, $"{fileNameWithoutExtension} ({counter}){fileExtension}");
                counter++;
            }
        }

        try
        {
            _currentDownloadClient =  new FileDownloadClient(_client.HttpClient, DownloadFileName, fullSavePath, 4096);
            var downloadTask = _currentDownloadClient.StartDownloadAsync();
            CurrentDownloads.Add(_currentDownloadClient);
            var result = await downloadTask;
            CurrentDownloads.Remove(_currentDownloadClient);
            switch (result)
            {
                case FileDownloadResult.SuccessfulDownload:
                {
                    DownloadedFiles.Add(_currentDownloadClient);
                    break;
                }
                case FileDownloadResult.FileNotFound:
                {
                    CanceledDownloads.Add(_currentDownloadClient);
                    MessageBox.Show($"Downloading of the file {_currentDownloadClient.RequestedFileName} has been terminated: no file with such name is available for downloading",
                        "Download terminated", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
                }
                case FileDownloadResult.StoppedDownload:
                {
                    CanceledDownloads.Add(_currentDownloadClient);
                    MessageBox.Show($"Downloading of the file {_currentDownloadClient.RequestedFileName} has been terminated: operation was cancelled by user",
                        "Download terminated", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Download Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ExecutePauseResumeCommand()
    {
        switch (_currentDownloadClient.DownloadState)
        {
            case FileDownloadState.Paused:
            {
                _currentDownloadClient.Resume();
                PauseResumeButtonText = "Pause";
                break;
            }
            case FileDownloadState.Running:
            {
                _currentDownloadClient.Pause();
                PauseResumeButtonText = "Resume";
                break;
            }
        }
    }

    private async void ExecuteStopDownloadCommand()
    {
        await _currentDownloadClient.StopAsync();
    }
    
    private void ExecuteSelectSavePathCommand()
    {
        var folderDialog = new CommonOpenFileDialog();
        folderDialog.IsFolderPicker = true;
        if (folderDialog.ShowDialog() == CommonFileDialogResult.Ok)
        {
            SavePath = folderDialog.FileName;
        }
    }

    private void ExecuteRenameFileCommand(object? param)
    {
        if (param is FileDownloadClient selectedDownloadClient)
        {
            var renameWindow = new RenameFileWindow();
            if (renameWindow.DataContext is RenameWindowViewModel renameFileVm)
            {
                renameFileVm.PreRenameFilePath = selectedDownloadClient.SavePath;
                renameWindow.ShowDialog();
                if (renameFileVm.RenamingResult)
                {
                    DownloadedFiles.FirstOrDefault(f => f.SavePath.Equals(selectedDownloadClient.SavePath))!
                        .SavePath = renameFileVm.NewFileFullPath;
                }
            }
        }
    }
}