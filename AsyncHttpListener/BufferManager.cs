using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Threading;

namespace AsyncHttpListener
{
    public class BufferManager
    {
        
        private static BufferManager _instance;
        public static BufferManager Instance { get { return _instance; } }
        static BufferManager()
        {
            _instance = new BufferManager(0x10000);
        }

        ConcurrentQueue<byte[]> _Buffers = new ConcurrentQueue<byte[]>();
        private int _NumBuffers;
        private int _BufferLength;

        public BufferManager(int bufferLength)
        {
            _BufferLength = bufferLength;
        }

        public Buffer CheckOut()
        {
            

            byte[] buf;
            _Buffers.TryDequeue(out buf);
            if (buf == null)
            {
                Interlocked.Increment(ref _NumBuffers);
                return new Buffer(new byte[_BufferLength], this);
            }

            return new Buffer(buf, this);
        }
        public void Return(Buffer buffer)
        {
            _Buffers.Enqueue(buffer.Memory);
            
        }

        public int NumCheckedOut
        {
            get
            {
                return _NumBuffers - _Buffers.Count;
            }
        }
        public int NumTotal
        {
            get
            {
                return _NumBuffers;
            }
        }
    }
}
