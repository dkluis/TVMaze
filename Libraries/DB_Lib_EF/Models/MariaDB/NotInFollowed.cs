﻿namespace DB_Lib_EF.Models.MariaDB;

public class NotInFollowed
{
    public int ShowsTvmShowId { get; set; }

    public string ShowName { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? Url { get; set; }

    public int? FollowedTvmShowId { get; set; }
}
