using System;
using System.Diagnostics;
using System.IO;

namespace Common_Lib
{
    public class Logger
    {
        //private readonly string file;
        private readonly string filepath;
        private readonly string fullfilepath;
        private readonly int level;
        protected string app;
        protected Stopwatch timer = new();

       //public Logger(string filename = null, string appl = null, string infilepath = null);
        public Logger(string filename, string appl, string infilepath)
        {
            timer.Start();
            app = appl;
            /*
            if (filename == null || filename == "")
            {
                file = "Logger.log";
            }
            else
            {
                file = filename;
            }
            if (appl == null || appl == "")
            {
                app = "Logger";
            }
            else
            {
                app = appl;
            }

            if (infilepath is null)
            {
                Common.EnvInfo env = new();
                level = Int16.Parse(Common.ReadConfig("LogLevel"));
                if (env.OS == "Windows")
                {
                    filepath = Common.ReadConfig("PCLogPath");
                }
                else
                {
                    filepath = Common.ReadConfig("MacLogPath");
                }
                fullfilepath = Path.Combine(filepath, file);
            }
            else
            {
                filepath = infilepath;
                fullfilepath = Path.Combine(filepath, file);
            }
            */
            
            filepath = infilepath;
            fullfilepath = Path.Combine(filepath, filename);

            if (!File.Exists(this.fullfilepath))
            {
                File.Create(this.fullfilepath).Close();
            }
            Console.WriteLine($"Logfile name is {fullfilepath} ");
        }

        public void Start(string application = null)
        {
            if (application is not null)
            {
                app = application;
            }
            Write($"{app} Started  ##########################################", app, 0);
            EmptyLine();
        }

        public void Stop()
        {
            timer.Stop();
            EmptyLine();
            Write($"{app} Finished ###################  in {timer.ElapsedMilliseconds} mSec  #######################", app, 0);
            EmptyLine(3);
        }

        public void Empty()
        {
            using StreamWriter file = new(fullfilepath, false);
            file.Write("");
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

        public void WriteNoHead(string message, bool newline = true)
        {
            using StreamWriter file = new(fullfilepath, true);
            if(newline) { file.WriteLine(message); } else { file.Write(message); }
        }

        public void WriteNoHead(string[] messages, bool newline = true)
        {
            using StreamWriter file = new(fullfilepath, true);
            foreach (string msg in messages)
            {
                if (newline) { file.WriteLine(msg); } else { file.Write(msg); }
            }
        }

        public static string ReadKey()
        {
            return "";
        }
    }
}



