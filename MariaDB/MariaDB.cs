﻿using System;
using MySqlConnector;

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

        public MariaDB(string CI = @"server=ca-server.local; database=Test-TVM-DB; uid=dick; pwd=Sandy3942")
        {
            success = false;
            exception = new Exception();
            try
            {
                conn = new MySqlConnection(CI);
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

        public MySqlDataReader ExecNonQuery()
        {
            success = true;
            exception = new Exception();
            try
            {
                if (!connOpen) { Open(); };
                rows = cmd.ExecuteNonQuery();
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

        void IDisposable.Dispose()
        {
            //throw new NotImplementedException();
            Console.WriteLine("Here we need to implement the MariaDB Dispose Code, like Closing the connection");
            this.Close();
        }
    }
}