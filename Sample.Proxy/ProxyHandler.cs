using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AsyncHttpListener;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Collections.Specialized;

namespace Sample.Proxy
{
    class ProxyHandler : IRequestHandler
    {
        static ConcurrentDictionary<int, Process> _Processes = new ConcurrentDictionary<int, Process>();
        static HashSet<string> _ExcludeResponseHeaders = new HashSet<string> { "Content-Length", "Transfer-Encoding" };
        static HashSet<string> _ExcludeRequestHeaders = new HashSet<string> { "Connection", "Accept", "Host", "User-Agent", "Referer", "If-Modified-Since" };

        private HttpListenerContext _Context;
        private DateTime _start;
        private Uri _SourceUrl;

        public void Handle(HttpListenerContext context)
        {
            this._Context = context;
            _start = DateTime.UtcNow;

            ProxyRequest();
        }
        
        private void ProxyRequest()
        {
            string proxyUrl = proxyUrl = _Context.Request.Url.PathAndQuery.TrimStart(new char[] { '/' }).Replace(":/", "://");
            _SourceUrl = null;
            if (!Uri.TryCreate(proxyUrl, UriKind.Absolute, out _SourceUrl))
            {
                WriteResponse(400, "Bad request", string.Format("Bad Url - {0}", proxyUrl));
                return;
            }
            try
            {
                Server.Debug("Requesting {0}", _SourceUrl);
                var request = (HttpWebRequest)HttpWebRequest.Create(_SourceUrl);
                request.Method = _Context.Request.HttpMethod;
                //request.AllowAutoRedirect = false;
                CopyRequestMetadata(_Context.Request, request);

                if (request.Method.ToLower() != "get")
                {
                    request.BeginGetRequestStream(new AsyncCallback(SendRequest), request);
                }
                else
                {
                    request.BeginGetResponse(new AsyncCallback(GetReponseCallback), request);
                }
            }
            catch (Exception ex)
            {
                WriteResponse(500, "Internal Server Error", ex.Message);
            }
        }

        private void WriteResponse(int statusCode, string statusDescription, string message)
        {
            HttpListenerResponse response = _Context.Response;
            response.StatusCode = statusCode;
            response.StatusDescription = statusDescription;
            response.WriteString(message);
            Server.Error(message);
        }

        void SendRequest(IAsyncResult result)
        {
            var request = ((WebRequest)result.AsyncState);
            Stream inputStream = request.EndGetRequestStream(result);
            //should be asynchronous
            _Context.Request.InputStream.CopyToAsync(inputStream, args =>
            {
                request.BeginGetResponse(new AsyncCallback(GetReponseCallback), request);
            });
        }

        void GetReponseCallback(IAsyncResult result)
        {

            var request = ((WebRequest)result.AsyncState);
            HttpWebResponse response = null;
            try
            {
                response = (HttpWebResponse)request.EndGetResponse(result);
            }
            catch (WebException ex)
            {
                response = (HttpWebResponse)ex.Response;
                if (response == null)
                {
                    WriteResponse(503, "Internal Server Error", ex.Message);
                    return;
                }
            }

            CopyResponseMetadata(response, _Context.Response);
            response.GetResponseStream().CopyToAsync(_Context.Response.OutputStream, args =>
            {
                Server.Debug("{0} - {1} - {2} ms", _Context.Request.HttpMethod, _Context.Request.Url, (DateTime.UtcNow - _start).TotalMilliseconds);
                _Context.Response.Close();
                response.Close();
                GC.Collect(0, GCCollectionMode.Optimized);
            });

        }

        private void CopyResponseMetadata(HttpWebResponse source, HttpListenerResponse destination)
        {
            destination.ContentType = source.ContentType;
            destination.StatusCode = (int)source.StatusCode;
            destination.StatusDescription = source.StatusDescription;

            CopyHeaders(_ExcludeResponseHeaders, source.Headers, destination.Headers);
        }

        private void CopyRequestMetadata(HttpListenerRequest source, HttpWebRequest destination)
        {
            //destination.Connection = source.Headers["Connection"];
            destination.Accept = source.Headers["Accept"];
            destination.Host = _SourceUrl.Host;
            destination.UserAgent = source.Headers["User-Agent"];
            destination.Referer = source.Headers["Referer"];
            string ifModifiedSinceString = source.Headers["If-Modified-Since"];
            DateTime ifModifiedSince;
            if (DateTime.TryParse(ifModifiedSinceString, out ifModifiedSince))
            {
                destination.IfModifiedSince = ifModifiedSince;
            }
            CopyHeaders(_ExcludeRequestHeaders, source.Headers, destination.Headers);
        }


        private void CopyHeaders(HashSet<string> excludeList, NameValueCollection source, NameValueCollection destination)
        {
            for (int i = 0; i < source.Count; i++)
            {
                var headerName = source.AllKeys[i];
                if (excludeList.Contains(headerName))
                {
                    continue;
                }
                try
                {
                    destination[headerName] = source[i];
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Could not copy header {1} due to error: {0}", ex.Message, headerName);
                }
            }

        }
    }
}
