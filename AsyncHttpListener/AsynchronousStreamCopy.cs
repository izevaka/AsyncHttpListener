using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;

namespace AsyncHttpListener
{
    public class AsynchronousStreamCopy : IDisposable
    {
        Stream _Source;
        Stream _Destination;
        Action<CompletedArgs> _CompletedCallback;
        Buffer _Buffer = BufferManager.Instance.CheckOut();
        
        public AsynchronousStreamCopy(Stream source, Stream destination, Action<CompletedArgs> completedCallback)
        {
            _Source = source;
            _Destination = destination;
            _CompletedCallback = completedCallback;
        }

        public void Start()
        {
            try
            {
                _Source.BeginRead(_Buffer.Memory, 0, _Buffer.Memory.Length, OnBeginRead, null);
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
        }

        private void OnError(Exception ex)
        {
            if (_CompletedCallback != null)
            {
                Server.Error("Exception copying stream - {0}", ex.Message);
                _CompletedCallback(new CompletedArgs(false, ex));
            }
        }
        private void OnSuccess()
        {
            if (_CompletedCallback != null)
            {
                _CompletedCallback(new CompletedArgs(true, null));
            }
        }

        private void OnBeginRead(IAsyncResult result)
        {
            Server.RegisterThread();
            try
            {
                var readBytes = _Source.EndRead(result);
                _Buffer.BytesWritten = readBytes;

                if (_Buffer.BytesWritten > 0){
                    _Destination.BeginWrite(_Buffer.Memory, 0, _Buffer.BytesWritten, OnBeginWrite, null);
                }
                else
                {
                    OnSuccess();
                }
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
        }

        private void OnBeginWrite(IAsyncResult result)
        {
            Server.RegisterThread();
            try
            {
                _Destination.EndWrite(result);
                _Source.BeginRead(_Buffer.Memory, 0, _Buffer.Memory.Length, OnBeginRead, null);
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
        }


        public void Dispose()
        {
            _Buffer.Dispose();
        }
    }
}
