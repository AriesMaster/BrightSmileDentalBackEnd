# BrightSmile Dental | Backend API

**ASP.NET Core 10 · C# · Entity Framework Core · PostgreSQL · JWT**

A role-based REST API for a dental practice management system. BrightSmile Dental combines an Angular frontend with an ASP.NET Core backend to support patient accounts, dental services, appointment scheduling, and dentist/admin workflows.

> **Project status:** Core backend features are implemented and have undergone manual local API testing. The project remains under active development; production deployment, complete end-to-end integration, and automated testing require further verification.

**[Live Angular frontend](https://brightsmiledentalfrontend.netlify.app/)** · **[Frontend repository](https://github.com/AriesMaster/BrightSmileDentalFrontEnd)** · **[Portfolio](https://thabo.netlify.app/)**

## Technical stack

| Layer | Technologies |
| --- | --- |
| API | C#, ASP.NET Core 10 Web API, REST, DTOs |
| Data access | Entity Framework Core 10, Npgsql |
| Database | Supabase PostgreSQL |
| Security | JWT bearer authentication, role-based authorization, BCrypt |
| API documentation | Swagger / OpenAPI |
| Frontend | Angular, TypeScript, Angular Material |
| Development tools | Visual Studio, Git, GitHub, Swagger UI |

## Architecture

```text
Angular SPA (Netlify / localhost:4200)
             |
             | HTTPS + JSON + Bearer JWT
             v
ASP.NET Core Web API
  |-- AuthController          Registration, login, password reset
  |-- UsersController         Current user identity
  |-- ProfilesController      Profile read/update
  |-- ServicesController      Public catalogue; Admin CRUD
  |-- AppointmentsController  Booking and lifecycle operations
  |
  |-- DTOs and validation
  |-- JWT authentication / role authorization
  |-- EF Core DbContext and transactions
             |
             v
Supabase PostgreSQL
  Users -- Profiles
  Users -- Appointments (Patient / Dentist)
  Services -- Appointments
```

The API uses controller-based endpoints, dependency injection, request/response DTOs, EF Core persistence, and role/ownership checks. It is **not** presented as a full Clean Architecture implementation.

## Implemented features

### Authentication and account security

- Patient registration with associated profile creation.
- Login with BCrypt password verification and JWT issuance.
- Roles: **Admin**, **Dentist**, and **Patient**.
- JWT signature, issuer, audience, and lifetime validation.
- Protected routes and resource-level authorization.
- Forgot-password and reset-password endpoints using cryptographically generated tokens; only token hashes are stored, and tokens expire after one hour.
- Database uniqueness constraint on user email, with conflict handling for duplicate registrations.

**Verification note:** Password reset endpoints are implemented, but successful email delivery and the complete production reset journey have not been confirmed end-to-end.

### Profiles and dental services

- Authenticated users can retrieve and update their own profiles.
- Public dental service listing and service detail endpoints.
- Admin-only create, update, and delete operations for services.
- Validation for service names, descriptions, and prices.
- Duplicate service-name checks and protection against deleting services referenced by appointments.

### Appointment management

- Patients create bookings, view their own appointments, reschedule eligible bookings, and cancel active bookings.
- Dentists view assigned appointments and manage permitted status transitions.
- Admins can view appointments and perform authorized management actions.
- Booking requests validate future **UTC** dates, dentist role, and selected service.
- Appointments occupy **30-minute intervals**.
- Pending and Confirmed bookings reserve a time slot; cancelled bookings release it.
- Appointment status lifecycle: **Pending → Confirmed → Completed**, with cancellation of eligible active bookings.

#### Scheduling conflict prevention

The API detects interval overlaps, not merely identical start times. For a dentist with an existing 09:00–09:30 appointment:

| Requested slot | Expected outcome |
| --- | --- |
| 09:15–09:45 | Rejected — overlaps |
| 09:30–10:00 | Allowed — adjacent slot |
| 09:00–09:30 after cancellation | Allowed — slot released |

Appointment creation, rescheduling, cancellation, and status updates use database transactions and PostgreSQL transaction-level **advisory locks scoped to the dentist** to coordinate schedule changes.

**Verification note:** This logic is implemented and local conflict cases were manually tested; concurrent-load stress testing is still outstanding.

## API endpoints

### Authentication and profile

| Method | Endpoint | Access |
| --- | --- | --- |
| POST | `/api/Auth/register` | Public — patient registration |
| POST | `/api/Auth/login` | Public |
| POST | `/api/Auth/forgot-password` | Public |
| POST | `/api/Auth/reset-password` | Public |
| GET | `/api/Users/me` | Authenticated |
| GET | `/api/Profiles/me` | Authenticated |
| PUT | `/api/Profiles/me` | Authenticated |

### Dental services

| Method | Endpoint | Access |
| --- | --- | --- |
| GET | `/api/Services` | Public |
| GET | `/api/Services/{id}` | Public |
| POST | `/api/Services` | Admin |
| PUT | `/api/Services/{id}` | Admin |
| DELETE | `/api/Services/{id}` | Admin |

### Appointments

| Method | Endpoint | Access |
| --- | --- | --- |
| GET | `/api/Appointments` | Admin |
| GET | `/api/Appointments/me` | Patient |
| GET | `/api/Appointments/dentist/me` | Dentist |
| GET | `/api/Appointments/{id}` | Authorized participant or Admin |
| POST | `/api/Appointments` | Patient |
| PUT | `/api/Appointments/{id}` | Patient — own eligible booking |
| PATCH | `/api/Appointments/{id}/status` | Assigned Dentist or Admin |
| PATCH | `/api/Appointments/{id}/cancel` | Patient (own booking) or Admin |

Endpoints return appropriate HTTP results including `200`, `201`, `204`, `400`, `401`, `403`, `404`, and `409` depending on the operation and outcome.

## Data model

| Entity | Key fields / relationships |
| --- | --- |
| User | Email (unique), PasswordHash, Role, password-reset fields |
| Profile | UserId (one-to-one with User), name, phone, date of birth, gender |
| Service | Name, Description, Price |
| Appointment | PatientId, DoctorId, ServiceId, AppointmentDate, Status, AdditionalInfo |

Entity Framework Core manages relationships and migrations. Appointment patient and dentist foreign keys use restricted delete behavior to protect related records.

## Local development

### Prerequisites

- .NET 10 SDK
- PostgreSQL database (Supabase or local)
- EF Core CLI tools (`dotnet-ef`)
- A valid JWT signing key (at least 32 bytes)

### 1. Clone and restore

```bash
git clone https://github.com/AriesMaster/BrightSmileDentalBackEnd.git
cd BrightSmileDentalBackEnd
dotnet restore
```

Run the following commands from the directory containing the backend `.csproj` file.

### 2. Configure secrets

Use **.NET user secrets** or environment variables for credentials; do not commit a real database password or JWT key.

```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=YOUR_HOST;Port=5432;Database=postgres;Username=YOUR_USER;Password=YOUR_PASSWORD;SSL Mode=Require"
dotnet user-secrets set "Jwt:Key" "REPLACE_WITH_A_RANDOM_SECRET_OF_AT_LEAST_32_BYTES"
dotnet user-secrets set "Jwt:Issuer" "BrightSmileDental"
dotnet user-secrets set "Jwt:Audience" "BrightSmileDentalUsers"
```

For password-reset emails, configure the email service's required SMTP settings and `App:ClientUrl` for the Angular reset-password page.

### 3. Apply migrations and run

```bash
dotnet ef database update
dotnet run
```

In **Development**, the application's startup code also applies pending EF Core migrations and seeds demo users if necessary. Swagger UI is enabled in Development:

```text
https://localhost:<PORT>/swagger
```

Use the actual local port shown by `dotnet run`. In Swagger, authorize with the JWT returned by `POST /api/Auth/login` to test protected endpoints.

**CORS:** The backend currently permits `http://localhost:4200` and `https://brightsmiledentalfrontend.netlify.app`.

## Testing and verification

Core backend behavior was manually tested using Swagger UI during development (17–19 November 2026). These results are **manual local API checks**, not an automated test suite.

| Scenario | Observed result |
| --- | --- |
| Admin and patient login | `200 OK` |
| Service create / list / update | `201 / 200 / 200` |
| Duplicate service name | `409 Conflict` |
| Patient attempts Admin-only service creation | `403 Forbidden` |
| Invalid negative service price | `400 Bad Request` |
| Appointment creation | `201 Created` |
| Overlapping booking | `409 Conflict` |
| Adjacent 30-minute booking | `201 Created` |
| Conflicting reschedule | `409 Conflict` |
| Valid reschedule | `200 OK` |
| Cancel and rebook released slot | `200 / 201` |

## Current status and roadmap

**Implemented in the backend source**
- JWT authentication, patient registration, password-reset endpoints, and user/profile APIs.
- Public dental service reads and Admin-restricted service CRUD.
- Appointment booking, retrieval, rescheduling, cancellation, and status transitions.
- Role and ownership checks, DTO validation, EF Core persistence, and scheduling conflict prevention.

**Further verification / planned improvements**
- Verify the latest deployed backend and complete frontend-to-production-API integration.
- Validate password-reset email delivery end-to-end.
- Add automated unit and integration tests, concurrency tests, and broader authorization coverage.
- Improve OpenAPI response metadata, production diagnostics, and deployment documentation.
- Continue Angular Admin and Dentist dashboard integration and workflows.

## Container support

The repository includes a multi-stage `Dockerfile` using the .NET 10 SDK for publishing and the ASP.NET Core 10 runtime for execution. The container listens on port `10000`.

```bash
docker build -t brightsmile-api .
docker run --rm -p 10000:10000 --env-file .env brightsmile-api
```

Create `.env` locally with the required database, JWT and email settings, using the ASP.NET Core environment-variable convention (for example, `ConnectionStrings__DefaultConnection` and `Jwt__Key`). **Never commit `.env` or credentials.** A successful image build or production deployment has not been independently verified here; the Dockerfile is an existing implementation, not evidence of a tested deployment pipeline.

## Related resources

- **Frontend:** https://github.com/AriesMaster/BrightSmileDentalFrontEnd
- **Live frontend:** https://brightsmiledentalfrontend.netlify.app/
- **Portfolio:** https://thabo.netlify.app/
- **Backend documentation:** Available through the BrightSmile Dental case study on the portfolio.

## Developer

**Thabo Tankiso Mavundza** — Junior Full-Stack Software Developer  
C# · ASP.NET Core · Angular · TypeScript · SQL · PostgreSQL

Built as a portfolio project demonstrating practical API development, database modelling, authentication, authorization, and business-rule implementation.
