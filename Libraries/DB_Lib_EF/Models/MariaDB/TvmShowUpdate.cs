using System;
using System.Collections.Generic;

namespace DB_Lib_EF.Models.MariaDB;

public partial class TvmShowUpdate
{
    public int Id { get; set; }

    public int TvmShowId { get; set; }

    public int TvmUpdateEpoch { get; set; }

    public DateOnly? TvmUpdateDate { get; set; }

    public virtual Show? Show { get; set; }
}
