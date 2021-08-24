# TVMaze System

## Initial Setup of TVMaze
## Manual setup

***More to be added***
***MySql or MariaDB server is required***
***Certain Directories and a configfile are required***

### Use DBeaver (or commandline, or ...) to drop and create the tables in your DB

1. Copy the script from DB_Lib>Scripts>CreateBaseDB.sql
1. NOTE:  Make sure to update the values statement for Table LastShowEvaluated before executing the script.
1. Execute the whole script

### Execute in sequence

***Initialize Items***
1.  InitializeShowEpochs        One Time Only
1.  InitializeEpisodes          One Time Only

***Show Items***
1.  UpdateFollowed                                
1.  RefreshShows                              
1.  RefreshShowRss          
1.  UpdateShowEpochs
                            
***Episode Items***
1.  UpdateEpisodes
1.  UpdateEpisodesFromPlex (and TVMaze)
1.  RefreshEpisodes


***Operational Items***
1.  FindEpisodesMagnets
1.  Cleanup PlexMedia directories

***OnDemand Items***
1. Web UI Tvmaze

# Ongoing Daily, Weekly, etc Sequences
## Daily Sequence (xx minutes)

1. UpdateEpisodesFromPlex   - 15 Minutes (01)
1. Web UI Tvmaze            - On Demand

## Daily Sequence (xx hourly)

1. UpdateFollowed           - 6 Hourly  (45)
1. UpdateShowEpochs         - 1 Hourly  (05)
1. UpdateEpisodes           - 1 Hourly after UpdateShowEpochs (10)

## Daily Sequence (at xx)

***Repeated within a time window***
1. FindEpisodeMagnets       - Between 4am and 8pm every 30 minutes (15) on CA-Media
1. RefreshShowRss           - Between 3am and 8pm every 2 Hours (00)

***Once a day***
1. RefreshShows             - 12:00 am
1. RefreshEpisodes          -  1:00 am

## Weekly Sequence (xx day of week)

## Monthly Sequence (xx day of Month)


