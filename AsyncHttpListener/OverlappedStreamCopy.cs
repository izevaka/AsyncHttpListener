using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace AsyncHttpListener
{
    public class OverlappedStreamCopy :IDisposable
    {
        Stream _Source;
        Stream _Destination;
        Action<CompletedArgs> _CompletedCallback;
        Buffer _WriteBuffer, _NextWriteBuffer, _ReadBuffer, _NextReadBuffer;
        Buffer _Buffer1, _Buffer2;
        private int _BytesProcessed = 0;


        public OverlappedStreamCopy(Stream source, Stream destination, Action<CompletedArgs> completedCallback)
        {
            _Source = source;
            _Destination = destination;
            _CompletedCallback = completedCallback;

            _Buffer1 =_ReadBuffer= BufferManager.Instance.CheckOut();
            _Buffer2 = _NextReadBuffer = BufferManager.Instance.CheckOut();
        }

        public void Start()
        {
            try
            {
                _Source.BeginRead(_ReadBuffer.Memory, 0, _ReadBuffer.Memory.Length, OnBytesRead, _ReadBuffer);
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
                Server.Error("Exception copying stream - {0}", ex.ToString());
                _CompletedCallback(new CompletedArgs(false, ex));
            }
        }
        private void OnSuccess()
        {
            Server.StopProgress(this);
            if (_CompletedCallback != null)
            {
                _CompletedCallback(new CompletedArgs(true, null));
            }
        }

        private void OnBytesRead(IAsyncResult result)
        {
            Server.RegisterThread();
            try
            {
                var readBytes = _Source.EndRead(result);
                var currentBuffer = (Buffer)result.AsyncState;

                currentBuffer.BytesWritten = readBytes;

                var oldWriteBuffer = Interlocked.CompareExchange(ref _WriteBuffer, currentBuffer, null);

                if (oldWriteBuffer == null)
                {
                    //write buffer was empty, meaning we are no longer writing

                    if (readBytes > 0)
                    {
                        var buf = Interlocked.Exchange(ref _NextReadBuffer, null);

                        Interlocked.Exchange(ref _ReadBuffer, buf);

                        var readResult = _Source.BeginRead(buf.Memory, 0, buf.Memory.Length, OnBytesRead, buf);
                    }
                    else
                    {
                        _ReadBuffer = null;
                    }
                    //start write operation
                    _Destination.BeginWrite(currentBuffer.Memory, 0, currentBuffer.BytesWritten, OnBytesWritten, currentBuffer);

                }
                else
                {
                    _ReadBuffer = null;
                    Interlocked.Exchange(ref _NextWriteBuffer, currentBuffer);   
                }
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
        }

        private void OnBytesWritten(IAsyncResult result)
        {
            Server.RegisterThread();
            try
            {
                _Destination.EndWrite(result);
                var currentBuffer = (Buffer)result.AsyncState;

                UpdateBytes(currentBuffer.BytesWritten);
                
                var oldReadBuffer = Interlocked.CompareExchange(ref _ReadBuffer, currentBuffer, null);

                if (oldReadBuffer == null)
                {
                    var buf = Interlocked.Exchange(ref _NextWriteBuffer, null);
                    
                    Interlocked.Exchange(ref _WriteBuffer, buf);

                    if (buf != null && buf.BytesWritten > 0)
                    {
                        _Destination.BeginWrite(buf.Memory, 0, buf.BytesWritten, OnBytesWritten, buf);
                    }
                    else
                    {
                        OnSuccess();
                        return;
                    }

                    //reading was paused
                    _Source.BeginRead(currentBuffer.Memory, 0, currentBuffer.Memory.Length, OnBytesRead, currentBuffer);
                }
                else
                {
                    _WriteBuffer = null;
                    Interlocked.Exchange(ref _NextReadBuffer, currentBuffer);
                }

            }
            catch (Exception ex)
            {
                OnError(ex);
            }
        }

        private void UpdateBytes(int bytesWritten)
        {
            return;
            Interlocked.Add(ref _BytesProcessed, bytesWritten);
            Server.RegisterProgress(this, _BytesProcessed);
        }


        public void Dispose()
        {
            _Buffer1.Dispose();
            _Buffer2.Dispose();
        }
    }
}
