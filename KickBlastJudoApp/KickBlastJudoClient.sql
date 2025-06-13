CREATE DATABASE KickBlastJudoDB;

-- 1. Plan table
CREATE TABLE [Plan] (
    PlanId    INT IDENTITY(1,1) PRIMARY KEY,
    PlanName  VARCHAR(50) NOT NULL UNIQUE,
    WeeklyFee DECIMAL(10,2) NOT NULL CHECK (WeeklyFee >= 0)
);

-- 2. WeightCategory table
CREATE TABLE WeightCategory (
    CategoryId    INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName  VARCHAR(50) NOT NULL UNIQUE,
    UpperWeightKg DECIMAL(10,2) NULL,
);

-- 3. Athlete table
CREATE TABLE Athlete (
    AthleteId       INT IDENTITY(1,1) PRIMARY KEY,
    Name            VARCHAR(100) NOT NULL,
    PlanId          INT NOT NULL,
    CurrentWeightKg DECIMAL(10,2) NOT NULL CHECK (CurrentWeightKg >= 0),
    CategoryId      INT NOT NULL,
    DateJoined      DATE NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    Active          BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_Athlete_Plan FOREIGN KEY (PlanId)
        REFERENCES [Plan](PlanId),
    CONSTRAINT FK_Athlete_Category FOREIGN KEY (CategoryId)
        REFERENCES WeightCategory(CategoryId)
);

-- 4. CompetitionEntry table
CREATE TABLE CompetitionEntry (
    CompetitionEntryId INT IDENTITY(1,1) PRIMARY KEY,
    AthleteId          INT NOT NULL,
    CompetitionDate    DATE NOT NULL,
    Fee                DECIMAL(10,2) NOT NULL DEFAULT 220.00 CHECK (Fee >= 0),
    CONSTRAINT FK_CompetitionEntry_Athlete FOREIGN KEY (AthleteId)
        REFERENCES Athlete(AthleteId)
);


-- 5. CoachingSession table
CREATE TABLE CoachingSession (
    CoachingSessionId INT IDENTITY(1,1) PRIMARY KEY,
    AthleteId         INT NOT NULL,
    SessionDate       DATE NOT NULL,
    Hours             DECIMAL(5,2) NOT NULL CHECK (Hours > 0 AND Hours <= 5),
    Rate              DECIMAL(10,2) NOT NULL DEFAULT 90.50 CHECK (Rate >= 0),
    CONSTRAINT FK_CoachingSession_Athlete FOREIGN KEY (AthleteId)
        REFERENCES Athlete(AthleteId)
);


INSERT INTO [Plan] (PlanName, WeeklyFee) 
VALUES
   ('Beginner', 250.00),
   ('Intermediate', 300.00),
   ('Elite', 350.00);




INSERT INTO WeightCategory (CategoryName, UpperWeightKg) 
VALUES
   ('Flyweight', 66),
   ('Lightweight', 73),
   ('Light-Middleweight', 81),
   ('Middleweight', 90),
   ('Light-Heavyweight', 100),
   ('Heavyweight', NULL);


select * from [Plan];
 
select * from WeightCategory;