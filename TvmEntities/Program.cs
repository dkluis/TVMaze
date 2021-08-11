using Common_Lib;

namespace TvmEntities
{
    class Program
    {
        static void Main()
        {
            Logger log = new("TvmEntities.log");
            log.Start("TvmEntities");

            Show show = new("New-Test-DB", log);
            show.FillViaTvmaze(1);
            log.Write($"Show: Exists TVM {show.showExistOnTvm}, Followed: {show.isFollowed}, Filled: {show.isFilled}: " +
                $"{show.Id}, {show.TvmShowId}, {show.ShowName}, {show.CleanedShowName}, {show.Finder}, {show.AltShowName}, " +
                $"{show.UpdateDate}, Status for Tvm: {show.TvmStatus}, Status {show.ShowStatus}");
            log.Write($"{show.TvmCountry}, {show.TvmImage}, {show.TvmImdb}, {show.TvmLanguage}, {show.TvmNetwork}, {show.TvmSummary}");
            show.Reset();
            
            show.FillViaTvmaze(15);
            log.Write($"Show: Exists TVM {show.showExistOnTvm}, Followed: {show.isFollowed}, Filled: {show.isFilled}: " +
                $"{show.Id}, {show.TvmShowId}, {show.ShowName}, {show.CleanedShowName}, {show.Finder}, {show.AltShowName}, " +
                $"{show.UpdateDate}, Status for Tvm: {show.TvmStatus}, Status {show.ShowStatus}");
            log.Write($"{show.TvmCountry}, {show.TvmImage}, {show.TvmImdb}, {show.TvmLanguage}, {show.TvmNetwork}, {show.TvmSummary}");
            show.Reset();

            show.FillViaTvmaze(18);
            log.Write($"Show: Exists TVM {show.showExistOnTvm}, Followed: {show.isFollowed}, Filled: {show.isFilled}: " +
                $"{show.Id}, {show.TvmShowId}, {show.ShowName}, {show.CleanedShowName}, {show.Finder}, {show.AltShowName}, " +
                $"{show.UpdateDate}, Status for Tvm: {show.TvmStatus}, Status {show.ShowStatus}");
            log.Write($"{show.TvmCountry}, {show.TvmImage}, {show.TvmImdb}, {show.TvmLanguage}, {show.TvmNetwork}, {show.TvmSummary}");
            show.Reset();

            show.FillViaTvmaze(17);
            log.Write($"Show: Exists TVM {show.showExistOnTvm}, Followed: {show.isFollowed}, Filled: {show.isFilled}: " +
                $"{show.Id}, {show.TvmShowId}, {show.ShowName}, {show.CleanedShowName}, {show.Finder}, {show.AltShowName}, " +
                $"{show.UpdateDate}, Status for Tvm: {show.TvmStatus}, Status {show.ShowStatus}");
            log.Write($"{show.TvmCountry}, {show.TvmImage}, {show.TvmImdb}, {show.TvmLanguage}, {show.TvmNetwork}, {show.TvmSummary}");
            show.Reset();

            log.Stop();
        }
    }
}
