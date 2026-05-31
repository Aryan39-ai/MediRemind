# MediRemind — Free Medication Reminder & Tracker

**ITEC323 Assessment 3** · ACU Peter Faber Business School · Semester 1 2026

MediRemind is a dynamic, mobile-responsive ASP.NET web application that helps
people — especially older adults, chronically ill patients, and their caregivers
— stay on top of their medication schedules.

## Tech Stack
- **ASP.NET 8 Razor Pages** (server-side)
- **Tailwind CSS** (mobile-first responsive layout)
- **Entity Framework Core + SQLite** (database)
- **BCrypt** (password hashing)

## Features
- 💊 Add, edit, delete medications with dosage and times
- 📅 Today's schedule view with one-tap "mark taken" buttons
- 📊 7-day and 30-day adherence statistics
- 📋 Full dose history with filter (All / Taken / Missed)
- 👥 Caregiver access sharing
- 🔒 Session-based auth with BCrypt-hashed passwords

## Running the app
```bash
cd MediRemind
dotnet restore
dotnet run
```

The SQLite database (mediremind.db) is created automatically on first run.

## No API keys needed
This project uses no external paid APIs — everything runs locally.
