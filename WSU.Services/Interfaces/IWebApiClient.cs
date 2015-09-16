using System.Net.Http;
using System.Threading.Tasks;

namespace WSU.Services.Interfaces
{
    public interface IWebApiClient
    {
        HttpClient HttpClient { get; }
        Task<HttpResponseMessage> GetAsync(string request);
        Task<T> ReadAsync<T>(string request) where T : new();
        Task<string> ReadAsync(string request);
        Task PostAsJsonAsync(string request, object data);
        Task PostFileAsync<T>(string request, T data, byte[] file);
    }
}