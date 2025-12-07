using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Spectre.Console;
using Spectre.Console.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rabbitmq_trace_dump
{
    internal class TraceDump
    {

        public TraceDump(ProgramOptions options)
        {
            this.ProgramOptions = options;
        }
        private ProgramOptions ProgramOptions;

        private static List<(int recordIndex, long position)> _recordPositions = new List<(int, long)>(1000);
        private static int _currentRecordIndex;
        private static bool _seeking = false;

        public void REadFileREAD(string tracelogPath, TextWriter output)
        {
            JsonWriterSettings payloadWriterSettings = new JsonWriterSettings() { Indent = (ProgramOptions.Pretty || ProgramOptions.Interactive) };

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
                /*int*/
                _currentRecordIndex = -1;

                //while (jsonreader.Read())
                while (true)
                {
                    _currentRecordIndex++;
                    _recordPosition = fs.Position;
                    string currentLine = sr.ReadLine();

                    if (sr.EndOfStream || currentLine == null)
                    {
                        if (ProgramOptions.Interactive)
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

                    if (ProgramOptions.Interactive)
                    {
                        //display top banner when in interactive mode
                        Console.WriteLine("RecordIndex={0} Position={1}", _currentRecordIndex, _recordPosition);
                    }

                    //if skipping is active, then keep skipping
                    if (_skipCountTarget > 0) { _skipCountTarget--; continue; }

                    var payloadDecoded = false;
                    var jobject = JToken.Parse(currentLine) as JObject;

                    if (CheckRecordFilter(jobject, output, out payloadDecoded) == true)
                    {
                        skipCount++;

                        if (ProgramOptions.Interactive)
                        {
                            output.WriteLine("/{0}={1}", ProgramOptions.SearchKey, ProgramOptions.SearchValue);
                            if (skipCount > -1) output.WriteLine("skipped: {0} -----------------------------------------------", skipCount);
                            //output.WriteLine(jobject);
                        }
                        continue;
                    }

                    //write current rabbitmq record as json and decode the payload into a BsonDocument
                    DisplayRecord(payloadDecoded, jobject, output);

                user_interactive:
                    if (ProgramOptions.Interactive)
                    {
                        ConsoleKeyInfo key = Console.ReadKey(intercept: true);
                        if (key.KeyChar == '/')
                        {
                            Console.Write("/");
                            string searchFor = Console.ReadLine();
                            Program.ParseSearch(ProgramOptions, searchFor);
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

                                case ConsoleKey.End:
                                    Console.Clear();
                                    Console.WriteLine("Seeking to end of file...");

                                    // Read through all remaining records to build position index
                                    while (!sr.EndOfStream)
                                    {
                                        long pos = fs.Position;
                                        string line = sr.ReadLine();
                                        if (line == null) break;

                                        _currentRecordIndex++;

                                        // Record position if moving beyond previous max
                                        if (fs.Position > _maxPosition)
                                        {
                                            _recordPositions.Add((_currentRecordIndex, pos));
                                            _maxPosition = fs.Position;
                                        }
                                    }

                                    // Backtrack to the last record
                                    if (_recordPositions.Count > 0)
                                    {
                                        var lastRecord = _recordPositions[_recordPositions.Count - 1];
                                        fs.Position = lastRecord.position;
                                        _currentRecordIndex = lastRecord.recordIndex - 1; // -1 because loop increments
                                    }

                                    Console.WriteLine("At record {0} of {1}", _currentRecordIndex + 1, _recordPositions.Count);
                                    break;

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
                                    if (int.TryParse(skipcount, out _skipCountTarget) == true) { }
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
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine("Failed to reset.  " + ex.ToString());
                                    }
                                    break;

                                case ConsoleKey.H:
                                    Console.WriteLine(@"[R] and [Home]: reset to beginning  [S]:skip [count]  [Spacebar] and [Down]:Clear/Next record  ");
                                    Console.WriteLine(@"[Up]:back  [End]:seek to last record");
                                    break;

                                default:
                                    goto user_interactive;
                            }
                        }
                    }
                }


            }
        }

        private bool CheckRecordFilter(JObject jobject, TextWriter output, out bool payloadDecoded)
        {
            payloadDecoded = false;

            if (ProgramOptions.SearchKey == null) 
                return false;

            try
            {
                if (ProgramOptions.SearchKey.StartsWith("payload."))
                {
                    payloadDecoded = true;
                    DecodePayload(jobject);
                }

                JToken token = jobject.SelectToken(ProgramOptions.SearchKey);
                if (token == null) { return false; }

                string val = token.Value<string>();
                if (string.Compare(val, ProgramOptions.SearchValue, true) != 0) { return false; }
            }
            catch (Exception ex)
            {
                string ex2 = ex.ToString();
            }

            return true;
        }

        private void DisplayRecord(bool payloadDecoded, JObject jobject, TextWriter output)
        {
            //remove any properties that are specified as Hidden
            if (ProgramOptions.HiddenProperties != null)
            {
                foreach (var prop in ProgramOptions.HiddenProperties) jobject.Remove(prop);
            }

            var new_jobject = payloadDecoded ? jobject : DecodePayload(jobject);
            string new_jobject_json = Newtonsoft.Json.JsonConvert.SerializeObject(new_jobject, ProgramOptions.Pretty ? Formatting.Indented : Formatting.None);

            AnsiConsole.Write(new JsonText(new_jobject_json)
                .BracesColor(Color.Gray)
                .BracketColor(Color.Gray)
                .StringColor(Color.Yellow)
                .NumberColor(Color.Yellow)
                .BooleanColor(Color.Yellow)
                .NullColor(Color.Yellow)
            );

        }

        private JObject DecodePayload(JObject jobject)
        {
            JToken payload = jobject["payload"];

            if (payload != null && payload.Type == JTokenType.String)
            {
                string payloadValue = payload.Value<string>();

                if (string.IsNullOrEmpty(payloadValue) == false)
                {
                    byte[] bytes;
                    try
                    {
                        bytes = Convert.FromBase64String(payloadValue);
                    }
                    catch { bytes = null; }

                    try
                    {
                        var doc = BsonSerializer.Deserialize<BsonDocument>(bytes);

                        // Use RelaxedExtendedJson to output standard JSON-compatible format
                        var jsonWriterSettings = new JsonWriterSettings
                        {
                            Indent = false,
                            OutputMode = JsonOutputMode.RelaxedExtendedJson
                        };
                        var jsonString = doc.ToJson(jsonWriterSettings);
                        jobject["payload"] = JObject.Parse(jsonString);

                        bytes = null;
                    }
                    catch (FormatException f_ex)
                    {
                        if (Console.IsOutputRedirected) Console.Error.WriteLine(f_ex.Message);
                    }

                    if (bytes != null)
                    {
                        try
                        {
                            jobject["payload"] = System.Text.Encoding.ASCII.GetString(bytes);
                            bytes = null;
                        }
                        catch { }
                    }
                }
            }

            return jobject;
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
