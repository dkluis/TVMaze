﻿using System;
using System.Configuration;
using System.IO;
using System.Text.RegularExpressions;

namespace Common_Lib;

public static partial class Common
{
    public static string ReadConfig(string key)
    {
        try
        {
            var appSettings = ConfigurationManager.AppSettings;
            var result      = appSettings[key] ?? "Not Found";

            return result;
        }
        catch (ConfigurationErrorsException e)
        {
            Console.WriteLine($"Error reading app settings in Common {e.Message}");

            return $"Error Reading app.config: {e.BareMessage} ::: {e.InnerException}";
        }
    }

    public static string RemoveSpecialCharsInShowName(string showName)
    {
        showName = showName.Replace("\u2026", "").Replace("..", "").Replace(".", " ").Replace(",", "").Replace("'", "").Replace("   ", " ").Replace("  ", " ").Replace("\"", "").Replace("/", "").Replace(":", "").Replace("?", "").Replace("|", "").Replace("&#039;", "").Replace("&amp;", "and").Replace("&", "and").Replace("°", "").Trim().ToLower();

        return showName;
    }

    public static string RemoveSuffixFromShowName(string showName)
    {
        var wrappedYear = WrappedYearRegex().Split(showName);

        if (wrappedYear.Length == 2) return wrappedYear[0];

        var plainYear = PlainYearRegex().Split(showName);

        if (plainYear.Length == 2) return plainYear[0];

        var wrappedCountry = WrappedCountryRegex().Split(showName);

        return wrappedCountry.Length == 2 ? wrappedCountry[0] : showName;
    }

    public static string BuildSeasonEpisodeString(int seasNum, int epiNum) { return "s" + seasNum.ToString().PadLeft(2, '0') + "e" + epiNum.ToString().PadLeft(2, '0'); }

    public static string BuildSeasonOnly(int seasNum) { return "s" + seasNum.ToString().PadLeft(2, '0'); }

    public static string? ConvertEpochToDate(int epoch)
    {
        DateTime datetime = new(1970, 1, 1, 0, 0, 0);
        datetime = datetime.AddSeconds(epoch);
        var date = datetime.ToString("yyyy-MM-dd");

        return date;
    }

    public static int ConvertDateToEpoch(string? date)
    {
        var ts    = ConvertDateToDateTime(date) - new DateTime(1970, 1, 1, 0, 0, 0);
        var epoch = Convert.ToInt32(ts.TotalSeconds);

        return epoch;
    }

    private static DateTime ConvertDateToDateTime(string? date)
    {
        var      dItems   = date!.Split("-");
        DateTime datetime = new();

        if (dItems.Length != 3) return datetime;
        datetime = new DateTime(int.Parse(dItems[0]), int.Parse(dItems[1]), int.Parse(dItems[2]), 0, 0, 0);

        return datetime;
    }

    public static string? AddDaysToDate(string? date, int days)
    {
        var calculatedDt = ConvertDateToDateTime(date);
        calculatedDt = calculatedDt.AddDays(days);
        date         = calculatedDt.ToString("yyyy-MM-DD");

        return date;
    }

    public static string? SubtractDaysFromDate(string? date, int days)
    {
        var calculatedDt = ConvertDateToDateTime(date);
        calculatedDt = calculatedDt.AddDays(-days);
        date         = calculatedDt.ToString("yyyy-MM-DD");

        return date;
    }

    [GeneratedRegex("[(]2[0-2][0-3][0-9][)]", RegexOptions.IgnoreCase, "en-US")] private static partial Regex WrappedYearRegex();

    [GeneratedRegex("2[0-2][0-3][0-9]", RegexOptions.IgnoreCase, "en-US")] private static partial Regex PlainYearRegex();

    [GeneratedRegex("[(][a-z][a-z][)]", RegexOptions.IgnoreCase, "en-US")] private static partial Regex WrappedCountryRegex();

    public class EnvInfo
    {
        public readonly string  Drive;
        public readonly string  MachineName;
        public readonly string  Os;
        public readonly string  UserName;
        public readonly string? WorkingDrive;
        public readonly string  WorkingPath;

        public EnvInfo()
        {
            var os  = Environment.OSVersion;
            var pid = os.Platform;

            switch (pid)
            {
                case PlatformID.Win32NT:
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                case PlatformID.WinCE:
                    Os    = "Windows";
                    Drive = @"C:\";

                    break;

                case PlatformID.Unix:
                case PlatformID.MacOSX:
                    Os    = "Linux";
                    Drive = "/";

                    break;

                case PlatformID.Xbox:
                case PlatformID.Other:
                default:
                    Os    = "Unknown";
                    Drive = "Unknown";

                    break;
            }

            MachineName  = Environment.MachineName;
            WorkingPath  = Environment.CurrentDirectory;
            WorkingDrive = Path.GetPathRoot(WorkingPath);
            UserName     = Environment.UserName;
        }
    }
}
