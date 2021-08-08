DROP table Shows;

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
) ENGINE=InnoDB AUTO_INCREMENT=12 DEFAULT CHARSET=utf8mb4 COMMENT='ASP.NET EF Tables';

Drop Table Episodes;

CREATE TABLE `Episodes` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `ShowId` int(11) NOT NULL,
  `TvmEpisodeId` int(11) NOT NULL,
  `TvmUrl` varchar(175) NOT NULL DEFAULT ' ',
  `SeasonEpisode` varchar(10) NOT NULL,
  `Season` int(11) NOT NULL,
  `Episode` int(11) NOT NULL,
  `BroadcastDate` date NOT NULL DEFAULT '1970-01-01',
  `PlexStatus` varchar(10) NOT NULL DEFAULT ' ',
  `PlexDate` date DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `Episodes_FK` (`ShowId`),
  KEY `Episodes_FK_1` (`PlexStatus`),
  CONSTRAINT `Episodes_FK` FOREIGN KEY (`ShowId`) REFERENCES `Shows` (`Id`),
  CONSTRAINT `Episodes_FK_1` FOREIGN KEY (`PlexStatus`) REFERENCES `PlexStatuses` (`PlexStatus`)
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4;

alter table Shows auto_increment = 1;
alter table Episodes auto_increment = 1;
