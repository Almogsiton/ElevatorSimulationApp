# Elevator Simulation System

## Project Structure

The project is divided into two main parts:

### Backend (.NET Core API)
- **Directory:** `server/api/`
- **Technology:** ASP.NET Core 8.0 with Entity Framework Core
- **Role:** API server managing business logic, database, and elevator algorithm
- **Main Components:**
  - `Controllers/` - API endpoints
  - `Services/` - Business logic services
  - `Models/` - Models, DTOs and Enums
  - `Data/` - Entity Framework Context
  - `Hubs/` - SignalR Hubs for real-time communication

### Frontend (React)
- **Directory:** `client/`
- **Technology:** React 18 with Vite
- **Role:** Interactive user interface for elevator simulation
- **Main Components:**
  - `pages/` - Application pages
  - `components/` - React components
  - `services/` - API services
  - `contexts/` - React Contexts for state management

## Database Schema

The system uses SQL Server database with the following tables:

### Main Tables:

- **Users**
  - `Id` (int, PK)
  - `Email` (nvarchar(255), not null)
  - `Password` (nvarchar(255), not null)

- **Buildings**
  - `Id` (int, PK)
  - `Name` (nvarchar(255), not null)
  - `NumberOfFloors` (int, not null)
  - `UserId` (int, FK to Users)

- **Elevators**
  - `Id` (int, PK)
  - `BuildingId` (int, FK to Buildings)
  - `CurrentFloor` (int, not null)
  - `Status` (int, not null) — Enum
  - `Direction` (int, not null) — Enum
  - `DoorStatus` (int, not null) — Enum

- **ElevatorCalls**
  - `Id` (int, PK)
  - `BuildingId` (int, FK to Buildings)
  - `RequestedFloor` (int, not null)
  - `DestinationFloor` (int, nullable)
  - `CallTime` (datetime, not null)
  - `IsHandled` (bit, not null)

- **ElevatorCallAssignments**
  - `Id` (int, PK)
  - `ElevatorId` (int, FK to Elevators)
  - `ElevatorCallId` (int, FK to ElevatorCalls)
  - `AssignmentTime` (datetime, not null)

### Entity Relationship Diagram (ERD) - Textual Description:
- Each building has one elevator (One-to-One).
- Each building has many elevator calls (One-to-Many).
- Each elevator call can have an assignment to an elevator (ElevatorCallAssignment).
- Each user can have multiple buildings.

**Note:**
The complete table definitions can be found in the `server/api/tablesCreation.txt` file or in Entity Framework migration files.

## Project Running Instructions

### Prerequisites
- .NET 8.0 SDK
- Node.js 18+ and npm
- SQL Server or SQL Server Express
- Visual Studio 2022 or VS Code

### Database Setup
1. **Install SQL Server:**
   - Install SQL Server Express or SQL Server
   - Create a new database named `ElevatorSimulationDB`

2. **Update Connection String:**
   - Open the file `server/api/appsettings.json`
   - Update the Connection String according to your SQL Server settings:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=localhost;Database=ElevatorSimulationDB;Trusted_Connection=true;TrustServerCertificate=true;"
   }
   ```

### Dependencies Installation

#### Backend
```bash
cd server/api
dotnet restore
dotnet ef database update
```

#### Frontend
```bash
cd client
npm install
```

### Running the Project

#### Start Backend Server
```bash
cd server/api
dotnet run
```
The server will run on `https://localhost:7001` or `http://localhost:5001`

#### Start Frontend Client
```bash
cd client
npm run dev
```
The application will open on `http://localhost:5173`

## Technological Choices and Algorithm Approach

### Technology Stack

#### Backend
- **ASP.NET Core 8.0:** Modern and stable framework for API development
- **Entity Framework Core:** Advanced ORM for database management
- **SignalR:** For real-time communication between server and client
- **JWT Authentication:** For secure user authentication
- **Background Service:** For managing elevator simulation in the background

#### Frontend
- **React 18:** Modern and efficient UI library
- **CSS Modules:** For isolated style management
- **SignalR Client:** For real-time connection to server

### Elevator Algorithm Approach

#### Algorithm Principles:
1. **Target Management:** The elevator manages a sorted list of targets
2. **Movement Direction:** The elevator moves in one direction until all targets in that direction are handled
3. **SCAN Technique:** The elevator passes through all floors in the current movement direction
4. **Door Management:** Opening and closing doors with defined timers (2 seconds)

#### Movement Logic:
- **Idle State:** The elevator waits for new calls
- **Moving State:** The elevator moves toward the next target
- **Opening/Closing Doors State:** Managing door opening and closing
- **Call Processing:** Marking calls as completed when the elevator reaches a floor

#### Optimizations:
- **Target Sorting:** Targets are sorted by movement direction
- **On-the-way Collection:** The elevator collects calls in the current movement direction
- **Memory Management:** Using Dictionaries for efficient elevator state management
- **Duplicate Prevention:** Preventing duplicate calls for the same floor

#### Smooth Animations:
- **Elevator Animations:** Smooth movement between floors with CSS transitions
- **Door Animations:** Smooth opening and closing of elevator doors
- **Button Animations:** Visual effects when clicking buttons
- **Loading Animations:** Spinners and smooth loading

#### Loading States:
- **Loading States:** Loading states when sending HTTP requests
- **Error Handling:** Error handling with clear messages
- **Toast Notifications:** Short messages for immediate user feedback

#### Additional Improvements:
- **Responsive Design:** Support for different screen sizes
- **Real-time Updates:** Real-time updates via SignalR
- **Intuitive Navigation:** Intuitive navigation between pages
- **Visual Feedback:** Visual feedback for every action

### Advanced Architecture:
- **Separation of Concerns:** Clear separation between layers
- **Dependency Injection:** Using DI for dependency management
- **Service Layer:** Service layer for business logic management
- **DTO Pattern:** Using DTOs for data transfer
- **Configuration Management:** Centralized configuration management
- **Error Handling:** Comprehensive error handling
- **Logging:** Advanced logging system

### Important Notes:
- **One Elevator per Building:** The system supports one elevator per building (not multiple elevators)
- **SCAN Algorithm:** Basic SCAN algorithm implementation for elevator movement management
- **User Authentication:** Authentication system with JWT tokens
- **Real-time Communication:** Using SignalR for real-time updates

The system is ready for use and suitable for future extensions!













