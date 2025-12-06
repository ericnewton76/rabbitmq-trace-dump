using System;
using System.IO;
using System.Text;
using FluentAssertions;
using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using rabbitmq_trace_dump;

namespace rabbitmqTraceDump_Tests
{
    [TestFixture]
    public class TraceLogReader_Tests
    {
        private string _sampleFilePath;

        [SetUp]
        public void SetUp()
        {
            _sampleFilePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Samples", "TraceLogReader_Tests", "testing2.json");
        }

        [Test]
        public void Read_SingleJsonLine_ReturnsParsedJObject()
        {
            using var reader = new TraceLogReader(_sampleFilePath);

            reader.Read(out var jobject).Should().BeTrue();
            jobject.Should().NotBeNull();
        }

        [Test]
        public void Read_MultipleJsonLines_ReturnsEachInSequence()
        {
            using var reader = new TraceLogReader(_sampleFilePath);

            reader.Read(out var obj1).Should().BeTrue();
            obj1.Should().NotBeNull();

            reader.Read(out var obj2).Should().BeTrue();
            obj2.Should().NotBeNull();

            reader.Read(out var obj3).Should().BeTrue();
            obj3.Should().NotBeNull();
        }

        [Test]
        public void CurrentLineIndex_InitialValue_IsMinusOne()
        {
            using var reader = new TraceLogReader(_sampleFilePath);

            reader.CurrentLineIndex.Should().Be(-1);
        }

        [Test]
        public void CurrentLineIndex_AfterRead_IncrementsCorrectly()
        {
            using var reader = new TraceLogReader(_sampleFilePath);

            reader.Read(out _);
            reader.CurrentLineIndex.Should().Be(0);

            reader.Read(out _);
            reader.CurrentLineIndex.Should().Be(1);
        }

        [Test]
        public void SeekLines_BackwardAfterReading_ReturnsToEarlierLine()
        {
            using var reader = new TraceLogReader(_sampleFilePath);

            reader.Read(out var firstObj); // line 0
            reader.Read(out _); // line 1
            reader.Read(out _); // line 2

            reader.SeekLines(-2).Should().BeTrue();

            reader.Read(out var jobject).Should().BeTrue();
            // After seeking back 2 lines from line 2, we should be at line 1
            reader.CurrentLineIndex.Should().Be(1);
        }

        [Test]
        public void SeekLines_ForwardFromMiddle_SkipsLines()
        {
            using var reader = new TraceLogReader(_sampleFilePath);

            // Read all lines first to discover positions
            reader.Read(out _); // line 0
            reader.Read(out _); // line 1
            reader.Read(out _); // line 2

            // Reset to beginning
            reader.Reset();

            // Now seek forward
            reader.SeekLines(2).Should().BeTrue();

            reader.Read(out var jobject).Should().BeTrue();
            reader.CurrentLineIndex.Should().Be(2);
        }

        [Test]
        public void SeekToLine_ValidIndex_PositionsCorrectly()
        {
            using var reader = new TraceLogReader(_sampleFilePath);

            // Read all lines first to discover positions
            reader.Read(out _); // line 0
            reader.Read(out _); // line 1
            reader.Read(out _); // line 2

            // Seek to line 1
            reader.SeekToLine(1).Should().BeTrue();

            reader.Read(out var jobject).Should().BeTrue();
            reader.CurrentLineIndex.Should().Be(1);
        }

        [Test]
        public void SeekToLine_NegativeIndex_ReturnsFalse()
        {
            using var reader = new TraceLogReader(_sampleFilePath);

            reader.SeekToLine(-1).Should().BeFalse();
        }

        [Test]
        public void Reset_AfterReading_ReturnsToBeginning()
        {
            using var reader = new TraceLogReader(_sampleFilePath);

            reader.Read(out var firstObj);
            reader.Read(out _);

            reader.Reset();

            reader.CurrentLineIndex.Should().Be(-1);
            reader.Read(out var jobject).Should().BeTrue();
            jobject.Should().NotBeNull();
            reader.CurrentLineIndex.Should().Be(0);
        }

        [Test]
        public void KnownLineCount_AfterReadingLines_IncrementsCorrectly()
        {
            using var reader = new TraceLogReader(_sampleFilePath);

            reader.KnownLineCount.Should().Be(1); // Initial position

            reader.Read(out _);
            reader.KnownLineCount.Should().BeGreaterThan(1);
        }

        [Test]
        public void Dispose_CalledMultipleTimes_DoesNotThrow()
        {
            var reader = new TraceLogReader(_sampleFilePath);

            Action act = () =>
            {
                reader.Dispose();
                reader.Dispose();
            };

            act.Should().NotThrow();
        }
    }
}