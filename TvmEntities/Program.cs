﻿using Common_Lib;

namespace TvmEntities
{
    class Program
    {
        static void Main()
        {
            Logger log = new("TvmEntities.log");
            log.Start("TvmEntities");

            using (Show show = new("New-Test-DB", log))
            {
                show.FillViaTvmaze(53057);

                log.Write($"Show: Exists TVM {show.showExistOnTvm}, Followed: {show.isFollowed}, Filled: {show.isFilled}: " +
                    $"Id {show.Id}, TvmShowId {show.TvmShowId}, ShowName {show.ShowName}, Cleaned {show.CleanedShowName}, Finder {show.Finder}, Alt {show.AltShowName}, " +
                    $"U Date {show.UpdateDate}, P Date {show.PremiereDate} Status for Tvm: {show.TvmStatus}, Status {show.ShowStatus}");

                log.Write($"Country {show.TvmCountry}, Image {show.TvmImage}, Imdb {show.TvmImdb}, Language {show.TvmLanguage}, Network {show.TvmNetwork}, Summary {show.TvmSummary}");

                if (show.isForReview)
                {
                    log.Write($"Insert Result is {show.DbInsert()}");
                }
                else
                {
                    log.Write($"Show is NOT rated for Review");
                }
            }

            log.Stop();
        }
    }
}