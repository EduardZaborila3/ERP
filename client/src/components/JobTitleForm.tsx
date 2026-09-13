import { useEffect, useState } from 'react';
import { authService } from '../services/authService';

interface JobTitle {
  id: number;
  name: string;
}

interface JobTitleFormProps {
  apiUrl?: string;
  onJobTitleCreated?: (newJobTitle: JobTitle) => void;
  onJobTitleDeleted?: (deletedId: number) => void;
}

export default function JobTitleForm({ 
  apiUrl = import.meta.env.VITE_API_URL || 'http://localhost:5110/api', 
  onJobTitleCreated,
  onJobTitleDeleted
}: JobTitleFormProps) {
  const [jobTitles, setJobTitles] = useState<JobTitle[]>([]);
  const [name, setName] = useState('');
  const [loading, setLoading] = useState(false);
  const [fetching, setFetching] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [searchQuery, setSearchQuery] = useState('');

  const fetchJobTitles = async () => {
    setFetching(true);
    try {
      const response = await authService.authFetch(`${apiUrl}/job-titles`);
      if (response.ok) {
        const data = await response.json();
        setJobTitles(data);
      }
    } catch (err) {
      console.error('Error fetching job titles:', err);
    } finally {
      setFetching(false);
    }
  };

  useEffect(() => {
    fetchJobTitles();
  }, [apiUrl]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim()) {
      setError('Vă rugăm să introduceți denumirea funcției.');
      return;
    }

    setLoading(true);
    setError(null);
    setSuccess(null);

    try {
      const response = await authService.authFetch(`${apiUrl}/job-titles`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ name: name.trim() }),
      });

      if (!response.ok) {
        const errText = await response.text();
        throw new Error(errText || 'A apărut o eroare la crearea funcției.');
      }

      const createdJobTitle: JobTitle = await response.json();
      setSuccess(`Funcția "${createdJobTitle.name}" a fost adăugată cu succes!`);
      setName('');
      
      setJobTitles((prev) => [...prev, createdJobTitle]);

      if (onJobTitleCreated) {
        onJobTitleCreated(createdJobTitle);
      }
    } catch (err: any) {
      setError(err.message || 'Nu s-a putut salva funcția.');
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = async (id: number, titleName: string) => {
    if (!window.confirm(`Sigur doriți să ștergeți funcția "${titleName}"?`)) return;

    try {
      const response = await authService.authFetch(`${apiUrl}/job-titles/${id}`, {
        method: 'DELETE',
      });

      if (response.ok) {
        setJobTitles((prev) => prev.filter((item) => item.id !== id));
        if (onJobTitleDeleted) {
          onJobTitleDeleted(id);
        }
      } else {
        const errText = await response.text();
        alert(errText || 'Nu s-a putut șterge funcția.');
      }
    } catch (err) {
      console.error('Error deleting job title:', err);
    }
  };

  const filteredTitles = jobTitles.filter((jt) =>
    jt.name.toLowerCase().includes(searchQuery.toLowerCase())
  );

  return (
    <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(340px, 1fr))', gap: '24px', alignItems: 'start' }}>
      {/* Left Column: Form to create new Job Title */}
      <div className="dashboard-card glassmorphism" style={{ padding: '24px' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '12px', marginBottom: '20px' }}>
          <div style={{ background: 'var(--color-primary-light)', padding: '10px', borderRadius: '10px', color: 'var(--color-primary)', display: 'flex' }}>
            <svg viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="currentColor" strokeWidth="2">
              <rect x="2" y="7" width="20" height="14" rx="2" ry="2"></rect>
              <path d="M16 21V5a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v16"></path>
            </svg>
          </div>
          <div>
            <h3 style={{ margin: 0, fontSize: '1.15rem' }}>Adăugare Funcție Nouă</h3>
            <p style={{ margin: '2px 0 0 0', color: 'var(--color-text-muted)', fontSize: '0.85rem' }}>
              Adaugă titluri de funcţii pentru membrii echipei
            </p>
          </div>
        </div>

        {error && (
          <div className="alert alert-danger fade-in" style={{ marginBottom: '16px' }}>
            <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2">
              <circle cx="12" cy="12" r="10"></circle>
              <line x1="12" y1="8" x2="12" y2="12"></line>
              <line x1="12" y1="16" x2="12.01" y2="16"></line>
            </svg>
            <span>{error}</span>
          </div>
        )}

        {success && (
          <div className="alert alert-success fade-in" style={{ backgroundColor: 'rgba(16, 185, 129, 0.15)', borderColor: '#10b981', color: '#10b981', padding: '12px 16px', borderRadius: '8px', marginBottom: '16px', display: 'flex', alignItems: 'center', gap: '8px', fontSize: '0.9rem' }}>
            <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2">
              <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path>
              <polyline points="22 4 12 14.01 9 11.01"></polyline>
            </svg>
            <span>{success}</span>
          </div>
        )}

        <form onSubmit={handleSubmit} className="auth-form" style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
          <div className="form-group" style={{ margin: 0 }}>
            <label htmlFor="jobTitleName" style={{ fontWeight: 600, fontSize: '0.9rem', marginBottom: '6px', display: 'block' }}>
              Denumire Funcție
            </label>
            <input
              id="jobTitleName"
              type="text"
              placeholder="ex. Inginer / Contabil"
              value={name}
              onChange={(e) => setName(e.target.value)}
              required
              disabled={loading}
              style={{ width: '100%', boxSizing: 'border-box' }}
            />
          </div>

          <button
            type="submit"
            className="btn btn-primary"
            disabled={loading}
            style={{ width: '100%', padding: '12px 20px', fontSize: '0.95rem', display: 'flex', justifyContent: 'center', alignItems: 'center', gap: '8px' }}
          >
            {loading ? (
              <span className="spinner-wrapper">
                <span className="spinner"></span>
                <span>Se salvează...</span>
              </span>
            ) : (
              <>
                <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2">
                  <line x1="12" y1="5" x2="12" y2="19"></line>
                  <line x1="5" y1="12" x2="19" y2="12"></line>
                </svg>
                <span>Salvează Funcția</span>
              </>
            )}
          </button>
        </form>
      </div>

      {/* Right Column: Existing Job Titles List */}
      <div className="dashboard-card glassmorphism" style={{ padding: '24px' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px', flexWrap: 'wrap', gap: '8px' }}>
          <div>
            <h3 style={{ margin: 0, fontSize: '1.15rem' }}>
              Funcții înregistrate ({filteredTitles.length})
            </h3>
            <p style={{ margin: '2px 0 0 0', color: 'var(--color-text-muted)', fontSize: '0.85rem' }}>
              Lista completă a titlurilor de funcţii din sistem
            </p>
          </div>
          <button type="button" onClick={fetchJobTitles} className="btn-link" style={{ fontSize: '0.85rem' }}>
            Refresh Listă
          </button>
        </div>

        {jobTitles.length > 5 && (
          <div className="form-group" style={{ marginBottom: '16px' }}>
            <input
              type="text"
              placeholder="Caută o funcție..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              style={{ width: '100%', padding: '8px 12px', fontSize: '0.85rem', boxSizing: 'border-box' }}
            />
          </div>
        )}

        {fetching ? (
          <div style={{ padding: '20px', textAlign: 'center', color: 'var(--color-text-muted)', fontSize: '0.9rem' }}>
            Se încarcă funcțiile...
          </div>
        ) : filteredTitles.length === 0 ? (
          <div style={{ padding: '20px', textAlign: 'center', color: 'var(--color-text-muted)', fontSize: '0.9rem' }}>
            {searchQuery ? 'Nicio funcție nu se potrivește căutării.' : 'Nu există funcții înregistrate.'}
          </div>
        ) : (
          <div style={{ display: 'flex', flexDirection: 'column', gap: '8px', maxHeight: '420px', overflowY: 'auto', paddingRight: '4px' }}>
            {filteredTitles.map((jt) => (
              <div
                key={jt.id}
                style={{
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'center',
                  padding: '12px 16px',
                  borderRadius: '10px',
                  background: 'var(--color-bg-input)',
                  border: '1px solid var(--color-border)',
                  transition: 'border-color 0.2s ease'
                }}
              >
                <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
                  <div style={{ width: '8px', height: '8px', borderRadius: '50%', background: 'var(--color-primary)' }}></div>
                  <span style={{ fontWeight: 500, color: 'var(--color-text-main)', fontSize: '0.95rem' }}>
                    {jt.name}
                  </span>
                </div>

                <button
                  type="button"
                  onClick={() => handleDelete(jt.id, jt.name)}
                  style={{
                    background: 'transparent',
                    border: 'none',
                    color: '#ef4444',
                    cursor: 'pointer',
                    padding: '6px',
                    borderRadius: '6px',
                    display: 'flex',
                    alignItems: 'center',
                    transition: 'background 0.2s ease'
                  }}
                  title="Șterge funcția"
                >
                  <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2">
                    <polyline points="3 6 5 6 21 6"></polyline>
                    <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path>
                  </svg>
                </button>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
