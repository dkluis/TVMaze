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
            
            // https://www.opensubtitles.org/en/search/sublanguageid-eng/searchonlytvseries-on/season-2/episode-1/subformat-srt/moviename-one+lane+bridge
            // https://www.opensubtitles.org/en/subtitleserve/sub/8774960
            // https://www.opensubtitles.org/en/subtitleserve/sub/8774962
            // https://www.opensubtitles.org/en/subtitleserve/sub/7492295
            
            log.Stop();
    }
}