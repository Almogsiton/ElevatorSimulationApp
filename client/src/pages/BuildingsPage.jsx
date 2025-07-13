import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { buildingService } from '../services/apiService';
import { useAuth } from '../contexts/AuthContext';

const BuildingsPage = () => {
  const [buildings, setBuildings] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [newBuilding, setNewBuilding] = useState({ name: '', numberOfFloors: '1' });
  const [creating, setCreating] = useState(false);
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
      await buildingService.createBuilding(newBuilding.name, parseInt(newBuilding.numberOfFloors), user.userId);
      setNewBuilding({ name: '', numberOfFloors: '1' });
      setShowCreateForm(false);
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
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
          <h2>My Buildings</h2>
          <button 
            className="btn" 
            onClick={() => setShowCreateForm(!showCreateForm)}
          >
            {showCreateForm ? 'Cancel' : 'Add Building'}
          </button>
        </div>

        {error && <div className="error">{error}</div>}

        {showCreateForm && (
          <div className="card" style={{ marginBottom: '20px' }}>
            <h3>Create New Building</h3>
            <form onSubmit={handleCreateBuilding}>
              <div className="form-group">
                <label htmlFor="buildingName">Building Name</label>
                <input
                  type="text"
                  id="buildingName"
                  className="form-control"
                  value={newBuilding.name}
                  onChange={(e) => setNewBuilding({ ...newBuilding, name: e.target.value })}
                  required
                />
              </div>
              <div className="form-group">
                <label htmlFor="numberOfFloors">Number of Floors</label>
                <input
                  type="number"
                  id="numberOfFloors"
                  className="form-control"
                  min="1"
                  max="100"
                  value={newBuilding.numberOfFloors}
                  onChange={(e) => setNewBuilding({ ...newBuilding, numberOfFloors: e.target.value })}
                  required
                />
              </div>
              <button type="submit" className="btn" disabled={creating}>
                {creating ? 'Creating...' : 'Create Building'}
              </button>
            </form>
          </div>
        )}

        {buildings.length === 0 ? (
          <div className="card">
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