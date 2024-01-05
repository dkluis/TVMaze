using System;
using System.Collections.Generic;

namespace DB_Lib_EF.Models.MariaDB;

public partial class ShowsToRefresh
{
    public int TvmShowId { get; set; }

    public string TvmStatus { get; set; } = null!;

    public DateOnly PremiereDate { get; set; }

    public DateOnly UpdateDate { get; set; }

    public string ShowName { get; set; } = null!;

    public string? TvmUrl { get; set; }

    public string ShowStatus { get; set; } = null!;
}
