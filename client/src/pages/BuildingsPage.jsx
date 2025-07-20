// דף הבניינים - מציג את רשימת הבניינים של המשתמש ומאפשר יצירת בניינים חדשים
// Buildings page - displays user's buildings list and allows creating new buildings

import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { buildingService } from '../services/apiService';
import { useAuth } from '../contexts/AuthContext';
import { BUILDING_CONFIG } from '../config/config';
import '../styles/BuildingsPage.css';

const BuildingsPage = () => {
  const [buildings, setBuildings] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [newBuilding, setNewBuilding] = useState({ name: '', numberOfFloors: BUILDING_CONFIG.MIN_FLOORS.toString() });
  const [creating, setCreating] = useState(false);
  const [nameError, setNameError] = useState('');
  const { user } = useAuth();
  const navigate = useNavigate();

  useEffect(() => {
    loadBuildings();
  }, []);

  const loadBuildings = async () => {
    try {
      const data = await buildingService.getUserBuildings(user.userId);
      setBuildings(data);
    } catch (error) {
      setError(error.message);
    } finally {
      setLoading(false);
    }
  };

  const handleCreateBuilding = async (e) => {
    e.preventDefault();
    setCreating(true);

    try {
      // בדוק אם מספר הקומות בטווח הנכון
      const floors = parseInt(newBuilding.numberOfFloors);
      if (floors < BUILDING_CONFIG.MIN_FLOORS || floors > BUILDING_CONFIG.MAX_FLOORS) {
        setError(`Number of floors must be between ${BUILDING_CONFIG.MIN_FLOORS} and ${BUILDING_CONFIG.MAX_FLOORS}`);
        setCreating(false);
        return;
      }

      // בדוק אם כבר קיים בניין עם אותו שם
      const existingBuilding = buildings.find(building => 
        building.name.toLowerCase().trim() === newBuilding.name.toLowerCase().trim()
      );
      
      if (existingBuilding) {
        setError('A building with this name already exists. Please choose a different name.');
        setCreating(false);
        return;
      }

      await buildingService.createBuilding(newBuilding.name, parseInt(newBuilding.numberOfFloors), user.userId);
      setNewBuilding({ name: '', numberOfFloors: '1' });
      setShowCreateForm(false);
      setError(''); // נקה שגיאות קודמות
      await loadBuildings();
    } catch (error) {
      setError(error.message);
    } finally {
      setCreating(false);
    }
  };

  const handleBuildingClick = (buildingId) => {
    navigate(`/building/${buildingId}`);
  };

  const handleNameChange = (e) => {
    const name = e.target.value;
    setNewBuilding({ ...newBuilding, name });
    
    // בדוק אם השם כבר קיים
    if (name.trim()) {
      const existingBuilding = buildings.find(building => 
        building.name.toLowerCase().trim() === name.toLowerCase().trim()
      );
      
      if (existingBuilding) {
        setNameError('A building with this name already exists');
      } else {
        setNameError('');
      }
    } else {
      setNameError('');
    }
  };

  if (loading) {
    return (
      <div className="container">
        <div className="card">
          <p>Loading buildings...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="container">
      <div className="card">
        <div className="buildings-header">
          <h2>My Buildings</h2>
          <button 
            className="btn" 
            onClick={() => {
              setShowCreateForm(!showCreateForm);
              if (showCreateForm) {
                // אם סוגרים את הטופס, נקה שגיאות ושדות
                setNewBuilding({ name: '', numberOfFloors: BUILDING_CONFIG.MIN_FLOORS.toString() });
                setNameError('');
                setError('');
              }
            }}
          >
            {showCreateForm ? 'Cancel' : 'Add Building'}
          </button>
        </div>

        {error && <div className="error">{error}</div>}

        {showCreateForm && (
          <div className="card create-form-container">
            <h3>Create New Building</h3>
            <form onSubmit={handleCreateBuilding}>
              <div className="form-group">
                <label htmlFor="buildingName">Building Name</label>
                <input
                  type="text"
                  id="buildingName"
                  className={`form-control ${nameError ? 'error' : ''}`}
                  value={newBuilding.name}
                  onChange={handleNameChange}
                  required
                />
                {nameError && <div className="error-message">{nameError}</div>}
              </div>
              <div className="form-group">
                <label htmlFor="numberOfFloors">Number of Floors</label>
                <input
                  type="number"
                  id="numberOfFloors"
                  className="form-control"
                  min={BUILDING_CONFIG.MIN_FLOORS}
                  max={BUILDING_CONFIG.MAX_FLOORS}
                  value={newBuilding.numberOfFloors}
                  onChange={(e) => setNewBuilding({ ...newBuilding, numberOfFloors: e.target.value })}
                  required
                />
                <small className="form-help-text">
                  Minimum: {BUILDING_CONFIG.MIN_FLOORS}, Maximum: {BUILDING_CONFIG.MAX_FLOORS}
                </small>
              </div>
              <button type="submit" className="btn" disabled={creating || nameError || !newBuilding.name.trim()}>
                {creating ? 'Creating...' : 'Create Building'}
              </button>
            </form>
          </div>
        )}

        {buildings.length === 0 ? (
          <div className="card empty-state">
            <p>No buildings found. Create your first building to get started!</p>
          </div>
        ) : (
          <div className="building-grid">
            {buildings.map((building) => (
              <div
                key={building.id}
                className="building-card"
                onClick={() => handleBuildingClick(building.id)}
              >
                <h3>{building.name}</h3>
                <p>{building.numberOfFloors} floors</p>
                <button className="btn btn-secondary">View Building</button>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
};

export default BuildingsPage; 