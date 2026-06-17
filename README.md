

# BrightSmile Dental Backend API

The BrightSmile Dental Backend is a RESTful Web API built using ASP.NET Core, designed to power a modern dental management system. It handles users, profiles, services, and appointments while connecting to a Supabase PostgreSQL database using Entity Framework Core.

This backend is designed to work with an Angular frontend, forming a full-stack dental management platform.

---

# Tech Stack

* ASP.NET Core Web API (.NET)
* Entity Framework Core (Code First)
* PostgreSQL (Supabase)
* Swagger / OpenAPI
* C#
* RESTful Architecture

---

# System Architecture

```text
Angular Frontend
        ↓
ASP.NET Core Web API
        ↓
Entity Framework Core (ORM)
        ↓
Supabase PostgreSQL Database
```

---

# Core Features

## User Management

* Register users (Admin, Doctor, Patient roles)
* Secure password storage (hashed passwords)
* Role-based system foundation

## Profile Management

* User personal information
* Contact details
* One-to-one relationship with users

## Dental Services

* Create and manage dental services
* Store service descriptions and pricing

## Appointment System

* Book appointments between patients and doctors
* Track appointment status (Pending, Confirmed, Completed, Cancelled)
* Link services to appointments

---

# Database Design (Simplified)

## Users

* Id
* Email
* PasswordHash
* Role

## Profiles

* Id
* UserId
* FirstName
* LastName
* PhoneNumber

## Services

* Id
* Name
* Description
* Price

## Appointments

* Id
* PatientId
* DoctorId
* ServiceId
* AppointmentDate
* Status
* AdditionalInfo

---

# Setup Instructions

## 1. Clone Repository

```bash
git clone https://github.com/your-username/BrightSmileDentalBackEnd.git
cd BrightSmileDentalBackEnd
```

---

## 2. Restore Dependencies

```bash
dotnet restore
```

---

## 3. Configure Database

Update your appsettings.json with your Supabase connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=YOUR_SUPABASE_HOST;Port=5432;Database=postgres;Username=postgres.xxx;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true"
  }
}
```

---

## 4. Run Entity Framework Migrations

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

---

## 5. Run the API

```bash
dotnet run
```

---

# API Documentation (Swagger)

Once the application is running, open:

```
https://localhost:{PORT}/swagger
```

Swagger provides interactive API documentation for testing endpoints.

---

# Security Features

* Passwords stored using hashing (not plain text)
* Secure PostgreSQL connection via Supabase SSL
* Role-based system design (Admin, Doctor, Patient)
* Backend-first architecture (Angular consumes API only)

---

# Project Status

## Completed

* Project setup
* EF Core configuration
* Database design
* Supabase connection setup

## In Progress

* Authentication system
* User management APIs
* Appointment endpoints

## Planned

* JWT authentication
* Angular frontend integration
* Email notifications
* Admin dashboard
* Deployment to cloud (Azure / Render)

---

# Purpose of This Project

This project is built for:

* Learning full-stack development
* Practicing clean architecture
* Building a portfolio-ready system
* Demonstrating real-world backend API design

---

# Author

AriesMaster

Full-stack developer building real-world systems using ASP.NET Core and Angular.

---

# License

This project is intended for educational and portfolio purposes.

