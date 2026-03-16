-- Wonderland Private Server - Database Initialization
-- Database: wlo

CREATE TABLE IF NOT EXISTS `user` (
    `userID`          INT UNSIGNED    NOT NULL AUTO_INCREMENT,
    `username`        VARCHAR(50)     NOT NULL,
    `password`        VARCHAR(255)    NOT NULL,
    `character1ID`    INT UNSIGNED    DEFAULT 0,
    `character2ID`    INT UNSIGNED    DEFAULT 0,
    `IM`              INT             DEFAULT 0,
    `char_delete_code` VARCHAR(50)    DEFAULT NULL,
    `members_pass_salt` VARCHAR(255)  DEFAULT NULL,
    PRIMARY KEY (`userID`),
    UNIQUE KEY `idx_username` (`username`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `characters` (
    `charID`          INT UNSIGNED    NOT NULL,
    `slot`            TINYINT UNSIGNED NOT NULL DEFAULT 1,
    `name`            VARCHAR(50)     NOT NULL,
    `name_clean`      VARCHAR(50)     NOT NULL,
    `nickname`        VARCHAR(50)     DEFAULT '',
    `head`            TINYINT UNSIGNED NOT NULL DEFAULT 0,
    `body`            TINYINT UNSIGNED NOT NULL DEFAULT 1,
    `location_map`    SMALLINT UNSIGNED NOT NULL DEFAULT 60000,
    `location_x`      SMALLINT UNSIGNED NOT NULL DEFAULT 602,
    `location_y`      SMALLINT UNSIGNED NOT NULL DEFAULT 455,
    `haircolor`       SMALLINT UNSIGNED NOT NULL DEFAULT 0,
    `skincolor`       SMALLINT UNSIGNED NOT NULL DEFAULT 0,
    `clothingcolor`   SMALLINT UNSIGNED NOT NULL DEFAULT 0,
    `eyecolor`        SMALLINT UNSIGNED NOT NULL DEFAULT 0,
    `gold`            INT             NOT NULL DEFAULT 0,
    `element`         TINYINT UNSIGNED NOT NULL DEFAULT 0,
    `rebirth`         TINYINT UNSIGNED NOT NULL DEFAULT 0,
    `job`             TINYINT UNSIGNED NOT NULL DEFAULT 0,
    `online`          TINYINT UNSIGNED NOT NULL DEFAULT 0,
    PRIMARY KEY (`charID`),
    UNIQUE KEY `idx_name_clean` (`name_clean`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `charactersExtData` (
    `charID`          INT UNSIGNED    NOT NULL,
    `Settings`        TEXT            DEFAULT NULL,
    `Friends`         TEXT            DEFAULT NULL,
    `Guild`           VARCHAR(50)     DEFAULT '0',
    `Mail`            TEXT            DEFAULT NULL,
    PRIMARY KEY (`charID`),
    FOREIGN KEY (`charID`) REFERENCES `characters`(`charID`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `stats` (
    `statIdx`         INT UNSIGNED    NOT NULL AUTO_INCREMENT,
    `charID`          INT UNSIGNED    NOT NULL,
    `statID`          INT             NOT NULL,
    `StatusUp`        BIGINT          NOT NULL DEFAULT 0,
    `potential`       INT             NOT NULL DEFAULT 0,
    PRIMARY KEY (`statIdx`),
    KEY `idx_char_stat` (`charID`, `statID`),
    FOREIGN KEY (`charID`) REFERENCES `characters`(`charID`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `inventory` (
    `invIdx`          INT UNSIGNED    NOT NULL,
    `charID`          INT UNSIGNED    NOT NULL,
    `storID`          INT UNSIGNED    NOT NULL DEFAULT 0 COMMENT '0=inventory, 1=equipment',
    `itemID`          SMALLINT UNSIGNED NOT NULL DEFAULT 0,
    `dmg`             TINYINT UNSIGNED NOT NULL DEFAULT 0,
    `qty`             TINYINT UNSIGNED NOT NULL DEFAULT 0,
    `pos`             TINYINT UNSIGNED NOT NULL DEFAULT 0,
    `socketID`        INT             NOT NULL DEFAULT 0,
    `bombID`          INT             NOT NULL DEFAULT 0,
    `sewID`           INT             NOT NULL DEFAULT 0,
    `forge`           INT             NOT NULL DEFAULT 0,
    PRIMARY KEY (`invIdx`, `charID`, `storID`),
    KEY `idx_char_stor` (`charID`, `storID`),
    FOREIGN KEY (`charID`) REFERENCES `characters`(`charID`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Insert a test admin account (password: admin)
INSERT INTO `user` (`username`, `password`, `IM`) VALUES ('admin', 'admin', 0);
