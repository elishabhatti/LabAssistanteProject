# 🛠️ Online Help Desk (OHD) - Campus Management System

Online Help Desk ek professional web application hai jo campus ke mukhtalif facilities (Labs, Library, Hostels, etc.) ki service requests ko automate aur manage karne ke liye banaya gaya hai. Ye project **ASP.NET Core MVC** aur **SQL Server** par mabni hai.

---

## 🚀 Key Modules & Role-Based Access

System mein 4 main roles hain, aur har role ka apna makhsoos dashboard hai:

### 1. 👤 End-User (Students/Faculty)
* **Create Requests:** Kisi bhi facility ke liye nayi service request generate karna.
* **Severity Levels:** Maslay ki shiddat (Low, Medium, High, Critical) muntakhib karna.
* **Track Status:** Apni purani requests ka status check karna.

### 2. 👨‍💼 Facility-Head
* **Request Management:** Apni facility se judi tamaam requests ko dekhna.
* **Assign Task:** Unassigned requests ko kisi specific **Assignee** ko dena.
* **Reports:** Monthly summary aur audit reports check karna.

### 3. 🛠️ Assignee (Technician)
* **Work Dashboard:** Sirf wahi requests dekhna jo unhein assign ki gayi hain.
* **Status Updates:** Work-in-progress, Closed, ya Rejected status update karna.
* **Remarks:** Kaam khatam karne ke baad feedback ya "Answer" save karna.

### 4. 🔑 Administrator
* **User Management:** Naye accounts banana aur roles assign karna.
* **Facility Management:** Campus mein nayi facilities (e.g. New Mess or Lab) add karna.
* **System Overview:** Pura system control karna.

---

## 💻 Tech Stack



* **Backend:** ASP.NET Core 8.0/9.0 (C#)
* **Database:** Microsoft SQL Server
* **ORM:** Entity Framework Core
* **Frontend:** Bootstrap 5, HTML5, CSS3, jQuery
* **Security:** Role-Based Authorization & Session Management

---

## 📊 Database Design

Project ka database normalized hai aur isme niche diye gaye tables shamil hain:

* **Users:** User details aur roles (`enduser`, `facility_head`, `assignee`, `admin`).
* **Facilities:** Campus facilities ki list.
* **Requests:** Requests ka main data (Description, Severity, Status).
* **History:** Status change hone ka mukammal record (Audit Trail).



---

## 🛠️ Setup & Installation

1. **Repository Clone Karein:**
   ```bash
   git clone [https://github.com/YOUR_USERNAME/LabAssistanteProject.git](https://github.com/YOUR_USERNAME/LabAssistanteProject.git)
