
drop table PlexStatuses;
drop table TvmStatuses;
drop table ShowStatuses;
drop table TvmShowUpdates;
drop table Episodes;
drop table Shows;


CREATE TABLE `PlexStatuses` (
  `PlexStatus` varchar(10) NOT NULL,
  PRIMARY KEY (`PlexStatus`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `TvmStatuses` (
  `TvmStatus` varchar(20) NOT NULL DEFAULT ' ',
  PRIMARY KEY (`TvmStatus`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `ShowStatuses` (
  `ShowStatus` varchar(20) NOT NULL DEFAULT ' ',
  PRIMARY KEY (`ShowStatus`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `Shows` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `TvmShowId` int(11) NOT NULL,
  `TvmStatus` varchar(10) NOT NULL,
  `TvmUrl` varchar(175) DEFAULT ' ',
  `ShowName` varchar(100) NOT NULL,
  `ShowStatus` varchar(20) NOT NULL,
  `PremiereDate` date NOT NULL DEFAULT '1970-01-01',
  `Finder` varchar(10) NOT NULL DEFAULT 'Multi',
  `CleanedShowName` varchar(100) NOT NULL DEFAULT ' ',
  `AltShowname` varchar(100) NOT NULL DEFAULT ' ',
  `UpdateDate` date NOT NULL DEFAULT curdate(),
  PRIMARY KEY (`Id`),
  UNIQUE KEY `Shows_UN` (`TvmShowId`),
  KEY `Shows_ShowName_IDX` (`ShowName`) USING BTREE,
  KEY `Shows_CleanedShowName_IDX` (`CleanedShowName`) USING BTREE,
  KEY `Shows_FK` (`TvmStatus`),
  KEY `Shows_FK_1` (`ShowStatus`),
  CONSTRAINT `Shows_FK` FOREIGN KEY (`TvmStatus`) REFERENCES `TvmStatuses` (`TvmStatus`),
  CONSTRAINT `Shows_FK_1` FOREIGN KEY (`ShowStatus`) REFERENCES `ShowStatuses` (`ShowStatus`)
) ENGINE=InnoDB AUTO_INCREMENT=1549 DEFAULT CHARSET=utf8mb4 COMMENT='ASP.NET EF Tables';

CREATE TABLE `Episodes` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `TvmShowId` int(11) NOT NULL,
  `TvmEpisodeId` int(11) NOT NULL,
  `TvmUrl` varchar(175) NOT NULL DEFAULT ' ',
  `SeasonEpisode` varchar(10) NOT NULL,
  `Season` int(11) NOT NULL,
  `Episode` int(11) NOT NULL,
  `BroadcastDate` date DEFAULT NULL,
  `PlexStatus` varchar(10) NOT NULL DEFAULT ' ',
  `PlexDate` date DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `Episodes_UN` (`TvmShowId`,`TvmEpisodeId`),
  KEY `Episodes_FK` (`TvmShowId`),
  KEY `Episodes_FK_1` (`PlexStatus`),
  CONSTRAINT `Episodes_FK` FOREIGN KEY (`TvmShowId`) REFERENCES `Shows` (`TvmShowId`),
  CONSTRAINT `Episodes_FK_1` FOREIGN KEY (`PlexStatus`) REFERENCES `PlexStatuses` (`PlexStatus`)
) ENGINE=InnoDB AUTO_INCREMENT=2674 DEFAULT CHARSET=utf8mb4;

INSERT INTO PlexStatuses (PlexStatus) VALUES
	 (' '),
	 ('Downloaded'),
	 ('Skipped'),
	 ('Watched');
	
INSERT INTO ShowStatuses (ShowStatus) VALUES
	 (' '),
	 ('Ended'),
	 ('In Development'),
	 ('Running'),
	 ('To Be Determined');

INSERT INTO TvmStatuses (TvmStatus) VALUES
	 (' '),
	 ('Followed'),
	 ('Following'),
	 ('New'),
	 ('Skipping'),
	 ('Undecided');

CREATE TABLE `TvmShowUpdates` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `TvmShowId` int(11) NOT NULL,
  `TvmUpdateEpoch` int(11) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `TvmShowUpdates_UN` (`TvmShowId`),
  CONSTRAINT `TvmShowUpdates_FK` FOREIGN KEY (`TvmShowId`) REFERENCES `Shows` (`TvmShowId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
