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
        var db = new TvMaze();
        db.Logs.Add(rec);
        db.SaveChanges();

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
}
