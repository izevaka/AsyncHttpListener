using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace AsyncHttpListener.Tests
{
    [TestFixture]
    public class BufferManagerTests
    {
        private const int _BufferSize = 76;
        private BufferManager _Manager;
        [SetUp]
        public void Setup()
        {
            _Manager = new BufferManager(_BufferSize);
        }
        [Test]
        public void CheckOut_should_return_a_buffer_of_same_size_as_manager()
        {
            var buffer = _Manager.CheckOut();
            Assert.That(buffer, Is.Not.Null);
            Assert.That(buffer.Memory.Length, Is.EqualTo(_BufferSize));
        }

        [Test]
        public void Checkout_should_increment_NumBuffers()
        {
            var buffer = _Manager.CheckOut();
            _Manager.CheckOut();
            Assert.That(_Manager.NumCheckedOut, Is.EqualTo(2));
        }
    }
}
