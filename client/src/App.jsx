import React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider, useAuth } from './contexts/AuthContext';
import Navbar from './components/Navbar';
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import BuildingsPage from './pages/BuildingsPage';
import BuildingSimulationPage from './pages/BuildingSimulationPage';

const PrivateRoute = ({ children }) => {
  const { user, loading } = useAuth();
  
  if (loading) {
    return <div>Loading...</div>;
  }
  
  return user ? children : <Navigate to="/login" />;
};

const AppContent = () => {
  return (
    <Router>
      <Navbar />
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
        <Route 
          path="/buildings" 
          element={
            <PrivateRoute>
              <BuildingsPage />
            </PrivateRoute>
          } 
        />
        <Route 
          path="/building/:buildingId" 
          element={
            <PrivateRoute>
              <BuildingSimulationPage />
            </PrivateRoute>
          } 
        />
        <Route path="/" element={<Navigate to="/buildings" />} />
      </Routes>
    </Router>
  );
};

const App = () => {
  return (
    <AuthProvider>
      <AppContent />
    </AuthProvider>
  );
};

export default App; 