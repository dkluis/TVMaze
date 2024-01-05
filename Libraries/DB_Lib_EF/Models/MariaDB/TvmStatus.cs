using System;
using System.Collections.Generic;

namespace DB_Lib_EF.Models.MariaDB;

public partial class TvmStatus
{
    public string TvmStatus1 { get; set; } = null!;

    public virtual ICollection<Show> Shows { get; set; } = new List<Show>();
}
