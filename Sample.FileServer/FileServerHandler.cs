using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using AsyncHttpListener;
using System.Collections.Concurrent;
using System.Threading;
using System.IO;

namespace Sample.FileServer
{
    class FileServerHandler : IRequestHandler
    {

        /// <summary>
        /// Initializes a new instance of the FileServerHandler class.
        /// </summary>
        /// <param name="context"></param>
        public FileServerHandler(string basePath)
        {
            _BasePath = basePath;
        }

        public void Handle(HttpListenerContext context)
        {
            Server.RegisterThread();

            var _start = DateTime.UtcNow;

            _Context = context;
            
            var fileName = ConvertUrlPathToLocalPath(context.Request.Url.GetComponents(UriComponents.Path, UriFormat.Unescaped));
            if (!File.Exists(fileName))
            {
                _Context.Response.StatusCode = 404;
                _Context.Response.WriteString("Not found");
                return;
            }
            try
            {
                SetContentType(fileName);

                var stream = GetFileStream(fileName);
                context.Response.ContentLength64 = stream.Length;

                context.Response.StatusCode = 200;

                Interlocked.Add(ref _Incoming, 1);
                stream.CopyToAsync(context.Response.OutputStream, args =>
                {
                    Interlocked.Add(ref _Processed, 1);
                    Server.RegisterThread();
                    try
                    {
                        Server.Debug("{0} - {1} - {2} ms", context.Request.HttpMethod, context.Request.Url, (DateTime.UtcNow - _start).TotalMilliseconds);
                        stream.Dispose();
                        context.Response.Close();
                        GC.Collect(0, GCCollectionMode.Optimized);
                    }
                    catch (Exception ex)
                    {
                        Server.Error("Error closing connection: {0}", ex.Message);
                    }
                });
            }
            catch(Exception ex)
            {
                _Context.Response.StatusCode = 500;
                _Context.Response.WriteString("Error serving file. " + ex.Message);
            }
        }

        private void SetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName);

            string mimeType;
            mimeType = MIMEAssistant.GetMIMEType(fileName);

            _Context.Response.ContentType = mimeType;
        }

        private string ConvertUrlPathToLocalPath(string requestPath)
        {
            var converted = requestPath.Replace('/', System.IO.Path.DirectorySeparatorChar);
            return Path.Combine(_BasePath,  converted);
        }

        public Stream GetFileStream(string requestPath)
        {
            return File.OpenRead(requestPath);
        }

        static int _Processed;
        public static int Processed { get { return _Processed; } }
        static int _Incoming;
        private HttpListenerContext _Context;
        private string _BasePath;
        public static int Incoming { get { return _Incoming; } }

    }
}
