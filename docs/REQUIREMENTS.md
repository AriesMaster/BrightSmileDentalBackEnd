# BrightSmile Dental — Software Requirements Specification (SRS)

**System:** BrightSmile Dental  
**Document status:** As-implemented requirements baseline for a deployed portfolio application  
**Scope:** Angular client and ASP.NET Core 10 REST API  
**Related documents:** [Architecture](ARCHITECTURE.md) · [Database design](DATABASE.md) · [Backend README](../README.md)

## 1. Purpose and scope

BrightSmile Dental is a role-based dental practice management system. It supports patient registration and profiles, public service and dentist discovery, appointment booking and lifecycle management, administrator operations and appointment reporting.

This SRS documents requirements supported by the current implementation and identifies areas needing further verification. It does **not** imply that formal stakeholder sign-off, usability studies or comprehensive automated testing have occurred.

## 2. User roles and access

| Actor | Responsibilities and access |
| --- | --- |
| Visitor | View public dental services and dentist directory; register and sign in |
| Patient | Maintain own profile, find available appointment slots, book and manage eligible own appointments |
| Dentist | View assigned appointments and perform permitted status transitions |
| Administrator | Manage services and dentist accounts; inspect patient records, appointments and analytics |

Backend JWT authentication, role authorization and ownership checks govern access. Angular guards provide client-side navigation controls, not the security boundary.

## 3. Functional requirements

Identifiers are stable references for implementation and future tests. **Implemented** means supported in the source, not necessarily covered by automated tests.

### FR-AUTH — Authentication and accounts

| ID | Requirement | Status |
| --- | --- | --- |
| FR-AUTH-01 | Visitors shall be able to register a patient account with a corresponding profile. | Implemented |
| FR-AUTH-02 | The API shall reject duplicate email registration and validate required registration fields. | Implemented |
| FR-AUTH-03 | Registered users shall authenticate with email and password and receive a JWT on success. | Implemented |
| FR-AUTH-04 | Passwords shall be stored and checked using BCrypt hashes. | Implemented |
| FR-AUTH-05 | The API shall enforce Admin, Dentist and Patient roles on protected operations. | Implemented |
| FR-AUTH-06 | Users shall be able to request a password-reset link without disclosing whether an email exists. | Implemented; production email delivery needs verification |
| FR-AUTH-07 | Reset tokens shall be cryptographically generated, stored as hashes, expire after one hour and be invalidated after use. | Implemented |

### FR-PROFILE — User profiles

| ID | Requirement | Status |
| --- | --- | --- |
| FR-PROFILE-01 | An authenticated user shall be able to retrieve their own profile. | Implemented |
| FR-PROFILE-02 | An authenticated user shall be able to create or update their own profile using the supported endpoint. | Implemented |
| FR-PROFILE-03 | Profile updates shall validate supported personal details and reject invalid dates of birth. | Implemented |

### FR-SERVICE — Dental services

| ID | Requirement | Status |
| --- | --- | --- |
| FR-SERVICE-01 | Visitors shall be able to list services and view an individual service. | Implemented |
| FR-SERVICE-02 | Administrators shall be able to create and update services. | Implemented |
| FR-SERVICE-03 | Administrators shall be able to delete services that have no associated appointments. | Implemented |
| FR-SERVICE-04 | The API shall validate service names and prices and reject duplicate service names ignoring case. | Implemented in API logic |

### FR-DENTIST — Dentist information

| ID | Requirement | Status |
| --- | --- | --- |
| FR-DENTIST-01 | Visitors shall be able to view public dentist directory information. | Implemented |
| FR-DENTIST-02 | Administrators shall be able to view dentist records and appointment summaries. | Implemented |
| FR-DENTIST-03 | Administrators shall be able to create dentist accounts. | Implemented |

### FR-APPT — Appointment scheduling

| ID | Requirement | Status |
| --- | --- | --- |
| FR-APPT-01 | Patients shall be able to query available appointment slots for a dentist and date. | Implemented |
| FR-APPT-02 | Patients shall be able to create appointments for valid dentists and services. | Implemented |
| FR-APPT-03 | Patients shall be able to view their own appointments. | Implemented |
| FR-APPT-04 | Dentists shall be able to view their assigned appointments. | Implemented |
| FR-APPT-05 | Administrators shall be able to list appointments. | Implemented |
| FR-APPT-06 | Authorized users shall be able to retrieve an individual appointment, subject to ownership checks. | Implemented |
| FR-APPT-07 | Patients shall be able to reschedule eligible pending appointments. | Implemented |
| FR-APPT-08 | Patients and administrators shall be able to cancel eligible appointments subject to authorization and state rules. | Implemented |
| FR-APPT-09 | Assigned dentists and administrators shall be able to perform permitted appointment status changes. | Implemented |
| FR-APPT-10 | The API shall reject conflicting active bookings for the same dentist. | Implemented; concurrent-load verification outstanding |

### FR-ADMIN — Administrative records and reporting

| ID | Requirement | Status |
| --- | --- | --- |
| FR-ADMIN-01 | Administrators shall be able to view patient lists with appointment counts. | Implemented |
| FR-ADMIN-02 | Administrators shall be able to view a patient's profile and appointment history. | Implemented |
| FR-ADMIN-03 | Administrators shall be able to request a date-filtered appointment reporting overview. | Implemented |
| FR-ADMIN-04 | Reporting shall include status breakdowns, daily trends, service popularity and dentist workload. | Implemented |
| FR-ADMIN-05 | Reporting shall distinguish estimated service values from actual payments or revenue. | Implemented as estimates; payment collection is not represented |

## 4. Business rules

| ID | Rule |
| --- | --- |
| BR-01 | Appointment timestamps supplied for booking and rescheduling must be future UTC values and within two years. |
| BR-02 | Practice hours are Monday–Friday, 09:00–17:00 South African Standard Time (UTC+02:00). |
| BR-03 | Appointments occupy 30 minutes; valid starts are at :00 or :30, ending no later than 17:00. |
| BR-04 | Pending and Confirmed appointments reserve a dentist's time slot; overlapping bookings are rejected. |
| BR-05 | A cancelled appointment no longer reserves its slot. |
| BR-06 | Appointment status changes and cancellation are constrained by the current status and the caller's role or ownership. |
| BR-07 | A service referenced by any appointment cannot be deleted through the API. |
| BR-08 | User email addresses have a database unique index. |
| BR-09 | Report date ranges are inclusive and limited to 366 days; default reporting covers the latest 30 days in SAST. |
| BR-10 | Reset tokens expire after one hour; only their hashes are persisted. |

## 5. Non-functional requirements

These are design expectations and current engineering controls. Where no measured evidence exists, the document does not claim a performance or availability guarantee.

| ID | Requirement or quality attribute | Evidence / verification status |
| --- | --- | --- |
| NFR-SEC-01 | Validate JWT signature, issuer, audience and lifetime on authenticated requests. | Implemented in API startup |
| NFR-SEC-02 | Enforce server-side role and resource ownership restrictions. | Implemented in controllers; automated coverage outstanding |
| NFR-SEC-03 | Protect secrets using environment configuration or local user secrets. | Supported by configuration; production secret inventory not documented |
| NFR-DATA-01 | Persist data in PostgreSQL through EF Core and maintain schema with migrations. | Implemented |
| NFR-DATA-02 | Coordinate concurrent scheduling writes with transactions and dentist-scoped advisory locks. | Implemented; load testing outstanding |
| NFR-API-01 | Expose JSON REST endpoints with appropriate HTTP status codes and an OpenAPI reference. | Implemented; Scalar is configured |
| NFR-DEPLOY-01 | Deploy the Angular client and ASP.NET Core API independently and connect them over HTTPS. | Deployed to Netlify and Render |
| NFR-TEST-01 | Verify core business rules, authorization and scheduling conflicts with repeatable tests. | Manual scenarios recorded; automated tests planned |
| NFR-OPS-01 | Provide repeatable build and deployment checks. | Docker support exists; CI/CD gates planned |
| NFR-UX-01 | Provide role-oriented navigation and usable forms for supported workflows. | Implemented; formal accessibility/usability assessment not recorded |

No specific uptime, latency, throughput, recovery-time or recovery-point objective has been agreed or measured for this portfolio project.

## 6. Acceptance criteria and test scenarios

These scenarios are intended as executable future test cases. Previously observed manual results are summarized in the backend README; the list below is not a claim that every scenario has automated coverage.

| ID | Given | When | Then |
| --- | --- | --- | --- |
| AC-01 | A new valid patient account | The visitor registers | A patient account and profile are created |
| AC-02 | An existing email | Another registration uses it | The API rejects the duplicate |
| AC-03 | Valid credentials | The user logs in | A JWT and account role are returned |
| AC-04 | A patient JWT | The patient attempts admin service creation | The API denies access |
| AC-05 | A valid unoccupied 30-minute slot | A patient books it | The appointment is created with Pending status |
| AC-06 | A Pending or Confirmed booking exists for a dentist | Another patient requests an overlapping slot | The API returns a conflict |
| AC-07 | A booking ends at 09:30 | Another patient books 09:30–10:00 | The adjacent booking can succeed if otherwise valid |
| AC-08 | An active appointment is cancelled | Another patient books its released slot | The slot can be booked |
| AC-09 | A service has linked appointments | An administrator tries to delete it | Deletion is rejected |
| AC-10 | A patient attempts to retrieve another patient's appointment | The API checks ownership | Access is forbidden |
| AC-11 | A reset token is expired or invalid | Password reset is attempted | The API rejects the reset |
| AC-12 | An administrator requests an invalid or oversized report date range | The report endpoint validates dates | The API rejects the request |

## 7. Constraints and assumptions

- Backend: ASP.NET Core 10, EF Core 10, PostgreSQL.
- Frontend: Angular application deployed separately.
- User roles are represented as strings in the current account model.
- South African time is UTC+02:00 year-round.
- The current implementation does not include a payment collection ledger.
- The current architecture is a controller-based layered application, not microservices or a formally separated Clean Architecture implementation.

## 8. Out of scope / future verification

- Automated unit, integration and end-to-end test suites.
- Concurrent-load stress tests for booking locks.
- Production password-reset email delivery verification.
- Defined availability, latency and recovery objectives.
- Formal stakeholder approval, accessibility audit and penetration testing.
- CI/CD build and test gates.

## 9. Traceability

| Requirement area | Primary implementation |
| --- | --- |
| Authentication | `AuthController`, `JwtService`, `User` |
| Profiles | `ProfilesController`, `Profile` |
| Services | `ServicesController`, `Service` |
| Dentists | `DentistsController`, `User` |
| Scheduling | `AppointmentsController`, `Appointment` |
| Reporting | `ReportsController` |
| Database integrity | `ApplicationDbContext`, EF Core migrations |
| API reference | `Program.cs`, Scalar and OpenAPI |

For detailed API contracts, use the [Scalar reference](https://brightsmiledentalbackend.onrender.com/api-docs) when production documentation is enabled.
