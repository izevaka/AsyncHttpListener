using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using AsyncHttpListener;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Collections.Specialized;

namespace Sample.Proxy
{
    class ProxyProgram
    {
        static void Main(string[] args)
        {
            Server server = new Server("*", 8013, () => new ProxyHandler());
            server.Start();
            Console.ReadKey();
            server.Stop();
        }
    }
}
