using System.Diagnostics;
using Common_Lib;

namespace PlexMediaServerReset
{
    internal static class RefreshOneShow
    {
        private static void Main()
        {
            const string thisProgram = "Plex MS Reset";
            AppInfo appInfo = new("TVMaze", thisProgram, "DbAlternate");
            var log = appInfo.TxtFile;
            log.Start();
            
            var process = Process.GetProcessesByName("Plex Media Server");
            if (process.Length == 1)
            {
                process[0].Kill();
                log.Write($"Kill Plex Media Server with Pid {process[0].Id}", "Plex Reset", 2);
                Thread.Sleep(5000);
                var reset = Process.Start("open", "-a \"Plex Media Server\"");
                log.Write($"Started Plex Media Server with Pid {reset.Id}", "Plex Reset", 2);
            }
            
            log.Stop();
        }
    }
}