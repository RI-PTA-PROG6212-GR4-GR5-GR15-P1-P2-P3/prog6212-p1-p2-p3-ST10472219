IF DB_ID('RaceDayDB') IS NOT NULL
BEGIN
    ALTER DATABASE RaceDayDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE RaceDayDB;
END
GO

CREATE DATABASE RaceDayDB;
GO

USE RaceDayDB;
GO

/* =========================================================
   TABLE: Organiser
   ========================================================= */
CREATE TABLE Organiser (
    OrganiserID     INT IDENTITY(1,1) PRIMARY KEY,
    FullName        NVARCHAR(100)   NOT NULL,
    Email           NVARCHAR(150)   NOT NULL UNIQUE,
    PasswordHash    NVARCHAR(256)   NOT NULL,
    PhoneNumber     NVARCHAR(20)    NULL,
    CreatedAt       DATETIME2       NOT NULL DEFAULT GETDATE()
);
GO

/* =========================================================
   TABLE: Participant
   ========================================================= */
CREATE TABLE Participant (
    ParticipantID   INT IDENTITY(1,1) PRIMARY KEY,
    FullName        NVARCHAR(100)   NOT NULL,
    Email           NVARCHAR(150)   NOT NULL UNIQUE,
    PasswordHash    NVARCHAR(256)   NOT NULL,
    PhoneNumber     NVARCHAR(20)    NULL,
    DateOfBirth     DATE            NULL,
    CreatedAt       DATETIME2       NOT NULL DEFAULT GETDATE()
);
GO

/* =========================================================
   TABLE: Event
   ========================================================= */
CREATE TABLE Event (
    EventID         INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserID     INT             NOT NULL,
    EventName       NVARCHAR(150)   NOT NULL,
    EventDate       DATETIME2       NOT NULL,
    Location        NVARCHAR(200)   NOT NULL,
    Description     NVARCHAR(1000)  NULL,
    EventType       NVARCHAR(20)    NOT NULL DEFAULT 'Running',
    Status          NVARCHAR(20)    NOT NULL DEFAULT 'Scheduled',
    CreatedAt       DATETIME2       NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Event_Organiser FOREIGN KEY (OrganiserID)
        REFERENCES Organiser(OrganiserID),
    CONSTRAINT CK_Event_Type CHECK (EventType IN ('Running','Cycling','Walking')),
    CONSTRAINT CK_Event_Status CHECK (Status IN ('Scheduled','Cancelled','Completed'))
);
GO

/* =========================================================
   TABLE: Category
   ========================================================= */
CREATE TABLE Category (
    CategoryID      INT IDENTITY(1,1) PRIMARY KEY,
    EventID         INT             NOT NULL,
    CategoryName    NVARCHAR(100)   NOT NULL,
    DistanceKm      DECIMAL(6,2)    NOT NULL,
    EntryFee        DECIMAL(8,2)    NOT NULL DEFAULT 0,
    MaxParticipants INT             NOT NULL DEFAULT 100,
    CONSTRAINT FK_Category_Event FOREIGN KEY (EventID)
        REFERENCES Event(EventID) ON DELETE CASCADE,
    CONSTRAINT CK_Category_Distance CHECK (DistanceKm > 0),
    CONSTRAINT CK_Category_MaxParticipants CHECK (MaxParticipants > 0)
);
GO

/* =========================================================
   TABLE: Enrolment
   ========================================================= */
CREATE TABLE Enrolment (
    EnrolmentID     INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantID   INT             NOT NULL,
    CategoryID      INT             NOT NULL,
    EnrolmentDate   DATETIME2       NOT NULL DEFAULT GETDATE(),
    BibNumber       NVARCHAR(10)    NOT NULL UNIQUE,
    Status          NVARCHAR(20)    NOT NULL DEFAULT 'Confirmed',
    CONSTRAINT FK_Enrolment_Participant FOREIGN KEY (ParticipantID)
        REFERENCES Participant(ParticipantID),
    CONSTRAINT FK_Enrolment_Category FOREIGN KEY (CategoryID)
        REFERENCES Category(CategoryID),
    CONSTRAINT CK_Enrolment_Status CHECK (Status IN ('Pending','Confirmed','Cancelled')),
    CONSTRAINT UQ_Enrolment_Participant_Category UNIQUE (ParticipantID, CategoryID)
);
GO

/* =========================================================
   TABLE: Result
   ========================================================= */
CREATE TABLE Result (
    ResultID        INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentID     INT             NOT NULL UNIQUE,
    FinishTime      TIME            NULL,
    Position        INT             NULL,
    Status          NVARCHAR(20)    NOT NULL DEFAULT 'Finished',
    CapturedAt      DATETIME2       NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Result_Enrolment FOREIGN KEY (EnrolmentID)
        REFERENCES Enrolment(EnrolmentID) ON DELETE CASCADE,
    CONSTRAINT CK_Result_Status CHECK (Status IN ('Finished','DNF','DSQ'))
);
GO

/* =========================================================
   SEED DATA
   ========================================================= */

-- Organisers (2)
INSERT INTO Organiser (FullName, Email, PasswordHash, PhoneNumber) VALUES
('Thabo Mokoena', 'thabo.mokoena@raceday.co.za', 'HASHED_PASSWORD_1', '0821234567'),
('Lindiwe Nkosi', 'lindiwe.nkosi@raceday.co.za', 'HASHED_PASSWORD_2', '0839876543');
GO

-- Participants (2)
INSERT INTO Participant (FullName, Email, PasswordHash, PhoneNumber, DateOfBirth) VALUES
('Sipho Dlamini', 'sipho.dlamini@gmail.com', 'HASHED_PASSWORD_3', '0715551234', '1995-03-14'),
('Anelisa Botha', 'anelisa.botha@gmail.com', 'HASHED_PASSWORD_4', '0725559876', '1998-11-02');
GO

-- Events (3)
INSERT INTO Event (OrganiserID, EventName, EventDate, Location, Description, EventType, Status) VALUES
(1, 'Pretoria Spring Fun Run', '2026-09-20 07:00:00', 'Union Buildings, Pretoria', 'A community fun run through the Pretoria CBD.', 'Running', 'Scheduled'),
(1, 'Tshwane Cycle Challenge', '2026-10-11 06:30:00', 'Loftus Versfeld, Pretoria', 'Road cycling event for all skill levels.', 'Cycling', 'Scheduled'),
(2, 'Joburg Heritage Walk', '2026-09-24 08:00:00', 'Constitution Hill, Johannesburg', 'A heritage-themed community walk.', 'Walking', 'Scheduled');
GO

-- Categories (at least one per event)
INSERT INTO Category (EventID, CategoryName, DistanceKm, EntryFee, MaxParticipants) VALUES
(1, '5km Fun Run', 5.00, 100.00, 500),
(1, '10km Race', 10.00, 150.00, 300),
(2, '40km Road Ride', 40.00, 250.00, 200),
(2, '80km Road Ride', 80.00, 350.00, 150),
(3, '5km Heritage Walk', 5.00, 50.00, 400);
GO

-- Sample Enrolments
INSERT INTO Enrolment (ParticipantID, CategoryID, BibNumber, Status) VALUES
(1, 1, 'BIB0001', 'Confirmed'),
(1, 3, 'BIB0002', 'Confirmed'),
(2, 2, 'BIB0003', 'Confirmed'),
(2, 5, 'BIB0004', 'Confirmed');
GO

-- Sample Results (for completed enrolments)
INSERT INTO Result (EnrolmentID, FinishTime, Position, Status) VALUES
(1, '00:28:45', 12, 'Finished'),
(3, '00:52:10', 5, 'Finished');
GO