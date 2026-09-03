[![Review Assignment Due Date](https://classroom.github.com/assets/deadline-readme-button-22041afd0340ce965d47ae6ef1cefeee28c7c493a6346c4f15d667ab976d596c.svg)](https://classroom.github.com/a/mr-hqvA6)

RACEDAY SYSTEM

Complete Database Schema & Technical Documentation

Part 1 – System Planning, Database & CI/CD

 

Document Item
Details
System
RaceDay Event Management System
Database
RaceDayDB
Database Platform
Microsoft SQL Server
Management Tool
SQL Server Management Studio (SSMS)
Language
T-SQL
Version Control
GitHub
CI/CD
GitHub Actions


1. Overview
The RaceDay System is an event management system designed for running, walking and cycling events in South Africa. The system allows Event Organisers to create and manage events, create event categories, manage participant enrolments and capture race results. Participants can register for events, select categories, view their enrolments and track their results.

The system supports:

• Organiser management
• Participant management
• Event creation and management
• Event category management
• Participant enrolments
• Race results and performance tracking
• Role-based access through Organiser and Participant roles
2. Technology Stack
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
3. Database Schema
Core Entities – 6 Tables
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
4. Entity Relationship Diagram (ERD)
The RaceDay ERD represents the relationships between the six database entities. The ERD image is stored in the repository as docs/RaceDay_ERD.png.

Relationship
Cardinality
Meaning
Organiser → Event
1 : Many
One Organiser can organise many Events.
Event → Category
1 : Many
One Event can have many Categories.
Participant → Enrolment
1 : Many
One Participant can have many Enrolments.
Category → Enrolment
1 : Many
One Category can have many Enrolments.
Enrolment → Result
1 : 0..1
An Enrolment can have zero or one Result.
5. Installation Guide
IMPORTANT: Follow these instructions in order.

1. Open SQL Server Management Studio (SSMS).
2. Connect to a SQL Server instance.
3. Open RaceDay_Database.sql.
4. Run the complete SQL script.
5. The script creates the RaceDayDB database.
6. The script creates all six tables in dependency order.
7. Primary keys, foreign keys and other constraints are created.
8. Sample data is inserted into the database.
9. Run the verification queries to confirm that the database is working.
Important: The script drops an existing RaceDayDB database before recreating it. Do not run it against a database containing important data.

6. Recommended Table Creation Order
Order
Table
Reason
1
Organiser
Independent parent table
2
Participant
Independent parent table
3
Event
References Organiser
4
Category
References Event
5
Enrolment
References Participant and Category
6
Result
References Enrolment
7. Database Constraints
The database uses constraints to maintain data integrity.

Constraint
Examples / Purpose
Primary Keys
OrganiserID, ParticipantID, EventID, CategoryID, EnrolmentID, ResultID
Foreign Keys
Connect Event, Category, Enrolment and Result to their parent records
NOT NULL
Prevents required fields from being left empty
UNIQUE
Prevents duplicate email addresses and duplicate bib numbers
DEFAULT
Provides default values such as CreatedAt and Status
CHECK
Restricts values such as EventType, Status and positive distances
8. Seed Data
Data Type
Quantity
Organisers
2
Participants
2
Events
3
Event Categories
5
Sample Enrolments
4
Sample Results
2
9. SQL Verification Queries
These queries can be run in SSMS after the database has been created.

9.1 View All Organisers
SELECT
   OrganiserID,
   FullName,
   Email,
   PhoneNumber,
   CreatedAt
FROM Organiser;
GO
9.2 View All Participants
SELECT
   ParticipantID,
   FullName,
   Email,
   PhoneNumber,
   DateOfBirth,
   CreatedAt
FROM Participant;
GO
9.3 View Events with Organisers
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
9.4 View Events and Categories
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
9.5 View Participant Enrolments
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
9.6 View Race Results
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
10. Database Reset
WARNING: This deletes the RaceDayDB database and all of its data.

Reset Database
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
11. API Endpoint Plan
The API Endpoint Plan describes the REST API required for the RaceDay system. The complete endpoint plan is stored in docs/RaceDay_API_Endpoint_Plan.docx.

Area
Endpoints / Functions
Authentication
Organiser registration, Participant registration, Organiser login, Participant login
Organisers
View profile, update profile, create events, update events, delete events
Events & Categories
View events, view event, create categories, view categories, update categories, delete categories
Participants & Enrolments
View profile, update profile, create enrolments, view enrolments, cancel enrolments
Results
Capture results, view event results, view participant results
12. User Roles
Role
Main Responsibilities
Organiser
Create, edit and delete events; manage categories; view enrolments; capture results.
Participant
Create an account; browse events; view categories; enrol in events; view enrolments and results.
13. GitHub Actions / CI
GitHub Actions is used to validate the RaceDay Part 1 repository structure. The workflow checks that the docs folder exists and that the required ERD, API Endpoint Plan and SQL database script are present.

Workflow Location: .github/workflows/validate.yml
name: Validate RaceDay Part 1

on:
 push:
   branches:
     - main
 pull_request:
   branches:
     - main
A screenshot of the successful green CI build is included in the repository as docs/CI-Green-Build.png.

14. Project Structure
Repository Structure
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
15. Testing
10. Create RaceDayDB using the SQL script.
11. Confirm that all six tables are created.
12. Confirm that primary and foreign key relationships exist.
13. Confirm that the required seed data has been inserted.
14. Run the verification queries.
15. Confirm that the GitHub Actions workflow completes successfully.
16. Academic Integrity
I confirm that all design decisions reflect my understanding of the RaceDay system. I have reviewed and validated the database schema, ERD, API Endpoint Plan and SQL script. The final submission represents my own work and analysis.

17. Tools Used
• SQL Server Management Studio (SSMS)
• Microsoft SQL Server
• GitHub
• GitHub Actions
• Mermaid Live Editor
• Microsoft Word
• Text Editor
18. References
• Microsoft SQL Server Documentation
• SQL Server Management Studio (SSMS) Documentation
• Microsoft SQL Server Foreign Key Constraints Documentation
