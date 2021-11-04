# What do the different Console Applications Do and Why?

## Initialization Apps

The goal is to be able to initialize TVMaze Local through these apps and have a working systems inside of an hour.
Excluding the setup of your MySql or MariaDB, your webserver for the UI, Transmission, ShowRss and Catch. The assumption
is that you are already a TVMaze user but that is not a requirement, you can start completely for zero.

There will be some minimal information coming about ShowRss, Catch and Transmission. You will have to know the DB
Management, Web server, MacOS, Crontab, Shell scripting already.

### Initialize Show Epochs App

The InitializeShowEpoch app is the application that does the initial sync of Show Epochs (Last update of a show and it's
episodes -on TVMaze Web- in Linux epoch date/time format)
between TVMaze Web and TVMaze Local syncing all followed, after which the normal operational Show Related apps will do
the rest.

### Initialize Episodes Apps

The Initialize Episodes app is the applicationt that does the inital sync of all previous episodes information between
TVMaze Web and TVMaze Local of all followed shows, after which the normal operational Episode Related apps will do the
rest.

## Shows Related Apps

## Episodes Related Apps

## ShowRss Related Apps

### Refresh ShowRss

The RefreshShowRss app is the application that captures the ShowRss shows and uses that information to find (via name,
cleaned name and alternate name)
the appropriate show in the TVMaze local db to update so that this show is ignored in the Start Transmission Transfer
application.

## Plex Related App

### Sync Watched events

The SyncWatchEvents app is the application that reads and detect when an episodes is marked watched by Plex and updates
TVMaze Web and Local with that information

1. Read Plex's Sqlite3 DB
2. Validate against TVMaze local db that this is a new watched event
3. Update TVMaze web via premium APIs that the episode is watched
4. Update the TVMaze local db that the episode is watched

## Transmission Related App

### Start Transmission Transfer event

The StartTransmission app is the applicaiton that is triggered when an episode (or all episodes of a season) need to be
acquired. This is based on the episode information in the TVMaze local db and ShowRss managed shows are ignored since
they automatically get acquired Catch based on ShowRss feeds.

1. The episodes that have an airdate before today and are not managed by ShowRss are selected for processing
1. If a selected episode is a season starter then the search mechnanism will try to find a provider that has the whole
   season available first
    1. If found, then all other episodes of the same season are ignored in the search
    1. If not found, then each episode goes through the provider search separately
1. Any found episode or season is passed along to Transmission for transfer
1. Any found episode or season is processed into TVMaze web and TVMaze local when they are acquired, to stop triggering
   another transfer event for the same season or episode.

### Sync Transmission Receipt events

The SyncTranmission app is the application that is triggered when Transmission finishes a transfer event and move the
transfered media to Plex and also updates TVMaze web and TVMaze local that the media is acquired

1. Transmission is setup to trigger a shell script when the transfer is finished
2. The shell script run the SyncTransmission app
3. Evaluates the media and uses configuration information to move the media from the receiving location (Transmission
   directories) to the Plex Media directory separating them into movies, tvshows, etc.
4. Update TVMaze web and local for episodes and whole seasons that they are acquired and available on Plex.
