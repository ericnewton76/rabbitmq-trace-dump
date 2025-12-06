using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rabbitmq_trace_dump
{
    internal class TraceLogReader : IDisposable
    {
        private UnbufferedStreamReader _reader;
        private List<long> _linePositions = new List<long>();
        private int _currentLineIndex = -1;

        public TraceLogReader(string filepath)
        {
            _reader = new UnbufferedStreamReader(filepath);
            // Record the starting position (first line)
            _linePositions.Add(0);
        }

        /// <summary>
        /// Gets the current line index (0-based).
        /// </summary>
        public int CurrentLineIndex => _currentLineIndex;

        /// <summary>
        /// Gets the number of known line positions.
        /// </summary>
        public int KnownLineCount => _linePositions.Count;

        /// <summary>
        /// Reads the next JSON object from the current position.
        /// </summary>
        /// <param name="jobject">The parsed JObject, or null if at end or parse error.</param>
        /// <returns>True if a valid JSON object was read; otherwise false.</returns>
        public bool Read(out JObject jobject)
        {
            _currentLineIndex++;

            // Ensure we have the position for the current line
            if (_currentLineIndex < _linePositions.Count)
            {
                _reader.Seek(_linePositions[_currentLineIndex], SeekOrigin.Begin);
            }

            long positionBeforeRead = _reader.Mark();

            string json = _reader.ReadLine();
            if (json == null)
            {
                jobject = null;
                _currentLineIndex--;
                return false;
            }

            // Record the next line's position if we haven't seen it yet
            long nextLinePosition = _reader.Mark();
            if (_currentLineIndex + 1 >= _linePositions.Count)
            {
                _linePositions.Add(nextLinePosition);
            }

            try
            {
                jobject = JObject.Parse(json);
                return true;
            }
            catch (Exception)
            {
                jobject = null;
                return false;   
            }
        }

        /// <summary>
        /// Seeks forward or backward by the specified number of lines.
        /// </summary>
        /// <param name="lineOffset">Number of lines to move. Positive = forward, negative = backward.</param>
        /// <returns>True if seek was successful; false if out of bounds.</returns>
        public bool SeekLines(int lineOffset)
        {
            int targetIndex = _currentLineIndex + lineOffset;

            if (targetIndex < -1)
            {
                targetIndex = -1; // Before first line (next Read will get line 0)
            }

            // If seeking forward beyond known positions, read ahead to discover them
            if (targetIndex >= _linePositions.Count)
            {
                // Move to last known position and read forward
                int linesToRead = targetIndex - _currentLineIndex;
                _reader.Seek(_linePositions[_linePositions.Count - 1], SeekOrigin.Begin);
                _currentLineIndex = _linePositions.Count - 2;

                for (int i = 0; i < linesToRead && _currentLineIndex < targetIndex; i++)
                {
                    if (!Read(out _))
                    {
                        return false; // Hit end of file
                    }
                }

                // Position for next read
                _currentLineIndex = targetIndex - 1;
                if (_currentLineIndex >= 0 && _currentLineIndex < _linePositions.Count)
                {
                    _reader.Seek(_linePositions[_currentLineIndex + 1], SeekOrigin.Begin);
                }
                return true;
            }

            _currentLineIndex = targetIndex;

            if (_currentLineIndex >= 0 && _currentLineIndex < _linePositions.Count - 1)
            {
                _reader.Seek(_linePositions[_currentLineIndex + 1], SeekOrigin.Begin);
            }
            else if (_currentLineIndex == -1)
            {
                _reader.Seek(0, SeekOrigin.Begin);
            }

            return true;
        }

        /// <summary>
        /// Seeks to a specific line index (0-based).
        /// </summary>
        /// <param name="lineIndex">The line index to seek to. Next Read() will return this line.</param>
        /// <returns>True if successful; false if out of bounds.</returns>
        public bool SeekToLine(int lineIndex)
        {
            if (lineIndex < 0) return false;

            int offset = lineIndex - _currentLineIndex - 1;
            return SeekLines(offset);
        }

        /// <summary>
        /// Resets the reader to the beginning of the file.
        /// </summary>
        public void Reset()
        {
            _currentLineIndex = -1;
            _reader.Seek(0, SeekOrigin.Begin);
        }

        public void Dispose()
        {
            _reader?.Dispose();
        }
    }
}
