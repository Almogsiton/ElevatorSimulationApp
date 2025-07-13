# Elevator Simulation Client

A React-based client application for the Elevator Simulation system.

## Features

- User authentication (register/login)
- Building management
- Real-time elevator simulation with SignalR
- Interactive floor buttons
- Elevator status visualization

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

## Usage

1. Register a new account or login with existing credentials
2. Create buildings with specified number of floors
3. Click on a building to enter the simulation
4. Use the floor buttons to call the elevator
5. When the elevator arrives, select your destination floor

## Technologies Used

- React 18
- React Router DOM
- Microsoft SignalR
- Vite
- Modern CSS with Flexbox and Grid 