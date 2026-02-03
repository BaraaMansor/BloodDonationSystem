# Blood Donation Management System

A comprehensive ASP.NET MVC application for managing blood donation activities between donors, hospitals, and blood banks.

## Features

- User Management (Admin, Donor, Hospital roles)
- Blood Donation Tracking
- Blood Request Management
- Reports and Statistics
- Role-based Access Control

## Technologies Used

- ASP.NET Core 8.0 MVC
- Entity Framework Core
- SQL Server (LocalDB)
- Bootstrap 5
- Font Awesome Icons

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- SQL Server LocalDB
- Visual Studio 2022 or VS Code

### Installation

1. Clone or download the project
2. Open the project in Visual Studio or VS Code
3. Restore NuGet packages:

   ```
   dotnet restore
   ```

4. Update the database:

   ```
   dotnet ef database update
   ```

5. Run the application:

   ```
   dotnet run
   ```

6. Open your browser and navigate to `https://localhost:5001`

## Project Structure

- **Models/** - Data models and entities
- **Controllers/** - MVC controllers
- **Views/** - Razor views
- **Data/** - Database context and migrations
- **wwwroot/** - Static files (CSS, JS, libraries)

## Default Users

After seeding the database, you can login with:

- **Admin:** admin@bloodbank.com / Admin123!

You can register new Donor and Hospital accounts through the registration page.
