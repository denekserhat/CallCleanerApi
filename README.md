# CallCleaner API

Backend API for the CallCleaner mobile application, designed to block spam calls based on user settings and community reports.

## Features

*   **User Management:** Secure registration and JWT-based authentication. Profile updates.
*   **Spam Blocking Settings:**
    *   Multiple blocking modes (All, Known Spammers, Custom).
    *   Personal whitelist management.
    *   Configurable working hours for blocking.
    *   Notification preferences.
*   **Blocked Call Handling:**
    *   Logging of blocked calls per user.
    *   Statistics on blocked calls (today, week, total).
    *   Ability to report incorrectly blocked calls.
*   **Community Spam Reporting:**
    *   Users can report spam numbers with categories (telemarketing, scam, etc.).
    *   View recent calls for easy reporting.
*   **Spam Detection:**
    *   Check the spam status and risk score of any number.
    *   Real-time check for incoming calls (intended for mobile app integration).
    *   Detailed information lookup for reported numbers.
*   **Application & System:**
    *   App version check endpoint.
    *   Privacy policy information.
*   **Data Synchronization:** Endpoints for syncing settings and blocked numbers between device and server.

## Technology Stack (Assumed)

*   .NET / ASP.NET Core
*   Entity Framework Core (for data access)
*   JWT Authentication
*   (Potentially others like caching, logging frameworks)

## Getting Started

*(Bu bölüm daha sonra doldurulabilir: Gerekli SDK'lar, veritabanı kurulumu, yapılandırma adımları vb.)*

1.  **Clone the repository:**
    ```bash
    git clone https://github.com/denekserhat/CallCleanerApi.git
    cd CallCleanerApi
    ```
2.  **Configure database connection:**
    *(Update `appsettings.json` or use user secrets)*
3.  **Apply database migrations:**
    ```bash
    dotnet ef database update
    ```
4.  **Run the application:**
    ```bash
    dotnet run --project src/presentation/CallCleaner.API/CallCleaner.API.csproj 
    ```
    *(Proje yolu farklıysa güncelleyin)*

## API Documentation

*(API endpoint'lerinin Swagger veya Postman gibi bir araçla belgelendirilmesi önerilir. Varsa buraya link eklenebilir.)*

---
