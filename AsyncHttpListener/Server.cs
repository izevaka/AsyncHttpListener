using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;

namespace AsyncHttpListener
{
    public class Server
    {

        private static ConcurrentDictionary<int, object> ThreadsInvolved = new ConcurrentDictionary<int, object>();
        public static void RegisterThread()
        {
            ThreadsInvolved.TryAdd(Thread.CurrentThread.GetHashCode(), null);
        }

        public static void PrintThreads()
        {
            Info("Used {0} threads", ThreadsInvolved.Count);
        }


        private Func<IRequestHandler> _HandlerFactory;
        private HttpListener _listener;
        private string _Binding;
        private Thread _ServerThread;
        public Server(string host, int port, Func<IRequestHandler> handlerFactory)
        {
            _listener = new HttpListener();
            _Binding = string.Format("http://{0}:{1}/", host, port);
            _listener.Prefixes.Add(_Binding);
            _HandlerFactory = handlerFactory;
        }

        public void Start()
        {
            Console.WriteLine("Starting http listener on port on {0}", _Binding);

            _listener.Start();

            _ServerThread = new Thread(RunServer) { Name = "HttpServer Dispatcher Thread" };

            _ServerThread.Start();
        }

        private void RunServer()
        {
            IAsyncResult latestRequest = null;
            while (_listener.IsListening)
            {
                latestRequest = latestRequest ?? _listener.BeginGetContext(OnGetContext, null);
                bool waitResult = latestRequest.AsyncWaitHandle.WaitOne(500);
                if (waitResult)
                {
                    latestRequest = null;
                }
            }
        }


        public void Stop()
        {
            _listener.Stop();
            _ServerThread.Join();
        }


        void OnGetContext(IAsyncResult result)
        {
            if (_listener.IsListening)
            {
                var context = _listener.EndGetContext(result);
                _HandlerFactory().Handle(context);
            }
        }
        private static void DumpHeaders(HttpListenerContext context)
        {
            Server.Debug("Request {0} - {1}", context.Request.HttpMethod, context.Request.Url);
            foreach (var header in context.Request.Headers.AllKeys)
            {
                Console.WriteLine("\t{0}:{1}", header, context.Request.Headers[header]);
            }
        }


        private static void WriteLog(string fmt, params object[] obj)
        {
            Console.Write("[{0}] - [{1}] - ", Thread.CurrentThread.ManagedThreadId, DateTime.UtcNow.ToString("HH:mm:ss.ffff"));
            Console.WriteLine(fmt, obj);
            
        }
        public static void Error(string fmt, params object[] obj)
        {
            WriteLog(fmt, obj);
        }
        public static void Info(string fmt, params object[] obj)
        {
            WriteLog(fmt, obj);
        }
        public static void Debug(string fmt, params object[] obj)
        {
            if (false)
            {
                WriteLog(fmt, obj);
            }
        }


        static ConcurrentDictionary<object, int> _progress = new ConcurrentDictionary<object,int>();

        internal static void StopProgress(object copier)
        {
            int bytes;
            _progress.TryRemove(copier, out bytes);
            
        }

        internal static void RegisterProgress(object copier, int _BytesProcessed)
        {
            _progress[copier] = _BytesProcessed;
        }

        public static IDictionary<object, int> GetProgress()
        {
            return _progress;
        }
    }
}
