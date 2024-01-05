using System;
using System.Collections.Generic;

namespace DB_Lib_EF.Models.MariaDB;

public partial class ActionItem
{
    public int Id { get; set; }

    public string Program { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string UpdateDateTime { get; set; } = null!;
}
