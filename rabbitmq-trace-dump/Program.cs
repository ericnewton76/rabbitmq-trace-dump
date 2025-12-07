using CommandLine;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Spectre.Console;
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

            RunSettings runsettings;

            if (results.Errors.Count() == 0)
            {
                if (ValidateOptions(options, out runsettings) == false) return;

                if (options.Interactive.GetValueOrDefault() && string.IsNullOrEmpty(options.InputFile))
                {
                    SelectFile(runsettings);
                    if (string.IsNullOrEmpty(runsettings.InputFile)) return;
                }

                try
                {
                    var traceDump = new TraceDump(runsettings);
                    traceDump.REadFileREAD(runsettings.InputFile, Console.Out);
                }
                catch(Exception ex)
                {
                    AnsiConsole.WriteException(ex);
                }
            }
        }

        private static void SelectFile(RunSettings options)
        {
            var directory = "C:/var/tmp/rabbitmq-tracing"; //default directory that rabbitmq on windows writes trace files to

            //list files and output a prompt using Spectre.Console
            var selectedFile = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("Select a trace file to open:")
                .PageSize(10)
                .AddChoices(Directory.GetFiles(directory, "*.log")));

            if (string.IsNullOrEmpty(selectedFile) == false) options.InputFile = selectedFile;
        }   

        private static bool ValidateOptions(ProgramOptions options, out RunSettings runsettings)
        {
            runsettings = new RunSettings();

            if (options == null || String.IsNullOrEmpty(options.InputFile))
            {
                if (Console.IsInputRedirected)
                {
                    runsettings.InputFile = "<stdin>";
                    return true;
                }
            }

            //transfer from options
            runsettings.Interactive = options.Interactive.GetValueOrDefault();
            runsettings.Pretty = options.Pretty.GetValueOrDefault();
            runsettings.HiddenProperties = options.HiddenProperties ?? Enumerable.Empty<string>();

            //check for NO_COLOR=1 environment variable to disable colors
            var no_color = Environment.GetEnvironmentVariable("NO_COLOR");
            if (no_color == "1")
            {
                runsettings.NoColor = true; //we end up disabling using AnsiConsole for output because its slower for json output (literally parsing the json its outputting)
            }

            // Validate input file, make sure it exists, check if console input is redirected
            if (string.IsNullOrEmpty(options.InputFile) == false)
            {
                if (File.Exists(options.InputFile) == false)
                {
                    Console.WriteLine("Input file '{0}' wasnt found.", options.InputFile);
                    return false;
                }
            }

            runsettings.InputFile = options.InputFile;

            if (string.IsNullOrEmpty(options.Filter) == false)
            { 
                ParseSearch(runsettings, options.Filter); 
            }

            return true;
        }

        internal static void ParseSearch(RunSettings runsettings, string searchValue)
        {
            if (string.IsNullOrEmpty(searchValue))
            {
                runsettings.SearchOp = SearchOperator.None;
                runsettings.SearchKey = null;
                runsettings.SearchValue = null;
                return;
            }

            DetermineSearchOp(searchValue, out int search_operator_index, out int search_operator_length, out SearchOperator search_op);

            runsettings.SearchOp = search_op;
            runsettings.SearchKey = searchValue.Substring(0, search_operator_index);
            runsettings.SearchValue = searchValue.Substring(search_operator_index + search_operator_length);
        }

        /// <summary>
        /// Read user input and try to determine the search operator and its position in the string.
        /// Operators (checked in order): ~== (contains), != or &lt;&gt; (not equals), ^= (starts with), 
        /// $= (ends with), ~= (regex), == (equals), = (equals)
        /// </summary>
        internal static void DetermineSearchOp(string searchValue, out int search_operator_index, out int search_operator_length, out SearchOperator search_op)
        {
            search_operator_index = -1;
            search_operator_length = 0;
            search_op = SearchOperator.None;

            // Check for ~== (contains) - must check before ~= 
            if ((search_operator_index = searchValue.IndexOf("~==")) > 0)
            {
                search_operator_length = 3;
                search_op = SearchOperator.Contains;
                return;
            }

            // Check for != or <> (not equals)
            if ((search_operator_index = searchValue.IndexOf("!=")) > 0 
                || (search_operator_index = searchValue.IndexOf("<>")) > 0)
            {
                search_operator_length = 2;
                search_op = SearchOperator.NotEquals;
                return;
            }

            // Check for ^= (starts with)
            if ((search_operator_index = searchValue.IndexOf("^=")) > 0)
            {
                search_operator_length = 2;
                search_op = SearchOperator.StartsWith;
                return;
            }

            // Check for $= (ends with)
            if ((search_operator_index = searchValue.IndexOf("$=")) > 0)
            {
                search_operator_length = 2;
                search_op = SearchOperator.EndsWith;
                return;
            }

            // Check for ~= (regex) - must check after ~==
            if ((search_operator_index = searchValue.IndexOf("~=")) > 0)
            {
                search_operator_length = 2;
                search_op = SearchOperator.Regex;
                return;
            }

            // Check for == (equals)
            if ((search_operator_index = searchValue.IndexOf("==")) > 0)
            {
                search_operator_length = 2;
                search_op = SearchOperator.Equals;
                return;
            }

            // Check for = (equals) - must be last
            if ((search_operator_index = searchValue.IndexOf("=")) > 0)
            {
                search_operator_length = 1;
                search_op = SearchOperator.Equals;
                return;
            }
        }

    }
}
