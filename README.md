[![Review Assignment Due Date](https://classroom.github.com/assets/deadline-readme-button-22041afd0340ce965d47ae6ef1cefeee28c7c493a6346c4f15d667ab976d596c.svg)](https://classroom.github.com/a/mr-hqvA6)

RaceDay System - Complete Database Schema
Part 1 – Database, ERD, API Planning and CI/CD Documentation
Overview
The RaceDay System is an event management system designed for running, walking and cycling events in South Africa. The database supports event organisers, participants, event categories, enrolments and race results.
•	Organiser management
•	Participant management
•	Event creation and management
•	Event category management
•	Participant enrolments
•	Race results and performance tracking
•	Role-based access through Organiser and Participant roles
Technology Stack
Component	Technology
Database	Microsoft SQL Server
Management Tool	SQL Server Management Studio (SSMS)
Language	T-SQL
Version Control	GitHub
CI/CD	GitHub Actions
ERD Tool	Mermaid Live Editor
Documentation	Microsoft Word / Markdown
Database Schema
Core Entities (6 Tables)
#	Entity	Description
1	ORGANISER	Stores event organiser information
2	PARTICIPANT	Stores participant information
3	EVENT	Stores running, walking and cycling events
4	CATEGORY	Stores categories available for each event
5	ENROLMENT	Stores participant enrolments in event categories
6	RESULT	Stores race results for participant enrolments
Entity Relationship Diagram (ERD)
The ERD shows the relationships between the six entities used by the RaceDay system.
 
Figure 1: RaceDay Entity Relationship Diagram
Relationship	Cardinality	Description
Organiser → Event	1 : Many	One Organiser can organise many Events.
Event → Category	1 : Many	One Event can have many Categories.
Participant → Enrolment	1 : Many	One Participant can have many Enrolments.
Category → Enrolment	1 : Many	One Category can have many Enrolments.
Enrolment → Result	1 : 0..1	An Enrolment can have zero or one Result.
Installation Guide
IMPORTANT: Follow These Instructions in Order
1.	Open SQL Server Management Studio (SSMS).
2.	Connect to a SQL Server instance.
3.	Open RaceDay_Database.sql.
4.	Run the complete SQL script.
5.	The script creates the RaceDayDB database.
6.	The script creates all six tables in dependency order.
7.	Primary keys, foreign keys and other constraints are created.
8.	Sample data is inserted into the database.
9.	Use the verification queries to confirm the database is working.
Important: The script drops an existing RaceDayDB database before recreating it. Do not run it against a database containing important data.
Recommended Table Creation Order
Order	Table	Dependency
1	Organiser	Independent parent table
2	Participant	Independent parent table
3	Event	References Organiser
4	Category	References Event
5	Enrolment	References Participant and Category
6	Result	References Enrolment
Database Creation and Table Schema
Main database schema
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

CREATE TABLE Organiser (
    OrganiserID INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(150) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(256) NOT NULL,
    PhoneNumber NVARCHAR(20) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
);
GO

CREATE TABLE Participant (
    ParticipantID INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(150) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(256) NOT NULL,
    PhoneNumber NVARCHAR(20) NULL,
    DateOfBirth DATE NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
);
GO

CREATE TABLE Event (
    EventID INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserID INT NOT NULL,
    EventName NVARCHAR(150) NOT NULL,
    EventDate DATETIME2 NOT NULL,
    Location NVARCHAR(200) NOT NULL,
    Description NVARCHAR(1000) NULL,
    EventType NVARCHAR(20) NOT NULL DEFAULT 'Running',
    Status NVARCHAR(20) NOT NULL DEFAULT 'Scheduled',
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Event_Organiser FOREIGN KEY (OrganiserID)
        REFERENCES Organiser(OrganiserID),
    CONSTRAINT CK_Event_Type CHECK
        (EventType IN ('Running','Cycling','Walking')),
    CONSTRAINT CK_Event_Status CHECK
        (Status IN ('Scheduled','Cancelled','Completed'))
);
GO

CREATE TABLE Category (
    CategoryID INT IDENTITY(1,1) PRIMARY KEY,
    EventID INT NOT NULL,
    CategoryName NVARCHAR(100) NOT NULL,
    DistanceKm DECIMAL(6,2) NOT NULL,
    EntryFee DECIMAL(8,2) NOT NULL DEFAULT 0,
    MaxParticipants INT NOT NULL DEFAULT 100,
    CONSTRAINT FK_Category_Event FOREIGN KEY (EventID)
        REFERENCES Event(EventID) ON DELETE CASCADE,
    CONSTRAINT CK_Category_Distance CHECK (DistanceKm > 0),
    CONSTRAINT CK_Category_MaxParticipants CHECK (MaxParticipants > 0)
);
GO

CREATE TABLE Enrolment (
    EnrolmentID INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantID INT NOT NULL,
    CategoryID INT NOT NULL,
    EnrolmentDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    BibNumber NVARCHAR(10) NOT NULL UNIQUE,
    Status NVARCHAR(20) NOT NULL DEFAULT 'Confirmed',
    CONSTRAINT FK_Enrolment_Participant FOREIGN KEY (ParticipantID)
        REFERENCES Participant(ParticipantID),
    CONSTRAINT FK_Enrolment_Category FOREIGN KEY (CategoryID)
        REFERENCES Category(CategoryID),
    CONSTRAINT CK_Enrolment_Status CHECK
        (Status IN ('Pending','Confirmed','Cancelled')),
    CONSTRAINT UQ_Enrolment_Participant_Category
        UNIQUE (ParticipantID, CategoryID)
);
GO

CREATE TABLE Result (
    ResultID INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentID INT NOT NULL UNIQUE,
    FinishTime TIME NULL,
    Position INT NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'Finished',
    CapturedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Result_Enrolment FOREIGN KEY (EnrolmentID)
        REFERENCES Enrolment(EnrolmentID) ON DELETE CASCADE,
    CONSTRAINT CK_Result_Status CHECK
        (Status IN ('Finished','DNF','DSQ'))
);
GO
Seed Data
Data Type	Quantity
Organisers	2
Participants	2
Events	3
Event Categories	5
Sample Enrolments	4
Sample Results	2
Sample data
-- Organisers
INSERT INTO Organiser (FullName, Email, PasswordHash, PhoneNumber) VALUES
('Thabo Mokoena', 'thabo.mokoena@raceday.co.za',
 'HASHED_PASSWORD_1', '0821234567'),
('Lindiwe Nkosi', 'lindiwe.nkosi@raceday.co.za',
 'HASHED_PASSWORD_2', '0839876543');
GO

-- Participants
INSERT INTO Participant
(FullName, Email, PasswordHash, PhoneNumber, DateOfBirth) VALUES
('Sipho Dlamini', 'sipho.dlamini@gmail.com',
 'HASHED_PASSWORD_3', '0715551234', '1995-03-14'),
('Anelisa Botha', 'anelisa.botha@gmail.com',
 'HASHED_PASSWORD_4', '0725559876', '1998-11-02');
GO

-- Events
INSERT INTO Event
(OrganiserID, EventName, EventDate, Location, Description, EventType, Status) VALUES
(1, 'Pretoria Spring Fun Run', '2026-09-20 07:00:00',
 'Union Buildings, Pretoria',
 'A community fun run through the Pretoria CBD.',
 'Running', 'Scheduled'),
(1, 'Tshwane Cycle Challenge', '2026-10-11 06:30:00',
 'Loftus Versfeld, Pretoria',
 'Road cycling event for all skill levels.',
 'Cycling', 'Scheduled'),
(2, 'Joburg Heritage Walk', '2026-09-24 08:00:00',
 'Constitution Hill, Johannesburg',
 'A heritage-themed community walk.',
 'Walking', 'Scheduled');
GO

-- Categories
INSERT INTO Category
(EventID, CategoryName, DistanceKm, EntryFee, MaxParticipants) VALUES
(1, '5km Fun Run', 5.00, 100.00, 500),
(1, '10km Race', 10.00, 150.00, 300),
(2, '40km Road Ride', 40.00, 250.00, 200),
(2, '80km Road Ride', 80.00, 350.00, 150),
(3, '5km Heritage Walk', 5.00, 50.00, 400);
GO

-- Sample Enrolments
INSERT INTO Enrolment
(ParticipantID, CategoryID, BibNumber, Status) VALUES
(1, 1, 'BIB0001', 'Confirmed'),
(1, 3, 'BIB0002', 'Confirmed'),
(2, 2, 'BIB0003', 'Confirmed'),
(2, 5, 'BIB0004', 'Confirmed');
GO

-- Sample Results
INSERT INTO Result
(EnrolmentID, FinishTime, Position, Status) VALUES
(1, '00:28:45', 12, 'Finished'),
(3, '00:52:10', 5, 'Finished');
GO
Verification Queries
Run these queries in SSMS after the database has been created.
1. View All Organisers
SELECT
    OrganiserID,
    FullName,
    Email,
    PhoneNumber,
    CreatedAt
FROM Organiser;
GO
2. View All Participants
SELECT
    ParticipantID,
    FullName,
    Email,
    PhoneNumber,
    DateOfBirth,
    CreatedAt
FROM Participant;
GO
3. View All Events with Organisers
SELECT
    e.EventID,
    e.EventName,
    e.EventDate,
    e.Location,
    e.EventType,
    e.Status,
    o.FullName AS Organiser
FROM Event e
INNER JOIN Organiser o
    ON e.OrganiserID = o.OrganiserID;
GO
4. View Events and Categories
SELECT
    e.EventName,
    c.CategoryName,
    c.DistanceKm,
    c.EntryFee,
    c.MaxParticipants
FROM Event e
INNER JOIN Category c
    ON e.EventID = c.EventID
ORDER BY e.EventID;
GO
5. View Participant Enrolments
SELECT
    p.FullName AS Participant,
    e.EventName,
    c.CategoryName,
    en.BibNumber,
    en.EnrolmentDate,
    en.Status
FROM Enrolment en
INNER JOIN Participant p
    ON en.ParticipantID = p.ParticipantID
INNER JOIN Category c
    ON en.CategoryID = c.CategoryID
INNER JOIN Event e
    ON c.EventID = e.EventID;
GO
6. View Race Results
SELECT
    p.FullName AS Participant,
    e.EventName,
    c.CategoryName,
    r.FinishTime,
    r.Position,
    r.Status
FROM Result r
INNER JOIN Enrolment en
    ON r.EnrolmentID = en.EnrolmentID
INNER JOIN Participant p
    ON en.ParticipantID = p.ParticipantID
INNER JOIN Category c
    ON en.CategoryID = c.CategoryID
INNER JOIN Event e
    ON c.EventID = e.EventID;
GO
How to Drop / Reset the Database
WARNING: This deletes the RaceDayDB database and all of its data.
Reset database
USE master;
GO

IF DB_ID('RaceDayDB') IS NOT NULL
BEGIN
    ALTER DATABASE RaceDayDB
    SET SINGLE_USER WITH ROLLBACK IMMEDIATE;

    DROP DATABASE RaceDayDB;
END
GO
API Endpoint Plan
The API Endpoint Plan describes the REST API required for the RaceDay system. The complete endpoint plan is stored as RaceDay_API_Endpoint_Plan.docx in the docs folder.
Area	Main Endpoints / Functions
Authentication	Organiser registration; Participant registration; Organiser login; Participant login
User Profile	View and update the logged-in user's profile
Events	List events; view event details; create, update and delete events
Categories	View categories; create, update and delete categories
Enrolments	Enrol in a category; view own enrolments; cancel enrolments; organiser view of event enrolments
Results	Capture results; update results; view personal results; view event results
User Roles
Role	Main Responsibilities
Organiser	Create, edit and delete events; manage categories; view event enrolments; capture and update participant results.
Participant	Create an account; browse events; view categories; enrol in events; view enrolments and personal results.
GitHub Actions / CI
GitHub Actions validates the required Part 1 repository structure. The workflow checks that the docs folder exists and that the ERD, API Endpoint Plan and SQL database script are present.
Workflow: .github/workflows/validate.yml
name: Validate RaceDay Part 1

on:
  push:
    branches:
      - main
  pull_request:
    branches:
      - main

jobs:
  validate:
    runs-on: ubuntu-latest

    steps:
      - name: Checkout repository
        uses: actions/checkout@v4

      - name: Check required docs folder
        run: |
          test -d docs

      - name: Check required ERD
        run: |
          test -f docs/RaceDay_ERD.png

      - name: Check required API Endpoint Plan
        run: |
          test -f docs/RaceDay_API_Endpoint_Plan.docx

      - name: Check required SQL script
        run: |
          test -f docs/RaceDay_Database.sql
A screenshot of the successful green build is included in the repository as docs/CI-Green-Build.png.
Project Structure
RaceDay/
│
├── docs/
│   ├── RaceDay_ERD.png
│   ├── RaceDay_API_Endpoint_Plan.docx
│   ├── RaceDay_Database.sql
│   └── CI-Green-Build.png
│
├── .github/
│   └── workflows/
│       └── validate.yml
│
└── README.md
Testing
10.	Create RaceDayDB using the SQL script.
11.	Confirm that all six tables are created.
12.	Confirm the primary and foreign key relationships.
13.	Confirm that the required sample data has been inserted.
14.	Run the verification queries.
15.	Confirm that GitHub Actions completes successfully.
Statement of Academic Integrity
I confirm that all design decisions reflect my understanding of the RaceDay system. I have reviewed and validated the database schema, ERD, API Endpoint Plan and SQL script. The final submission represents my own work and analysis.
Tools Used
Tool	Purpose
SQL Server Management Studio (SSMS)	Database creation, SQL execution and testing
Microsoft SQL Server	Database platform
GitHub	Version control and repository hosting
GitHub Actions	Continuous integration validation
Mermaid Live Editor	ERD creation
Microsoft Word	Documentation
Text Editor	Editing project files
References
•	Microsoft SQL Server Documentation
•	SQL Server Management Studio (SSMS) Documentation
•	Microsoft SQL Server Foreign Key Constraints Documentation

