using System;
using Common_Lib;
using Web_Lib;

var thisProgram = "Count My Episodes";
Console.WriteLine($"{DateTime.Now}: {thisProgram}");
AppInfo appinfo = new("TVMaze", thisProgram, "DbAlternate");
var     log     = appinfo.TxtFile;
log.Start();

WebApi tvmapi      = new(appinfo);
var    jsoncontent = tvmapi.ConvertHttpToJArray(tvmapi.GetAllEpisodes());

var count = 0;
foreach (var episode in jsoncontent)
{
    var episode_id = episode["episode_id"] ?? "";
    log.Write(episode.ToString(), "", 0);
    count++;
}

log.Write($"Processed {count} Episodes");

log.Stop();
