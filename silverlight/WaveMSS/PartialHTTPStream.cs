using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace video_js.WaveMSS
{
    /// <summary>
    /// Partial HTTP Stream for CLR Core.
    /// Based on https://codereview.stackexchange.com/questions/70679/seekable-http-range-stream
    /// </summary>
    public class PartialHTTPStream : Stream
    {
        private readonly Int64 cacheLength = 1024;
        private const Int32 noDataAvaiable = 0;
        private MemoryStream stream = null;
        private Int64 currentChunkNumber = -1;
        private Int64? length = null;
        private Boolean isDisposed = false;

        public PartialHTTPStream(String url)
            : this(url, 1024) { }

        public PartialHTTPStream(String url, Int64 cacheLength)
        {
            if (cacheLength > 0) { this.cacheLength = cacheLength; }
            Url = url;
        }

        public String Url { get; private set; }

        public override Boolean CanRead
        {
            get
            {
                EnsureNotDisposed();
                return true;
            }
        }

        public override Boolean CanWrite
        {
            get
            {
                EnsureNotDisposed();
                return false;
            }
        }

        public override Boolean CanSeek
        {
            get
            {
                EnsureNotDisposed();
                return true;
            }
        }

        public override Int64 Length
        {
            get
            {
                EnsureNotDisposed();
                if (length == null)
                {
                    HttpWebRequest request = WebRequest.CreateHttp(Url);
                    request.Method = "HEAD";
                    AutoResetEvent w = new AutoResetEvent(false);
                    request.BeginGetResponse(asyncState =>
                        {
                            using (WebResponse response = ((HttpWebRequest)asyncState.AsyncState).EndGetResponse(asyncState))
                            {
                                length = response.ContentLength;
                                w.Set();
                            }
                        }, request
                    );
                    w.WaitOne();
                }
                
                return length.Value;
            }
        }

        public override Int64 Position
        {
            get
            {
                EnsureNotDisposed();
                Int64 streamPosition = (stream != null) ? stream.Position : 0;
                Int64 position = (currentChunkNumber != -1) ? currentChunkNumber * cacheLength : 0;

                return position + streamPosition;
            }
            set
            {
                EnsureNotDisposed();
                EnsurePositiv(value, "Position");
                Seek(value);
            }
        }

        public override Int64 Seek(Int64 offset, SeekOrigin origin)
        {
            EnsureNotDisposed();
            switch (origin)
            {
                case SeekOrigin.Begin:
                    break;
                case SeekOrigin.Current:
                    offset = Position + offset;
                    break;
                default:
                    if (Length > -1)
                    {
                        offset = Length + offset;
                    }
                    break;
            }

            return Seek(offset);
        }

        private Int64 Seek(Int64 offset)
        {
            Int64 chunkNumber = offset / cacheLength;

            if (currentChunkNumber != chunkNumber)
            {
                ReadChunk(chunkNumber);
                currentChunkNumber = chunkNumber;
            }
            offset = offset - currentChunkNumber * cacheLength;

            stream.Seek(offset, SeekOrigin.Begin);

            return Position;
        }

        private void ReadNextChunk()
        {
            currentChunkNumber += 1;
            ReadChunk(currentChunkNumber);
        }

        private void ReadChunk(Int64 chunkNumberToRead)
        {
            Int64 rangeStart = chunkNumberToRead * cacheLength;

            Int64 rangeEnd = rangeStart + cacheLength - 1;
            if (Length > -1)
            {
                if (rangeStart > Length) { return; }

                if (rangeStart + cacheLength > Length)
                {
                    rangeEnd = Length - 1;
                }
            }
            if (stream != null) { stream.Close(); }
            stream = new MemoryStream((int)cacheLength);

            HttpWebRequest request = HttpWebRequest.CreateHttp(Url);
            request.Headers[HttpRequestHeader.Range] = "bytes=" + rangeStart + "-" + rangeEnd;

            AutoResetEvent w = new AutoResetEvent(false);
            request.BeginGetResponse(asyncState =>
                {
                    using (WebResponse response = ((HttpWebRequest)asyncState.AsyncState).EndGetResponse(asyncState))
                    {
                        response.GetResponseStream().CopyTo(stream);
                    }
                    stream.Position = 0;
                    w.Set();
                }, request
            );
            w.WaitOne();
        }

        public override void Close()
        {
            EnsureNotDisposed();

            base.Close();
            if (stream != null) { stream.Close(); }
            isDisposed = true;
        }

        public override Int32 Read(Byte[] buffer, Int32 offset, Int32 count)
        {
            EnsureNotDisposed();

            EnsureNotNull(buffer, "buffer");
            EnsurePositiv(offset, "offset");
            EnsurePositiv(count, "count");

            if (buffer.Length - offset < count) { throw new ArgumentException("count"); }

            if (stream == null) { ReadNextChunk(); }

            if (Length > -1)
            {
                if (Position >= Length) { return noDataAvaiable; }

                if (Position + count > Length)
                {
                    count = (Int32)(Length - Position);
                }
            }

            Int32 bytesRead = stream.Read(buffer, offset, count);
            Int32 totalBytesRead = bytesRead;
            count -= bytesRead;

            while (count > noDataAvaiable)
            {
                ReadNextChunk();
                offset = offset + bytesRead;
                bytesRead = stream.Read(buffer, offset, count);
                count -= bytesRead;
                totalBytesRead = totalBytesRead + bytesRead;
            }

            return totalBytesRead;

        }

        public override void SetLength(Int64 value)
        {
            EnsureNotDisposed();
            throw new NotImplementedException();
        }

        public override void Write(Byte[] buffer, Int32 offset, Int32 count)
        {
            EnsureNotDisposed();
            throw new NotImplementedException();
        }

        public override void Flush()
        {
            EnsureNotDisposed();
        }

        private void EnsureNotNull(Object obj, String name)
        {
            if (obj != null) { return; }
            throw new ArgumentNullException(name);
        }
        private void EnsureNotDisposed()
        {
            if (!isDisposed) { return; }
            throw new ObjectDisposedException("PartialHTTPStream");
        }
        private void EnsurePositiv(Int32 value, String name)
        {
            if (value > -1) { return; }
            throw new ArgumentOutOfRangeException(name);
        }
        private void EnsurePositiv(Int64 value, String name)
        {
            if (value > -1) { return; }
            throw new ArgumentOutOfRangeException(name);
        }
        private void EnsureNegativ(Int64 value, String name)
        {
            if (value < 0) { return; }
            throw new ArgumentOutOfRangeException(name);
        }
    }
}
