using System;
using System.Collections.Generic;
using System.Linq;

namespace AsyncHttpListener
{
    public class CompletedArgs
    {
        public CompletedArgs(bool successful, Exception exception)
        {
            Successful = successful;
            Exception = exception;
        }
        public bool Successful { get; private set; }
        public Exception Exception { get; private set; }

    }
}
