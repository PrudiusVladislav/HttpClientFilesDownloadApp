using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CSharpWPF_Client.Infrastructure;

namespace CSharpWPF_Client.ClientLogic;

public class FileDownloadClient : ObservableObject
{
    private readonly HttpClient _httpClient;
    private readonly string _source;
    private readonly int _chunkSize;
    private volatile bool _paused;
    private volatile bool _stopped;
    private volatile bool _downloadEnded;
    private int _bytesWritten;
    private string _savePath;
    private string _downloadedFileName;
    public FileDownloadState DownloadState
    {
        get
        {
            if(_paused)
                return FileDownloadState.Paused;
            return _stopped ? FileDownloadState.Stopped : FileDownloadState.Running;
        }
    }
    public string RequestedFileName { get; }
    public string DownloadedFileName
    {
        get => _downloadedFileName;
        set
        {
            if (_downloadedFileName != value)
            {
                _downloadedFileName = value;
                OnPropertyChanged();
            }
        }
    }
    public int ContentLength { get; private set; }

    public string SavePath
    {
        get => _savePath;
        set
        {
            if (_savePath != value)
            {
                _savePath = value;
                OnPropertyChanged();
                DownloadedFileName = Path.GetFileName(_savePath);
            }
        }
    }
    
    public FileDownloadClient(HttpClient httpClient, string requestedFileName, string savePath, int chunkSize)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        RequestedFileName = requestedFileName;
        _source = $"api/files/{requestedFileName}";
        SavePath = savePath;
        _chunkSize = chunkSize;
        _paused = false;
        _stopped = false;
        _downloadEnded = true;        
        
        ContentLength = Convert.ToInt32(GetContentLength());
        _bytesWritten = 0;
    }

    private long GetContentLength()
    {
        var response = _httpClient.Send(new HttpRequestMessage(HttpMethod.Head, _source));
        return !response.IsSuccessStatusCode ? -1 :  int.Parse(response.Content.Headers.FirstOrDefault(h => h.Key.Equals("Content-Length")).Value.First());
    }
    
    public async Task<FileDownloadResult> StartDownloadAsync()
    {
        _downloadEnded = false;
        var response = await _httpClient.GetAsync(_source);

        if (!response.IsSuccessStatusCode)
            return FileDownloadResult.FileNotFound;
        await using var responseStream = await response.Content.ReadAsStreamAsync();
        await using var fs = new FileStream(SavePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        var buffer = new byte[_chunkSize];
        //counter, to make the download freeze for only specified number of times
        var sleepTestCounter = 0;
        while (!_stopped && _bytesWritten < ContentLength)
        {
            if (_paused)
            {
                await Task.Delay(250);
                continue;
            }

            var bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length);
            
            if (bytesRead == 0) break;

            await fs.WriteAsync(buffer, 0, bytesRead);
            _bytesWritten += bytesRead;
            //simulating some work/download process being done so we would able to test how pause/resume and stop methods work
            if (_bytesWritten >= ContentLength / 2 && sleepTestCounter < 1)
            {
                await Task.Delay(2000);
                sleepTestCounter++;
            }
        }
        await fs.FlushAsync();
        _downloadEnded = true;
        return _stopped ? FileDownloadResult.StoppedDownload : FileDownloadResult.SuccessfulDownload;
    }

    public void Pause()
    {
        if(!_downloadEnded)
            _paused = true;
    }

    public void Resume()
    {
        if(!_downloadEnded)
            _paused = false;
    }
    
    public async Task StopAsync()
    {
        _stopped = true;
        
        //wait until StartDownloadAsync ends the iteration it is currently on, and disposes the FileStream that uses the file at the end of save path,
        //so we can access it 
        while (!_downloadEnded)
        {
           await Task.Delay(100);
        }
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
        }
        _bytesWritten = 0;
    }
}