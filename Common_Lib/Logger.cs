using Common_Lib;
using System;
using System.IO;

namespace Common_Lib
{
    public class Logger
    {
        private string logfile;
        private string logpath;
        private string fulllogpath;
        private int level;

        public Logger(string logname = null)
        {
            if (logname == null || logname == "")
            {
                logfile = "Logger.log";
            }
            else
            {
                logfile = logname;
            }

            Common.EnvInfo env = new();
            Common com = new();
            level = Int16.Parse(com.ReadConfig("LogLevel"));
            if (env.OS == "Windows")
            {
                logpath = com.ReadConfig("PCLogPath");
            }
            else
            {
                logpath = com.ReadConfig("MacLogPath");
            }
            fulllogpath = Path.Combine(logpath, logfile);

            if (!File.Exists(fulllogpath))
            {
                File.Create(fulllogpath).Close();
            }
            Console.WriteLine($"Logfile name is {fulllogpath} ");
        }

        public void Write(string message, string function = "", int loglevel = 1, bool append = true)
        {
            if (loglevel <= level)
            {
                using StreamWriter file = new(fulllogpath, append);
                file.WriteLine($"{DateTime.Now}: {function.PadRight(20)}: {loglevel.ToString().PadRight(3)} --> {message}");
            }
        }

        public async void WriteAsync(string message, string function = "", int loglevel = 1, bool append = true)
        {
            if (loglevel <= level)
            {
                using StreamWriter file = new(fulllogpath, append);
                await file.WriteLineAsync($"{DateTime.Now}: {function.PadRight(20)}: {loglevel.ToString().PadRight(3)} --> {message}");
            }
        }

        public void Write(string[] messages, string function = "", int loglevel = 1, bool append = true)
        {
            if (loglevel <= level)
            {
                using StreamWriter file = new(fulllogpath, append);
                foreach (string msg in messages)
                {
                    file.WriteLine($"{DateTime.Now}: {function.PadRight(20)}: {loglevel.ToString().PadRight(2)}--> {msg}");
                }
            }
        }
    }
}
