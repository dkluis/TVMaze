using Common_Lib;
using DB_Lib_EF.Models.MariaDB;

namespace DB_Lib_EF.Entities;

public sealed class LogEntity : IDisposable
{
    private bool _disposed;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Release managed resources here
            }

            // Release unmanaged resources here

            _disposed = true;
        }
    }

    ~LogEntity() { Dispose(false); }

    public static void RecordEntry(Log rec)
    {
        try
        {
            var db = new TvMaze();
            TruncateFields(rec);
            db.Logs.Add(rec);
            db.SaveChanges();
        }
        catch (Exception e)
        {
            var appInfo = new AppInfo("TVMaze", "LogEntity", "DbAlternate");
            appInfo.TxtFile.Write($"Abort: {e.Message}");
        }
    }

    private static void TruncateFields(Log rec)
    {
        rec.Program = TruncateStringLength(rec.Program, 30);
        rec.Function = TruncateStringLength(rec.Function, 30);
        rec.Message = TruncateStringLength(rec.Message, 5000);
    }

    private static string TruncateStringLength(string field, int maxLength) { return field.Length > maxLength ? field[..maxLength] : field; }
}
public static class LogModel
{
    public static void Record(Log rec)
    {
        using var logs = new LogEntity();
        LogEntity.RecordEntry(rec);
    }

    public static void Record(string program, string function, string message, int level = 1)
    {
        var log = new Log {RecordedDate = DateTime.Now, Program = program, Function = function, Message = message, Level = level};
        Record(log);
    }

    public static void Start(string program, string function = "Main", string message = "Started Processing", int level = 1)
    {
        var log = new Log {RecordedDate = DateTime.Now, Program = program, Function = function, Message = message, Level = level};
        log.Program = "CronLog";
        log.Function = program;
        log.Level = 0;
        Record(log);
        Thread.Sleep(100);
        log.Program = program;
        log.Function = function;
        log.Level = level;
        log.RecordedDate = DateTime.Now;
        Record(log);
    }

    public static void Stop(string program, string function = "Main", string message = "Stopped Processing", int level = 1)
    {
        var log = new Log {RecordedDate = DateTime.Now, Program = program, Function = function, Message = message, Level = level};
        Record(log);
        Thread.Sleep(100);
        log.RecordedDate = DateTime.Now;
        log.Program = "CronLog";
        log.Function = program;
        log.Level = 0;
        Record(log);
    }

    public static void InActive(string program, string function = "Main", string message = "System is set to InActive", int level = 1)
    {
        var log = new Log {RecordedDate = DateTime.Now, Program = program, Function = function, Message = message, Level = level};
        Record(log);
        Thread.Sleep(100);
        log.RecordedDate = DateTime.Now;
        log.Program = "CronLog";
        log.Function = program;
        log.Level = 0;
        Record(log);
    }

    public static bool IsSystemActive()
    {
        var db = new TvMaze();
        return  db.Configurations.Single(c => c.Key == "IsSystemActive").Value == "true";
    }
}
