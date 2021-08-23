using Common_Lib;
using MySqlConnector;
using System;

namespace DB_Lib
{
    public class MariaDB : IDisposable
    {
        private readonly MySqlConnection conn;
        private bool connOpen;
        private MySqlCommand cmd;
        private MySqlDataReader rdr;
        //private MySqlDataAdapter da;
        public bool success;
        public int rows;
        public Exception exception;
        public TextFileHandler mdblog;

        public MariaDB(AppInfo appinfo)
        {
            mdblog = appinfo.TxtFile;

            success = false;
            exception = new Exception();
            try
            {
                //string connstr = Common.ReadConfig(appinfo.DbConnection);
                conn = new MySqlConnection(appinfo.ActiveDBConn);
                success = true;
            }
            catch (Exception e)
            {
                exception = e;
                mdblog.Write($"MariaDB Class Connection Error: {e.Message}", null, 0);
            }
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
            // exception = new Exception();
            try
            {
                if (!connOpen) { Open(); };
                cmd = new MySqlCommand(sql, conn);
                return (cmd);
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
                if (!connOpen) { Open(); };
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
            cmd = this.Command(sql);
            success = true;
            exception = new Exception();
            try
            {
                if (!connOpen) { Open(); };
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
                if (!connOpen) { Open(); };
                rows = cmd.ExecuteNonQuery();
                if (rows !> 0) { success = false; }
                return rows;
            }
            catch (Exception e)
            {
                exception = e;
                if (!ignore) { mdblog.Write($"MariaDB Class ExecNonQuery Error: {e.Message}", null, 0); }
                success = false;
                return rows;
            }
        }

        public int ExecNonQuery(string sql, bool ignore = false)
        {
            cmd = this.Command(sql);
            success = true;
            exception = new Exception();
            try
            {
                if (!connOpen) { Open(); };
                rows = cmd.ExecuteNonQuery();
                if (rows! > 0) { success = false; }
                return rows;
            }
            catch (Exception e)
            {
                exception = e;
                if (!ignore) { mdblog.Write($"MariaDB Class ExecNonQuery Error: {e.Message} for {sql}", null, 0); }
                success = false;
                return rows;
            }
        }

        void IDisposable.Dispose()
        {
            this.Close();
            GC.SuppressFinalize(this);
        }
    }
}
