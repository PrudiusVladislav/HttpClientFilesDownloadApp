using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using System.Windows.Shapes;
using CSharpWPF_Client.Infrastructure;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace CSharpWPF_Client;

public class MainViewModel : ObservableObject
{
    private string _downloadFileName;
    private string _pauseResumeButtonText;
    private string _savePath;
    private FileCommand _selectedFileCommandOption;
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
        DownloadCommand = new RelayCommand((action) =>
        {
            ExecuteDownloadCommand();
        }, o => !(string.IsNullOrWhiteSpace(DownloadFileName) || string.IsNullOrWhiteSpace(SavePath) || !Directory.Exists(SavePath)));
        SelectSavePathCommand = new RelayCommand((action) =>
        {
            ExecuteSelectSavePathCommand();
        }, o => true);
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
        await _client.DownloadFileAsync(DownloadFileName, fullSavePath);
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