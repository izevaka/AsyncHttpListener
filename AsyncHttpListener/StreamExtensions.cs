using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Threading;
using System.Diagnostics;

namespace AsyncHttpListener
{
    public static class StreamExtensions
    {
        public static void CopyToAsync(this Stream source, Stream dest, Action<CompletedArgs> completionCallback)
        {
            AsynchronousStreamCopy copy = null;
            copy = new AsynchronousStreamCopy(source, dest, args =>
            {
                copy.Dispose();
                completionCallback(args); 
            });
            copy.Start();
        }
        public static void CopyToSync(this Stream source, Stream dest, Action<CompletedArgs> completionCallback)
        {
            try
            {
                source.CopyTo(dest);
            }
            catch (Exception ex)
            {
                completionCallback(new CompletedArgs(false, ex));
                return;
            }
            completionCallback(new CompletedArgs(true, null));
        }
    }
}
