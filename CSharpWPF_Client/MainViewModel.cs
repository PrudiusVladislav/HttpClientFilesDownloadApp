using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shapes;
using CSharpWPF_Client.Infrastructure;
using Microsoft.WindowsAPICodePack.Dialogs;
using CSharpWPF_Client.ClientLogic;

namespace CSharpWPF_Client;

public class MainViewModel : ObservableObject
{
    private string _downloadFileName;
    private string _pauseResumeButtonText;
    private string _savePath;
    private FileCommand _selectedFileCommandOption;
    private FileDownloadClient _currentDownloadClient;
    private readonly Client _client = new Client();
    
    public ObservableCollection<string> DownloadedFiles { get; set; } = new();
    public ObservableCollection<string> CanceledDownloads { get; set; } = new();
    public ObservableCollection<string> CurrentDownloads { get; set; } = new();
    
    
    public ICommand DownloadCommand { get; set; }
    public ICommand PauseResumeCommand { get; set; }
    public ICommand StopDownloadCommand { get; set; }
    public ICommand ExecuteFileOptionCommand { get; set; }
    public ICommand SelectSavePathCommand { get; set; }
    
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
        set
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
    public FileCommand SelectedFileCommandOption
    {
        get => _selectedFileCommandOption;
        set
        {
            _selectedFileCommandOption = value;
            OnPropertyChanged();
        }
    }

    public MainViewModel()
    {
        PauseResumeButtonText = "Pause";
        
        DownloadCommand = new RelayCommand((action) =>
        {
            ExecuteDownloadCommand();
        }, o => !(string.IsNullOrWhiteSpace(DownloadFileName) || string.IsNullOrWhiteSpace(SavePath) || !Directory.Exists(SavePath)));
        SelectSavePathCommand = new RelayCommand((action) =>
        {
            ExecuteSelectSavePathCommand();
        }, o => true);
        PauseResumeCommand = new RelayCommand((action) =>
        {
            ExecutePauseResumeCommand();
        }, o => CurrentDownloads.Count > 0);
        StopDownloadCommand = new RelayCommand((action) =>
        {
            _currentDownloadClient.Stop();
        }, o => CurrentDownloads.Count > 0);
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

        _currentDownloadClient =  new FileDownloadClient(_client.HttpClient, DownloadFileName, fullSavePath, 4096);
        var downloadTask = _currentDownloadClient.StartDownloadAsync();
        var downloadName = System.IO.Path.GetFileName(_currentDownloadClient.SavePath);
        CurrentDownloads.Add(downloadName);
        var result = await downloadTask;
        CurrentDownloads.Remove(downloadName);
        switch (result)
        {
            case FileDownloadResult.SuccessfulDownload:
            {
                DownloadedFiles.Add(downloadName);
                break;
            }
            case FileDownloadResult.FileNotFound:
            {
                CanceledDownloads.Add(downloadName);
                MessageBox.Show($"Downloading of the file {downloadName} has been terminated: no file with such name is available for downloading",
                    "Download terminated", MessageBoxButton.OK, MessageBoxImage.Error);
                break;
            }
            case FileDownloadResult.StoppedDownload:
            {
                CanceledDownloads.Add(downloadName);
                MessageBox.Show($"Downloading of the file {downloadName} has been terminated: operation was cancelled by user",
                    "Download terminated", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                break;
            }
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
    
    private void ExecuteSelectSavePathCommand()
    {
        var folderDialog = new CommonOpenFileDialog();
        folderDialog.IsFolderPicker = true;
        if (folderDialog.ShowDialog() == CommonFileDialogResult.Ok)
        {
            SavePath = folderDialog.FileName;
        }
    }
}