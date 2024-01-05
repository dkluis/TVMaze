using System;
using System.Collections.Generic;

namespace DB_Lib_EF.Models.MariaDB;

public partial class Followed
{
    public int Id { get; set; }

    public int TvmShowId { get; set; }

    public DateOnly UpdateDate { get; set; }
}
