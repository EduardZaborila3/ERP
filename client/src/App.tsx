import { useState, useEffect } from 'react';
import Login from './components/Login';
import SetPassword from './components/SetPassword';
import PendingApproval from './components/PendingApproval';
import Dashboard from './components/Dashboard';
import { authService, type UserProfile } from './services/authService';
import './App.css';

type AuthView = 'loading' | 'login' | 'setPassword' | 'pending' | 'dashboard';

function App() {
  const [currentView, setCurrentView] = useState<AuthView>('loading');
  const [userProfile, setUserProfile] = useState<UserProfile | null>(null);
  const [pendingEmail, setPendingEmail] = useState('');

  useEffect(() => {
    // Check hash for recovery / set-password link on startup
    const checkHashAndSession = async () => {
      const hash = window.location.hash || '';
      if (hash.includes('type=recovery') || hash.includes('token=')) {
        setCurrentView('setPassword');
        return;
      }

      // Check active session via .NET JWT token
      const user = await authService.getMe();
      if (user) {
        setUserProfile(user);
        setCurrentView('dashboard');
      } else {
        setCurrentView('login');
      }
    };

    checkHashAndSession();

    // Listen to hash changes (e.g. user clicks recovery link while tab is open)
    const handleHashChange = () => {
      if (window.location.hash.includes('type=recovery') || window.location.hash.includes('token=')) {
        setCurrentView('setPassword');
      }
    };

    window.addEventListener('hashchange', handleHashChange);
    return () => window.removeEventListener('hashchange', handleHashChange);
  }, []);

  const handleLoginSuccess = (profile: UserProfile) => {
    setUserProfile(profile);
    setCurrentView('dashboard');
  };

  const handlePendingApproval = (email: string) => {
    setPendingEmail(email);
    setCurrentView('pending');
  };

  const handleLogout = () => {
    authService.logout();
    setUserProfile(null);
    setCurrentView('login');
  };

  if (currentView === 'loading') {
    return (
      <div className="loading-screen">
        <div className="spinner-container">
          <div className="spinner large"></div>
          <p>Verifying session security...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="app-layout">
      {/* Background design elements */}
      <div className="bg-glow bg-glow-left"></div>
      <div className="bg-glow bg-glow-right"></div>
      
      <div className={`content-container ${currentView === 'dashboard' ? 'dashboard-view' : ''}`}>
        {currentView === 'login' && (
          <Login 
            onLoginSuccess={handleLoginSuccess}
            onPendingApproval={handlePendingApproval}
          />
        )}
        
        {currentView === 'setPassword' && (
          <SetPassword 
            onPasswordSetSuccess={(profile) => {
              if (profile) {
                setUserProfile(profile);
                setCurrentView('dashboard');
              } else {
                setCurrentView('login');
              }
            }}
            onCancel={() => {
              window.location.hash = '';
              setCurrentView('login');
            }}
          />
        )}

        {currentView === 'pending' && (
          <PendingApproval 
            email={pendingEmail} 
            onBackToLogin={() => setCurrentView('login')}
          />
        )}

        {currentView === 'dashboard' && userProfile && (
          <Dashboard 
            userProfile={userProfile} 
            onLogout={handleLogout}
          />
        )}
      </div>
    </div>
  );
}

export default App;
