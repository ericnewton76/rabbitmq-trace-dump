using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rabbitmq_trace_dump
{

    /// <summary>
    /// 
    /// </summary>
    // comes from https://stackoverflow.com/a/2496729/323456
    public class UnbufferedStreamReader : TextReader
    {
        Stream _baseStream;
        long _seekPositionMark;

        public bool EndOfStream
        {
            get => false;
        }

        public UnbufferedStreamReader(string path)
        {
            _baseStream = new FileStream(path, FileMode.Open);
        }

        public UnbufferedStreamReader(Stream stream)
        {
            if (stream.CanSeek == false) throw new ArgumentException("Stream is not seekable.");
            _baseStream = stream;
        }

        public long Seek(long offset, SeekOrigin seekOrigin)
        {
            return _baseStream.Seek(offset, seekOrigin);
        }

        public long FindBack(string x)
        {
            return _Find(x, false);
        }

        private long _Find(string x, bool direction)
        {
            byte lookFor = (byte)x[0];

            if(direction == false && _baseStream.Position > 0) _baseStream.Seek(-2, SeekOrigin.Current);

            while (true)
            {
                if (direction == false && _baseStream.Position == 0) return 0;
                if (direction == true && _baseStream.Position == _baseStream.Length) return _baseStream.Position;

                if (_baseStream.Position == _baseStream.Length) _baseStream.Seek(-1, SeekOrigin.Current);
                int b = _baseStream.ReadByte();

                if (b == lookFor) break;

                _baseStream.Seek(-2, SeekOrigin.Current);
            }
            return _baseStream.Position;
        }

        List<byte> _bytes = new List<byte>(1000);

        // This method assumes lines end with a line feed.
        // You may need to modify this method if your stream
        // follows the Windows convention of \r\n or some other 
        // convention that isn't just \n
        public override string ReadLine()
        {
            _seekPositionMark = _baseStream.Position;
            _bytes.Clear();

            int current;
            while ((current = Read()) != -1 && current != (int)'\n')
            {
                byte b = (byte)current;
                _bytes.Add(b);
            }

            if (_bytes.Count == 0)
                return null;
            else
                return Encoding.ASCII.GetString(_bytes.ToArray());
        }

        // Read works differently than the `Read()` method of a 
        // TextReader. It reads the next BYTE rather than the next character
        public override int Read()
        {
            return _baseStream.ReadByte();
        }

        public override int Peek()
        {
            throw new NotImplementedException();
        }

        public override int Read(char[] buffer, int index, int count)
        {
            throw new NotImplementedException();
            /*
            Encoding e = Encoding.UTF8;

            List<byte> bytes = new List<byte>(count);
            int current;
            while ((current = Read()) != -1 && current != (int)'\n')
            {
                byte b = (byte)current;
                bytes.Add(b);
            }

            e.GetChars(bytes.ToArray());

            return Encoding.ASCII.GetString(bytes.ToArray());



            int c = 0;
            while(c < count)
            {
                e.GetChars()
            }
            return s.Read(buffer, index, count);*/
        }

        public override int ReadBlock(char[] buffer, int index, int count)
        {
            throw new NotImplementedException();
        }

        public override string ReadToEnd()
        {
            throw new NotImplementedException();
        }

        #region Close / Dispose
        public override void Close()
        {
            _baseStream.Close();
        }
        protected override void Dispose(bool disposing)
        {
            _baseStream.Dispose();
        }
        #endregion

        #region Mark / SeekMark
        /// <summary>
        /// Mark a position in the basestream
        /// </summary>
        /// <returns></returns>
        public long Mark()
        {
            _seekPositionMark = _baseStream.Position;
            return _seekPositionMark;
        }

        /// <summary>
        /// Seek to the marked position
        /// </summary>
        /// <returns></returns>
        public long SeekMark()
        {
            _baseStream.Position = _seekPositionMark;
            return _baseStream.Position;
        }
#endregion

    }
}
