using System;
using Common_Lib;
using MySqlConnector;
using Polly;
using Polly.Retry;

namespace DB_Lib;

public class MariaDb : IDisposable
{
    private readonly MySqlConnection _conn;
    private MySqlCommand _cmd = new();
    private bool _connOpen;
    private MySqlDataReader? _rdr;
    private int _rows;
    private readonly string _thisProgram;
    public bool Success;
    private static readonly RetryPolicy RetryPolicy = Policy.Handle<MySqlException>().WaitAndRetry(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

    public MariaDb(AppInfo appInfo)
    {
        _thisProgram = appInfo.Program;
        Success = false;
        try
        {
            _conn = new MySqlConnection(appInfo.ActiveDbConn);
            Success = true;
        }
        catch (MySqlException e)
        {
            Console.WriteLine($"MariaDB Connect Exception (ErrorCode: {e.Number}): {e.Message}");
            throw new ArgumentException("MariaDB Connect Exception occured {e.Number} - {e.Message}");
            // var logRec = new Log
            // {
            //     RecordedDate = DateTime.Now,
            //     Program = _thisProgram,
            //     Function = "Connecting To DB",
            //     Message = $"Error: {e.Message} ::: {e.InnerException}",
            //     Level = 20,
            // };
            // LogModel.Record(logRec);
        }
    }

    void IDisposable.Dispose()
    {
        Close();
        GC.SuppressFinalize(this);
    }

    private void Open()
    {
        Success = false;

        try
        {
            RetryPolicy.Execute(
                    () =>
                    {
                        _conn.Open();
                        _connOpen = true;
                    }
            );
            Success = true;
        }
        catch (MySqlException e)
        {
            Console.WriteLine($"MariaDB Open Exception (ErrorCode: {e.Number}): {e.Message}");
            throw new ArgumentException("MariaDB Open Exception occured {e.Number} - {e.Message}");
            // var logRec = new Log
            // {
            //     RecordedDate = DateTime.Now,
            //     Program = _thisProgram,
            //     Function = "Opening To DB",
            //     Message = $"Error: {e.Number} ::: {e.Message} ::: {e.InnerException}",
            //     Level = 20,
            // };
            // LogModel.Record(logRec);
            // Success = false;
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
        catch (MySqlException e)
        {
            Console.WriteLine($"MariaDB Close Exception (ErrorCode: {e.Number}): {e.Message}");
            throw new ArgumentException("MariaDB Close Exception occured {e.Number} - {e.Message}");
            // var logRec = new Log
            // {
            //     RecordedDate = DateTime.Now,
            //     Program = _thisProgram,
            //     Function = "Closing DB",
            //     Message = $"MariaDB Class Connection Error: {e.Message} ::: {e.InnerException}",
            //     Level = 20,
            // };
            // LogModel.Record(logRec);
            // Success = false;
        }
    }

    private MySqlCommand Command(string sql)
    {
        Success = true;

        try
        {
            if (!_connOpen)
            {
                Open();
            }
            _cmd = new MySqlCommand(sql, _conn);

            return _cmd;
        }
        catch (MySqlException e)
        {
            Console.WriteLine($"MariaDB Command Exception (ErrorCode: {e.Number}): {e.Message}");
            throw new ArgumentException("MariaDB Command Exception occured {e.Number} - {e.Message}");
            /*var logRec = new Log
            {
                RecordedDate = DateTime.Now,
                Program = _thisProgram,
                Function = "Execute Command",
                Message = $"Error: {e.Message} ::: {e.InnerException}",
                Level = 20,
            };
            LogModel.Record(logRec);
            Success = false;

            return _cmd;*/
        }
    }

    /*public MySqlDataReader ExecQuery()
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
            var logRec = new Log
            {
                RecordedDate = DateTime.Now,
                Program = _thisProgram,
                Function = "Execute Query Cmd",
                Message = $"Error: {e.Message} ::: {e.InnerException}",
                Level = 20,
            };
            LogModel.Record(logRec);
            Success = false;

            return _rdr!;
        }
    }*/

    public MySqlDataReader ExecQuery(string sql)
    {
        _cmd = Command(sql);
        Success = true;

        try
        {
            if (!_connOpen)
            {
                Open();
            }
            _rdr = RetryPolicy.Execute(() => _cmd.ExecuteReader());

            return _rdr;
        }
        catch (MySqlException e)
        {
            Console.WriteLine($"MariaDB ExecQuery Exception (ErrorCode: {e.Number}): {e.Message}");
            throw new ArgumentException("MariaDB ExecQuery Exception occured {e.Number} - {e.Message}");
            /*var logRec = new Log
            {
                RecordedDate = DateTime.Now,
                Program = _thisProgram,
                Function = "Execute Query String",
                Message = $"Error: {e.Message} ::: {e.InnerException}",
                Level = 20,
            };
            LogModel.Record(logRec);
            Success = false;

            return _rdr!;*/
        }
    }

    /*public int ExecNonQuery(bool ignore = false)
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
            var logRec = new Log
            {
                RecordedDate = DateTime.Now,
                Program = _thisProgram,
                Function = "Execute NonQuery Cmd",
                Message = $"Error: {e.Message} ::: {e.InnerException}",
                Level = 20,
            };
            LogModel.Record(logRec);
            Success = false;

            return _rows;
        }
    }*/

    public int ExecNonQuery(string sql, bool ignore = false)
    {
        _cmd = Command(sql);
        Success = true;

        try
        {
            if (!_connOpen)
            {
                Open();
            }
            _rows = RetryPolicy.Execute(() => _cmd.ExecuteNonQuery());
            if (_rows > 0)
            {
                Success = false;
            }

            return _rows;
        }
        catch (MySqlException e)
        {
            Console.WriteLine($"MariaDB ExecQuery Exception (ErrorCode: {e.Number}): {e.Message}");
            throw new ArgumentException("MariaDB ExecQuery Exception occured {e.Number} - {e.Message}");
            /*var logRec = new Log
            {
                RecordedDate = DateTime.Now,
                Program = _thisProgram,
                Function = "Execute NonQuery String",
                Message = $"Error: {e.Message} ::: {e.InnerException}",
                Level = 20,
            };
            LogModel.Record(logRec);
            Success = false;

            return _rows;*/
        }
    }
}
