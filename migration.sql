CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;
ALTER DATABASE CHARACTER SET utf8mb4;

CREATE TABLE `game_tags` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `Name` longtext CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_game_tags` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `games` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `Description` longtext CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_games` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `users` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `Username` longtext CHARACTER SET utf8mb4 NOT NULL,
    `PasswordHashed` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Nickname` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Signature` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Gender` int NOT NULL,
    `Status` int NOT NULL,
    CONSTRAINT `PK_users` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `recruitments` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `GameId` bigint NOT NULL,
    `Title` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Description` longtext CHARACTER SET utf8mb4 NOT NULL,
    `MaxParticipants` int NOT NULL,
    `CurrParticipants` int NOT NULL,
    `Status` int NOT NULL,
    `UpdatedAt` datetime(6) NOT NULL,
    `ExpiresAt` datetime(6) NOT NULL,
    `UserId` bigint NULL,
    CONSTRAINT `PK_recruitments` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_recruitments_games_GameId` FOREIGN KEY (`GameId`) REFERENCES `games` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_recruitments_users_UserId` FOREIGN KEY (`UserId`) REFERENCES `users` (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `user_tags` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `Name` longtext CHARACTER SET utf8mb4 NOT NULL,
    `UserId` bigint NULL,
    CONSTRAINT `PK_user_tags` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_user_tags_users_UserId` FOREIGN KEY (`UserId`) REFERENCES `users` (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `chats` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `RecruitmentId` bigint NOT NULL,
    `RecruiterId` bigint NOT NULL,
    `ResponserId` bigint NOT NULL,
    `NewMsgsCntForRecruiter` int NOT NULL,
    `NewMsgsCntForResponser` int NOT NULL,
    `Status` int NOT NULL,
    `UserId` bigint NULL,
    CONSTRAINT `PK_chats` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_chats_recruitments_RecruitmentId` FOREIGN KEY (`RecruitmentId`) REFERENCES `recruitments` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_chats_users_RecruiterId` FOREIGN KEY (`RecruiterId`) REFERENCES `users` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_chats_users_ResponserId` FOREIGN KEY (`ResponserId`) REFERENCES `users` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_chats_users_UserId` FOREIGN KEY (`UserId`) REFERENCES `users` (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `responses` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `RecruitmentId` bigint NOT NULL,
    `ResponderId` bigint NOT NULL,
    `RecruiterId` bigint NOT NULL,
    `GreetingMessage` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Status` int NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_responses` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_responses_recruitments_RecruitmentId` FOREIGN KEY (`RecruitmentId`) REFERENCES `recruitments` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_responses_users_RecruiterId` FOREIGN KEY (`RecruiterId`) REFERENCES `users` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_responses_users_ResponderId` FOREIGN KEY (`ResponderId`) REFERENCES `users` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `messages` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `SenderId` bigint NOT NULL,
    `Content` longtext CHARACTER SET utf8mb4 NOT NULL,
    `SentAt` datetime(6) NOT NULL,
    `ChatId` bigint NULL,
    CONSTRAINT `PK_messages` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_messages_chats_ChatId` FOREIGN KEY (`ChatId`) REFERENCES `chats` (`Id`),
    CONSTRAINT `FK_messages_users_SenderId` FOREIGN KEY (`SenderId`) REFERENCES `users` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_chats_RecruiterId` ON `chats` (`RecruiterId`);

CREATE INDEX `IX_chats_RecruitmentId` ON `chats` (`RecruitmentId`);

CREATE INDEX `IX_chats_ResponserId` ON `chats` (`ResponserId`);

CREATE INDEX `IX_chats_UserId` ON `chats` (`UserId`);

CREATE INDEX `IX_messages_ChatId` ON `messages` (`ChatId`);

CREATE INDEX `IX_messages_SenderId` ON `messages` (`SenderId`);

CREATE INDEX `IX_recruitments_GameId` ON `recruitments` (`GameId`);

CREATE INDEX `IX_recruitments_UserId` ON `recruitments` (`UserId`);

CREATE INDEX `IX_responses_RecruiterId` ON `responses` (`RecruiterId`);

CREATE INDEX `IX_responses_RecruitmentId` ON `responses` (`RecruitmentId`);

CREATE INDEX `IX_responses_ResponderId` ON `responses` (`ResponderId`);

CREATE INDEX `IX_user_tags_UserId` ON `user_tags` (`UserId`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260721181321_InitResponse', '9.0.0');

COMMIT;

