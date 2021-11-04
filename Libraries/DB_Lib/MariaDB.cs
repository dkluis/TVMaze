using System;
using Common_Lib;
using MySqlConnector;

namespace DB_Lib
{
    public class MariaDb : IDisposable
    {
        private readonly MySqlConnection _conn;
        public readonly TextFileHandler MDbLog;
        private MySqlCommand _cmd;
        private bool _connOpen;
        private MySqlDataReader _rdr;
        public Exception Exception;

        public int Rows;

        //private MySqlDataAdapter da;
        public bool Success;

        public MariaDb(AppInfo appInfo)
        {
            MDbLog = appInfo.TxtFile;

            Success = false;
            Exception = new Exception();
            try
            {
                _conn = new MySqlConnection(appInfo.ActiveDbConn);
                Success = true;
            }
            catch (Exception e)
            {
                Exception = e;
                MDbLog.Write($"MariaDB Class Connection Error: {e.Message}", null, 0);
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
            Exception = new Exception();
            try
            {
                _conn.Open();
                _connOpen = true;
            }
            catch (Exception e)
            {
                Exception = e;
                MDbLog.Write($"MariaDB Class Open Error: {e.Message}", null, 0);
                Success = false;
            }
        }

        public void Close()
        {
            Success = true;
            Exception = new Exception();
            try
            {
                _conn.Close();
                _connOpen = false;
            }
            catch (Exception e)
            {
                Exception = e;
                MDbLog.Write($"MariaDB Class Close Error: {e.Message}", null, 0);
                Success = false;
            }
        }

        public MySqlCommand Command(string sql)
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
                Exception = e;
                MDbLog.Write($"MariaDB Class Command Error: {e.Message} for {sql}", null, 0);
                Success = false;
                return _cmd;
            }
        }

        public MySqlDataReader ExecQuery()
        {
            Success = true;
            Exception = new Exception();
            try
            {
                if (!_connOpen) Open();
                _rdr = _cmd.ExecuteReader();
                return _rdr;
            }
            catch (Exception e)
            {
                Exception = e;
                MDbLog.Write($"MariaDB Class ExecQuery Error: {e.Message}", null, 0);
                Success = false;
                return _rdr;
            }
        }

        public MySqlDataReader ExecQuery(string sql)
        {
            _cmd = Command(sql);
            Success = true;
            Exception = new Exception();
            try
            {
                if (!_connOpen) Open();
                _rdr = _cmd.ExecuteReader();
                return _rdr;
            }
            catch (Exception e)
            {
                Exception = e;
                MDbLog.Write($"MariaDB Class ExecQuery Error: {e.Message} for {sql}", null, 0);
                Success = false;
                return _rdr;
            }
        }

        public int ExecNonQuery(bool ignore = false)
        {
            Success = true;
            Exception = new Exception();
            try
            {
                if (!_connOpen) Open();
                Rows = _cmd.ExecuteNonQuery();
                if (Rows ! > 0) Success = false;
                return Rows;
            }
            catch (Exception e)
            {
                Exception = e;
                if (!ignore) MDbLog.Write($"MariaDB Class ExecNonQuery Error: {e.Message}", null, 0);
                Success = false;
                return Rows;
            }
        }

        public int ExecNonQuery(string sql, bool ignore = false)
        {
            _cmd = Command(sql);
            Success = true;
            Exception = new Exception();
            try
            {
                if (!_connOpen) Open();
                Rows = _cmd.ExecuteNonQuery();
                if (Rows! > 0) Success = false;
                return Rows;
            }
            catch (Exception e)
            {
                Exception = e;
                if (!ignore) MDbLog.Write($"MariaDB Class ExecNonQuery Error: {e.Message} for {sql}", null, 0);
                Success = false;
                return Rows;
            }
        }
    }
}