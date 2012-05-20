using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;

namespace AsyncHttpListener
{
    public interface IRequestHandler
    {
        void Handle(HttpListenerContext context);
    }
}