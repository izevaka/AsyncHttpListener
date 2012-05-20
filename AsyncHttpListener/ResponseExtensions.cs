using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

namespace AsyncHttpListener
{
    public static class ResponseExtensions
    {
        public static void WriteString(this HttpListenerResponse response, string content)
        {
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(content);

            var output = new MemoryStream(buffer);
                
            output.CopyToAsync(response.OutputStream, (result) => {
                output.Close();
                response.Close();
            });   
        }
    }
}
