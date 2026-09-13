import { TaskStatus, type AppTask } from '../../services/taskService';

interface TaskDetailModalProps {
  task: AppTask | null;
  onClose: () => void;
  onEdit: (task: AppTask) => void;
  onDelete: (taskId: number) => void;
  onStatusChange: (taskId: number, newStatus: TaskStatus) => void;
}

export default function TaskDetailModal({
  task,
  onClose,
  onEdit,
  onDelete,
  onStatusChange
}: TaskDetailModalProps) {
  if (!task) return null;

  const getStatusBadge = (status: TaskStatus) => {
    switch (status) {
      case TaskStatus.Pending:
        return { label: 'În așteptare', bg: 'rgba(234, 179, 8, 0.15)', color: '#ca8a04', border: '#eab308' };
      case TaskStatus.InProgress:
        return { label: 'În lucru', bg: 'rgba(59, 130, 246, 0.15)', color: '#2563eb', border: '#3b82f6' };
      case TaskStatus.Completed:
        return { label: 'Finalizat', bg: 'rgba(22, 93, 39, 0.15)', color: '#165D27', border: '#165D27' };
      default:
        return { label: 'Necunoscut', bg: 'rgba(156, 163, 175, 0.15)', color: '#6b7280', border: '#9ca3af' };
    }
  };

  const statusBadge = getStatusBadge(task.status);

  const formatDate = (isoString?: string | null) => {
    if (!isoString) return 'Fără termen limită';
    const d = new Date(isoString);
    return d.toLocaleString('ro-RO', {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  const isOverdue = task.deadline && new Date(task.deadline) < new Date() && task.status !== TaskStatus.Completed;

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div
        className="modal-content glassmorphism fade-in"
        style={{ maxWidth: '860px', width: '95%', maxHeight: '90vh', overflowY: 'auto', padding: '22px 28px' }}
        onClick={(e) => e.stopPropagation()}
      >
        <div className="modal-header" style={{ paddingBottom: '12px' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <span
              style={{
                backgroundColor: statusBadge.bg,
                color: statusBadge.color,
                border: `1px solid ${statusBadge.border}`,
                padding: '4px 10px',
                borderRadius: '20px',
                fontSize: '0.8rem',
                fontWeight: 600
              }}
            >
              {statusBadge.label}
            </span>
            <span style={{ color: 'var(--color-text-muted)', fontSize: '0.85rem' }}>
              Task #{task.id}
            </span>
          </div>
          <button type="button" className="btn-close" onClick={onClose} aria-label="Închide">
            <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2">
              <line x1="18" y1="6" x2="6" y2="18"></line>
              <line x1="6" y1="6" x2="18" y2="18"></line>
            </svg>
          </button>
        </div>

        <div style={{ marginTop: '14px' }}>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(360px, 1fr))', gap: '20px', alignItems: 'start' }}>
            {/* Left Column: Title, Description, Quick Status */}
            <div>
              <h2 style={{ margin: '0 0 10px', fontSize: '1.3rem', color: 'var(--color-text-main)', wordBreak: 'break-word', overflowWrap: 'anywhere' }}>
                {task.title}
              </h2>

              <div
                style={{
                  background: 'var(--color-bg-input)',
                  padding: '12px 14px',
                  borderRadius: '8px',
                  border: '1px solid var(--color-border)',
                  lineHeight: 1.5,
                  color: 'var(--color-text-main)',
                  whiteSpace: 'pre-wrap',
                  wordBreak: 'break-word',
                  overflowWrap: 'anywhere',
                  fontSize: '0.9rem',
                  maxHeight: '160px',
                  overflowY: 'auto'
                }}
              >
                {task.description}
              </div>

              {/* Quick status change buttons */}
              <div style={{ marginTop: '14px' }}>
                <span style={{ fontSize: '0.82rem', fontWeight: 600, color: 'var(--color-text-muted)', display: 'block', marginBottom: '6px' }}>
                  Schimbă Starea Rapid:
                </span>
                <div style={{ display: 'flex', gap: '6px', flexWrap: 'wrap' }}>
                  {[
                    { status: TaskStatus.Pending, label: 'În așteptare' },
                    { status: TaskStatus.InProgress, label: 'În lucru' },
                    { status: TaskStatus.Completed, label: 'Finalizat' }
                  ].map((s) => (
                    <button
                      key={s.status}
                      type="button"
                      onClick={() => onStatusChange(task.id, s.status)}
                      style={{
                        padding: '5px 12px',
                        borderRadius: '6px',
                        fontSize: '0.82rem',
                        fontWeight: task.status === s.status ? 600 : 400,
                        border: task.status === s.status ? '2px solid var(--color-primary)' : '1px solid var(--color-border)',
                        background: task.status === s.status ? 'var(--color-primary-light)' : 'var(--color-bg-card)',
                        color: task.status === s.status ? 'var(--color-primary)' : 'var(--color-text-main)',
                        cursor: 'pointer',
                        transition: 'all 0.15s ease'
                      }}
                    >
                      {s.label}
                    </button>
                  ))}
                </div>
              </div>
            </div>

            {/* Right Column: Metadata & Assignees */}
            <div style={{ display: 'flex', flexDirection: 'column', gap: '14px' }}>
              {/* Metadata Box */}
              <div
                style={{
                  display: 'grid',
                  gridTemplateColumns: '1fr 1fr',
                  gap: '12px',
                  background: 'var(--color-bg-input)',
                  padding: '12px 14px',
                  borderRadius: '8px',
                  border: '1px solid var(--color-border)'
                }}
              >
                <div>
                  <span style={{ fontSize: '0.78rem', color: 'var(--color-text-muted)', display: 'block', marginBottom: '2px' }}>
                    Termen Limită
                  </span>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '5px', fontSize: '0.85rem', fontWeight: 500 }}>
                    <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke={isOverdue ? '#ef4444' : 'currentColor'} strokeWidth="2">
                      <circle cx="12" cy="12" r="10"></circle>
                      <polyline points="12 6 12 12 16 14"></polyline>
                    </svg>
                    <span style={{ color: isOverdue ? '#ef4444' : 'inherit' }}>
                      {formatDate(task.deadline)}
                    </span>
                    {isOverdue && (
                      <span style={{ background: 'rgba(239, 68, 68, 0.15)', color: '#ef4444', fontSize: '0.68rem', padding: '1px 5px', borderRadius: '4px', fontWeight: 600 }}>
                        Expirat
                      </span>
                    )}
                  </div>
                </div>

                <div>
                  <span style={{ fontSize: '0.78rem', color: 'var(--color-text-muted)', display: 'block', marginBottom: '2px' }}>
                    Creat de
                  </span>
                  <div style={{ fontSize: '0.85rem', fontWeight: 500 }}>
                    {task.owner ? `${task.owner.firstName} ${task.owner.lastName}` : 'Administrator'}
                  </div>
                </div>

                <div style={{ gridColumn: '1 / -1' }}>
                  <span style={{ fontSize: '0.78rem', color: 'var(--color-text-muted)', display: 'block', marginBottom: '2px' }}>
                    Data Creării
                  </span>
                  <div style={{ fontSize: '0.85rem', fontWeight: 500 }}>
                    {formatDate(task.createdAt)}
                  </div>
                </div>
              </div>

              {/* Assignees Section */}
              <div>
                <span style={{ fontSize: '0.82rem', fontWeight: 600, color: 'var(--color-text-muted)', display: 'block', marginBottom: '8px' }}>
                  Membri Asignați ({task.assignees.length})
                </span>
                {task.assignees.length === 0 ? (
                  <p style={{ color: 'var(--color-text-muted)', fontSize: '0.85rem', fontStyle: 'italic', margin: 0 }}>
                    Niciun utilizator asignat momentan.
                  </p>
                ) : (
                  <div style={{ display: 'flex', flexWrap: 'wrap', gap: '8px', maxHeight: '110px', overflowY: 'auto' }}>
                    {task.assignees.map((assignee) => (
                      <div
                        key={assignee.id}
                        style={{
                          display: 'flex',
                          alignItems: 'center',
                          gap: '6px',
                          padding: '4px 10px',
                          borderRadius: '6px',
                          background: 'var(--color-bg-input)',
                          border: '1px solid var(--color-border)'
                        }}
                      >
                        <div
                          style={{
                            width: '22px',
                            height: '22px',
                            borderRadius: '50%',
                            background: 'var(--color-primary)',
                            color: '#fff',
                            display: 'flex',
                            alignItems: 'center',
                            justifyContent: 'center',
                            fontSize: '0.7rem',
                            fontWeight: 600
                          }}
                        >
                          {assignee.firstName?.[0] || 'U'}
                          {assignee.lastName?.[0] || ''}
                        </div>
                        <div>
                          <div style={{ fontSize: '0.8rem', fontWeight: 500 }}>
                            {assignee.firstName} {assignee.lastName}
                          </div>
                          <div style={{ fontSize: '0.7rem', color: 'var(--color-text-muted)' }}>
                            {assignee.email}
                          </div>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </div>
          </div>

          {/* Action buttons */}
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: '16px', borderTop: '1px solid var(--color-border)', paddingTop: '14px' }}>
            <button
              type="button"
              onClick={() => {
                if (window.confirm(`Sigur doriți să ștergeți task-ul "${task.title}"?`)) {
                  onDelete(task.id);
                  onClose();
                }
              }}
              style={{
                padding: '8px 14px',
                fontSize: '0.85rem',
                backgroundColor: 'rgba(239, 68, 68, 0.15)',
                color: '#ef4444',
                border: '1px solid #ef4444',
                borderRadius: '8px',
                cursor: 'pointer',
                display: 'flex',
                alignItems: 'center',
                gap: '6px'
              }}
            >
              <svg viewBox="0 0 24 24" width="15" height="15" fill="none" stroke="currentColor" strokeWidth="2">
                <polyline points="3 6 5 6 21 6"></polyline>
                <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path>
              </svg>
              Șterge Task
            </button>

            <div style={{ display: 'flex', gap: '10px' }}>
              <button
                type="button"
                className="btn btn-outline"
                onClick={onClose}
                style={{ width: 'auto', padding: '8px 16px', fontSize: '0.88rem' }}
              >
                Închide
              </button>
              <button
                type="button"
                className="btn btn-primary"
                onClick={() => {
                  onClose();
                  onEdit(task);
                }}
                style={{ width: 'auto', padding: '8px 18px', fontSize: '0.88rem', display: 'flex', alignItems: 'center', gap: '6px' }}
              >
                <svg viewBox="0 0 24 24" width="15" height="15" fill="none" stroke="currentColor" strokeWidth="2">
                  <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path>
                  <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path>
                </svg>
                Editează Task
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
