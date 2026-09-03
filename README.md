[![Review Assignment Due Date](https://classroom.github.com/assets/deadline-readme-button-22041afd0340ce965d47ae6ef1cefeee28c7c493a6346c4f15d667ab976d596c.svg)](https://classroom.github.com/a/mr-hqvA6)

Overview about RaceDay System
The RaceDay System is an event management system designed for running, walking and cycling events in South Africa. The system allows Event Organisers to create and manage events, create event categories, manage participant enrolments and capture race results. Participants can register for events, select categories, view their enrolments and track their results.

The RaceDay database supports:

• Organiser management
• Participant management
• Event creation and management
• Event category management
• Participant enrolments
• Race results and performance tracking
• Role-based access through Organiser and Participant roles
Technology Stack
Component

Technology

Database

Microsoft SQL Server

Management Tool

SQL Server Management Studio (SSMS)

Language

T-SQL

Version Control

GitHub

CI/CD

GitHub Actions

Documentation

Microsoft Word / Markdown

ERD Tool

Mermaid Live Editor

Database Schema
Core Entities (6 Tables)
#

Entity

Description

1

ORGANISER

Stores event organiser information

2

PARTICIPANT

Stores participant information

3

EVENT

Stores running, walking and cycling events

4

CATEGORY

Stores categories available for each event

5

ENROLMENT

Stores participant enrolments into event categories

6

RESULT

Stores race results for participant enrolments

Entity Relationship Diagram (ERD)
The RaceDay ERD represents the relationships between the six database entities. The ERD image is included in the GitHub repository under the docs folder as RaceDay_ERD.png.

Main Relationships
• One Organiser can organise many Events.
• One Event can have many Categories.
• One Participant can have many Enrolments.
• One Category can have many Enrolments.
• One Enrolment can have zero or one Result.
Installation Guide
Important: Follow These Instructions in Order
The RaceDay SQL script creates the database and all required tables before inserting the sample data.

1. Open SQL Server Management Studio (SSMS).
2. Connect to a SQL Server instance.
3. Open RaceDay_Database.sql.
4. Run the complete SQL script.
5. The script creates the RaceDayDB database.
6. The script creates all six tables in the correct dependency order.
7. Primary keys, foreign keys and other constraints are created.
8. Sample data is inserted into the database.
9. Verification queries can then be used to check the database.
The database script is designed to run on a clean SQL Server instance.

Important: The script drops an existing RaceDayDB database before recreating it. Do not run it against a database containing important data.

Recommended Table Creation Order
To avoid foreign key and dependency errors, the tables are created in the following order:

10. Organiser
11. Participant
12. Event
13. Category
14. Enrolment
15. Result
Database Constraints
The database uses several constraints to maintain data integrity.

Primary Keys
• OrganiserID
• ParticipantID
• EventID
• CategoryID
• EnrolmentID
• ResultID
Foreign Keys
• Event.OrganiserID → Organiser.OrganiserID
• Category.EventID → Event.EventID
• Enrolment.ParticipantID → Participant.ParticipantID
• Enrolment.CategoryID → Category.CategoryID
• Result.EnrolmentID → Enrolment.EnrolmentID
Other Constraints
• NOT NULL
• UNIQUE
• DEFAULT
• CHECK
Seed Data
The SQL script includes realistic sample data for testing.

• 2 Organisers
• 2 Participants
• 3 Events
• 5 Event Categories
• 4 Sample Enrolments
• 2 Sample Results
The events include running, cycling and walking activities.

Verification Queries
After running the SQL script in SSMS, the following queries can be used to verify that the database is working correctly.

View All Organisers
SELECT
   OrganiserID,
   FullName,
   Email,
   PhoneNumber,
   CreatedAt
FROM Organiser;
GO

View All Participants
SELECT
   ParticipantID,
   FullName,
   Email,
   PhoneNumber,
   DateOfBirth,
   CreatedAt
FROM Participant;
GO

View All Events with Organisers
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

View Events and Their Categories
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

View Participant Enrolments
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

View Race Results
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

How to Reset the Database
WARNING: This deletes the RaceDay database and all of its data.

USE master;
GO

IF DB_ID('RaceDayDB') IS NOT NULL
BEGIN
   ALTER DATABASE RaceDayDB
   SET SINGLE_USER
   WITH ROLLBACK IMMEDIATE;

   DROP DATABASE RaceDayDB;
END
GO

The complete database can then be recreated by running docs/RaceDay_Database.sql.

API Endpoint Plan
The RaceDay API Endpoint Plan describes the REST API that will be used by the system. The complete API Endpoint Plan is available in docs/RaceDay_API_Endpoint_Plan.docx.

Authentication
• Organiser registration
• Participant registration
• Organiser login
• Participant login
Organisers
• View organiser profile
• Update organiser profile
• Create events
• Update events
• Delete events
Events and Categories
• View all events
• View a specific event
• Create event categories
• View event categories
• Update categories
• Delete categories
Participants and Enrolments
• View participant profile
• Update participant profile
• Create enrolments
• View participant enrolments
• Cancel enrolments
Results
• Capture results
• View event results
• View participant results
User Roles
Organiser
The Organiser is responsible for managing events.

• Create events
• Edit events
• Delete events
• Create and manage categories
• View event enrolments
• Capture participant results
Participant
The Participant uses the system to participate in events.

• Create an account
• Browse upcoming events
• View event categories
• Enrol in events
• View their enrolments
• View their results
GitHub Actions / CI
GitHub Actions is used to validate the RaceDay Part 1 repository structure. The workflow checks that the docs folder exists and that the ERD image, API Endpoint Plan and SQL database script are present.

Workflow location: .github/workflows/validate.yml

Successful CI Build
The successful GitHub Actions build screenshot is included in the repository as docs/CI-Green-Build.png.

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
16. Create the RaceDayDB database using the SQL script.
17. Check that all six tables are created.
18. Check the primary and foreign key relationships.
19. Check that the sample data has been inserted.
20. Run the verification queries.
21. Check that the GitHub Actions workflow completes successfully.
Academic Integrity
I confirm that all design decisions reflect my understanding of the RaceDay system. I have reviewed and validated the database schema, ERD, API Endpoint Plan and SQL script. The final submission represents my own work and analysis.

Tools Used
• SQL Server Management Studio (SSMS)
• Microsoft SQL Server
• GitHub
• GitHub Actions
• Mermaid Live Editor
• Microsoft Word
• Text Editor
References
• Microsoft SQL Server Documentation
• SQL Server Management Studio (SSMS) Documentation
• Microsoft SQL Server Foreign Key Constraints Documentation
