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
        Stream s;
        long _seekPositionMark;

        public bool EndOfStream
        {
            get => false;
        }

        public UnbufferedStreamReader(string path)
        {
            s = new FileStream(path, FileMode.Open);
        }

        public UnbufferedStreamReader(Stream stream)
        {
            if (stream.CanSeek == false) throw new ArgumentException("Stream is not seekable.");
            s = stream;
        }

        public long Seek(long offset, SeekOrigin seekOrigin)
        {
            return s.Seek(offset, seekOrigin);
        }

        public long Mark()
        {
            _seekPositionMark = s.Position;
            return _seekPositionMark;
        }
        public long FindBack(string x)
        {
            return _Find(x, false);
        }

        private long _Find(string x, bool direction)
        {
            byte lookFor = (byte)x[0];

            if(direction == false && s.Position > 0) s.Seek(-2, SeekOrigin.Current);

            while (true)
            {
                if (direction == false && s.Position == 0) return 0;
                if (direction == true && s.Position == s.Length) return s.Position;

                if (s.Position == s.Length) s.Seek(-1, SeekOrigin.Current);
                int b = s.ReadByte();

                if (b == lookFor) break;

                s.Seek(-2, SeekOrigin.Current);
            }
            return s.Position;
        }

        public long SeekMark()
        {
            s.Position = _seekPositionMark;
            return s.Position;
        }

        List<byte> _bytes = new List<byte>(1000);

        // This method assumes lines end with a line feed.
        // You may need to modify this method if your stream
        // follows the Windows convention of \r\n or some other 
        // convention that isn't just \n
        public override string ReadLine()
        {
            _seekPositionMark = s.Position;
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
            return s.ReadByte();
        }

        public override void Close()
        {
            s.Close();
        }
        protected override void Dispose(bool disposing)
        {
            s.Dispose();
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
    }
}
