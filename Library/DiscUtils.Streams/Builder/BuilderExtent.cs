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

using LTRData.Extensions.Buffers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DiscUtils.Streams;

public abstract class BuilderExtent : IDisposable
{
    public BuilderExtent(long start, long length)
    {
        Start = start;
        Length = length;
    }

    public long Length { get; }

    public long Start { get; }

    /// <summary>
    /// Gets the parts of the stream that are stored.
    /// </summary>
    /// <remarks>This may be an empty enumeration if all bytes are zero.</remarks>
    public virtual IEnumerable<StreamExtent> StreamExtents
        => SingleValueEnumerable.Get(new StreamExtent(Start, Length));

    public abstract void PrepareForRead();

    public abstract int Read(long diskOffset, byte[] block, int offset, int count);

    public virtual ValueTask<int> ReadAsync(long diskOffset, byte[] block, int offset, int count, CancellationToken cancellationToken) =>
        new(Read(diskOffset, block, offset, count));

    public abstract int Read(long diskOffset, Span<byte> block);

    public virtual ValueTask<int> ReadAsync(long diskOffset, Memory<byte> block, CancellationToken cancellationToken) =>
        new(Read(diskOffset, block.Span));

    public abstract void DisposeReadState();

    protected abstract void Dispose(bool disposing);

    // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    ~BuilderExtent()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}