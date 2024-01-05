using System;
using System.Collections.Generic;

namespace DB_Lib_EF.Models.MariaDB;

public partial class ShowRssFeed
{
    public int Id { get; set; }

    public string ShowName { get; set; } = null!;

    public bool? Processed { get; set; }

    public string Url { get; set; } = null!;

    public string UpdateDate { get; set; } = null!;
}
