// API Service - handles all HTTP requests to the backend server
// Provides authentication, building management, and elevator call services

import { API_CONFIG } from '../config/config.js';

// Get default headers for API requests
const getAuthHeaders = () => {
  return {
    'Content-Type': 'application/json'
  };
};

// Handle API response and throw errors if needed
const handleResponse = async (response) => {
  if (!response.ok) {
    const error = await response.json().catch(() => ({ message: 'An error occurred' }));
    throw new Error(error.message || 'Request failed');
  }
  return response.json();
};

// Authentication service for user registration and login
export const authService = {
  // Register new user with email and password
  async register(email, password) {
    const response = await fetch(`${API_CONFIG.BASE_URL}/auth/register`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password })
    });
    return handleResponse(response);
  },

  // Login existing user with email and password
  async login(email, password) {
    const response = await fetch(`${API_CONFIG.BASE_URL}/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password })
    });
    return handleResponse(response);
  }
};

// Building management service for CRUD operations
export const buildingService = {
  // Get all buildings for a specific user
  async getUserBuildings(userId) {
    const response = await fetch(`${API_CONFIG.BASE_URL}/buildings/get/buildings/user/${userId}`, {
      headers: getAuthHeaders()
    });
    return handleResponse(response);
  },

  // Get specific building details by ID and user
  async getBuilding(buildingId, userId) {
    const response = await fetch(`${API_CONFIG.BASE_URL}/buildings/get/${buildingId}/user/${userId}`, {
      headers: getAuthHeaders()
    });
    return handleResponse(response);
  },

  // Create new building with name, floors count, and user ID
  async createBuilding(name, numberOfFloors, userId) {
    const response = await fetch(`${API_CONFIG.BASE_URL}/buildings/create/user/${userId}`, {
      method: 'POST',
      headers: getAuthHeaders(),
      body: JSON.stringify({ name, numberOfFloors })
    });
    return handleResponse(response);
  }
};

// Elevator call service for managing elevator requests
export const elevatorCallService = {
  // Create new elevator call with building, floor, and optional destination
  async createCall(buildingId, requestedFloor, destinationFloor = null) {
    const requestBody = { buildingId, requestedFloor, destinationFloor };
    console.log('Sending elevator call request:', requestBody);
    const response = await fetch(`${API_CONFIG.BASE_URL}/elevatorcalls/create`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(requestBody)
    });
    return handleResponse(response);
  },

  // Update existing call with destination floor
  async updateCall(callId, destinationFloor) {
    const response = await fetch(`${API_CONFIG.BASE_URL}/elevatorcalls/update/${callId}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ destinationFloor })
    });
    return handleResponse(response);
  },

  // Get all calls for a specific building
  async getBuildingCalls(buildingId) {
    const response = await fetch(`${API_CONFIG.BASE_URL}/elevatorcalls/get/building/calls/${buildingId}`);
    return handleResponse(response);
  }
}; 