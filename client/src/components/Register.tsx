import { useState, useEffect } from 'react';
import { authService } from '../services/authService';

interface Role {
  id: number;
  name: string;
}

interface RegisterProps {
  onRegisterSuccess: (email: string) => void;
  onNavigateToLogin: () => void;
}

export default function Register({ onRegisterSuccess, onNavigateToLogin }: RegisterProps) {
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [roleId, setRoleId] = useState<string>('2');
  const [roles, setRoles] = useState<Role[]>([]);
  const [loading, setLoading] = useState(false);
  const [fetchingRoles, setFetchingRoles] = useState(true);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  const apiUrl = import.meta.env.VITE_API_URL || 'http://localhost:5110/api';

  useEffect(() => {
    const fetchRoles = async () => {
      try {
        const response = await fetch(`${apiUrl}/roles`);
        if (!response.ok) {
          throw new Error('Failed to fetch user roles.');
        }
        const data = await response.json();
        setRoles(data);
        if (data.length > 0) {
          setRoleId(data[data.length - 1].id.toString());
        }
      } catch (err: any) {
        console.error('Error fetching roles:', err);
        setRoles([
          { id: 1, name: 'Admin' },
          { id: 2, name: 'Inginer' }
        ]);
        setRoleId('2');
      } finally {
        setFetchingRoles(false);
      }
    };

    fetchRoles();
  }, [apiUrl]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setErrorMsg(null);

    try {
      await authService.register({
        email: email.trim(),
        password,
        firstName: firstName.trim(),
        lastName: lastName.trim(),
        appRoleId: parseInt(roleId) || 2,
        jobTitleId: 1
      });

      onRegisterSuccess(email);
    } catch (err: any) {
      setErrorMsg(err.message || 'A apărut o eroare la crearea contului.');
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
          <h2>Creează-ţi un cont</h2>
          <p className="subtitle">Trimite o cerere pentru crearea contului</p>
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

        <form onSubmit={handleSubmit} className="auth-form">
          <div className="form-row">
            <div className="form-group col-6">
              <label htmlFor="firstName">Prenume</label>
              <input
                id="firstName"
                type="text"
                placeholder="Ion"
                value={firstName}
                onChange={(e) => setFirstName(e.target.value)}
                maxLength={25}
                required
                disabled={loading}
              />
            </div>
            <div className="form-group col-6">
              <label htmlFor="lastName">Nume de familie</label>
              <input
                id="lastName"
                type="text"
                placeholder="Popescu"
                value={lastName}
                onChange={(e) => setLastName(e.target.value)}
                maxLength={25}
                required
                disabled={loading}
              />
            </div>
          </div>

          <div className="form-group">
            <label htmlFor="email">Email</label>
            <div className="input-wrapper">
              <input
                id="email"
                type="email"
                placeholder="nume@companie.com"
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
            <label htmlFor="password">Parolă</label>
            <div className="input-wrapper">
              <input
                id="password"
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
            <label htmlFor="role">Rol</label>
            <div className="input-wrapper">
              <select
                id="role"
                value={roleId}
                onChange={(e) => setRoleId(e.target.value)}
                required
                disabled={loading || fetchingRoles}
              >
                {fetchingRoles ? (
                  <option value="">Loading roles...</option>
                ) : (
                  roles.map((role) => (
                    <option key={role.id} value={role.id}>
                      {role.name}
                    </option>
                  ))
                )}
              </select>
              <span className="input-icon">
                <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2">
                  <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"></path>
                  <circle cx="9" cy="7" r="4"></circle>
                  <path d="M23 21v-2a4 4 0 0 0-3-3.87"></path>
                  <path d="M16 3.13a4 4 0 0 1 0 7.75"></path>
                </svg>
              </span>
            </div>
          </div>

          <button type="submit" className="btn btn-primary" disabled={loading}>
            {loading ? (
              <span className="spinner-wrapper">
                <span className="spinner"></span>
                <span>Înregistrare...</span>
              </span>
            ) : (
              'Înregistrează-te'
            )}
          </button>
        </form>

        <div className="auth-footer">
          <p>
            Ai deja un cont?{' '}
            <button type="button" onClick={onNavigateToLogin} className="btn-link">
              Autentifică-te
            </button>
          </p>
        </div>
      </div>
    </div>
  );
}
