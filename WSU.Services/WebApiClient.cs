using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WSU.Services.Authentification;
using WSU.Services.Interfaces;

namespace WSU.Services
{
    public abstract class WebApiClient : IWebApiClient
    {
        public HttpClient HttpClient { get; protected set; }

        protected WebApiClient() { }

        protected WebApiClient(string apiUrl)
            : this(new HttpClient { BaseAddress = new Uri(apiUrl) })
        {
        }

        protected WebApiClient(HttpClient client)
        {
            HttpClient = client;
            HttpClient.DefaultRequestHeaders.Accept.Clear();
            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<HttpResponseMessage> GetAsync(string request)
        {
            return await HttpClient.GetAsync(request);
        }

        public async Task<T> ReadAsync<T>(string request) where T : new()
        {
            HttpResponseMessage responseMessage = await HttpClient.GetAsync(request).ConfigureAwait(false);
            responseMessage.EnsureSuccessStatusCode();

            return JsonConvert.DeserializeObject<T>(await responseMessage.Content.ReadAsStringAsync());
        }

        public async Task<string> ReadAsync(string request)
        {
            HttpResponseMessage responseMessage = await HttpClient.GetAsync(request).ConfigureAwait(false);
            responseMessage.EnsureSuccessStatusCode();

            return await responseMessage.Content.ReadAsStringAsync();
        }

        public async Task PostAsJsonAsync(string request, object data)
        {
            HttpResponseMessage responseMessage = await HttpClient.PostAsJsonAsync(request, data).ConfigureAwait(false);
            responseMessage.EnsureSuccessStatusCode();

            await responseMessage.Content.ReadAsStringAsync();
        }

        public async Task PostFileAsync<T>(string request, T data, byte[] file)
        {
            var content = new MultipartFormDataContent
            {
                new ObjectContent<T>(data, new JsonMediaTypeFormatter(), (MediaTypeHeaderValue)null),
                { new StreamContent(new MemoryStream(file)), "file", "file.jpg" }
            };
            HttpResponseMessage responseMessage = await HttpClient.PostAsync(request, content);
            responseMessage.EnsureSuccessStatusCode();

            await responseMessage.Content.ReadAsStringAsync();
        }
    }

    public class SimpleWebApiClient : WebApiClient
    {
        public SimpleWebApiClient(string apiUrl)
            : base(apiUrl)
        {
        }
    }

    public class HmacAuthWebApiClient : WebApiClient
    {
        public HmacAuthWebApiClient(string apiUrl, string appId, string apiKey)
        {
            HmacAuthDelegatingHandler handler = new HmacAuthDelegatingHandler(appId, apiKey);
            HttpClient = HttpClientFactory.Create(handler);
            HttpClient.BaseAddress = new Uri(apiUrl);
            HttpClient.DefaultRequestHeaders.Accept.Clear();
            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
    }
}
