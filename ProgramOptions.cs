using CommandLine;
using MongoDB.Bson.Serialization.Attributes;

namespace rabbitmq_trace_dump
{
    internal class ProgramOptions
    {
        public ProgramOptions()
        {
        }

        [Option('i')]
        public bool Interactive { get; set; } = false;

        [Option('p')]        
        public bool Pretty { get; set; } = false;

        [Value(index:0)]
        public string InputFile { get; set; }

        [Option("Search")]
        public string Search { get; set; }
        
        public string SearchKey { get; set; }
        public string SearchValue { get; internal set; }
        public int SearchOp { get; set; }
    }
}