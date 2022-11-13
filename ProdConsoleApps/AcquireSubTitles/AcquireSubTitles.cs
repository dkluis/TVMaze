using Common_Lib;

namespace AcquireSubTitles;

internal static class Program
{
    private static void Main()
    {
            const string thisProgram = "Acquire SubTitles";
            Console.WriteLine($"{DateTime.Now}: {thisProgram}");
            AppInfo appInfo = new("TVMaze", thisProgram, "DbAlternate");
            var log = appInfo.TxtFile;
            log.Start();
            
            
            
            log.Stop();
    }
}