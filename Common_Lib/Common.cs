using System;
using System.Configuration;
using System.IO;

namespace Common_Lib
{
    public class Common
    {
        #region Config

        public string ReadConfig(string key)
        {
            try
            {
                var appSettings = ConfigurationManager.AppSettings;
                string result = appSettings[key] ?? "Not Found";
                return result;
            }
            catch (ConfigurationErrorsException e)
            {
                Console.WriteLine($"Error reading app settings in Common {e.Message}");
                return $"Error Reading app.config: {e.BareMessage}";
            }
        }

        public class EnvInfo
        {
            public readonly string OS;
            public readonly string MachineName;
            public readonly string UserName;
            public readonly string WorkingPath;
            public readonly string WorkingDrive;
            public readonly string Drive;

            public EnvInfo()
            {
                OperatingSystem os = Environment.OSVersion;
                PlatformID pid = os.Platform;
                switch (pid)
                {
                    case PlatformID.Win32NT:
                    case PlatformID.Win32S:
                    case PlatformID.Win32Windows:
                    case PlatformID.WinCE:
                        OS = "Windows";
                        Drive = @"C:\";
                        break;
                    case PlatformID.Unix:
                    case PlatformID.MacOSX:
                        OS = "Linux";
                        Drive = @"/";
                        break;
                    default:
                        OS = "Unknown";
                        Drive = "Unknown";
                        break;
                }
                MachineName = Environment.MachineName.ToString();
                WorkingPath = Environment.CurrentDirectory;
                WorkingDrive = Path.GetPathRoot(WorkingPath);
                UserName = Environment.UserName;
            }
        }

        #endregion

        #region TVShows

        public int ReturnSeason(string episode)
        {
            return 0;
        }

        public int ReturnEpisode(string episode)
        {
            return 1;
        }

        #endregion
    }
}
