# Elevator Simulation Client

A React-based client application for the Elevator Simulation system.

## Features

- User authentication (register/login)
- Building management
- Real-time elevator simulation with SignalR
- Interactive floor buttons
- Elevator status visualization

## How to Use

1. **Register / Login:**
   - On the main screen, you can register with an email address and password (password must be at least 6 characters).
   - If you already have an account, log in with your email and password.

2. **Building Management:**
   - After logging in, you will see a page with all your buildings.
   - Add a new building by clicking "Add Building", entering a name and number of floors (within the allowed range), and saving.
   - You cannot create two buildings with the same name.

3. **Enter Simulation:**
   - Click on a building card to enter the elevator simulation for that building.

4. **Using the Simulation:**
   - View the elevator status (current floor, direction, door state).
   - Press the floor buttons to call the elevator.
   - When the elevator arrives, you can select a destination floor (if required).

5. **Logout:**
   - You can log out via the top menu (if available).

## Setup

1. Install dependencies:
```bash
npm install
```

2. Make sure the server is running on `https://localhost:7001`

3. Start the development server:
```bash
npm run dev
```

The application will be available at `http://localhost:3000`

## Technologies Used

- React 18
- React Router DOM
- Microsoft SignalR
- Vite
- Modern CSS with Flexbox and Grid 