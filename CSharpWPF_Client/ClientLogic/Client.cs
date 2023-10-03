using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace CSharpWPF_Client.ClientLogic;

public class Client
{
    public HttpClient HttpClient { get; } = new HttpClient()
    {
        BaseAddress = new Uri("https://localhost:7092")
    };
}