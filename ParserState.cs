using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rabbitmq_trace_dump
{
    internal class ParserState
    {

        public int SkipCount { get; set; }
        public string CurrentLine { get; set; }
        public long Position { get; set; }


    }
}
