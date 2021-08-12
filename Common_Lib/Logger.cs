using System;
using System.Diagnostics;
using System.IO;

namespace Common_Lib
{
    public class Logger
    {
        private readonly string logfile;
        private readonly string logpath;
        private readonly string fulllogpath;
        private readonly int level;
        protected string app;
        protected Stopwatch timer = new();

        public Logger(string logname = null, string appl = null)
        {
            timer.Start();
            if (logname == null || logname == "")
            {
                logfile = "Logger.log";
            }
            else
            {
                logfile = logname;
            }
            if (appl == null || appl == "")
            {
                app = "Logger";
            }
            else
            {
                app = appl;
            }

            Common.EnvInfo env = new();
            level = Int16.Parse(Common.ReadConfig("LogLevel"));
            if (env.OS == "Windows")
            {
                logpath = Common.ReadConfig("PCLogPath");
            }
            else
            {
                logpath = Common.ReadConfig("MacLogPath");
            }
            fulllogpath = Path.Combine(logpath, logfile);

            if (!File.Exists(fulllogpath))
            {
                File.Create(fulllogpath).Close();
            }
            Console.WriteLine($"Logfile name is {fulllogpath} ");
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

        public void Write(string message, string function = "", int loglevel = 3, bool append = true)
        {
            if (function == "" || function == null) { function = app; }
            if (loglevel <= level)
            {
                using StreamWriter file = new(fulllogpath, append);
                file.WriteLine($"{DateTime.Now}: {function,-20}: {loglevel,-2} --> {message}");
            }
        }

        public void Write(string[] messages, string function = "", int loglevel = 3, bool append = true)
        {
            if (function == "" || function == null) { function = app; }
            if (loglevel <= level)
            {
                using StreamWriter file = new(fulllogpath, append);
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
            using StreamWriter file = new(fulllogpath, true);
            while (idx <= lines)
            {
                file.WriteLine($"");
                idx++;
            }
        }

        public void WriteNoHead(string message, bool append = true)
        {
            using StreamWriter file = new(fulllogpath, append);
            file.WriteLine(message);
        }

        public void WriteNoHead(string[] messages, bool append = true)
        {
            using StreamWriter file = new(fulllogpath, append);
            foreach (string msg in messages)
            {
                file.WriteLine(msg);
            }
        }

        public string ReadKey()
        {
            return "";
        }
    }
}



