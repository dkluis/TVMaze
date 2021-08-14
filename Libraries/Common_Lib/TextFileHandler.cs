using System;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json.Linq;
using Common_Lib;

namespace Common_Lib
{
    public class TextFileHandler
    {
        private readonly string filepath;
        private readonly string fullfilepath;
        public int level;
        protected string app;
        protected Stopwatch timer = new();

        public TextFileHandler(string filename, string appl, string infilepath, int loglevel)
        {
            timer.Start();
            app = appl;
            level = loglevel;

            filepath = infilepath;
            fullfilepath = Path.Combine(filepath, filename);

            if (!File.Exists(fullfilepath))
            {
                File.Create(fullfilepath).Close();
            }

        }

        public void Start()
        {
            EmptyLine();
            Write($"{app} Started  ##########################################", app, 0);
            EmptyLine();
        }

        public void Stop()
        {
            timer.Stop();
            EmptyLine();
            Write($"{app} Finished ###################  in {timer.ElapsedMilliseconds} mSec  #######################", app, 0);
            EmptyLine();
        }

        public void Write(string message, string function = "", int loglevel = 3, bool append = true)
        {
            if (function == "" || function == null) { function = app; }
            if (function.Length > 19) { function = function.Substring(0, 19); }
            if (loglevel <= level)
            {
                using StreamWriter file = new(fullfilepath, append);
                file.WriteLine($"{DateTime.Now}: {function,-20}: {loglevel,-2} --> {message}");
            }
        }

        public void Write(string[] messages, string function = "", int loglevel = 3, bool append = true)
        {
            if (function == "" || function == null) { function = app; }
            if (function.Length > 19) { function = function.Substring(0, 19); }
            if (loglevel <= level)
            {
                using StreamWriter file = new(fullfilepath, append);
                foreach (string msg in messages)
                {
                    file.WriteLine($"{DateTime.Now}: {function,-20}: {loglevel,-2}--> {msg}");
                }
            }
        }

        public void Elapsed()
        {
            EmptyLine();
            Write($"{app} Elapsed up to now: {timer.ElapsedMilliseconds} mSec", "Elapsed Time", 0);
            EmptyLine();
        }

        public void EmptyLine(int lines = 1)
        {
            int idx = 1;
            using StreamWriter file = new(fullfilepath, true);
            while (idx <= lines)
            {
                file.WriteLine($"");
                idx++;
            }
        }

        public void WriteNoHead(string message, bool newline = true, bool append = true)
        {
            using StreamWriter file = new(fullfilepath, append);
            if (newline) { file.WriteLine(message); } else { file.Write(message); }
        }

        public void WriteNoHead(string[] messages, bool newline = true, bool append = true)
        {
            using StreamWriter file = new(fullfilepath, append);
            foreach (string msg in messages)
            {
                if (newline) { file.WriteLine(msg); } else { file.Write(msg); }
            }
        }

        public string ReadKeyArray(string find)
        {
            if (!File.Exists(fullfilepath)) { return ""; }
            string filetext = File.ReadAllText(fullfilepath);
            JArray kvps = ConvertJsonTxt.ConvertStringToJArray(filetext);
            foreach (JToken rec in kvps)
            {
                if (rec[find] is null) { return ""; }
                if (rec[find].ToString() != "") { return rec[find].ToString(); }
            }
            return "";
        }

        public string ReadKeyObject(string find)
        {
            if (!File.Exists(fullfilepath)) { return ""; }
            string filetext = File.ReadAllText(fullfilepath);
            JObject kvps = ConvertJsonTxt.ConvertStringToJObject(filetext);
            foreach (var rec in kvps)
            {
                if (rec.Key.ToString() == find) { return rec.Value.ToString(); }
            }
            return "";
        }
    }
}

public class ReadKeyFromFile
{
    public string FindInArray(string fullfilepath, string find)
    {
        if (!File.Exists(fullfilepath)) { return "";  }
        string filetext = File.ReadAllText(fullfilepath);
        JArray kvps = ConvertJsonTxt .ConvertStringToJArray(filetext);
        foreach (JToken rec in kvps)
        {
            if (rec[find] is null) { return ""; }
            if (rec[find].ToString() != "") { return rec[find].ToString(); }
        }
        return "";
    }

    public string FindInObject(string fullfilepath, string find)
    {
        if (!File.Exists(fullfilepath)) { return ""; }
        string filetext = File.ReadAllText(fullfilepath);
        JObject kvps = ConvertJsonTxt.ConvertStringToJObject(filetext);
        foreach (var rec in kvps)
        {
            if (rec.Key.ToString() == find) { return rec.Value.ToString(); }
        }
        return "";
    }
}