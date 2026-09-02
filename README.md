[![Review Assignment Due Date](https://classroom.github.com/assets/deadline-readme-button-22041afd0340ce965d47ae6ef1cefeee28c7c493a6346c4f15d667ab976d596c.svg)](https://classroom.github.com/a/mr-hqvA6)
# RaceDay

## Project Description

RaceDay is a full-stack web-based event management system designed for the South African road running, walking, and cycling community.

The system allows Event Organisers to create and manage events, categories and participant results. Participants can browse upcoming events, enter events, view their enrolments and track their personal results.

## User Roles

### Organiser

The Organiser can:

- Create, edit and delete events
- Manage event categories
- View event enrolments
- Capture participant results

### Participant

The Participant can:

- Create an account
- Browse upcoming events
- Enter events by selecting a category
- View their enrolments
- Track their personal results

## Part 1 - System Planning and Database

Part 1 contains the planning and database components of the RaceDay system.

The `docs` folder contains:

- RaceDay ERD
- API Endpoint Plan
- SQL Database Script

The database contains six entities:

- Organiser
- Participant
- Event
- Category
- Enrolment
- Result

The SQL script creates the database, tables, primary keys, foreign keys, constraints and sample data.

## GitHub Actions / CI

GitHub Actions is used to validate the required Part 1 repository structure.

The workflow checks that the `docs` folder exists and contains the required ERD, API Endpoint Plan and SQL database script.

### Successful CI Build

![Successful CI Build](docs/CI-Green-Build.png)
