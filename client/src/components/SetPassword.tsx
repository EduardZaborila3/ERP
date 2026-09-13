import { useState } from 'react';
import { authService, type UserProfile } from '../services/authService';

interface SetPasswordProps {
  onPasswordSetSuccess: (profile?: UserProfile) => void;
  onCancel?: () => void;
}

export default function SetPassword({ onPasswordSetSuccess, onCancel }: SetPasswordProps) {
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const [successMsg, setSuccessMsg] = useState<string | null>(null);

  // Parse URL parameters for target email and reset token
  const hashStr = window.location.hash || '';
  const searchParams = new URLSearchParams(hashStr.includes('?') ? hashStr.split('?')[1] : hashStr.replace(/^#/, ''));
  const emailParam = searchParams.get('email') || '';
  const tokenParam = searchParams.get('token') || searchParams.get('t') || '';

  const [inputEmail, setInputEmail] = useState(emailParam);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMsg(null);
    setSuccessMsg(null);

    const emailToUse = (inputEmail || emailParam).trim();
    if (!emailToUse) {
      setErrorMsg('Adresa de email este obligatorie.');
      return;
    }

    if (!tokenParam) {
      setErrorMsg('Token-ul de resetare lipsește din link-ul primit. Vă rugăm să folosiți link-ul complet din email.');
      return;
    }

    if (password.length < 6) {
      setErrorMsg('Parola trebuie să aibă cel puţin 6 caractere.');
      return;
    }

    if (password !== confirmPassword) {
      setErrorMsg('Parolele introduse nu se potrivesc.');
      return;
    }

    setLoading(true);

    try {
      const response = await authService.setPassword(emailToUse, tokenParam, password);
      setSuccessMsg(response.message || 'Parola a fost setată cu succes! Contul a fost activat.');
      
      // Clean hash from URL
      window.location.hash = '';

      setTimeout(() => {
        onPasswordSetSuccess(response.user);
      }, 1500);
    } catch (err: any) {
      setErrorMsg(err.message || 'A apărut o eroare la salvarea parolei.');
    } finally {
      setLoading(false);
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
          <h2>Setează parola contului</h2>
          <p className="subtitle">Introdu noua ta parolă pentru a-ţi activa contul</p>
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

        {successMsg && (
          <div className="alert alert-success fade-in" style={{ backgroundColor: 'rgba(16, 185, 129, 0.15)', borderColor: '#10b981', color: '#10b981', padding: '12px 16px', borderRadius: '8px', marginBottom: '16px', display: 'flex', alignItems: 'center', gap: '8px' }}>
            <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" strokeWidth="2">
              <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path>
              <polyline points="22 4 12 14.01 9 11.01"></polyline>
            </svg>
            <span>{successMsg}</span>
          </div>
        )}

        <form onSubmit={handleSubmit} className="auth-form">
          {!emailParam && (
            <div className="form-group">
              <label htmlFor="user-email">Adresă Email</label>
              <div className="input-wrapper">
                <input
                  id="user-email"
                  type="email"
                  placeholder="name@company.com"
                  value={inputEmail}
                  onChange={(e) => setInputEmail(e.target.value)}
                  required
                  disabled={loading}
                />
              </div>
            </div>
          )}

          <div className="form-group">
            <label htmlFor="new-password">Noua Parolă</label>
            <div className="input-wrapper">
              <input
                id="new-password"
                type="password"
                placeholder="Minimum 6 caractere"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                minLength={6}
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

          <div className="form-group">
            <label htmlFor="confirm-password">Confirmă Parola</label>
            <div className="input-wrapper">
              <input
                id="confirm-password"
                type="password"
                placeholder="Reintrodu parola"
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                minLength={6}
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
                <span>Se salvează...</span>
              </span>
            ) : (
              'Salvează Parola'
            )}
          </button>
        </form>

        {onCancel && (
          <div className="auth-footer" style={{ marginTop: '16px' }}>
            <button type="button" onClick={onCancel} className="btn-link">
              Înapoi la autentificare
            </button>
          </div>
        )}
      </div>
    </div>
  );
}
