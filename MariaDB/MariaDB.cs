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
        public Logger mdblog;

        // private readonly Common common = new();

        // The username, password in the app.config xml file are used here for the testing only.
        // 2 Test databases are setup TestDB and ProdDB.   They are identical except for their data.
        // The TestDB is the default

        /*
        public MariaDB(string conninfo = null, Logger log = null)  //To Be Deprecated
        {
            if (log == null)
            {
                mdblog = new();
            }
            else
            {
                mdblog = log;
            }

            if (conninfo == null || conninfo == "")
            {
                conninfo = Common.ReadConfig("TestDB");
            }
            else
            {
                conninfo = Common.ReadConfig(conninfo);
            }
# if DEBUG   
            // mdblog.Write($"Configuration String is {conninfo} ", "MariaDB", 3);
# endif

            success = false;
            exception = new Exception();
            try
            {
                conn = new MySqlConnection(conninfo);
                success = true;
            }
            catch (Exception e)
            {
                exception = e;
                mdblog.Write($"MariaDB Class Connection Error: {e.Message}", "MariaDB", 0);
            }
        }
        */

        public MariaDB(AppInfo appinfo)
        {
            mdblog = appinfo.Log;
          
# if DEBUG   
            // mdblog.Write($"Configuration String is {conninfo} ", "MariaDB", 3);
# endif

            success = false;
            exception = new Exception();
            try
            {
                string connstr = Common.ReadConfig(appinfo.DbConnection);
                conn = new MySqlConnection(connstr);
                success = true;
            }
            catch (Exception e)
            {
                exception = e;
                mdblog.Write($"MariaDB Class Connection Error: {e.Message}", "MariaDB", 0);
            }
        }

        private void Open()
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
                mdblog.Write($"MariaDB Class Open Error: {e.Message}", "MariaDB", 0);
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
                mdblog.Write($"MariaDB Class Close Error: {e.Message}", "MariaDB", 0);
                success = false;
            }
        }

        public MySqlCommand Command(string sql)
        {
            success = true;
            exception = new Exception();
            try
            {
                if (!connOpen) { Open(); };
                cmd = new MySqlCommand(sql, conn);
                return (cmd);
            }
            catch (Exception e)
            {
                exception = e;
                mdblog.Write($"MariaDB Class Command Error: {e.Message} for {sql}", "MariaDB", 0);
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
                mdblog.Write($"MariaDB Class ExecQuery Error: {e.Message}", "MariaDB", 0);
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
                mdblog.Write($"MariaDB Class ExecQuery Error: {e.Message} for {sql}", "MariaDB", 0);
                success = false;
                return rdr;
            }
        }

        public int ExecNonQuery()
        {
            success = true;
            exception = new Exception();
            try
            {
                if (!connOpen) { Open(); };
                rows = cmd.ExecuteNonQuery();
                return rows;
            }
            catch (Exception e)
            {
                exception = e;
                mdblog.Write($"MariaDB Class ExecNonQuery Error: {e.Message}", "MariaDB", 0);
                success = false;
                return rows;
            }
        }

        public int ExecNonQuery(string sql)
        {
            cmd = this.Command(sql);
            success = true;
            exception = new Exception();
            try
            {
                if (!connOpen) { Open(); };
                rows = cmd.ExecuteNonQuery();
                return rows;
            }
            catch (Exception e)
            {
                exception = e;
                mdblog.Write($"MariaDB Class ExecNonQuery Error: {e.Message} for {sql}", "MariaDB", 0);
                success = false;
                return rows;
            }
        }

        void IDisposable.Dispose()
        {
            this.Close();
            conn.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
