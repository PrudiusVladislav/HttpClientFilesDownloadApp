using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace CSharpWPF_Client.Infrastructure;

public class Client
{
    private readonly HttpClient _httpClient;
    
    public Client(string baseUrl="https://localhost:7092")
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl)
        };
    }

    public async Task DownloadFileAsync(string fileName, string savePath)
    {
        var response = await _httpClient.GetAsync($"api/files/{fileName}");

        response.EnsureSuccessStatusCode();
        
        await using var stream = await response.Content.ReadAsStreamAsync();
        await using var fileStream = System.IO.File.Create(savePath);
        await stream.CopyToAsync(fileStream);
    }
}