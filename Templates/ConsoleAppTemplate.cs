using System;
using Common_Lib;

namespace This_Program
{
    class Program
    {
        static void Main(string[] args)
        {
            const string This_Program = "This Program";
            AppInfo appinfo = new("TVMaze", This_Program, "DB Needed from Config");
        }
    }
}
