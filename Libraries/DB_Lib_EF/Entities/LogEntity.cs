using Common_Lib;

using DB_Lib_EF.Models.MariaDB;

namespace DB_Lib_EF.Entities;

public class LogEntity : IDisposable
{
    private bool _disposed = false;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
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

    ~LogEntity()
    {
        Dispose(false);
    }

    public bool RecordEntry(Log rec)
    {
        try
        {
            var db                                     = new TvMaze();
            if (rec.Program.Length  > 30) rec.Program  = rec.Program.Substring(0, 30);
            if (rec.Function.Length > 30) rec.Function = rec.Function.Substring(0, 30);
            if (rec.Message.Length  > 300) rec.Message = rec.Message.Substring(0, 300);
            db.Logs.Add(rec);
            db.SaveChanges();
        }
        catch (Exception e)
        {
            var appInfo = new AppInfo("CronLog", "LogEntity", "DbAlternate");
            appInfo.TxtFile.Write($"Abort: {e.Message}");
            
            return false;
        }

        return true;
    }
}

public static class LogModel
{
    public static bool Record(Log rec)
    {
        using var logs = new LogEntity();

        return logs.RecordEntry(rec);
    }

    public static bool Record(string program, string function, string message, int level = 3)
    {
        var log = new Log
                  {
                      RecordedDate = DateTime.Now,
                      Program      = program,
                      Function     = function,
                      Message      = message,
                      Level        = level,
                  };

        return Record(log);
    }

    public static bool Start(string program, string function = "Main", string message = "Starting Processing", int level = 3)
    {
        var log = new Log
                  {
                      RecordedDate = DateTime.Now,
                      Program      = program,
                      Function     = function,
                      Message      = message,
                      Level        = level
                  };

        log.Program  = "CronLog";
        log.Function = program;
        log.Level    = 0;
        Record(log);
        Thread.Sleep(100);

        log.Program      = program;
        log.Function     = function;
        log.Level        = level;
        log.RecordedDate = DateTime.Now;

        return Record(log);
    }

    public static bool Stop(string program, string function = "Main", string message = "Stopped Processing", int level = 3)
    {
        var log = new Log
                  {
                      RecordedDate = DateTime.Now,
                      Program      = program,
                      Function     = function,
                      Message      = message,
                      Level        = level
                  };

        Record(log);
        Thread.Sleep(100);

        log.RecordedDate = DateTime.Now;
        log.Program      = "CronLog";
        log.Function     = program;
        log.Level        = 0;

        return Record(log);
    }
}
