using System;
using System.IO;
using System.Security.Cryptography;

namespace LibMSPackSharp
{
    /// <summary>
    /// Special stream that checksums data as it reads
    /// </summary>
    internal class TestStream : Stream
    {
        /// <summary>
        /// Length of the virtual file
        /// </summary>
        private long length;

        /// <summary>
        /// Position in the virtual file
        /// </summary>
        private long position;

        /// <summary>
        /// MD5 context for hashing
        /// </summary>
        public MD5 MD5Context { get; private set; } = MD5.Create();

        /// <inheritdoc/>
        public override bool CanRead => false;

        /// <inheritdoc/>
        public override bool CanSeek => true;

        /// <inheritdoc/>
        public override bool CanWrite => true;

        /// <inheritdoc/>
        public override long Length => length;

        /// <inheritdoc/>
        public override long Position { get => position; set => position = value; }

        /// <inheritdoc/>
        public override void Flush() { }

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count) => throw new NotImplementedException();

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();

        /// <inheritdoc/>
        public override void SetLength(long value) => throw new NotImplementedException();

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count) => MD5Context.TransformBlock(buffer, offset, count, buffer, offset);

        /// <inheritdoc/>
        public override void Close() => MD5Context.TransformFinalBlock(null, 0, 0);
    }
}
