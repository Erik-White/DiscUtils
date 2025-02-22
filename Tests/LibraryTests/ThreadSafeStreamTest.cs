//
// Copyright (c) 2008-2011, Kenneth Bell
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.IO;
using DiscUtils.Streams;
using Xunit;

namespace LibraryTests
{
    public class ThreadSafeStreamTest
    {
        [Fact]
        public void OpenView()
        {
            var tss = new ThreadSafeStream(SparseStream.FromStream(Stream.Null, Ownership.None));
            var view = tss.OpenView();
        }

        [Fact]
        public void ViewIO()
        {
            var memStream = new SparseMemoryStream();
            memStream.SetLength(1024);

            var tss = new ThreadSafeStream(memStream);

            var altView = tss.OpenView();

            // Check positions are independant
            tss.Position = 100;
            Assert.Equal(0, altView.Position);
            Assert.Equal(100, tss.Position);

            // Check I/O is synchronous
            var buffer = new byte[200];
            tss.WriteByte(99);
            altView.ReadExactly(buffer, 0, 200);
            Assert.Equal(99, buffer[100]);

            // Check positions are updated correctly
            Assert.Equal(200, altView.Position);
            Assert.Equal(101, tss.Position);
        }

        [Fact]
        public void ChangeLengthFails()
        {
            var memStream = new SparseMemoryStream();
            memStream.SetLength(2);

            var tss = new ThreadSafeStream(memStream);
            Assert.Equal(2, tss.Length);

            try
            {
                tss.SetLength(10);
                Assert.Fail("SetLength should fail");
            }
            catch (NotSupportedException)
            {
            }
        }

        [Fact]
        public void Extents()
        {
            var memStream = new SparseMemoryStream(new SparseMemoryBuffer(1), FileAccess.ReadWrite);
            memStream.SetLength(1024);

            var tss = new ThreadSafeStream(memStream);

            var altView = tss.OpenView();

            tss.Position = 100;
            tss.WriteByte(99);

            var extents = new List<StreamExtent>(altView.Extents);
            Assert.Single(extents);
            Assert.Equal(100, extents[0].Start);
            Assert.Equal(1, extents[0].Length);

            extents = new List<StreamExtent>(altView.GetExtentsInRange(10, 300));
            Assert.Single(extents);
            Assert.Equal(100, extents[0].Start);
            Assert.Equal(1, extents[0].Length);
        }

        [Fact]
        public void DisposeView()
        {
            var memStream = new SparseMemoryStream(new SparseMemoryBuffer(1), FileAccess.ReadWrite);
            memStream.SetLength(1024);

            var tss = new ThreadSafeStream(memStream);

            var altView = tss.OpenView();
            altView.Dispose();

            tss.ReadByte();

            var altView2 = tss.OpenView();
            altView2.ReadByte();
        }

        [Fact]
        public void Dispose_StopsView()
        {
            var memStream = new SparseMemoryStream(new SparseMemoryBuffer(1), FileAccess.ReadWrite);
            memStream.SetLength(1024);

            var tss = new ThreadSafeStream(memStream);

            var altView = tss.OpenView();
            tss.Dispose();

            try
            {
                altView.ReadByte();
                Assert.Fail("Disposed stream didn't stop view");
            }
            catch (ObjectDisposedException)
            {
            }
        }

        [Fact]
        public void Seek()
        {
            var memStream = new SparseMemoryStream(new SparseMemoryBuffer(1), FileAccess.ReadWrite);
            memStream.SetLength(1024);

            var tss = new ThreadSafeStream(memStream);

            tss.Seek(10, SeekOrigin.Begin);
            Assert.Equal(10, tss.Position);

            tss.Seek(10, SeekOrigin.Current);
            Assert.Equal(20, tss.Position);

            tss.Seek(-10, SeekOrigin.End);
            Assert.Equal(1014, tss.Position);
        }

        [Fact]
        public void CanWrite()
        {
            var memStream = new SparseMemoryStream(new SparseMemoryBuffer(1), FileAccess.ReadWrite);
            var tss = new ThreadSafeStream(memStream);
            Assert.True(tss.CanWrite);

            memStream = new SparseMemoryStream(new SparseMemoryBuffer(1), FileAccess.Read);
            tss = new ThreadSafeStream(memStream);
            Assert.False(tss.CanWrite);
        }

        [Fact]
        public void CanRead()
        {
            var memStream = new SparseMemoryStream(new SparseMemoryBuffer(1), FileAccess.ReadWrite);
            var tss = new ThreadSafeStream(memStream);
            Assert.True(tss.CanRead);

            memStream = new SparseMemoryStream(new SparseMemoryBuffer(1), FileAccess.Write);
            tss = new ThreadSafeStream(memStream);
            Assert.False(tss.CanRead);
        }
    }
}
