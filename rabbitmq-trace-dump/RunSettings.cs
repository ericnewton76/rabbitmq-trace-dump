using CommandLine;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace rabbitmq_trace_dump
{
    internal class RunSettings
    {
        public RunSettings()
        {
            //HiddenProperties = new[] { "node", "channel" };
        }

        /// <summary>
        /// List of properties to be hidden in output
        /// </summary>
        [Option("hide", Separator = ',', HelpText = "List of properties to be hidden in output")]
        public IEnumerable<string> HiddenProperties { get; set; }

        /// <summary>program should run in interactive mode.</summary>
        public bool Interactive { get; set; } = false;

        /// <summary>Gets or sets a value indicating whether the output should be formatted for readability.</summary>
        public bool Pretty { get; set; } = false;

        public string InputFile { get; set; }

      
        public string SearchKey { get; set; }
        public string SearchValue { get; internal set; }
        public SearchOperator SearchOp { get; set; }
    }
}