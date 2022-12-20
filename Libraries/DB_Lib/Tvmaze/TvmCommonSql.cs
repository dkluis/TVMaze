using System;
using Common_Lib;
using MySqlConnector;

namespace DB_Lib.Tvmaze;

public class TvmCommonSql : IDisposable
{
    private readonly MariaDb _db;
    private MySqlDataReader? _rdr;

    public TvmCommonSql(AppInfo appInfo)
    {
        _db = new MariaDb(appInfo);
    }


    public void Dispose()
    {
        _db.Close();
        GC.SuppressFinalize(this);
    }

    public int GetLastTvmShowIdInserted()
    {
        var lastShowInserted = 99999999;
        _rdr = _db.ExecQuery("select TvmShowId from TvmShowUpdates order by TvmShowId desc limit 1;");
        while (_rdr.Read()) lastShowInserted = int.Parse(_rdr["TvmShowid"].ToString()!);
        _db.Close();
        return lastShowInserted;
    }

    public bool IsShowIdFollowed(int showId)
    {
        var isFollowed = false;
        _rdr = _db.ExecQuery($"select TvmShowId from Followed where `TvmShowId` = {showId};");
        while (_rdr.Read()) isFollowed = true;
        _db.Close();
        return isFollowed;
    }

    public bool IsShowIdEnded(int showId)
    {
        var isEnded = false;
        _rdr = _db.ExecQuery($"select ShowStatus from Shows where `TvmShowId` = {showId};");
        while (_rdr.Read())
            if (_rdr["ShowStatus"].ToString() == "Ended")
                isEnded = true;

        _db.Close();
        return isEnded;
    }

    public int GetShowEpoch(int showId)
    {
        var epoch = 0;
        _rdr = _db.ExecQuery($"select `TvmUpdateEpoch` from TvmShowUpdates where `TvmShowId` = {showId};");

        while (_rdr.Read()) epoch = int.Parse(_rdr["TvmUpdateEpoch"].ToString()!);

        return epoch;
    }

    public int GetLastEvaluatedShow()
    {
        var epoch = 0;

        _rdr = _db.ExecQuery("select `ShowId` from LastShowEvaluated where `Id` = 1;");

        while (_rdr.Read()) epoch = int.Parse(_rdr["ShowId"].ToString()!);
        _db.Close();
        return epoch;
    }

    public void SetLastEvaluatedShow(int newLastEpoch)
    {
        _db.ExecNonQuery($"update LastShowEvaluated set `ShowId` = {newLastEpoch};");
        _db.Close();
    }
}
