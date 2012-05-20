using AsyncHttpListener;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Sample.FileServer
{
    class CmdParameters
    {
        public int Port { get; set; }
        public bool Valid { get; set; }
        public string LocalPath { get; set; }
    }

    class Program
    {
        private static ManualResetEventSlim _StopStatusThread;
        
        static void Main(string[] args)
        {
            var parameters = ParseArgs(args);
            if (!parameters.Valid)
            {
                Environment.Exit(1);
                return;
            }
            
            _StopStatusThread = new ManualResetEventSlim();
            new Thread(StatusThreadWorker).Start();

            var server = new Server("localhost", parameters.Port, () => new FileServerHandler(parameters.LocalPath));
            server.Start();

            Console.ReadKey();

            Server.Error("Stopping server");
            server.Stop();
            _StopStatusThread.Set();
        }

        static int lastIncoming = 0, lastProcessed = 0, lastCheckedOutBuffers = 0, lastTotalBuffers = 0;

        private static void StatusThreadWorker()
        {
            while (!_StopStatusThread.Wait(2000))
            {
                if (lastIncoming != FileServerHandler.Incoming || lastProcessed != FileServerHandler.Processed)
                {
                    lastIncoming = FileServerHandler.Incoming;
                    lastProcessed = FileServerHandler.Processed;

                    Server.Info("Request in: {0}, processed: {1}", lastIncoming, lastProcessed);
                }
                if (lastCheckedOutBuffers != BufferManager.Instance.NumCheckedOut || lastTotalBuffers != BufferManager.Instance.NumTotal)
                {
                    lastCheckedOutBuffers = BufferManager.Instance.NumCheckedOut;
                    lastTotalBuffers = BufferManager.Instance.NumTotal;
                    Server.Info("Buffers total: {0}, checked out: {1}", lastTotalBuffers, lastCheckedOutBuffers);
                }
            }
            Server.PrintThreads();
            var progress = Server.GetProgress();

            foreach (var kv in progress)
            {
                Server.Info("copier({0}): {1} bytes ({2} 64k buffers)", kv.Key.GetHashCode(), kv.Value, ((double)kv.Value) / 0x10000);
            }
        }
        
        private static CmdParameters ParseArgs(string[] args)
        {
            var ret = new CmdParameters();

            foreach (var arg in args)
            {
                var argsSplit = arg.Split(':');
                var argname = argsSplit.Length > 0 ? argsSplit[0] : null;
                var argvalue = argsSplit.Length > 1 ? string.Join(":", argsSplit.Skip(1).ToArray()) : null;

                ret.Valid = true;
                switch (argname)
                {
                    case "/port":
                        int port;
                        if (!int.TryParse(argvalue, out port))
                        {
                            Console.Error.WriteLine("Must specify port - /port:8017");
                            ret.Valid = false;
                        }
                        ret.Port = port;
                        break;
                    case "/path":
                        if (!Directory.Exists(argvalue))
                        {
                            Console.Error.WriteLine("Cannot find directory '{0}'", argvalue);
                            ret.Valid = false;
                        }
                        ret.LocalPath = argvalue;
                        break;
                }
            }

            return ret;
        }
    }
}
