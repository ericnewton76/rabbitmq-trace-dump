using CommandLine;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace rabbitmq_trace_dump
{


    /// <summary>
    /// parsed command line options
    /// </summary>
    public class ProgramOptions
    {
        public ProgramOptions()
        {
            //HiddenProperties = new[] { "node", "channel" };
        }

        /// <summary>
        /// List of properties to be hidden in output
        /// </summary>
        [Option("hide", Separator = ',', HelpText = "List of properties to be hidden in output")]
        public IEnumerable<string> HiddenProperties { get; set; }

        [Option('i')]
        public bool? Interactive { get; set; }

        [Option('p')]        
        public bool? Pretty { get; set; }

        [Value(index:0)]
        public string InputFile { get; set; }

        [Option]
        public string Filter { get; set; }
    }
}