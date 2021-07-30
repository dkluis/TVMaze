using System;
using MySqlConnector;
using Common_Lib;
using Log_Lib;

namespace DB_Lib
{
    public class MariaDB : IDisposable
    {
        private readonly MySqlConnection conn;
        private bool connOpen;
        private MySqlCommand cmd;
        private MySqlDataReader rdr;
        // private MySqlDataAdapter da;
        public bool success;
        public int rows;
        public Exception exception;

        // The username, password in the app.config xml file are used here for the testing only.
        // 2 Test databases are setup TestDB and ProdDB.   They are identical except for their data.
        // The TestDB is the default

        public MariaDB(string conninfo = null, Logger log = null)
        {
            Common com = new();
            if (conninfo == null || conninfo == "")
            {
                conninfo = com.ReadConfig("TestDB");
                if (log != null)
                {
                    log.Write($"Configuration String is {conninfo} ");
                }
            }
            else
            {
                conninfo = com.ReadConfig(conninfo);
                Console.WriteLine($"Configuration String is {conninfo} ");
            }
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
                Console.WriteLine($"MariaDB Class Connection Error: {e.Message}");
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
                Console.WriteLine($"MariaDB Class Open Error: {e.Message}");
                success = false;
            }
        }

        private void Close()
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
                Console.WriteLine($"MariaDB Class Close Error: {e.Message}");
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
                return(cmd);
            }
            catch(Exception e)
            {
                exception = e;
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
                success = false;
                return rows;
            }
        }

        void IDisposable.Dispose()
        {
            //throw new NotImplementedException();
            Console.WriteLine("Here we need to implement the MariaDB Dispose Code, like Closing the connection");
            this.Close();
        }
    }
}
