using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CSharpWPF_Client.ClientLogic;

public class FileDownloadClient
{
    private readonly HttpClient _httpClient;
    private readonly string _source;
    private readonly int _chunkSize;
    private volatile bool _paused;
    private volatile bool _stopped;
    private int _bytesWritten;

    public FileDownloadState DownloadState
    {
        get
        {
            if(_paused)
                return FileDownloadState.Paused;
            return _stopped ? FileDownloadState.Stopped : FileDownloadState.Running;
        }
    }
    
    public string SavePath { get; }
    public string FileName { get; }
    public int ContentLength { get; private set; }
    public bool IsDone => ContentLength == _bytesWritten;

    public FileDownloadClient(HttpClient httpClient, string fileName, string savePath, int chunkSize)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        FileName = fileName;
        _source = $"api/files/{fileName}";
        SavePath = savePath;
        _chunkSize = chunkSize;
        _paused = false;
        _stopped = false;
        
        ContentLength = Convert.ToInt32(GetContentLength());
        _bytesWritten = 0;
    }

    private long GetContentLength()
    {
        // var request = new HttpRequestMessage(HttpMethod.Head, _source);
        //
        // using var response = _httpClient.SendAsync(request).Result;

        var response = _httpClient.GetAsync(_source).Result;
        
        return int.Parse(response.Content.Headers.FirstOrDefault(h => h.Key.Equals("Content-Length")).Value.First());
        //return response.Content.Headers.ContentLength ?? 0;
    }
    
    public async Task<FileDownloadResult> StartDownloadAsync()
    {
        var response = await _httpClient.GetAsync(_source);

        if (!response.IsSuccessStatusCode)
            return FileDownloadResult.FileNotFound;
        await using var responseStream = await response.Content.ReadAsStreamAsync();
        await using var fs = new FileStream(SavePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        var buffer = new byte[_chunkSize];
        Console.WriteLine($"ContentLength: {ContentLength}");
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

        return _stopped ? FileDownloadResult.StoppedDownload : FileDownloadResult.SuccessfulDownload;
    }

    public void Pause()
    {
        _paused = true;
    }

    public void Resume()
    {
        _paused = false;
    }
    
    public void Stop()
    {
        _stopped = true;
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
        }
        _bytesWritten = 0;
    }
}