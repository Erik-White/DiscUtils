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
using System.IO;

namespace DiscUtils.Iscsi;

using System.Runtime.Serialization;
using System.Security.Permissions;

/// <summary>
/// Base exception for any iSCSI-related failures.
/// </summary>
[Serializable]
public class IscsiException : IOException
{
    /// <summary>
    /// Initializes a new instance of the IscsiException class.
    /// </summary>
    public IscsiException() {}

    /// <summary>
    /// Initializes a new instance of the IscsiException class.
    /// </summary>
    /// <param name="message">The reason for the exception.</param>
    public IscsiException(string message)
        : base(message) {}

    /// <summary>
    /// Initializes a new instance of the IscsiException class.
    /// </summary>
    /// <param name="message">The reason for the exception.</param>
    /// <param name="innerException">The inner exception.</param>
    public IscsiException(string message, Exception innerException)
        : base(message, innerException) {}

    /// <summary>
    /// Initializes a new instance of the IscsiException class.
    /// </summary>
    /// <param name="info">The serialization info.</param>
    /// <param name="context">Ther context.</param>
#if !NETCOREAPP
    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
#elif NET8_0_OR_GREATER
    [Obsolete("Deprecated serialization")]
#endif
    protected IscsiException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}