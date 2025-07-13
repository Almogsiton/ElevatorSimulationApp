import React from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';

const Navbar = () => {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  if (!user) return null;

  return (
    <nav className="navbar">
      <div className="navbar-content">
        <div className="navbar-brand">
          <Link to="/buildings" className="nav-link">Elevator Simulation</Link>
        </div>
        <div className="navbar-nav">
          <Link to="/buildings" className="nav-link">Buildings</Link>
          <span className="nav-link">{user.email}</span>
          <button onClick={handleLogout} className="btn btn-secondary">Logout</button>
        </div>
      </div>
    </nav>
  );
};

export default Navbar; 