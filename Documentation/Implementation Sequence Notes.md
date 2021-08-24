# TVMaze System

## Initial Setup of TVMaze
## Manual setup

***More to be added***
***MySql or MariaDB server is required***

## Setup Directories and ConfigFile

There is Tvmaze.cnf template (in the Template directory) to adjust by replacing the YOURXXXXX items with your username, password, tokens, etc.
After that you need to create a TVMaze directory in your home directory with the following sub-directories:  Apps, Inputs, Logs, and Scripts
Place the adjusted Tvmaze.cnf file in the TVMaze directory. (Not the in a sub-directory)

### Use DBeaver (or commandline, or ...) to drop and create the tables in your DB

**Repeat this for every DB you want to use**

The config file is setup to have a production, test and alternate DB setup in MariaDB.
If you only want a Prod then you only have to setup that one.  You can also change the DB (Schema) name as long as you update the config file as well.

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


