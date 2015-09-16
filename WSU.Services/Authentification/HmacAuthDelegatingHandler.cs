using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace WSU.Services.Authentification
{
    // Récupéré depuis http://bitoftech.net/2014/12/15/secure-asp-net-web-api-using-api-key-authentication-hmac-authentication/
    public class HmacAuthDelegatingHandler : DelegatingHandler
    {
        private readonly string _appId;
        private readonly string _apiKey;

        public HmacAuthDelegatingHandler(string appId, string apiKey)
        {
            _appId = appId;
            _apiKey = apiKey;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string requestContentBase64String = string.Empty;
            string requestUri = WebUtility.UrlEncode(request.RequestUri.AbsoluteUri.ToLower());
            string requestHttpMethod = request.Method.Method;

            // Calculate UNIX time
            string requestTimeStamp = GetUnixTimeStamp();

            // Create random nonce for each request
            string nonce = Guid.NewGuid().ToString("N");

            // Checking if the request contains body, usually will be null with HTTP GET and DELETE
            if (request.Content != null)
            {
                byte[] content = await request.Content.ReadAsByteArrayAsync();
                requestContentBase64String = Convert.ToBase64String(HmacUtils.HashRequest(content));
            }

            // Creating the raw signature string
            string signatureRawData = String.Format("{0}{1}{2}{3}{4}{5}", new Guid(_appId), requestHttpMethod, requestUri, requestTimeStamp, nonce, requestContentBase64String);

            string requestSignatureBase64String = HmacUtils.GenerateHMAC(_apiKey, signatureRawData);

            // Setting the values in the Authorization header using custom scheme (amx)
            request.Headers.Authorization = new AuthenticationHeaderValue("amx", string.Format("{0}:{1}:{2}:{3}", new Guid(_appId), requestSignatureBase64String, nonce, requestTimeStamp));

            HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            return response;
        }

        private static string GetUnixTimeStamp()
        {
            DateTime epochStart = new DateTime(1970, 01, 01, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan timeSpan = DateTime.UtcNow - epochStart;
            return Convert.ToUInt64(timeSpan.TotalSeconds).ToString();
        }
    }
}
