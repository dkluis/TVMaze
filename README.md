The author of this library are in no way responsible for any copyright infiringements caused by using this library or software using this library. There are many legitimate use-cases for torrents outside of piracy. This library was written as a training/education excerside for the author and with the intention to be used for such legal purposes.


# TVMaze Local


***TVMaze is a system to manage the TV Shows and Episdes you want to follow and get onto your Plex installation***


## Premise

TVMaze Local uses TVMaze (Web) to stay up to date with the world of TV and Plex as the Media Library manager, as well as ShowRss and Transmission

## Approach

TVmaze Local implements the public as well as user (premium) APIs from TVMaze (Web) to keep track of the shows you follow and its episodes, include acquired, watched and skip statuses.

Complete syncing between TVMaze local, TVMaze web, Plex, ShowRss and Transmission is the goal, with no user interaction where possible.

TVMaze local has "Review rules" for new shows added to TVMaze web and if a show passes the review rules it is added to TVMaze local for User approval or rejection, either decision will be synced out.

See the [Implementation Notes](https://github.com/dkluis/TVMaze/blob/8aafbbb9efaabc2a7347404f3a39faa9332d0c46/Documentation/Implementation%20Sequence%20Notes.md) in the Templates Directory.

## Technology

1. Written in C# (DotNet Core 5.0 and NewtonSoft)
    1. Implemented and Tested on MacOS (should work on Windows or Linux as well)
    1. Platypus (dotnet xxx.dll to xxx.app) (MacOS only)
1. MariaDB Server (mySql should as well)
    1. DBeaver (DB Management)
1. Apache (Web Server)
    1. TVMaze Local UI
1. Transmission (Media Transfer)
1. ShowRss (Media Transfer information feed)
1. Catch (Interface between ShowRss feed and Transmission)

## TVMaze Local Libraries

1. Web_Lib
    1. WebAPIs.cs (Handles all TVMaze web API activities)
    1. WebScrape.cs (Handles all other web activities )
1. Entities_lib (Handles all activities for:)
    1. Shows.cs     
    1. Episodes.cs  
    1. Followed.cs  (Track and manage all "Followed" shows)
1. DB_Lib
    1. MariaDB (Handles all MariaDB I/O)
1. Common_Lib
    1. AppInfo.cs (Handles all initialization of programs and libraries)
    1. Common.cs
    1. ConverJsonText.cs (Handles conversions to and from JSON)
    1. TextFileHandler.cs (Handles all Logging and Config File access for programs and libraries)
