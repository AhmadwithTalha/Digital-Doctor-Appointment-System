# 🏥 Digital Doctor Appointment System

*Digital Doctor Appointment System* is a full-featured hospital management application developed using **ASP.NET Core Web API**. This project helped me master **full .net core development, JWT authentication, API integration, SignalR notifications, and database management** in a real-world hospital management scenario.

This project was an exciting journey combining multiple concepts like user authentication, patient and doctor management, appointment queues, notifications, and role-based access control.

---

## 🚀 Project Overview

The *Digital Doctor Appointment System* allows users to:

* Register as a patient
* Book, view, and cancel appointments
* Receive real-time notifications for appointments
* Admins can manage doctors, schedules, and sub-admins

Through this project, I learned to:

* Structure a scalable full .net core web application
* Implement role-based authentication using **JWT tokens**
* Handle real-time updates using **SignalR**
* Persist and manage data efficiently in **SQL Server**

---

## 🧠 Learning Journey

### 🟢 Authentication & Authorization (JWT)

* Implemented secure login for Admin, SubAdmin, Doctor, and Patient
* Role-based access control for endpoints
* Token-based session management for API calls

### 🔵 Backend (ASP.NET Core + EF Core)

* CRUD operations for Patients, Doctors, Appointments, SubAdmins, and Schedules
* Used Entity Framework Core for database operations
* Applied business logic such as appointment token generation and queue management

### 🔴 Real-Time Notifications (SignalR)

* Sent notifications to patients and doctors about upcoming appointments
* Integrated groups for patient- and doctor-specific notifications

---

## ⚙ Core Features

### 🩺 Patient Management

* Register new patients
* Update profile and password
* View and cancel appointments
* Access appointment history

### 👨‍⚕️ Doctor & Schedule Management

* Admin can create/update doctor profiles
* Manage doctor schedules and availability
* Display doctor details to patients for appointment booking

### 🛎 Appointment Queue

* Automatic queue management based on appointment time
* Real-time notifications to patients and doctors when it’s their turn
* Status tracking: Pending, Done, Cancelled

### 🔐 SubAdmin Management

* Admin can add/update/delete sub-admins
* Assign responsibilities to sub-admins
* SubAdmin login for restricted operations

---

## 🛠 Technologies & Packages Used

| Technology / Package          | Description                        |
| ----------------------------- | ---------------------------------- |
| ASP.NET Core Web API          | Backend API development            |
| Entity Framework Core         | Database ORM                       |
| SQL Server                    | Relational database                |
| SignalR                       | Real-time notifications            |
| JWT / Microsoft.IdentityModel | Authentication and role management |
| BCrypt.Net                    | Password hashing                   |

---

## 🧩 Project Structure (Simplified)

**Backend (ASP.NET Core)**

```
Controllers/
  ├─ AuthController.cs
  ├─ PatientController.cs
  ├─ SubAdminController.cs
  ├─ DoctorScheduleController.cs
  └─ AppointmentController.cs
Services/
  ├─ NotificationService.cs
  └─ QueueService.cs
Hubs/
  └─ NotificationHub.cs
Helpers/
  └─ PasswordHelper.cs
Models/
DTOs/
Program.cs
appsettings.json
---

## 📸 App Screenshots (5 Pictures)

| Login / Signup                                                                      | Patient Dashboard                                                                   | Doctor Schedule                                                                     |
| ----------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------- |
| | ![Admin](https://github.com/AhmadwithTalha/Digital-Doctor-Appointment-System/blob/20bfd3a1963f86eec5bc5462bc0feea725edddcc/Screenshot2.png) | ![Screen2](https://raw.githubusercontent.com/AhmadwithTalha/Digital-Doctor-Appointment-System/branch_name/Screenshot2.png) | ![Screen3](https://raw.githubusercontent.com/AhmadwithTalha/Digital-Doctor-Appointment-System/branch_name/Screenshot3.png) |
 | ![](https://github.com/AhmadwithTalha/Digital-Doctor-Appointment-System/blob/20bfd3a1963f86eec5bc5462bc0feea725edddcc/Screenshot2.png) | ![](https://github.com/yourusername/Digital-Doctor-Appointment-System/assets/3.png) |

| Appointment Queue                                                                   | SubAdmin Management                                                                 |
| ----------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------- |
| ![](https://github.com/yourusername/Digital-Doctor-Appointment-System/assets/4.png) | ![](https://github.com/yourusername/Digital-Doctor-Appointment-System/assets/5.png) |

---

## 🧰 How to Run the Project

1. **Clone the repository**

   ```bash
   git clone https://github.com/yourusername/Digital-Doctor-Appointment-System.git
   ```

2. **Backend Setup (ASP.NET Core)**

   * Open in Visual Studio 2022
   * Restore NuGet packages
   * Update `appsettings.json` with your SQL Server connection
   * Run the project (`F5`)


3. **Test JWT Authentication & Notifications**

   * Login as Admin to create SubAdmins / Doctors
   * Book appointments as Patient
   * Observe real-time notifications via SignalR

---

## 💡 What I Learned

* Implemented **full .net core development** workflow from backend API
* Built **role-based authentication & authorization** using JWT
* Handled **real-time notifications** using SignalR
* Managed **appointment queues and business logic**
* Gained practical experience with **SQL Server + EF Core** for relational data

---

## 🌐 Connect with Me

🔗 *LinkedIn:* [https://www.linkedin.com/in/ahmad-talha-se](https://www.linkedin.com/in/ahmad-talha-se)
---

## ⭐ Final Thoughts

*Digital Doctor Appointment System* was a complete practical journey into **hospital management software**.
From authentication, API integration, SignalR, this project strengthened my **full .net core development skills**.

