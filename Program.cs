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

                REadFileREAD(options, options.InputFile, Console.Out);
            }
        }

        private static bool ValidateOptions(ProgramOptions options)
        {
            if (string.IsNullOrEmpty(options.InputFile)) { Console.WriteLine("No inputfile specified."); return false; }// options.InputFile = "C:/var/tmp/rabbitmq-tracing/test trace.log";
            if (File.Exists(options.InputFile) == false) { Console.WriteLine("Input file '{0}' wasnt found.", options.InputFile); return false; }

            if (options.Search != null) { ParseSearch(options, options.Search); return true; }

            return true;
        }

        private static void ParseSearch(ProgramOptions options, string searchValue)
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

        private static void DetermineSearchOp(string searchValue, out int search_operator_index, out int search_operator_length, out int search_op)
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

        private static List<(int recordIndex, long position)> _recordPositions = new List<(int, long)>(1000);
        private static int _currentRecordIndex;
        private static bool _seeking = false;

        private static void REadFileREAD(ProgramOptions options, string tracelogPath, TextWriter output)
        {
            JsonWriterSettings payloadWriterSettings = new JsonWriterSettings() { Indent = (options.Pretty || options.Interactive) };

            using (var fs = new FileStream(tracelogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {

                //var jsonreader = new JsonTextReader(new StreamReader(fs));

                //long lastPosition = -1;
                int skipCount = -1;
                int _skipCountTarget = 0;
                var sr = new UnbufferedStreamReader(fs);

                long _lastPosition = 0;
                long _maxPosition = 0;
                long _recordPosition = 0;
                /*int*/ _currentRecordIndex = -1;
                
                //while (jsonreader.Read())
                while (true)
                {
                    _currentRecordIndex++;
                    _recordPosition = fs.Position;
                    string currentLine = sr.ReadLine();

                    if (sr.EndOfStream || currentLine == null)
                    {
                        if (options.Interactive)
                        {
                            Console.WriteLine("EOF");
                            goto user_interactive;
                        }
                        else break;
                    }

                    _lastPosition = fs.Position;

                    //only mark record positions when moving forward
                    if (fs.Position > _maxPosition)
                    {
                        _recordPositions.Add((_currentRecordIndex, _recordPosition));
                        _maxPosition = fs.Position;
                    }

                    if (options.Interactive)
                    {
                        //display top banner when in interactive mode
                        Console.WriteLine("RecordIndex={0} Position={1}", _currentRecordIndex, _recordPosition);
                    }

                    //if skipping is active, then keep skipping
                    if (_skipCountTarget > 0) { _skipCountTarget--; continue; }

                    //write current rabbitmq record as json and decode the payload into a BsonDocument
                    if (DisplayRecord(currentLine, options, output, ref skipCount) == false) 
                        continue; //skipping due to active search

                user_interactive:
                    if (options.Interactive)
                    {
                        ConsoleKeyInfo key = Console.ReadKey(intercept: true);
                        if (key.KeyChar == '/')
                        {
                            Console.Write("/");
                            string searchFor = Console.ReadLine();
                            ParseSearch(options, searchFor);
                            continue;
                        }
                        else
                        {
                            switch (key.Key)
                            {
                                //    case ConsoleKey.RightArrow:
                                //    case ConsoleKey.DownArrow:

                                //        jsonreader = new JsonTextReader(sr);
                                //        jsonreader.Read();
                                //        jobject = JToken.Load(jsonreader);

                                //        break;

                                case ConsoleKey.UpArrow:
                                    Console.Clear();

                                    _currentRecordIndex--; 
                                    if (_currentRecordIndex < 0) _currentRecordIndex = 0;

                                    if (_currentRecordIndex > -1)
                                        fs.Position = _recordPositions[_currentRecordIndex].position;
                                    else
                                        fs.Position = 0;

                                    //subtract again because its incremented on first line of while(true)
                                    _currentRecordIndex--;

                                    //sr.FindBack("\n");
                                    //sr.SeekMark();
                                    _seeking = true;
                                    break;

                                case ConsoleKey.Spacebar:
                                case ConsoleKey.DownArrow:
                                    Console.Clear();
                                    _seeking = false;
                                    continue;

                                case ConsoleKey.S:
                                    Console.Write("skip count: ");
                                    string skipcount = Console.ReadLine();
                                    _skipCountTarget = int.Parse(skipcount);
                                    break;

                                case ConsoleKey.Escape:
                                    Environment.Exit(0);
                                    break;

                                case ConsoleKey.R:
                                    Console.WriteLine("Resetting to beginning of stream.");
                                    try
                                    {
                                        //sr.BaseStream.Seek(0, SeekOrigin.Begin);
                                        //sr.DiscardBufferedData();
                                        sr.Seek(0, SeekOrigin.Begin);
                                    }
                                    catch(Exception ex)
                                    {
                                        Console.WriteLine("Failed to reset.  " + ex.ToString());
                                    }
                                    break;

                                case ConsoleKey.H:
                                    Console.WriteLine(@"R: reset to beginning  S:skip [count]  Spacebar:Clear/Next record  ");
                                    Console.WriteLine(@"Up:back");
                                    break;

                                default:
                                    goto user_interactive;
                            }
                        }
                    }
                }


            }
        }

        private static bool DisplayRecord(string currentLine, ProgramOptions options, TextWriter output, ref int skipCount)
        {
            var jobject = JToken.Parse(currentLine) as JObject;

            if (options.SearchKey != null)
            {

                try
                {
                    JToken token = jobject[options.SearchKey];
                    if (token == null) { skipCount++; return false; }

                    string val = token.Value<string>();
                    if (string.Compare(val, options.SearchValue, true) != 0) { skipCount++; return false; }
                }
                catch (Exception ex)
                {
                    string ex2 = ex.ToString();
                }

                if (options.Interactive)
                {
                    output.WriteLine("/{0}={1}", options.SearchKey, options.SearchValue);
                    if (skipCount > -1) output.WriteLine("skipped: {0} -----------------------------------------------", skipCount);
                    //output.WriteLine(jobject);
                }

                skipCount = 0;

            }

            if (options.HiddenProperties != null)
            {
                foreach (var prop in options.HiddenProperties) jobject.Remove(prop);
            }

            JToken payload = jobject["payload"];
            BsonDocument doc = null;

            if (payload != null && payload.Type == JTokenType.String)
            {
                string payloadValue = payload.Value<string>();

                if (string.IsNullOrEmpty(payloadValue) == false)
                {
                    try

                    {
                        byte[] bytes = Convert.FromBase64String(payloadValue);

                        doc = BsonSerializer.Deserialize<BsonDocument>(bytes);

                        jobject["payload"] = "[[bsonDocument.ToString()]]";

                        //output.WriteLine("payload tostring:");
                        //output.WriteLine(doc.ToJson(new MongoDB.Bson.IO.JsonWriterSettings() {  Indent=true }));

                    }
                    catch (FormatException f_ex)
                    {
                        if (Console.IsOutputRedirected) Console.Error.WriteLine(f_ex.Message);
                    }
                }
            }

            if (doc != null)
            {
                output.WriteLine(
                    jobject.ToString(options.Pretty ? Formatting.Indented : Formatting.None)
                        .Replace("\"[[bsonDocument.ToString()]]\"", doc.ToJson(new JsonWriterSettings() { Indent = true }).Replace("\n\t", "\n\t\t"))
                );
            }
            else
            {
                output.WriteLine(jobject.ToString(options.Pretty ? Formatting.Indented : Formatting.None));
            }

            return true;
        }

        private static void SeekUntil(Stream s, int direction, char lookFor)
        {
            while (true)
            {
                if (direction == -1 && s.Position == 0) return;
                if (direction == 1 && s.Position == s.Length) return;

                if (s.Position == s.Length) s.Seek(-1, SeekOrigin.Current);
                int b = s.ReadByte(); 

                if (b == (int)lookFor) break;
                
                s.Seek(-2, SeekOrigin.Current);
            }
        }
    }
}
