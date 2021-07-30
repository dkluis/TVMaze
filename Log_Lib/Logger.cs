using System;
using Common_Lib;
using System.IO;

namespace Log_Lib
{
    public class Logger
    {
        private string logfile;
        private string logpath;
        private string fulllogpath;

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

        public void Write(string message, bool append = true)
        {
            using StreamWriter file = new(fulllogpath, append);
                file.WriteLine(message);
        }

        public void Write(string[] messages, bool append = true)
        {
            using StreamWriter file = new(fulllogpath, append);
                foreach (string msg in messages)
                {
                    file.WriteLine(msg);
                }
        }
    }
}
