drop table Episodes;
drop table PlexStatuses;

drop table Shows;
drop table TvmStatuses;
drop table ShowStatuses;
drop table TvmShowUpdates;
drop table Followed;

drop table LastShowEvaluated;
drop table ActionItems;

CREATE TABLE `LastShowEvaluated`
(
    `Id`     int(11) NOT NULL AUTO_INCREMENT,
    `ShowId` int(11) NOT NULL,
    PRIMARY KEY (`Id`)
) ENGINE = InnoDB
  AUTO_INCREMENT = 0
  DEFAULT CHARSET = utf8mb4;

INSERT INTO LastShowEvaluated (ShowId)
VALUES (57099);

CREATE TABLE `ActionItems`
(
    `Id`             int(11)      NOT NULL AUTO_INCREMENT,
    `Program`        varchar(25)  NOT NULL,
    `Message`        varchar(200) NOT NULL,
    `UpdateDateTime` varchar(20)  NOT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `ActionItems_UN` (`Program`, `Message`, `UpdateDateTime`)
) ENGINE = InnoDB
  AUTO_INCREMENT = 0
  DEFAULT CHARSET = utf8mb4;

CREATE TABLE `PlexStatuses`
(
    `PlexStatus` varchar(10) NOT NULL,
    PRIMARY KEY (`PlexStatus`)
) ENGINE = InnoDB
  DEFAULT CHARSET = utf8mb4;

CREATE TABLE `TvmStatuses`
(
    `TvmStatus` varchar(20) NOT NULL DEFAULT ' ',
    PRIMARY KEY (`TvmStatus`)
) ENGINE = InnoDB
  DEFAULT CHARSET = utf8mb4;

CREATE TABLE `ShowStatuses`
(
    `ShowStatus` varchar(20) NOT NULL DEFAULT ' ',
    PRIMARY KEY (`ShowStatus`)
) ENGINE = InnoDB
  DEFAULT CHARSET = utf8mb4;

CREATE TABLE `TvmShowUpdates`
(
    `Id`             int(11) NOT NULL AUTO_INCREMENT,
    `TvmShowId`      int(11) NOT NULL,
    `TvmUpdateEpoch` int(11) NOT NULL,
    `TvmUpdateDate`  date DEFAULT '1900-01-01',
    PRIMARY KEY (`Id`),
    UNIQUE KEY `TvmShowUpdates_TvmShowId` (`TvmShowId`)
) ENGINE = InnoDB
  AUTO_INCREMENT = 0
  DEFAULT CHARSET = utf8mb4;

INSERT INTO PlexStatuses (PlexStatus)
VALUES (' '),
       ('Acquired'),
       ('Skipped'),
       ('Watched');

INSERT INTO ShowStatuses (ShowStatus)
VALUES (' '),
       ('Ended'),
       ('In Development'),
       ('Running'),
       ('To Be Determined');

INSERT INTO TvmStatuses (TvmStatus)
VALUES (' '),
       ('Following'),
       ('New'),
       ('Skipping'),
       ('Undecided');

CREATE TABLE `Shows`
(
    `Id`              int(11)      NOT NULL AUTO_INCREMENT,
    `TvmShowId`       int(11)      NOT NULL,
    `TvmStatus`       varchar(10)  NOT NULL,
    `TvmUrl`          varchar(175)          DEFAULT ' ',
    `ShowName`        varchar(100) NOT NULL,
    `ShowStatus`      varchar(20)  NOT NULL,
    `PremiereDate`    date         NOT NULL DEFAULT '1970-01-01',
    `Finder`          varchar(10)  NOT NULL DEFAULT 'Multi',
    `CleanedShowName` varchar(100) NOT NULL DEFAULT ' ',
    `AltShowname`     varchar(100) NOT NULL DEFAULT ' ',
    `UpdateDate`      date         NOT NULL DEFAULT curdate(),
    PRIMARY KEY (`Id`),
    UNIQUE KEY `Shows_TvmShowId` (`TvmShowId`),
    KEY `Shows_ShowName_IDX` (`ShowName`) USING BTREE,
    KEY `Shows_CleanedShowName_IDX` (`CleanedShowName`) USING BTREE,
    KEY `Shows_FK` (`TvmStatus`),
    KEY `Shows_FK_1` (`ShowStatus`),
    CONSTRAINT `Shows_FK` FOREIGN KEY (`TvmStatus`) REFERENCES `TvmStatuses` (`TvmStatus`),
    CONSTRAINT `Shows_FK_1` FOREIGN KEY (`ShowStatus`) REFERENCES `ShowStatuses` (`ShowStatus`),
    CONSTRAINT `Shows_FK_2` FOREIGN KEY (`TvmShowId`) REFERENCES `TvmShowUpdates` (`TvmShowId`)
) ENGINE = InnoDB
  AUTO_INCREMENT = 0
  DEFAULT CHARSET = utf8mb4;

CREATE TABLE `Followed`
(
    `Id`         int(11) NOT NULL AUTO_INCREMENT,
    `TvmShowId`  int(11) NOT NULL,
    `UpdateDate` date    NOT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `TvmFollowedShows_UN` (`TvmShowId`)
) ENGINE = InnoDB
  AUTO_INCREMENT = 0
  DEFAULT CHARSET = utf8mb4;

CREATE TABLE `Episodes`
(
    `Id`            int(11)      NOT NULL AUTO_INCREMENT,
    `TvmShowId`     int(11)      NOT NULL,
    `TvmEpisodeId`  int(11)      NOT NULL,
    `TvmUrl`        varchar(255) NOT NULL DEFAULT ' ',
    `SeasonEpisode` varchar(10)  NOT NULL,
    `Season`        int(11)      NOT NULL,
    `Episode`       int(11)      NOT NULL,
    `BroadcastDate` date                  DEFAULT NULL,
    `PlexStatus`    varchar(10)  NOT NULL DEFAULT ' ',
    `PlexDate`      date                  DEFAULT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `Episodes_UN` (`TvmEpisodeId`),
    KEY `Episodes_FK` (`TvmShowId`),
    KEY `Episodes_FK_1` (`PlexStatus`),
    KEY `Episodes_TvmShowId_IDX` (`TvmShowId`) USING BTREE,
    CONSTRAINT `Episodes_FK` FOREIGN KEY (`TvmShowId`) REFERENCES `Shows` (`TvmShowId`) on DELETE cascade,
    CONSTRAINT `Episodes_FK_1` FOREIGN KEY (`PlexStatus`) REFERENCES `PlexStatuses` (`PlexStatus`)
) ENGINE = InnoDB
  AUTO_INCREMENT = 0
  DEFAULT CHARSET = utf8mb4;


CREATE OR REPLACE ALGORITHM = UNDEFINED VIEW `tvmazenewdb`.`episodestoacquire` AS
select `e`.`TvmShowId`       AS `TvmShowId`,
       `s`.`ShowName`        AS `ShowName`,
       `s`.`CleanedShowName` AS `CleanedShowName`,
       `s`.`AltShowname`     AS `AltShowName`,
       `e`.`TvmEpisodeId`    AS `TvmEpisodeId`,
       `e`.`TvmUrl`          AS `TvmUrl`,
       `e`.`SeasonEpisode`   AS `SeasonEpisode`,
       `e`.`Season`          AS `Season`,
       `e`.`Episode`         AS `Episode`,
       `e`.`BroadcastDate`   AS `BroadcastDate`,
       `e`.`PlexStatus`      AS `PlexStatus`,
       `e`.`PlexDate`        AS `PlexDate`,
       `s`.`Finder`          AS `Finder`
from (`tvmazenewdb`.`episodes` `e`
         join `tvmazenewdb`.`shows` `s` on
    (`e`.`TvmShowId` = `s`.`TvmShowId`))
where `e`.`BroadcastDate` <= curdate()
  and `e`.`PlexStatus` = ' '
order by `e`.`TvmShowId`,
         `e`.`SeasonEpisode`;

