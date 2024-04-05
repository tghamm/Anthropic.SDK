using System.Net;

namespace Anthropic.SDK.Tests;

public class FiddlerHttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient(string name)
    {
        return new HttpClient(new HttpClientHandler()
        {
            Proxy = new WebProxy("http://127.0.0.1:8888")
        });
    }
}