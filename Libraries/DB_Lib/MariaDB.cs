using System;
using Common_Lib;
using MySqlConnector;

namespace DB_Lib;

public class MariaDb : IDisposable
{
    private const string Function = "MariaDb";
    private readonly MySqlConnection _conn = new();
    private readonly TextFileHandler _mDbLog;
    private MySqlCommand _cmd = new();
    private bool _connOpen;
    private MySqlDataReader? _rdr;
    private int _rows;
    public bool Success;

    public MariaDb(AppInfo appInfo)
    {
        _mDbLog = appInfo.TxtFile;

        Success = false;
        try
        {
            _conn = new MySqlConnection(appInfo.ActiveDbConn);
            Success = true;
        }
        catch (Exception e)
        {
            _mDbLog.Write($"MariaDB Class Connection Error: {e.Message}", Function, 0);
        }
    }

    void IDisposable.Dispose()
    {
        Close();
        GC.SuppressFinalize(this);
    }

    public void Open()
    {
        Success = true;
        try
        {
            _conn.Open();
            _connOpen = true;
        }
        catch (Exception e)
        {
            _mDbLog.Write($"MariaDB Class Open Error: {e.Message}", Function, 0);
            Success = false;
        }
    }

    public void Close()
    {
        Success = true;
        try
        {
            _conn.Close();
            _connOpen = false;
        }
        catch (Exception e)
        {
            _mDbLog.Write($"MariaDB Class Close Error: {e.Message}", Function, 0);
            Success = false;
        }
    }

    private MySqlCommand Command(string sql)
    {
        Success = true;
        try
        {
            if (!_connOpen) Open();
            _cmd = new MySqlCommand(sql, _conn);
            return _cmd;
        }
        catch (Exception e)
        {
            _mDbLog.Write($"MariaDB Class Command Error: {e.Message} for {sql}", Function, 0);
            Success = false;
            return _cmd;
        }
    }

    public MySqlDataReader ExecQuery()
    {
        Success = true;
        try
        {
            if (!_connOpen) Open();
            _rdr = _cmd.ExecuteReader();
            return _rdr;
        }
        catch (Exception e)
        {
            _mDbLog.Write($"MariaDB Class ExecQuery Error: {e.Message}", Function, 0);
            Success = false;
            return _rdr!;
        }
    }

    public MySqlDataReader ExecQuery(string sql)
    {
        _cmd = Command(sql);
        Success = true;
        try
        {
            if (!_connOpen) Open();
            _rdr = _cmd.ExecuteReader();
            return _rdr;
        }
        catch (Exception e)
        {
            _mDbLog.Write($"MariaDB Class ExecQuery Error: {e.Message} for {sql}", Function, 0);
            Success = false;
            return _rdr!;
        }
    }

    public int ExecNonQuery(bool ignore = false)
    {
        Success = true;
        try
        {
            if (!_connOpen) Open();
            _rows = _cmd.ExecuteNonQuery();
            if (_rows > 0) Success = false;
            return _rows;
        }
        catch (Exception e)
        {
            if (!ignore) _mDbLog.Write($"MariaDB Class ExecNonQuery Error: {e.Message}", Function, 0);
            Success = false;
            return _rows;
        }
    }

    public int ExecNonQuery(string sql, bool ignore = false)
    {
        _cmd = Command(sql);
        Success = true;
        try
        {
            if (!_connOpen) Open();
            _rows = _cmd.ExecuteNonQuery();
            if (_rows > 0) Success = false;
            return _rows;
        }
        catch (Exception e)
        {
            if (!ignore) _mDbLog.Write($"MariaDB Class ExecNonQuery Error: {e.Message} for {sql}", Function, 0);
            Success = false;
            return _rows;
        }
    }
}