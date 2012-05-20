using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.IO;
using Moq;
using System.Reflection;
using System.Threading;
using System.Collections.Concurrent;

namespace AsyncHttpListener.Tests
{
    [TestFixture]
    public class StreamCopyTests
    {
        [Test]
        public void CopyStreamAsync_should_write_to_destinaion_stream_correctly()
        {
            var source = new MemoryStream(new byte[83642]);
            var fileStream = File.OpenRead(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "83642bytes.txt"));
            fileStream.CopyTo(source);
            var destination = new MemoryStream(new byte[83642]);
            source.Position = 0;


            int numThreads = 5;
            
            ConcurrentBag<CopyInfo> results = new ConcurrentBag<CopyInfo>();

            for (var i = 0; i < numThreads; i++)
            {
                
                ThreadPool.QueueUserWorkItem((s) =>
                {
                    CopyInfo info = new CopyInfo();
                    ManualResetEventSlim ev = new ManualResetEventSlim();
                    source.CopyToSync(destination, args =>
                    {
                        info.Exception = args.Exception;

                        
                        source.Position = 0;
                        destination.Position = 0;
                        try
                        {
                            info.ByteDifferent = AssertStreamsEqual(source, destination);
                        }
                        catch (Exception e)
                        {
                            info.Exception = e;
                        }
                        
                        results.Add(info);
                        ev.Set();
                    });
                    ev.Wait();
                });
            }

            while (results.Count != numThreads)
            {
                Thread.Sleep(2000);
            }

            foreach(var result in results){
                Assert.Null(result.Exception, result.Exception!=null ? result.Exception.Message : null);
                Assert.That(result.ByteDifferent, Is.EqualTo(-1));
            }
        }

        private int AssertStreamsEqual(Stream source, Stream destination)
        {
            int s = 0, i = -1;
            do
            {
                s = source.ReadByte();

                if (s != destination.ReadByte())
                    return i;
                i++;
            }
            while (s != -1);
            return -1;
        }

    }

    class CopyInfo
    {
        public Exception Exception {get;set;}
        public int ByteDifferent { get; set; }
    }
}
