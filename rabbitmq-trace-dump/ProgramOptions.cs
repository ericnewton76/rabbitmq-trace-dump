using CommandLine;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections;
using System.Collections.Generic;

namespace rabbitmq_trace_dump
{
    internal class ProgramOptions
    {
        public ProgramOptions()
        {
        }

        /// <summary>
        /// List of properties to be hidden in output
        /// </summary>
        [Option("hide", Separator = ',', HelpText = "List of properties to be hidden in output")]
        public IEnumerable<string> HiddenProperties { get; set; }

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
        public SearchOperator SearchOp { get; set; }
    }
}