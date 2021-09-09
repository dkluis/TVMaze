using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text.RegularExpressions;

namespace Common_Lib
{
    public class Common
    {
        #region Config

        public static string ReadConfig(string key)
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

        public static string RemoveSpecialCharsInShowname(string showname)
        {
            showname = showname.Replace("...", "")
                               .Replace("..", "")
                               .Replace(".", " ")
                               .Replace(",", "")
                               .Replace("   ", " ")
                               .Replace("  ", " ")
                               .Replace("'", "")
                               .Replace("\"", "")
                               .Replace("/", "")
                               .Replace(":", "")
                               .Replace("?", "")
                               .Replace("&#039;", "")
                               .Replace("&amp;", "and")
                               .Replace("&", "and")
                               .Trim()
                               .ToLower();
            showname = showname.Substring(0, showname.Length);  // TODO Fix for strange situation with What If...?   The dots are not handled or seen by the replace or the lenght of the string
            return showname;
        }

        public static string RemoveSuffixFromShowname(string showname)
        {
            string[] plainyear= Regex.Split(showname, "2[0-2][0-3][0-9]", RegexOptions.IgnoreCase);
            string[] wrappedyear = Regex.Split(showname, "(2[0-2][0-3][0-9])", RegexOptions.IgnoreCase);

            if (plainyear.Length == 2) { return plainyear[0]; }
            if (wrappedyear.Length == 2) { return wrappedyear[0]; }
            
            return showname.Replace("(US)", "");
        }

        public static string BuildSeasonEpisodeString(int seas_num, int epi_num)
        {
            return "s" + seas_num.ToString().PadLeft(2, '0') + "e" + epi_num.ToString().PadLeft(2, '0');
        }

        public static string BuildSeasonOnly(int seas_num)
        {
            return "s" + seas_num.ToString().PadLeft(2, '0');
        }

        #endregion

        #region Epochs & DateTime

        public static string ConvertEpochToDate(int epoch)
        {
            string date = "";
            DateTime datetime = new(1970, 1, 1, 0, 0, 0);
            datetime = datetime.AddSeconds(epoch);
            date = datetime.ToString("yyyy-MM-dd");
            return date;
        }

        public static int ConvertDateToEpoch(string date)
        {
            int epoch = 0;
            TimeSpan ts = ConvertDateToDateTime(date) - new DateTime(1970, 1, 1, 0, 0, 0);
            epoch = Convert.ToInt32(ts.TotalSeconds);
            return epoch;
        }

        public static DateTime ConvertDateToDateTime(string date)
        {
            String[] ditems = date.Split("-");
            DateTime datetime = new();
            if (ditems.Length != 3) { return datetime; }
            datetime = new(int.Parse(ditems[0].ToString()), int.Parse(ditems[1].ToString()), int.Parse(ditems[2].ToString()), 0, 0, 0);
            return datetime;
        }

        public static string AddDaysToDate(string date, int days)
        {
            DateTime calculateddt = ConvertDateToDateTime(date);
            calculateddt = calculateddt.AddDays(days);
            date = calculateddt.ToString("yyyy-MM-DD");
            return date;
        }

        public static string SubtractDaysFromDate(string date, int days)
        {
            DateTime calculateddt = ConvertDateToDateTime(date);
            calculateddt = calculateddt.AddDays(-days);
            date = calculateddt.ToString("yyyy-MM-DD");
            return date;
        }

        #endregion

    }
}
