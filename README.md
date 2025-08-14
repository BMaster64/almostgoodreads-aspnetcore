<<'PATCH'
*** Begin Patch
*** Add File: D:\Document\Eppede\PRN222\PRN222_Project\README.md
+## Almost GoodReads
+
+An ASP.NET Core Razor Pages web app for browsing books, writing and voting on reviews, and managing a personal library. Includes an admin area to manage books, genres, users, and reviews.
+
+### Features
+- **Books**: list, details, genres, average ratings
+- **Reviews**: create, edit, delete, vote on helpful reviews
+- **My Library**: add books with a status (e.g., planning, reading, finished)
+- **Authentication & Roles**: cookie-based auth; roles `User` and `Admin`
+- **Admin Pages**: manage books, genres, reviews, and users
+
+### Tech Stack
+- **.NET**: ASP.NET Core 8, Razor Pages
+- **Data**: Entity Framework Core 8 (SQL Server)
+- **UI**: Bootstrap 5, jQuery, unobtrusive validation
+
+## Getting Started
+
+### Prerequisites
+- .NET SDK 8.x
+- SQL Server (Express or Developer) or LocalDB
+- Optional: `dotnet-ef` tool for migrations
+
+Install EF tooling if you don't have it:
+```bash
+dotnet tool install -g dotnet-ef
+```
+
+### Clone and Restore
+```bash
+git clone <this-repo-url>
+cd PRN222_Project
+dotnet restore
+```
+
+### Configure Database
+The app uses the `MyCnn` connection string from `BookReviewWeb/appsettings.json`.
+
+Example (SQL Server Express):
+```json
+{
+  "ConnectionStrings": {
+    "MyCnn": "Data Source=localhost\\SQLEXPRESS;Initial Catalog=AlmostGoodReads;Trusted_Connection=SSPI;Encrypt=false;TrustServerCertificate=true"
+  }
+}
+```
+
+Alternatively, set via environment variable:
+```powershell
+$env:ConnectionStrings__MyCnn = "Data Source=localhost\\SQLEXPRESS;Initial Catalog=AlmostGoodReads;Trusted_Connection=SSPI;Encrypt=false;TrustServerCertificate=true"
+```
+
+### Database Migrations
+Apply migrations to create/update the database:
+```bash
+cd BookReviewWeb
+dotnet ef database update
+```
+
+### Run the App
+```bash
+dotnet run --project BookReviewWeb
+```
+By default, the app listens on `http://localhost:5000` and `https://localhost:5001` (or as shown in console output).
+
+## Configuration
+- `BookReviewWeb/appsettings.json`
+  - `ConnectionStrings:MyCnn`: SQL Server connection string
+  - `Logging`: log levels
+  - `AllowedHosts`: host filtering
+- Antiforgery header name is configured as `RequestVerificationToken` and is required for state-changing API calls.
+
+## API
+
+All API endpoints require authentication unless stated otherwise. Send antiforgery token in the `RequestVerificationToken` header for POST endpoints (see examples below).
+
+### Genres
+- **POST** `api/genres` — create a genre (Admin only)
+  - Body: `{ "genreName": "Fantasy" }`
+  - Responses: `200 OK { success: true, genreId, genreName }` or `400 Bad Request`
+
+### My Books
+- **POST** `api/MyBooks/update` — add or update your book status
+  - Body: `{ "bookId": 123, "status": 1 }`
+  - Response: `200 OK`
+- **POST** `api/MyBooks/remove` — remove a book from your library
+  - Body: `{ "bookId": 123 }`
+  - Response: `200 OK` or `404 Not Found`
+
+### Getting the Antiforgery Token in Razor Pages
+Include the antiforgery token in your page/form and send it as a header:
+```html
+<form method="post">
+    @Html.AntiForgeryToken()
+    <!-- form fields -->
+</form>
+```
+
+Example JavaScript fetch using the hidden token value:
+```javascript
+const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
+
+await fetch('/api/MyBooks/update', {
+  method: 'POST',
+  headers: {
+    'Content-Type': 'application/json',
+    'RequestVerificationToken': token
+  },
+  body: JSON.stringify({ bookId: 1, status: 1 })
+});
+```
+
+## Project Structure
+```text
+PRN222_Project/
+├─ BookReviewWeb/
+│  ├─ Controllers/          # API controllers (Genres, MyBooks)
+│  ├─ Migrations/           # EF Core migrations
+│  ├─ Models/               # EF Core DbContext and entities
+│  ├─ Pages/                # Razor Pages (Admin, Auth, Library, Reviews, User)
+│  ├─ wwwroot/              # Static assets (css, js, libs)
+│  ├─ Program.cs            # App bootstrapping
+│  ├─ appsettings.json      # Configuration
+│  └─ BookReviewWeb.csproj
+└─ README.md
+```
+
+## Development
+- Create a new migration:
+```bash
+dotnet ef migrations add <MigrationName> --project BookReviewWeb
+```
+- Update database:
+```bash
+dotnet ef database update --project BookReviewWeb
+```
