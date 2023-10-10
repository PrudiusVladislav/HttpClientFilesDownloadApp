
using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using CSharpWPF_Client.Infrastructure;

namespace CSharpWPF_Client;

public class RenameWindowViewModel : ObservableObject
{
    private string? _newFileName;
    private bool? _dialogResult;
    public string? PreRenameFilePath { get; set; }
    public ICommand RenameCommand { get; set; }
    public bool RenamingResult { get; private set; }

    public string NewFileFullPath => Path.Combine(Path.GetDirectoryName(PreRenameFilePath!)!,
        $"{NewFileName}{Path.GetExtension(PreRenameFilePath)}");
    
    public bool? DialogResult
    {
        get => _dialogResult; 
        set
        {
            _dialogResult = value;
            OnPropertyChanged();
        }
    }
    public string? NewFileName
    {
        get => _newFileName;
        set
        {
            _newFileName = value;
            OnPropertyChanged();
        }
    }

    public RenameWindowViewModel()
    {
        NewFileName = PreRenameFilePath;
        RenamingResult = false;
        
        RenameCommand = new RelayCommand((action) =>
            {
                ExecuteRenameFileCommand();
            },
            o => !(string.IsNullOrWhiteSpace(NewFileName) ||
                   Path.GetFileNameWithoutExtension(PreRenameFilePath!).Equals(NewFileName, StringComparison.OrdinalIgnoreCase)));
    }

    private void ExecuteRenameFileCommand()
    {
        if (Path.Exists(NewFileFullPath))
        {
            var mboxResult = MessageBox.Show("File with such name already exists.\n" +
                            "Do you want to overwrite content of the existing file (OK)\n " +
                            "OR cancel the renaming process and try a new name (CANCEL)?",
                "Invalid file name", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (mboxResult == MessageBoxResult.Cancel)
                return;
        }
        
        try
        {
            File.Move(PreRenameFilePath!, NewFileFullPath, true);
            MessageBox.Show("File renamed successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            RenamingResult = true;
            DialogResult = true; //closes the window
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
}