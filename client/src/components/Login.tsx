import { useState } from 'react';
import { authService, type UserProfile } from '../services/authService';

interface LoginProps {
  onLoginSuccess: (userProfile: UserProfile) => void;
  onPendingApproval?: (email: string) => void;
}

export default function Login({ onLoginSuccess }: LoginProps) {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const [resetLoading, setResetLoading] = useState(false);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const [infoMsg, setInfoMsg] = useState<string | null>(null);
  const [showForgotModal, setShowForgotModal] = useState(false);
  const [forgotEmail, setForgotEmail] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setErrorMsg(null);
    setInfoMsg(null);

    try {
      const response = await authService.login(email.trim(), password);
      onLoginSuccess(response.user);
    } catch (err: any) {
      setErrorMsg(err.message || 'A apărut o eroare la autentificare. Verificați datele introduse.');
    } finally {
      setLoading(false);
    }
  };

  const handleSendResetLink = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!forgotEmail) return;
    setResetLoading(true);
    setErrorMsg(null);
    setInfoMsg(null);

    try {
      const res = await authService.forgotPassword(forgotEmail.trim());
      setInfoMsg(res.message || `Un email cu link-ul pentru setarea parolei a fost trimis la adresa ${forgotEmail}.`);
      setShowForgotModal(false);
      setForgotEmail('');
    } catch (err: any) {
      setErrorMsg(err.message || 'Nu s-a putut trimite link-ul de resetare a parolei.');
    } finally {
      setResetLoading(false);
    }
  };

  return (
    <div className="auth-card-container">
      <div className="auth-card glassmorphism">
        <div className="auth-header">
          <div className="brand-logo">
            <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
              <path d="M12 2L2 7L12 12L22 7L12 2Z" stroke="var(--color-lobster-pink)" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
              <path d="M2 17L12 22L22 17" stroke="var(--color-lobster-pink)" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
              <path d="M2 12L12 17L22 12" stroke="var(--color-lobster-pink)" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
            </svg>
          </div>
          <h2>Autentifică-te în aplicaţie</h2>
          <p className="subtitle">Introdu datele pentru a accesa aplicaţia</p>
        </div>

        {errorMsg && (
          <div className="alert alert-danger fade-in">
            <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" strokeWidth="2">
              <circle cx="12" cy="12" r="10"></circle>
              <line x1="12" y1="8" x2="12" y2="12"></line>
              <line x1="12" y1="16" x2="12.01" y2="16"></line>
            </svg>
            <span>{errorMsg}</span>
          </div>
        )}

        {infoMsg && (
          <div className="alert alert-success fade-in" style={{ backgroundColor: 'rgba(16, 185, 129, 0.15)', borderColor: '#10b981', color: '#10b981', padding: '12px 16px', borderRadius: '8px', marginBottom: '16px', display: 'flex', alignItems: 'center', gap: '8px' }}>
            <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" strokeWidth="2">
              <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path>
              <polyline points="22 4 12 14.01 9 11.01"></polyline>
            </svg>
            <span>{infoMsg}</span>
          </div>
        )}

        <form onSubmit={handleSubmit} className="auth-form">
          <div className="form-group">
            <label htmlFor="email">Email</label>
            <div className="input-wrapper">
              <input
                id="email"
                type="email"
                placeholder="name@company.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
                disabled={loading}
              />
              <span className="input-icon">
                <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2">
                  <path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z"></path>
                  <polyline points="22,6 12,13 2,6"></polyline>
                </svg>
              </span>
            </div>
          </div>

          <div className="form-group">
            <div className="label-row" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <label htmlFor="password">Parolă</label>
              <button 
                type="button" 
                onClick={() => {
                  setForgotEmail(email);
                  setShowForgotModal(true);
                }} 
                className="btn-link"
                style={{ fontSize: '0.85rem' }}
              >
                Setare / Resetare parolă?
              </button>
            </div>
            <div className="input-wrapper">
              <input
                id="password"
                type="password"
                placeholder="••••••••"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
                disabled={loading}
              />
              <span className="input-icon">
                <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2">
                  <rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect>
                  <path d="M7 11V7a5 5 0 0 1 10 0v4"></path>
                </svg>
              </span>
            </div>
          </div>

          <button type="submit" className="btn btn-primary" disabled={loading}>
            {loading ? (
              <span className="spinner-wrapper">
                <span className="spinner"></span>
                <span>Conectare...</span>
              </span>
            ) : (
              'Conectează-te'
            )}
          </button>
        </form>

        {showForgotModal && (
          <div className="modal-backdrop fade-in" style={{ position: 'fixed', top: 0, left: 0, right: 0, bottom: 0, backgroundColor: 'rgba(0,0,0,0.6)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000 }}>
            <div className="auth-card glassmorphism" style={{ maxWidth: '400px', width: '90%', margin: 0 }}>
              <h3>Setare sau Resetare Parolă</h3>
              <p className="subtitle" style={{ fontSize: '0.9rem', marginBottom: '16px' }}>
                Introdu adresa ta de email pentru a primi link-ul de setare a parolei.
              </p>
              <form onSubmit={handleSendResetLink}>
                <div className="form-group">
                  <label htmlFor="forgot-email">Email</label>
                  <input
                    id="forgot-email"
                    type="email"
                    placeholder="name@company.com"
                    value={forgotEmail}
                    onChange={(e) => setForgotEmail(e.target.value)}
                    required
                    disabled={resetLoading}
                  />
                </div>
                <div style={{ display: 'flex', gap: '10px', marginTop: '20px' }}>
                  <button type="button" onClick={() => setShowForgotModal(false)} className="btn" style={{ background: 'transparent', border: '1px solid rgba(255,255,255,0.2)', color: 'white', flex: 1 }}>
                    Anulează
                  </button>
                  <button type="submit" className="btn btn-primary" disabled={resetLoading} style={{ flex: 1 }}>
                    {resetLoading ? 'Trimitere...' : 'Trimite Link'}
                  </button>
                </div>
              </form>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
