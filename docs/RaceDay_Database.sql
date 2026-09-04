--- ============================================
--- RaceDay Database Script
--- Part 1 - System Planning and Database
--- Run this script in SQL Server Management Studio (SSMS)
-- ============================================

CREATE DATABASE RaceDayDB;
GO

USE RaceDayDB;
GO

--- ============================================
--- TABLE: Users
-- ============================================
CREATE TABLE Users (
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Email NVARCHAR(150) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    Role NVARCHAR(20) NOT NULL DEFAULT 'Participant' CHECK (Role IN ('Organiser', 'Participant'))
);
GO

-- ============================================
-- TABLE: Events
-- ============================================
CREATE TABLE Events (
    EventID INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserID INT NOT NULL,
    Name NVARCHAR(150) NOT NULL,
    EventDate DATE NOT NULL,
    Location NVARCHAR(150) NOT NULL,
    RouteDescription NVARCHAR(500) NULL,
    CONSTRAINT FK_Events_Users FOREIGN KEY (OrganiserID) REFERENCES Users(UserID)
);
GO

-- ============================================
-- TABLE: Categories
-- ============================================
CREATE TABLE Categories (
    CategoryID INT IDENTITY(1,1) PRIMARY KEY,
    EventID INT NOT NULL,
    Name NVARCHAR(50) NOT NULL,
    DistanceKm DECIMAL(5,2) NOT NULL,
    CONSTRAINT FK_Categories_Events FOREIGN KEY (EventID) REFERENCES Events(EventID)
);
GO

-- ============================================
-- TABLE: Enrolments
-- ============================================
CREATE TABLE Enrolments (
    EnrolmentID INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantID INT NOT NULL,
    CategoryID INT NOT NULL,
    EnrolmentDate DATETIME NOT NULL DEFAULT GETDATE(),
    Status NVARCHAR(20) NOT NULL DEFAULT 'Confirmed' CHECK (Status IN ('Confirmed', 'Cancelled')),
    CONSTRAINT FK_Enrolments_Users FOREIGN KEY (ParticipantID) REFERENCES Users(UserID),
    CONSTRAINT FK_Enrolments_Categories FOREIGN KEY (CategoryID) REFERENCES Categories(CategoryID),
    CONSTRAINT UQ_Enrolment UNIQUE (ParticipantID, CategoryID)
);
GO

-- ============================================
-- TABLE: Results
-- ============================================
CREATE TABLE Results (
    ResultID INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentID INT NOT NULL UNIQUE,
    FinishTime TIME NULL,
    Position INT NULL,
    CONSTRAINT FK_Results_Enrolments FOREIGN KEY (EnrolmentID) REFERENCES Enrolments(EnrolmentID)
);
GO

-- ============================================
-- TABLE: WeatherInfo
-- ============================================
CREATE TABLE WeatherInfo (
    WeatherID INT IDENTITY(1,1) PRIMARY KEY,
    EventID INT NOT NULL,
    ForecastSummary NVARCHAR(200) NULL,
    TemperatureC DECIMAL(4,1) NULL,
    CONSTRAINT FK_WeatherInfo_Events FOREIGN KEY (EventID) REFERENCES Events(EventID)
);
GO

-- ============================================
-- SEED DATA
-- ============================================

-- Organisers
INSERT INTO Users (Name, Email, PasswordHash, Role) VALUES
('Adelia Antonio', 'adelia.organiser@raceday.com', 'HASHED_PASSWORD_1', 'Organiser'),
('Carlos Mendes', 'carlos.organiser@raceday.com', 'HASHED_PASSWORD_2', 'Organiser');

-- Participants
INSERT INTO Users (Name, Email, PasswordHash, Role) VALUES
('Neuza Antonio', 'neuza.participant@raceday.com', 'HASHED_PASSWORD_3', 'Participant'),
('Wilson Bastos', 'wilson.participant@raceday.com', 'HASHED_PASSWORD_4', 'Participant');

-- Events
INSERT INTO Events (OrganiserID, Name, EventDate, Location, RouteDescription) VALUES
(1, 'Cape Town Cycle Tour', '2026-03-08', 'Cape Town', 'Scenic coastal route around the Cape Peninsula'),
(1, 'Soweto Marathon', '2026-11-01', 'Soweto', 'Flat urban route through historic Soweto'),
(2, 'Two Oceans Half Marathon', '2026-04-04', 'Cape Town', 'Route along the Atlantic and Indian Ocean coastlines');

-- Categories
INSERT INTO Categories (EventID, Name, DistanceKm) VALUES
(1, '109km Cycle', 109.00),
(1, '35km Cycle', 35.00),
(2, '42.2km Marathon', 42.20),
(2, '10km Fun Run', 10.00),
(3, '21.1km Half Marathon', 21.10);

-- Enrolments
INSERT INTO Enrolments (ParticipantID, CategoryID, Status) VALUES
(3, 1, 'Confirmed'),
(4, 3, 'Confirmed'),
(3, 5, 'Confirmed');

-- Results
INSERT INTO Results (EnrolmentID, FinishTime, Position) VALUES
(1, '04:15:30', 120),
(2, '03:45:10', 58);

-- WeatherInfo
INSERT INTO WeatherInfo (EventID, ForecastSummary, TemperatureC) VALUES
(1, 'Sunny with light wind', 22.5),
(2, 'Partly cloudy', 18.0),
(3, 'Clear skies', 20.0);
GO
