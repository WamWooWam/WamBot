using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace WamBotEval.Languages.Globals
{
    public class XMLHttpRequest
    {
        private static HttpClient _client;
        private HttpResponseMessage _response;

        public XMLHttpRequest()
        {
            if (_client == null)
            {
                _client = new HttpClient();
            }
        }

        public ushort status => (ushort)(_response?.StatusCode ?? 0);
        public dynamic response { get; private set; }
        public string responseText { get; private set; }
        public string responseUrl => _response?.RequestMessage.RequestUri.ToString();
        public string statusText => _response != null ? $"{(int)_response.StatusCode} {_response.StatusCode}" : null;
        public ulong timeout
        {
            get => (ulong)_client.Timeout.TotalMilliseconds;
            set => _client.Timeout = TimeSpan.FromMilliseconds(value);
        }

        public void abort()
        {
            _client.CancelPendingRequests();
        }

        public string getAllResponseHeaders()
        {
            return _response != null ? string.Join("\r\n", _response.Headers) : null;
        }

        public string getResponseHeader(string name)
        {
            if (_response == null)
                return null;

            if (_response.Headers.TryGetValues(name, out var values))
            {
                return values.FirstOrDefault();
            }

            return null;
        }

        public event EventHandler<object> ready;

        public void open(string method, string url)
        {
            _response = _client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url), HttpCompletionOption.ResponseHeadersRead)
                .GetAwaiter().GetResult();
        }

        public void send()
        {
            response = _response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
            responseText = _response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        }
    }
}
