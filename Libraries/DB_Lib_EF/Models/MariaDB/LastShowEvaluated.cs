using System;
using System.Collections.Generic;

namespace DB_Lib_EF.Models.MariaDB;

public partial class LastShowEvaluated
{
    public int Id { get; set; }

    public int ShowId { get; set; }
}
