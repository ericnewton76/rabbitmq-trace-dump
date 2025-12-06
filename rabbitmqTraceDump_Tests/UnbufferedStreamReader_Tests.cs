using System;
using System.IO;
using System.Text;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using rabbitmq_trace_dump;

namespace rabbitmqTraceDump_Tests
{
    [TestFixture]
    public class UnbufferedStreamReader_Tests
    {
        [Test]
        public void Constructor_StreamNotSeekable_ThrowsArgumentException()
        {
            var mockStream = new Mock<Stream>();
            mockStream.Setup(s => s.CanSeek).Returns(false);

            Action act = () => new UnbufferedStreamReader(mockStream.Object);
            act.Should().Throw<ArgumentException>();
        }

        [Test]
        public void ReadLine_ReturnsLinesSeparatedByLF()
        {
            var content = "hello\nworld\n";
            using var ms = new MemoryStream(Encoding.ASCII.GetBytes(content));
            using var reader = new UnbufferedStreamReader(ms);

            reader.ReadLine().Should().Be("hello");
            reader.ReadLine().Should().Be("world");
            reader.ReadLine().Should().BeNull();
        }

        [Test]
        public void Read_ReturnsByteValues_AndMinusOneAtEnd()
        {
            var data = new byte[] { 0x41, 0x42 }; // 'A','B'
            using var ms = new MemoryStream(data);
            using var reader = new UnbufferedStreamReader(ms);

            reader.Read().Should().Be(0x41);
            reader.Read().Should().Be(0x42);
            reader.Read().Should().Be(-1);
        }

        [Test]
        public void MarkAndSeekMark_RestoresPosition()
        {
            var content = "line1\nline2\nline3\n";
            using var ms = new MemoryStream(Encoding.ASCII.GetBytes(content));
            using var reader = new UnbufferedStreamReader(ms);

            reader.ReadLine().Should().Be("line1");
            reader.Mark();
            reader.ReadLine().Should().Be("line2");
            reader.SeekMark();
            reader.ReadLine().Should().Be("line2");
        }

        [Test]
        public void FindBack_FindsLastOccurrence_ReturnsPositionAfterFoundByte()
        {
            var content = "abcxdefx\n"; // 'x' at indices 3 and 7 (0-based)
            using var ms = new MemoryStream(Encoding.ASCII.GetBytes(content));
            ms.Position = ms.Length; // start from end
            using var reader = new UnbufferedStreamReader(ms);

            long pos = reader.FindBack("x");

            // Implementation returns the stream Position after the found byte.
            // Last 'x' is at index 7 so returned position should be 8.
            pos.Should().Be(8);
        }

        [Test]
        public void Close_DisposesUnderlyingStream()
        {
            var ms = new MemoryStream(Encoding.ASCII.GetBytes("data"));
            var reader = new UnbufferedStreamReader(ms);

            reader.Close();

            Action act = () => ms.ReadByte();
            act.Should().Throw<ObjectDisposedException>();
        }
    }
}