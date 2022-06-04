using System;
using System.Collections.Generic;
using Common_Lib;
using DB_Lib;

namespace Entities_Lib
{
    public class Movies : IDisposable
    {
        public string Name;
        public string CleanedName;
        public string AltName;
        public string SeriesName;
        public int MovieNumber;
        public string FinderDate;
        public string MediaType;
        public bool Acquired;

        private readonly MariaDb _mDb;
        //private readonly TextFileHandler _log;

        public Movies(AppInfo appInfo)
        {
            _mDb = new MariaDb(appInfo);
            //_log = appInfo.TxtFile;
        }

        public bool DbInsert()
        {
            var hasRows = _mDb.ExecNonQuery("");
            return hasRows > 0;
        }

        public bool DbUpdate()
        {
            var hasRows = _mDb.ExecNonQuery("");
            return hasRows > 0; 
        }

        public bool DbDelete()
        {
            var hasRows = _mDb.ExecNonQuery("");
            return hasRows > 0;
        }

        public void Reset()
        {
            this.Name = "";
            this.CleanedName = null;
            this.AltName = null;
            this.SeriesName = null;
            this.MovieNumber = 0;
            this.FinderDate = null;
            this.MediaType = "MS";
            this.Acquired = false;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }

    public class MovieSql : IDisposable
    {
        private readonly AppInfo _appInfo;
        private readonly MariaDb _mDb;
        //private readonly TextFileHandler _log;

        public MovieSql(AppInfo appInfo)
        {
            _appInfo = appInfo;
            _mDb = new MariaDb(appInfo);
            //_log = _appInfo.TxtFile;
        }

        /// <summary>
        /// Retrieve all Movies unless the SingleMovie param is used
        /// </summary>
        /// <param name="singleMovie"></param>
        /// <returns>All Movies unless singleMovie param is used</returns>
        public List<Movies> Read(string singleMovie = "")
        {
            var allMovies = new List<Movies>();
            var sql = $"select * from Movies";
            var sqlWhere = "";
            if (singleMovie != "")
                sqlWhere =
                    $" where Name = '{singleMovie}' or CleanedName = '{singleMovie}' or AltName = '{singleMovie}'";
            const string sqlOrder = " order by SeriesName, MovieNumber, Name";
            var rdr = _mDb.ExecQuery(sql + sqlWhere + sqlOrder);
            while (rdr.Read())
            {
                using var movie = new Movies(_appInfo);
                movie.Name = rdr["Name"].ToString();
                movie.CleanedName = rdr["CleanedName"].ToString();
                movie.AltName = rdr["AltName"].ToString();
                movie.SeriesName = rdr["SeriesName"].ToString();
                movie.MovieNumber = int.Parse(rdr["MovieNumber"].ToString()!);
                movie.FinderDate = rdr["FinderDate"].ToString();
                movie.MediaType = rdr["MediaType"].ToString();
                movie.Acquired = bool.Parse(rdr["Acquired"].ToString()!);
                allMovies.Add(movie);
            }
            _mDb.Close();
            return allMovies;
        }
        
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}