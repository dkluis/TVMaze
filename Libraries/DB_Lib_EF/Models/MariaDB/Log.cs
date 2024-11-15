﻿namespace DB_Lib_EF.Models.MariaDB;

public class Log
{
    public DateTime RecordedDate { get; set; }

    public string Program { get; set; } = null!;

    public string Function { get; set; } = null!;

    public string Message { get; set; } = null!;

    public int Level { get; set; }
}
