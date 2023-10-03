using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace CSharpWPF_Client.ClientLogic;

public class FileDownloadClient
{
    private readonly HttpClient _httpClient;
    private readonly string _source;
    public string SavePath { get; }
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

    public int ContentLength { get; private set; }
    public bool IsDone => ContentLength == _bytesWritten;

    public FileDownloadClient(HttpClient httpClient, string fileName, string savePath, int chunkSize)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
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
        var request = new HttpRequestMessage(HttpMethod.Head, _source);

        using var response = _httpClient.SendAsync(request).Result;
        return response.Content.Headers.ContentLength ?? 0;
    }
    
    public async Task<FileDownloadResult> StartDownloadAsync()
    {
        var response = await _httpClient.GetAsync(_source);

        if (!response.IsSuccessStatusCode)
            return FileDownloadResult.FileNotFound;
        await using var responseStream = await response.Content.ReadAsStreamAsync();
        await using var fs = new FileStream(SavePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        var buffer = new byte[_chunkSize];

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