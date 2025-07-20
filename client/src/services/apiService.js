import { API_CONFIG } from '../config/config.js';

const getAuthHeaders = () => {
  return {
    'Content-Type': 'application/json'
  };
};

const handleResponse = async (response) => {
  if (!response.ok) {
    const error = await response.json().catch(() => ({ message: 'An error occurred' }));
    throw new Error(error.message || 'Request failed');
  }
  return response.json();
};

export const authService = {
  async register(email, password) {
    const response = await fetch(`${API_CONFIG.BASE_URL}/auth/register`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password })
    });
    return handleResponse(response);
  },

  async login(email, password) {
    const response = await fetch(`${API_CONFIG.BASE_URL}/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password })
    });
    return handleResponse(response);
  }
};

export const buildingService = {
  async getUserBuildings(userId) {
    const response = await fetch(`${API_CONFIG.BASE_URL}/buildings/get/buildings/user/${userId}`, {
      headers: getAuthHeaders()
    });
    return handleResponse(response);
  },

  async getBuilding(buildingId, userId) {
    const response = await fetch(`${API_CONFIG.BASE_URL}/buildings/get/${buildingId}/user/${userId}`, {
      headers: getAuthHeaders()
    });
    return handleResponse(response);
  },

  async createBuilding(name, numberOfFloors, userId) {
    const response = await fetch(`${API_CONFIG.BASE_URL}/buildings/create/user/${userId}`, {
      method: 'POST',
      headers: getAuthHeaders(),
      body: JSON.stringify({ name, numberOfFloors })
    });
    return handleResponse(response);
  }
};

export const elevatorCallService = {
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

  async updateCall(callId, destinationFloor) {
    const response = await fetch(`${API_CONFIG.BASE_URL}/elevatorcalls/update/${callId}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ destinationFloor })
    });
    return handleResponse(response);
  },

  async getBuildingCalls(buildingId) {
    const response = await fetch(`${API_CONFIG.BASE_URL}/elevatorcalls/get/building/calls/${buildingId}`);
    return handleResponse(response);
  }
}; 