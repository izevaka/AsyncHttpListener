using System;
using System.Collections.Generic;
using System.Linq;

namespace AsyncHttpListener
{
    public class Buffer : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the Buffer structure.
        /// </summary>
        public Buffer(byte[] memory, BufferManager manager)
        {
            Memory = memory;
            BytesWritten = 0;
            _Manager = manager;
        }
        public byte[] Memory;
        public int BytesWritten;
        private BufferManager _Manager;

        public void Dispose()
        {
            _Manager.Return(this);
        }
    }
}
