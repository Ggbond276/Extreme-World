










































-- -----------------------------------------------------------
-- Entity Designer DDL Script for MySQL Server 4.1 and higher
-- -----------------------------------------------------------
-- Date Created: 11/15/2024 19:35:34

-- Generated from EDMX file: C:\Users\op\Documents\Extreme-World\Src\Server\GameServer\GameServer\Entities.edmx
-- Target version: 3.0.0.0

-- --------------------------------------------------



-- --------------------------------------------------
-- Dropping existing FOREIGN KEY constraints
-- NOTE: if the constraint does not exist, an ignorable error will be reported.
-- --------------------------------------------------


--    ALTER TABLE `Users` DROP CONSTRAINT `FK_UserPlayer`;

--    ALTER TABLE `Characters` DROP CONSTRAINT `FK_PlayerCharacter`;


-- --------------------------------------------------
-- Dropping existing tables
-- --------------------------------------------------
SET foreign_key_checks = 0;

    DROP TABLE IF EXISTS `Users`;

    DROP TABLE IF EXISTS `Players`;

    DROP TABLE IF EXISTS `Characters`;

SET foreign_key_checks = 1;

-- --------------------------------------------------
-- Creating all tables
-- --------------------------------------------------


CREATE TABLE `Users`(
	`ID` int NOT NULL AUTO_INCREMENT UNIQUE, 
	`Username` longtext NOT NULL, 
	`Password` longtext NOT NULL, 
	`RegisterDate` datetime NOT NULL, 
	`Player_ID` int NOT NULL);

ALTER TABLE `Users` ADD PRIMARY KEY (`ID`);





CREATE TABLE `Players`(
	`ID` int NOT NULL AUTO_INCREMENT UNIQUE);

ALTER TABLE `Players` ADD PRIMARY KEY (`ID`);





CREATE TABLE `Characters`(
	`ID` int NOT NULL AUTO_INCREMENT UNIQUE, 
	`TID` int NOT NULL, 
	`Name` longtext NOT NULL, 
	`Class` int NOT NULL, 
	`MapID` int NOT NULL, 
	`MapPosX` int NOT NULL, 
	`MapPosY` int NOT NULL, 
	`MapPosZ` int NOT NULL, 
	`Player_ID` int NOT NULL);

ALTER TABLE `Characters` ADD PRIMARY KEY (`ID`);







-- --------------------------------------------------
-- Creating all FOREIGN KEY constraints
-- --------------------------------------------------


-- Creating foreign key on `Player_ID` in table 'Users'

ALTER TABLE `Users`
ADD CONSTRAINT `FK_UserPlayer`
    FOREIGN KEY (`Player_ID`)
    REFERENCES `Players`
        (`ID`)
    ON DELETE NO ACTION ON UPDATE NO ACTION;


-- Creating non-clustered index for FOREIGN KEY 'FK_UserPlayer'

CREATE INDEX `IX_FK_UserPlayer`
    ON `Users`
    (`Player_ID`);



-- Creating foreign key on `Player_ID` in table 'Characters'

ALTER TABLE `Characters`
ADD CONSTRAINT `FK_PlayerCharacter`
    FOREIGN KEY (`Player_ID`)
    REFERENCES `Players`
        (`ID`)
    ON DELETE NO ACTION ON UPDATE NO ACTION;


-- Creating non-clustered index for FOREIGN KEY 'FK_PlayerCharacter'

CREATE INDEX `IX_FK_PlayerCharacter`
    ON `Characters`
    (`Player_ID`);



-- --------------------------------------------------
-- Script has ended
-- --------------------------------------------------
