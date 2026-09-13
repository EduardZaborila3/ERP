import { useEffect, useState } from 'react';
import { authService, type UserProfile } from '../services/authService';
import JobTitleForm from './JobTitleForm';
import TaskManager from './TaskManager/TaskManager';

interface JobTitle {
  id: number;
  name: string;
}

interface SystemUser {
  id: number;
  email: string;
  firstName: string;
  lastName: string;
  roleId?: number;
  appRoleId?: number;
  jobTitleId?: number;
  isApproved?: boolean;
  isActive?: boolean;
}

interface DashboardProps {
  userProfile: UserProfile;
  onLogout: () => void;
}

export default function Dashboard({ userProfile, onLogout }: DashboardProps) {
  const [jobTitles, setJobTitles] = useState<JobTitle[]>([]);
  const [users, setUsers] = useState<SystemUser[]>([]);
  const [loadingUsers, setLoadingUsers] = useState<boolean>(false);
  
  // Admin form state
  const [newFirstName, setNewFirstName] = useState('');
  const [newLastName, setNewLastName] = useState('');
  const [newEmail, setNewEmail] = useState('');
  const [newJobTitleId, setNewJobTitleId] = useState<string>('');
  const [creatingUser, setCreatingUser] = useState(false);
  const [adminError, setAdminError] = useState<string | null>(null);
  const [adminSuccess, setAdminSuccess] = useState<string | null>(null);
  
  const [activeTab, setActiveTab] = useState<'profile' | 'tasks' | 'users' | 'job-titles'>('tasks');

  const apiUrl = import.meta.env.VITE_API_URL || 'http://localhost:5110/api';
  const currentRoleId = userProfile.appRoleId ?? userProfile.roleId ?? 1;
  const isAdmin = currentRoleId === 1;

  const fetchJobTitles = async () => {
    try {
      const response = await authService.authFetch(`${apiUrl}/job-titles`);
      if (response.ok) {
        const data: JobTitle[] = await response.json();
        setJobTitles(data);
        if (data.length > 0) {
          setNewJobTitleId((prev) => {
            const exists = data.some((jt) => jt.id.toString() === prev);
            return exists ? prev : data[0].id.toString();
          });
        }
      }
    } catch (err) {
      console.error('Error fetching job titles list:', err);
    }
  };

  const fetchUsers = async () => {
    setLoadingUsers(true);
    try {
      const response = await authService.authFetch(`${apiUrl}/users`);
      if (response.ok) {
        const data = await response.json();
        setUsers(data);
      }
    } catch (err) {
      console.error('Error fetching users list:', err);
    } finally {
      setLoadingUsers(false);
    }
  };

  const handleJobTitleCreated = (newJobTitle: JobTitle) => {
    setJobTitles((prev) => {
      if (prev.some((jt) => jt.id === newJobTitle.id)) return prev;
      return [...prev, newJobTitle];
    });
    setNewJobTitleId(newJobTitle.id.toString());
  };

  const handleJobTitleDeleted = (deletedId: number) => {
    setJobTitles((prev) => {
      const updated = prev.filter((jt) => jt.id !== deletedId);
      if (newJobTitleId === deletedId.toString() && updated.length > 0) {
        setNewJobTitleId(updated[0].id.toString());
      }
      return updated;
    });
  };

  useEffect(() => {
    fetchJobTitles();
    if (activeTab === 'users') {
      fetchUsers();
    }
  }, [apiUrl, activeTab]);

  const handleLogoutClick = () => {
    authService.logout();
    onLogout();
  };

  const handleCreateUser = async (e: React.FormEvent) => {
    e.preventDefault();
    setCreatingUser(true);
    setAdminError(null);
    setAdminSuccess(null);

    try {
      const jobTitleNum = parseInt(newJobTitleId || '1');
      const defaultAppRoleId = 2; // Standard user role

      // 1. Create user in ERP database
      const userResponse = await authService.authFetch(`${apiUrl}/users`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          email: newEmail.trim(),
          firstName: newFirstName.trim(),
          lastName: newLastName.trim(),
          appRoleId: defaultAppRoleId,
          jobTitleId: jobTitleNum,
          isActive: true
        })
      });

      if (!userResponse.ok) {
        const errText = await userResponse.text();
        throw new Error(errText || 'Eroare la crearea utilizatorului.');
      }

      // 2. Send invitation email with password set token
      const inviteResponse = await authService.authFetch(`${apiUrl}/users/invite-email`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          email: newEmail.trim(),
          firstName: newFirstName.trim(),
          lastName: newLastName.trim(),
          redirectUrl: window.location.origin
        })
      });

      if (!inviteResponse.ok) {
        const errText = await inviteResponse.text();
        console.warn('Eroare la trimiterea email-ului de invitație:', errText);
      }

      setAdminSuccess(
        `Contul a fost creat și email-ul pentru setarea parolei a fost trimis cu succes la adresa ${newEmail}!`
      );

      setNewFirstName('');
      setNewLastName('');
      setNewEmail('');

      fetchUsers();
    } catch (err: any) {
      console.error('Error creating user/sending email:', err);
      setAdminError(err.message || 'A apărut o eroare la crearea utilizatorului.');
    } finally {
      setCreatingUser(false);
    }
  };

  const handleDeleteUser = async (userId: number, userName: string) => {
    if (!window.confirm(`Sigur doriți să ștergeți utilizatorul ${userName}?`)) {
      return;
    }
    try {
      const response = await authService.authFetch(`${apiUrl}/users/${userId}`, {
        method: 'DELETE'
      });
      if (response.ok) {
        fetchUsers();
      } else {
        alert('Nu s-a putut șterge utilizatorul.');
      }
    } catch (err) {
      console.error('Eroare la ștergerea utilizatorului:', err);
    }
  };

  const getJobTitleLabel = (jobTitleId?: number) => {
    if (!jobTitleId) return 'Nespecificat';
    const jt = jobTitles.find((j) => j.id === jobTitleId);
    return jt ? jt.name : `Funcţie ${jobTitleId}`;
  };

  const getPageTitle = () => {
    switch (activeTab) {
      case 'profile':
        return 'Profilul Meu';
      case 'tasks':
        return 'Task Manager';
      case 'users':
        return 'Gestiune Utilizatori';
      case 'job-titles':
        return 'Gestiune Funcţii';
      default:
        return 'Dashboard';
    }
  };

  return (
    <div className="app-shell fade-in">
      {/* Left Sidebar Navigation */}
      <aside className="sidebar">
        <div>
          <div className="sidebar-header">
            <div className="brand-icon">
              <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path d="M12 2L2 7L12 12L22 7L12 2Z" stroke="var(--color-primary)" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
                <path d="M2 17L12 22L22 17" stroke="var(--color-primary)" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
                <path d="M2 12L12 17L22 12" stroke="var(--color-primary)" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
              </svg>
            </div>
            <h2>Enterprise ERP</h2>
          </div>

          <nav className="sidebar-nav">
            <button
              type="button"
              onClick={() => setActiveTab('profile')}
              className={`sidebar-nav-item ${activeTab === 'profile' ? 'active' : ''}`}
            >
              <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" strokeWidth="2">
                <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path>
                <circle cx="12" cy="7" r="4"></circle>
              </svg>
              <span>Profilul meu</span>
            </button>

            <button
              type="button"
              onClick={() => setActiveTab('tasks')}
              className={`sidebar-nav-item ${activeTab === 'tasks' ? 'active' : ''}`}
            >
              <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" strokeWidth="2">
                <path d="M9 11l3 3L22 4"></path>
                <path d="M21 12v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11"></path>
              </svg>
              <span>Task Manager</span>
            </button>

            <button
              type="button"
              onClick={() => setActiveTab('users')}
              className={`sidebar-nav-item ${activeTab === 'users' ? 'active' : ''}`}
            >
              <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" strokeWidth="2">
                <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"></path>
                <circle cx="9" cy="7" r="4"></circle>
                <path d="M23 21v-2a4 4 0 0 0-3-3.87"></path>
                <path d="M16 3.13a4 4 0 0 1 0 7.75"></path>
              </svg>
              <span>Gestiune Utilizatori</span>
            </button>

            <button
              type="button"
              onClick={() => setActiveTab('job-titles')}
              className={`sidebar-nav-item ${activeTab === 'job-titles' ? 'active' : ''}`}
            >
              <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" strokeWidth="2">
                <rect x="2" y="7" width="20" height="14" rx="2" ry="2"></rect>
                <path d="M16 21V5a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v16"></path>
              </svg>
              <span>Gestiune Funcţii</span>
            </button>
          </nav>
        </div>

        <div className="sidebar-footer">
          <div className="sidebar-user-card">
            <div className="avatar">
              {userProfile.firstName?.[0] || 'U'}
              {userProfile.lastName?.[0] || ''}
            </div>
            <div className="user-info-text" style={{ flex: 1, overflow: 'hidden' }}>
              <span className="username" style={{ textOverflow: 'ellipsis', overflow: 'hidden', whiteSpace: 'nowrap' }}>
                {userProfile.firstName} {userProfile.lastName}
              </span>
            </div>
          </div>

          <button type="button" onClick={handleLogoutClick} className="btn-logout" style={{ width: '100%', justifyContent: 'center' }}>
            <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2">
              <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"></path>
              <polyline points="16 17 21 12 16 7"></polyline>
              <line x1="21" y1="12" x2="9" y2="12"></line>
            </svg>
            <span>Log Out</span>
          </button>
        </div>
      </aside>

      {/* Main Content View Area */}
      <main className="main-content">
        <header className="main-header">
          <h1>{getPageTitle()}</h1>
        </header>

        {activeTab === 'profile' && (
          <div className="dashboard-grid">
            <div className="dashboard-card glassmorphism">
              <h3>Profil Utilizator</h3>
              <div className="profile-details-list">
                <div className="detail-item">
                  <span className="detail-label">Email</span>
                  <span className="detail-value">{userProfile.email}</span>
                </div>
                <div className="detail-item">
                  <span className="detail-label">Nume Complet</span>
                  <span className="detail-value">{userProfile.firstName} {userProfile.lastName}</span>
                </div>
                <div className="detail-item">
                  <span className="detail-label">Funcţie</span>
                  <span className="detail-value-badge">{getJobTitleLabel(userProfile.jobTitleId)}</span>
                </div>
              </div>
            </div>

            <div className="dashboard-card glassmorphism">
              <h3>Stare Sistem ERP</h3>
              <div className="metrics-grid">
                <div className="metric-box">
                  <span className="metric-num">Activ</span>
                  <span className="metric-title">Stare Cont</span>
                </div>
                <div className="metric-box">
                  <span className="metric-num">100%</span>
                  <span className="metric-title">Conexiune API</span>
                </div>
              </div>
              <p className="card-hint">
                Aplicaţia client este conectată securizat la API-ul .NET pe portul 5110.
              </p>
            </div>
          </div>
        )}

        {activeTab === 'users' && (
          <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
            {isAdmin && (
              <div className="dashboard-card glassmorphism" style={{ padding: '28px' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '12px', marginBottom: '20px' }}>
                  <div style={{ background: 'var(--color-primary-light)', padding: '10px', borderRadius: '10px', color: 'var(--color-primary)', display: 'flex' }}>
                    <svg viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="currentColor" strokeWidth="2">
                      <path d="M16 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"></path>
                      <circle cx="8.5" cy="7" r="4"></circle>
                      <line x1="20" y1="8" x2="20" y2="14"></line>
                      <line x1="17" y1="11" x2="23" y2="11"></line>
                    </svg>
                  </div>
                  <div>
                    <h3 style={{ margin: 0 }}>Creează un cont nou de utilizator</h3>
                    <p style={{ margin: '4px 0 0 0', color: 'var(--color-text-muted)', fontSize: '0.9rem' }}>
                      Introduceţi datele noului membru. Parola va fi setată personal de către utilizator prin link-ul trimis pe email.
                    </p>
                  </div>
                </div>

                {adminError && (
                  <div className="alert alert-danger fade-in" style={{ marginBottom: '16px' }}>
                    <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" strokeWidth="2">
                      <circle cx="12" cy="12" r="10"></circle>
                      <line x1="12" y1="8" x2="12" y2="12"></line>
                      <line x1="12" y1="16" x2="12.01" y2="16"></line>
                    </svg>
                    <span>{adminError}</span>
                  </div>
                )}

                {adminSuccess && (
                  <div className="alert alert-success fade-in" style={{ backgroundColor: 'rgba(16, 185, 129, 0.15)', borderColor: '#10b981', color: '#10b981', padding: '12px 16px', borderRadius: '8px', marginBottom: '16px', display: 'flex', alignItems: 'center', gap: '8px' }}>
                    <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" strokeWidth="2">
                      <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path>
                      <polyline points="22 4 12 14.01 9 11.01"></polyline>
                    </svg>
                    <span>{adminSuccess}</span>
                  </div>
                )}

                <form onSubmit={handleCreateUser} className="auth-form" style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))', gap: '16px' }}>
                  <div className="form-group">
                    <label htmlFor="newFirstName">Prenume</label>
                    <input
                      id="newFirstName"
                      type="text"
                      placeholder="Ion"
                      value={newFirstName}
                      onChange={(e) => setNewFirstName(e.target.value)}
                      required
                      disabled={creatingUser}
                    />
                  </div>

                  <div className="form-group">
                    <label htmlFor="newLastName">Nume de familie</label>
                    <input
                      id="newLastName"
                      type="text"
                      placeholder="Popescu"
                      value={newLastName}
                      onChange={(e) => setNewLastName(e.target.value)}
                      required
                      disabled={creatingUser}
                    />
                  </div>

                  <div className="form-group">
                    <label htmlFor="newEmail">Email</label>
                    <input
                      id="newEmail"
                      type="email"
                      placeholder="ion.popescu@companie.com"
                      value={newEmail}
                      onChange={(e) => setNewEmail(e.target.value)}
                      required
                      disabled={creatingUser}
                    />
                  </div>

                  <div className="form-group">
                    <label htmlFor="newJobTitle">Funcţie (Job Title)</label>
                    <select
                      id="newJobTitle"
                      value={newJobTitleId}
                      onChange={(e) => setNewJobTitleId(e.target.value)}
                      required
                      disabled={creatingUser}
                    >
                      {jobTitles.map((jt) => (
                        <option key={jt.id} value={jt.id}>
                          {jt.name}
                        </option>
                      ))}
                    </select>
                  </div>

                  <div style={{ gridColumn: '1 / -1', marginTop: '8px' }}>
                    <button type="submit" className="btn btn-primary" disabled={creatingUser} style={{ padding: '12px 24px', fontSize: '1rem' }}>
                      {creatingUser ? (
                        <span className="spinner-wrapper">
                          <span className="spinner"></span>
                          <span>Se creează contul...</span>
                        </span>
                      ) : (
                        'Creează Cont Utilizator'
                      )}
                    </button>
                  </div>
                </form>
              </div>
            )}

            {/* Existing users table */}
            <div className="dashboard-card glassmorphism" style={{ padding: '28px' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
                <h3 style={{ margin: 0 }}>Utilizatori înregistraţi ({users.length})</h3>
                <button type="button" onClick={fetchUsers} className="btn-link" style={{ fontSize: '0.9rem' }}>
                  Refresh Listă
                </button>
              </div>

              {loadingUsers ? (
                <div style={{ padding: '20px', textAlign: 'center', color: 'var(--color-text-muted)' }}>
                  Se încarcă lista de utilizatori...
                </div>
              ) : users.length === 0 ? (
                <div style={{ padding: '20px', textAlign: 'center', color: 'var(--color-text-muted)' }}>
                  Nu există utilizatori înregistraţi.
                </div>
              ) : (
                <div style={{ overflowX: 'auto' }}>
                  <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left' }}>
                    <thead>
                      <tr style={{ borderBottom: '1px solid var(--color-border)', color: 'var(--color-text-muted)', fontSize: '0.85rem' }}>
                        <th style={{ padding: '12px' }}>Utilizator</th>
                        <th style={{ padding: '12px' }}>Email</th>
                        <th style={{ padding: '12px' }}>Funcţie</th>
                        <th style={{ padding: '12px' }}>Status Cont</th>
                        <th style={{ padding: '12px', textAlign: 'right' }}>Acțiuni</th>
                      </tr>
                    </thead>
                    <tbody>
                      {users.map((u) => (
                        <tr key={u.id} style={{ borderBottom: '1px solid var(--color-border)' }}>
                          <td style={{ padding: '12px', fontWeight: 500 }}>
                            {u.firstName} {u.lastName}
                          </td>
                          <td style={{ padding: '12px', color: 'var(--color-text-muted)' }}>
                            {u.email}
                          </td>
                          <td style={{ padding: '12px' }}>
                            <span className="detail-value-badge">
                              {getJobTitleLabel(u.jobTitleId)}
                            </span>
                          </td>
                          <td style={{ padding: '12px' }}>
                            <span style={{ color: '#10b981', fontWeight: 500, fontSize: '0.85rem', display: 'flex', alignItems: 'center', gap: '4px' }}>
                              <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2">
                                <polyline points="20 6 9 17 4 12"></polyline>
                              </svg>
                              Activ
                            </span>
                          </td>
                          <td style={{ padding: '12px', textAlign: 'right' }}>
                            {isAdmin && u.id !== userProfile.id && (
                              <button
                                type="button"
                                onClick={() => handleDeleteUser(u.id, `${u.firstName} ${u.lastName}`)}
                                style={{
                                  padding: '6px 12px',
                                  fontSize: '0.8rem',
                                  backgroundColor: 'rgba(239, 68, 68, 0.15)',
                                  color: '#ef4444',
                                  border: '1px solid #ef4444',
                                  borderRadius: '6px',
                                  cursor: 'pointer'
                                }}
                              >
                                Șterge
                              </button>
                            )}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          </div>
        )}

        {activeTab === 'tasks' && (
          <TaskManager userProfile={userProfile} />
        )}

        {activeTab === 'job-titles' && (
          <JobTitleForm
            apiUrl={apiUrl}
            onJobTitleCreated={handleJobTitleCreated}
            onJobTitleDeleted={handleJobTitleDeleted}
          />
        )}
      </main>
    </div>
  );
}
