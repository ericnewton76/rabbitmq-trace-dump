using CommandLine;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace rabbitmq_trace_dump
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var results = CommandLine.Parser.Default.ParseArguments<ProgramOptions>(args);
            var options = results.Value;

            if (results.Errors.Count() == 0)
            {
                if (ValidateOptions(options) == false) return;

                var traceDump = new TraceDump(options);
                traceDump.REadFileREAD(options.InputFile, Console.Out);
            }
        }

        private static bool ValidateOptions(ProgramOptions options)
        {
            if (string.IsNullOrEmpty(options.InputFile)) { Console.WriteLine("No inputfile specified."); return false; }// options.InputFile = "C:/var/tmp/rabbitmq-tracing/test trace.log";
            if (File.Exists(options.InputFile) == false) { Console.WriteLine("Input file '{0}' wasnt found.", options.InputFile); return false; }

            if (options.Search != null) { ParseSearch(options, options.Search); return true; }

            return true;
        }

        internal static void ParseSearch(ProgramOptions options, string searchValue)
        {
            if (string.IsNullOrEmpty(searchValue))
            {
                options.SearchOp = 0;
                options.SearchKey = null;
                options.SearchValue = null;
                return;
            }

            int search_operator_index, search_operator_length, search_op;
            DetermineSearchOp(searchValue, out search_operator_index, out search_operator_length, out search_op);

            options.SearchOp = search_op;
            options.SearchKey = searchValue.Substring(0, search_operator_index);
            options.SearchValue = searchValue.Substring(search_operator_index + search_operator_length);
        }

        internal static void DetermineSearchOp(string searchValue, out int search_operator_index, out int search_operator_length, out int search_op)
        {
            search_operator_index = -1;
            search_operator_length = -1;
            search_op = -1;

            search_operator_index = searchValue.IndexOf("~==");
            if (search_operator_index > -1) { search_operator_length = 3; search_op = 2; }
            else
            {
                search_operator_index = searchValue.IndexOf("==");
                if (search_operator_index > -1) { search_operator_length = 2; search_op = 1; }
                else
                {
                    search_operator_index = searchValue.IndexOf("=");
                    if (search_operator_index > -1) { search_operator_length = 1; search_op = 1; }
                }
            }
        }

    }
}
