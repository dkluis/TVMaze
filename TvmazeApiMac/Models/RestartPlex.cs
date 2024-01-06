using System.Diagnostics;

namespace TvmazeApiMac.Models;

public class RestartPlex
{
    public string Execute()
    {
        var messageBack = "";
        var process     = Process.GetProcessesByName("Plex Media Serv");

        if (process.Length == 1)
        {
            messageBack += "Killing Plex Media Server Process ## ";
            process[0].Kill();
            Thread.Sleep(5000);
            messageBack += "Restarting Plex Media Server Process ## ";
            RunScript("/Users/dick/TVMazeLinux/Scripts/RestartPlexMediaServer.sh");
            messageBack += "Plex Media Server Restarted ## ";
        } else
        {
            messageBack += "Did not find Plex Media Server Running, trying to start it now. ## ";
            messageBack += "Trying Starting Plex Media Server Process ## ";
            RunScript("/Users/dick/TVMazeLinux/Scripts/RestartPlexMediaServer.sh");
            messageBack += "Plex Media Server Restarted ## ";
        }

        return messageBack;
    }

    private void RunScript(string scriptName)
    {
        using Process runScript = new();
        runScript.StartInfo.FileName               = scriptName;
        runScript.StartInfo.UseShellExecute        = true;
        runScript.StartInfo.RedirectStandardOutput = false;
        runScript.Start();
        runScript.WaitForExit();
    }
}
