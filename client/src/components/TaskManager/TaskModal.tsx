import React, { useState, useEffect } from 'react';
import { taskService, TaskStatus, type AppTask, type CreateTaskInput, type UpdateTaskInput } from '../../services/taskService';

interface SystemUser {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
}

interface TaskModalProps {
  isOpen: boolean;
  task: AppTask | null;
  availableUsers: SystemUser[];
  onClose: () => void;
  onSave: (savedTask: AppTask) => void;
}

export default function TaskModal({
  isOpen,
  task,
  availableUsers,
  onClose,
  onSave
}: TaskModalProps) {
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [status, setStatus] = useState<TaskStatus>(TaskStatus.Pending);
  const [deadline, setDeadline] = useState('');
  const [selectedAssigneeIds, setSelectedAssigneeIds] = useState<number[]>([]);
  const [assigneeSearch, setAssigneeSearch] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (task) {
      setTitle(task.title);
      setDescription(task.description);
      setStatus(task.status);
      setSelectedAssigneeIds(task.assignees.map((a) => a.id));
      if (task.deadline) {
        // Format ISO UTC string to YYYY-MM-DDTHH:mm for datetime-local input
        const d = new Date(task.deadline);
        const pad = (n: number) => n.toString().padStart(2, '0');
        const localIso = `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
        setDeadline(localIso);
      } else {
        setDeadline('');
      }
    } else {
      setTitle('');
      setDescription('');
      setStatus(TaskStatus.Pending);
      setDeadline('');
      setSelectedAssigneeIds([]);
    }
    setError(null);
    setAssigneeSearch('');
  }, [task, isOpen]);

  if (!isOpen) return null;

  const toggleAssignee = (userId: number) => {
    setSelectedAssigneeIds((prev) =>
      prev.includes(userId) ? prev.filter((id) => id !== userId) : [...prev, userId]
    );
  };

  const filteredUsers = availableUsers.filter((u) => {
    const q = assigneeSearch.toLowerCase();
    return (
      u.firstName.toLowerCase().includes(q) ||
      u.lastName.toLowerCase().includes(q) ||
      u.email.toLowerCase().includes(q)
    );
  });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!title.trim()) {
      setError('Titlul task-ului este obligatoriu.');
      return;
    }
    if (!description.trim()) {
      setError('Descrierea task-ului este obligatorie.');
      return;
    }

    setSubmitting(true);
    setError(null);

    try {
      const deadlineIso = deadline ? new Date(deadline).toISOString() : null;

      if (task) {
        const updateData: UpdateTaskInput = {
          title: title.trim(),
          description: description.trim(),
          status,
          deadline: deadlineIso,
          assigneeIds: selectedAssigneeIds
        };
        const updated = await taskService.updateTask(task.id, updateData);
        onSave(updated);
      } else {
        const createData: CreateTaskInput = {
          title: title.trim(),
          description: description.trim(),
          status,
          deadline: deadlineIso,
          assigneeIds: selectedAssigneeIds
        };
        const created = await taskService.createTask(createData);
        onSave(created);
      }
      onClose();
    } catch (err: any) {
      console.error('Error saving task:', err);
      setError(err.message || 'A apărut o eroare la salvarea task-ului.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div
        className="modal-content glassmorphism fade-in"
        style={{ maxWidth: '900px', width: '95%', maxHeight: '90vh', overflowY: 'auto', padding: '22px 28px' }}
        onClick={(e) => e.stopPropagation()}
      >
        <div className="modal-header" style={{ paddingBottom: '12px' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
            <div
              style={{
                background: 'var(--color-primary-light)',
                padding: '8px',
                borderRadius: '8px',
                color: 'var(--color-primary)',
                display: 'flex'
              }}
            >
              <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" strokeWidth="2">
                <path d="M9 11l3 3L22 4"></path>
                <path d="M21 12v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11"></path>
              </svg>
            </div>
            <div>
              <h3 style={{ margin: 0, fontSize: '1.2rem' }}>
                {task ? 'Editare Task' : 'Creează Task Nou'}
              </h3>
              <p style={{ margin: '2px 0 0', color: 'var(--color-text-muted)', fontSize: '0.82rem' }}>
                {task ? `Modifică detaliile task-ului #${task.id}` : 'Completează detaliile pentru a atribui un nou task echipei'}
              </p>
            </div>
          </div>
          <button type="button" className="btn-close" onClick={onClose} aria-label="Închide">
            <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2">
              <line x1="18" y1="6" x2="6" y2="18"></line>
              <line x1="6" y1="6" x2="18" y2="18"></line>
            </svg>
          </button>
        </div>

        {error && (
          <div className="alert alert-danger fade-in" style={{ marginTop: '12px', marginBottom: '12px', padding: '10px 14px' }}>
            <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2">
              <circle cx="12" cy="12" r="10"></circle>
              <line x1="12" y1="8" x2="12" y2="12"></line>
              <line x1="12" y1="16" x2="12.01" y2="16"></line>
            </svg>
            <span>{error}</span>
          </div>
        )}

        <form onSubmit={handleSubmit} className="auth-form" style={{ marginTop: '16px', gap: '14px' }}>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(360px, 1fr))', gap: '18px', alignItems: 'start' }}>
            {/* Left Column: Basic Details */}
            <div style={{ display: 'flex', flexDirection: 'column', gap: '14px' }}>
              <div className="form-group">
                <label htmlFor="taskTitle">Titlu Task *</label>
                <input
                  id="taskTitle"
                  type="text"
                  placeholder="Ex: Implementare modul rapoarte financiare"
                  value={title}
                  onChange={(e) => setTitle(e.target.value)}
                  required
                  disabled={submitting}
                  style={{
                    width: '100%',
                    padding: '10px 12px',
                    borderRadius: '8px',
                    border: '1px solid var(--color-border)',
                    background: 'var(--color-bg-input)',
                    color: 'var(--color-text-main)',
                    fontSize: '0.9rem'
                  }}
                />
              </div>

              {task ? (
                /* Edit mode: show both Status and Deadline */
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
                  <div className="form-group">
                    <label htmlFor="taskStatus">Status</label>
                    <select
                      id="taskStatus"
                      value={status}
                      onChange={(e) => setStatus(Number(e.target.value) as TaskStatus)}
                      disabled={submitting}
                    >
                      <option value={TaskStatus.Pending}>În așteptare (Pending)</option>
                      <option value={TaskStatus.InProgress}>În lucru (In Progress)</option>
                      <option value={TaskStatus.Completed}>Finalizat (Completed)</option>
                    </select>
                  </div>

                  <div className="form-group">
                    <label htmlFor="taskDeadline">Termen Limită</label>
                    <input
                      id="taskDeadline"
                      type="datetime-local"
                      value={deadline}
                      onChange={(e) => setDeadline(e.target.value)}
                      disabled={submitting}
                      style={{
                        width: '100%',
                        padding: '10px 12px',
                        borderRadius: '8px',
                        border: '1px solid var(--color-border)',
                        background: 'var(--color-bg-input)',
                        color: 'var(--color-text-main)',
                        fontSize: '0.9rem',
                        boxSizing: 'border-box'
                      }}
                    />
                  </div>
                </div>
              ) : (
                /* Create mode: Status is implicitly Pending, only show Deadline */
                <div className="form-group">
                  <label htmlFor="taskDeadline">Termen Limită (Opțional)</label>
                  <input
                    id="taskDeadline"
                    type="datetime-local"
                    value={deadline}
                    onChange={(e) => setDeadline(e.target.value)}
                    disabled={submitting}
                    style={{
                      width: '100%',
                      padding: '10px 12px',
                      borderRadius: '8px',
                      border: '1px solid var(--color-border)',
                      background: 'var(--color-bg-input)',
                      color: 'var(--color-text-main)',
                      fontSize: '0.9rem',
                      boxSizing: 'border-box'
                    }}
                  />
                </div>
              )}

              <div className="form-group">
                <label htmlFor="taskDescription">Descriere detaliată *</label>
                <textarea
                  id="taskDescription"
                  placeholder="Specificați cerințele, pașii necesari sau instrucțiunile pentru rezolvare..."
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  rows={4}
                  required
                  disabled={submitting}
                  style={{
                    width: '100%',
                    padding: '10px 12px',
                    borderRadius: '8px',
                    border: '1px solid var(--color-border)',
                    background: 'var(--color-bg-input)',
                    color: 'var(--color-text-main)',
                    fontSize: '0.9rem',
                    fontFamily: 'inherit',
                    resize: 'vertical',
                    boxSizing: 'border-box'
                  }}
                />
              </div>
            </div>

            {/* Right Column: Assignees */}
            <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
              <div className="form-group">
                <label>Persoane Atribuite (Assignees)</label>
                <div style={{ marginBottom: '6px' }}>
                  <input
                    type="text"
                    placeholder="Caută utilizator după nume sau email..."
                    value={assigneeSearch}
                    onChange={(e) => setAssigneeSearch(e.target.value)}
                    disabled={submitting}
                    style={{
                      width: '100%',
                      padding: '9px 12px',
                      borderRadius: '8px',
                      border: '1px solid var(--color-border)',
                      background: 'var(--color-bg-input)',
                      color: 'var(--color-text-main)',
                      fontSize: '0.88rem',
                      boxSizing: 'border-box'
                    }}
                  />
                </div>

                {/* Selected chips */}
                {selectedAssigneeIds.length > 0 && (
                  <div style={{ display: 'flex', flexWrap: 'wrap', gap: '6px', marginBottom: '6px', maxHeight: '60px', overflowY: 'auto' }}>
                    {selectedAssigneeIds.map((userId) => {
                      const u = availableUsers.find((user) => user.id === userId);
                      const name = u ? `${u.firstName} ${u.lastName}` : userId;
                      return (
                        <span
                          key={userId}
                          style={{
                            display: 'inline-flex',
                            alignItems: 'center',
                            gap: '5px',
                            padding: '3px 8px',
                            borderRadius: '16px',
                            background: 'var(--color-primary-light)',
                            color: 'var(--color-primary)',
                            fontSize: '0.82rem',
                            fontWeight: 500
                          }}
                        >
                          {name}
                          <button
                            type="button"
                            onClick={() => toggleAssignee(userId)}
                            style={{
                              background: 'none',
                              border: 'none',
                              color: 'var(--color-primary)',
                              cursor: 'pointer',
                              padding: 0,
                              display: 'flex',
                              alignItems: 'center'
                            }}
                          >
                            <svg viewBox="0 0 24 24" width="13" height="13" fill="none" stroke="currentColor" strokeWidth="2.5">
                              <line x1="18" y1="6" x2="6" y2="18"></line>
                              <line x1="6" y1="6" x2="18" y2="18"></line>
                            </svg>
                          </button>
                        </span>
                      );
                    })}
                  </div>
                )}

                {/* Scrollable user list with checkboxes */}
                <div
                  style={{
                    maxHeight: '160px',
                    overflowY: 'auto',
                    border: '1px solid var(--color-border)',
                    borderRadius: '8px',
                    background: 'var(--color-bg-input)',
                    padding: '4px'
                  }}
                >
                  {filteredUsers.length === 0 ? (
                    <div style={{ padding: '12px', textAlign: 'center', color: 'var(--color-text-muted)', fontSize: '0.85rem' }}>
                      Nu s-au găsit utilizatori.
                    </div>
                  ) : (
                    filteredUsers.map((u) => {
                      const isSelected = selectedAssigneeIds.includes(u.id);
                      return (
                        <label
                          key={u.id}
                          style={{
                            display: 'flex',
                            alignItems: 'center',
                            justifyContent: 'space-between',
                            padding: '6px 10px',
                            borderRadius: '6px',
                            cursor: 'pointer',
                            background: isSelected ? 'var(--color-primary-light)' : 'transparent',
                            marginBottom: '2px',
                            transition: 'background-color 0.15s ease'
                          }}
                        >
                          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                            <div
                              style={{
                                width: '26px',
                                height: '26px',
                                borderRadius: '50%',
                                background: 'var(--color-primary)',
                                color: '#fff',
                                display: 'flex',
                                alignItems: 'center',
                                justifyContent: 'center',
                                fontSize: '0.72rem',
                                fontWeight: 600
                              }}
                            >
                              {u.firstName?.[0] || 'U'}
                              {u.lastName?.[0] || ''}
                            </div>
                            <div>
                              <div style={{ fontWeight: 500, fontSize: '0.82rem' }}>
                                {u.firstName} {u.lastName}
                              </div>
                              <div style={{ fontSize: '0.72rem', color: 'var(--color-text-muted)' }}>
                                {u.email}
                              </div>
                            </div>
                          </div>
                          <input
                            type="checkbox"
                            checked={isSelected}
                            onChange={() => toggleAssignee(u.id)}
                            style={{ cursor: 'pointer', width: '15px', height: '15px' }}
                          />
                        </label>
                      );
                    })
                  )}
                </div>
              </div>
            </div>
          </div>

          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '10px', marginTop: '12px', borderTop: '1px solid var(--color-border)', paddingTop: '12px' }}>
            <button
              type="button"
              className="btn btn-outline"
              onClick={onClose}
              disabled={submitting}
              style={{ width: 'auto', padding: '9px 18px', fontSize: '0.9rem' }}
            >
              Anulează
            </button>
            <button
              type="submit"
              className="btn btn-primary"
              disabled={submitting}
              style={{ width: 'auto', padding: '9px 22px', fontSize: '0.9rem' }}
            >
              {submitting ? (
                <span className="spinner-wrapper">
                  <span className="spinner" style={{ width: '15px', height: '15px' }}></span>
                  <span>Se salvează...</span>
                </span>
              ) : task ? (
                'Salvează Modificările'
              ) : (
                'Creează Task'
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
