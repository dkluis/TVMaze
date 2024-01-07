How to (re) scaffold the EF info

dotnet ef dbContext scaffold "Server=ubuntumediahandler.local;port=3306;Database=TVMazeNewDB;uid=dick;pwd=Sandy3942" Pomelo.EntityFrameworkCore.MySql -o Models/MariaDB -f -c TvMaze --schema TvMazeProd --no-build

protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    var appInfo = new AppInfo("TVMaze", "TVMaze-EF", "DbAlternate");
    optionsBuilder.UseMySql(appInfo.ActiveDbConn, ServerVersion.Parse("10.6.16-mariadb"));
}
