using System;
using Common_Lib;
using MySqlConnector;

namespace DB_Lib
{
    public class MariaDB : IDisposable
    {
        private readonly MySqlConnection conn;
        private MySqlCommand cmd;
        private bool connOpen;
        public Exception exception;
        public TextFileHandler mdblog;
        private MySqlDataReader rdr;

        public int rows;

        //private MySqlDataAdapter da;
        public bool success;

        public MariaDB(AppInfo appinfo)
        {
            mdblog = appinfo.TxtFile;

            success = false;
            exception = new Exception();
            try
            {
                conn = new MySqlConnection(appinfo.ActiveDbConn);
                success = true;
            }
            catch (Exception e)
            {
                exception = e;
                mdblog.Write($"MariaDB Class Connection Error: {e.Message}", null, 0);
            }
        }

        void IDisposable.Dispose()
        {
            Close();
            GC.SuppressFinalize(this);
        }

        public void Open()
        {
            success = true;
            exception = new Exception();
            try
            {
                conn.Open();
                connOpen = true;
            }
            catch (Exception e)
            {
                exception = e;
                mdblog.Write($"MariaDB Class Open Error: {e.Message}", null, 0);
                success = false;
            }
        }

        public void Close()
        {
            success = true;
            exception = new Exception();
            try
            {
                conn.Close();
                connOpen = false;
            }
            catch (Exception e)
            {
                exception = e;
                mdblog.Write($"MariaDB Class Close Error: {e.Message}", null, 0);
                success = false;
            }
        }

        public MySqlCommand Command(string sql)
        {
            success = true;
            try
            {
                if (!connOpen) Open();
                ;
                cmd = new MySqlCommand(sql, conn);
                return cmd;
            }
            catch (Exception e)
            {
                exception = e;
                mdblog.Write($"MariaDB Class Command Error: {e.Message} for {sql}", null, 0);
                success = false;
                return cmd;
            }
        }

        public MySqlDataReader ExecQuery()
        {
            success = true;
            exception = new Exception();
            try
            {
                if (!connOpen) Open();
                ;
                rdr = cmd.ExecuteReader();
                return rdr;
            }
            catch (Exception e)
            {
                exception = e;
                mdblog.Write($"MariaDB Class ExecQuery Error: {e.Message}", null, 0);
                success = false;
                return rdr;
            }
        }

        public MySqlDataReader ExecQuery(string sql)
        {
            cmd = Command(sql);
            success = true;
            exception = new Exception();
            try
            {
                if (!connOpen) Open();
                ;
                rdr = cmd.ExecuteReader();
                return rdr;
            }
            catch (Exception e)
            {
                exception = e;
                mdblog.Write($"MariaDB Class ExecQuery Error: {e.Message} for {sql}", null, 0);
                success = false;
                return rdr;
            }
        }

        public int ExecNonQuery(bool ignore = false)
        {
            success = true;
            exception = new Exception();
            try
            {
                if (!connOpen) Open();
                ;
                rows = cmd.ExecuteNonQuery();
                if (rows ! > 0) success = false;
                return rows;
            }
            catch (Exception e)
            {
                exception = e;
                if (!ignore) mdblog.Write($"MariaDB Class ExecNonQuery Error: {e.Message}", null, 0);
                success = false;
                return rows;
            }
        }

        public int ExecNonQuery(string sql, bool ignore = false)
        {
            cmd = Command(sql);
            success = true;
            exception = new Exception();
            try
            {
                if (!connOpen) Open();
                ;
                rows = cmd.ExecuteNonQuery();
                if (rows! > 0) success = false;
                return rows;
            }
            catch (Exception e)
            {
                exception = e;
                if (!ignore) mdblog.Write($"MariaDB Class ExecNonQuery Error: {e.Message} for {sql}", null, 0);
                success = false;
                return rows;
            }
        }
    }
}