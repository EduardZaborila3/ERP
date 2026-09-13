import { useState, useEffect, useMemo } from 'react';
import { taskService, TaskStatus, type AppTask, type TaskSummary } from '../../services/taskService';
import { authService, type UserProfile } from '../../services/authService';
import TaskModal from './TaskModal';
import TaskDetailModal from './TaskDetailModal';

interface SystemUser {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
}

interface TaskManagerProps {
  userProfile: UserProfile;
}

export default function TaskManager({ userProfile }: TaskManagerProps) {
  const [tasks, setTasks] = useState<AppTask[]>([]);
  const [summary, setSummary] = useState<TaskSummary | null>(null);
  const [availableUsers, setAvailableUsers] = useState<SystemUser[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Filters state
  const [searchQuery, setSearchQuery] = useState('');
  const [statusFilter, setStatusFilter] = useState<string>('all');
  const [assigneeFilter, setAssigneeFilter] = useState<string>('all'); // 'all', 'me', or userId

  // Modals state
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [editingTask, setEditingTask] = useState<AppTask | null>(null);
  const [viewingTask, setViewingTask] = useState<AppTask | null>(null);

  const apiUrl = import.meta.env.VITE_API_URL || 'http://localhost:5110/api';

  const loadData = async () => {
    setLoading(true);
    setError(null);
    try {
      const [fetchedTasks, fetchedSummary] = await Promise.all([
        taskService.getTasks(),
        taskService.getTaskSummary()
      ]);
      setTasks(fetchedTasks);
      setSummary(fetchedSummary);
    } catch (err: any) {
      console.error('Error loading tasks data:', err);
      setError(err.message || 'Eroare la încărcarea sarcinilor.');
    } finally {
      setLoading(false);
    }
  };

  const loadUsers = async () => {
    try {
      const res = await authService.authFetch(`${apiUrl}/users`);
      if (res.ok) {
        const data = await res.json();
        setAvailableUsers(data);
      }
    } catch (err) {
      console.error('Error loading users:', err);
    }
  };

  useEffect(() => {
    loadData();
    loadUsers();
  }, [apiUrl]);

  const handleStatusChange = async (taskId: number, newStatus: TaskStatus) => {
    try {
      const updated = await taskService.updateTaskStatus(taskId, newStatus);
      setTasks((prev) => prev.map((t) => (t.id === taskId ? updated : t)));
      if (viewingTask && viewingTask.id === taskId) {
        setViewingTask(updated);
      }
      // Refresh summary in background
      taskService.getTaskSummary().then(setSummary).catch(console.error);
    } catch (err: any) {
      console.error('Error updating status:', err);
      alert(err.message || 'Nu s-a putut actualiza starea sarcinii.');
    }
  };

  const handleDeleteTask = async (taskId: number) => {
    try {
      await taskService.deleteTask(taskId);
      setTasks((prev) => prev.filter((t) => t.id !== taskId));
      if (viewingTask && viewingTask.id === taskId) {
        setViewingTask(null);
      }
      taskService.getTaskSummary().then(setSummary).catch(console.error);
    } catch (err: any) {
      console.error('Error deleting task:', err);
      alert(err.message || 'Nu s-a putut șterge sarcina.');
    }
  };

  const handleSaveTask = (savedTask: AppTask) => {
    setTasks((prev) => {
      const exists = prev.some((t) => t.id === savedTask.id);
      if (exists) {
        return prev.map((t) => (t.id === savedTask.id ? savedTask : t));
      } else {
        return [savedTask, ...prev];
      }
    });
    if (viewingTask && viewingTask.id === savedTask.id) {
      setViewingTask(savedTask);
    }
    taskService.getTaskSummary().then(setSummary).catch(console.error);
  };

  // Filtered tasks
  const filteredTasks = useMemo(() => {
    return tasks.filter((task) => {
      // Search
      if (searchQuery.trim()) {
        const q = searchQuery.toLowerCase();
        const matchesTitle = task.title.toLowerCase().includes(q);
        const matchesDesc = task.description.toLowerCase().includes(q);
        const matchesAssignee = task.assignees.some((a) =>
          `${a.firstName} ${a.lastName}`.toLowerCase().includes(q)
        );
        if (!matchesTitle && !matchesDesc && !matchesAssignee) return false;
      }

      // Status filter
      if (statusFilter !== 'all') {
        const numStatus = parseInt(statusFilter);
        if (task.status !== numStatus) return false;
      }

      // Assignee filter
      if (assigneeFilter === 'me') {
        const isAssignedToMe = task.assignees.some((a) => a.id === userProfile.id);
        const isOwnedByMe = task.ownerId === userProfile.id;
        if (!isAssignedToMe && !isOwnedByMe) return false;
      } else if (assigneeFilter !== 'all') {
        if (!task.assignees.some((a) => a.id.toString() === assigneeFilter)) return false;
      }

      return true;
    });
  }, [tasks, searchQuery, statusFilter, assigneeFilter, userProfile.id]);

  const kanbanColumns = [
    {
      status: TaskStatus.Pending,
      title: 'În așteptare',
      renderIcon: () => (
        <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2">
          <circle cx="12" cy="12" r="10"></circle>
          <polyline points="12 6 12 12 16 14"></polyline>
        </svg>
      ),
      color: '#ca8a04',
      badgeBg: 'rgba(234, 179, 8, 0.15)'
    },
    {
      status: TaskStatus.InProgress,
      title: 'În lucru',
      renderIcon: () => (
        <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2">
          <polygon points="13 2 3 14 12 14 11 22 21 10 12 10 13 2"></polygon>
        </svg>
      ),
      color: '#2563eb',
      badgeBg: 'rgba(59, 130, 246, 0.15)'
    },
    {
      status: TaskStatus.Completed,
      title: 'Finalizate',
      renderIcon: () => (
        <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2">
          <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path>
          <polyline points="22 4 12 14.01 9 11.01"></polyline>
        </svg>
      ),
      color: '#165D27',
      badgeBg: 'rgba(22, 93, 39, 0.15)'
    }
  ];

  const formatDateBadge = (deadlineIso?: string | null) => {
    if (!deadlineIso) return null;
    const deadline = new Date(deadlineIso);
    const now = new Date();
    const isOverdue = deadline < now;
    const isToday =
      deadline.getDate() === now.getDate() &&
      deadline.getMonth() === now.getMonth() &&
      deadline.getFullYear() === now.getFullYear();

    const formatted = deadline.toLocaleDateString('ro-RO', {
      day: 'numeric',
      month: 'short'
    });

    let badgeClass = 'deadline-badge-normal';
    let label = formatted;

    if (isOverdue) {
      badgeClass = 'deadline-badge-overdue';
      label = `Expirat (${formatted})`;
    } else if (isToday) {
      badgeClass = 'deadline-badge-today';
      label = 'Azi';
    }

    return (
      <span className={`task-deadline-badge ${badgeClass}`}>
        <svg viewBox="0 0 24 24" width="12" height="12" fill="none" stroke="currentColor" strokeWidth="2">
          <circle cx="12" cy="12" r="10"></circle>
          <polyline points="12 6 12 12 16 14"></polyline>
        </svg>
        {label}
      </span>
    );
  };

  return (
    <div className="task-manager-container fade-in">
      {/* Summary Cards */}
      {summary && (
        <div className="task-summary-grid">
          <div
            className="task-stat-card glassmorphism"
            onClick={() => setStatusFilter('all')}
            style={{ cursor: 'pointer' }}
          >
            <span className="task-stat-num">
              {summary ? summary.pendingTasks + summary.inProgressTasks + summary.completedTasks : tasks.length}
            </span>
            <span className="task-stat-title">Total Task-uri</span>
          </div>

          <div
            className="task-stat-card glassmorphism"
            onClick={() => setStatusFilter(TaskStatus.InProgress.toString())}
            style={{ cursor: 'pointer' }}
          >
            <span className="task-stat-num" style={{ color: '#2563eb' }}>
              {summary.inProgressTasks}
            </span>
            <span className="task-stat-title">În Lucru</span>
          </div>

          <div
            className="task-stat-card glassmorphism"
            onClick={() => setStatusFilter(TaskStatus.Pending.toString())}
            style={{ cursor: 'pointer' }}
          >
            <span className="task-stat-num" style={{ color: '#ca8a04' }}>
              {summary.pendingTasks}
            </span>
            <span className="task-stat-title">În Așteptare</span>
          </div>

          <div
            className="task-stat-card glassmorphism"
            onClick={() => setStatusFilter(TaskStatus.Completed.toString())}
            style={{ cursor: 'pointer' }}
          >
            <span className="task-stat-num" style={{ color: '#165D27' }}>
              {summary.completedTasks}
            </span>
            <span className="task-stat-title">Finalizate</span>
          </div>

          <div
            className="task-stat-card glassmorphism"
            style={{ borderLeft: summary.overdueTasks > 0 ? '3px solid #ef4444' : undefined }}
          >
            <span className="task-stat-num" style={{ color: summary.overdueTasks > 0 ? '#ef4444' : 'inherit' }}>
              {summary.overdueTasks}
            </span>
            <span className="task-stat-title">Termen Depășit</span>
          </div>
        </div>
      )}

      {/* Controls Bar: Search, Filters, and Task Nou Button */}
      <div className="task-controls-bar glassmorphism">
        <div className="search-input-box">
          <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2">
            <circle cx="11" cy="11" r="8"></circle>
            <line x1="21" y1="21" x2="16.65" y2="16.65"></line>
          </svg>
          <input
            type="text"
            placeholder="Caută task după titlu, descriere sau responsabil..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
          />
          {searchQuery && (
            <button
              type="button"
              onClick={() => setSearchQuery('')}
              style={{ background: 'none', border: 'none', cursor: 'pointer', color: 'var(--color-text-muted)', display: 'flex', alignItems: 'center', padding: '2px' }}
              aria-label="Șterge căutarea"
            >
              <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2">
                <line x1="18" y1="6" x2="6" y2="18"></line>
                <line x1="6" y1="6" x2="18" y2="18"></line>
              </svg>
            </button>
          )}
        </div>

        <div className="filter-group">
          {/* Status Filter */}
          <select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
            className="task-select-filter"
          >
            <option value="all">Toate Stările</option>
            <option value={TaskStatus.Pending.toString()}>În așteptare</option>
            <option value={TaskStatus.InProgress.toString()}>În lucru</option>
            <option value={TaskStatus.Completed.toString()}>Finalizate</option>
          </select>

          {/* Assignee Filter */}
          <select
            value={assigneeFilter}
            onChange={(e) => setAssigneeFilter(e.target.value)}
            className="task-select-filter"
          >
            <option value="all">Toți Membrii</option>
            <option value="me">Sarcini Proprii (Me)</option>
            {availableUsers.map((u) => (
              <option key={u.id} value={u.id}>
                {u.firstName} {u.lastName}
              </option>
            ))}
          </select>

          {/* Task Nou Button */}
          <button
            type="button"
            className="btn btn-primary"
            onClick={() => {
              setEditingTask(null);
              setIsEditModalOpen(true);
            }}
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              gap: '8px',
              padding: '9px 18px',
              fontSize: '0.9rem',
              width: 'auto',
              whiteSpace: 'nowrap'
            }}
          >
            <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2.5">
              <line x1="12" y1="5" x2="12" y2="19"></line>
              <line x1="5" y1="12" x2="19" y2="12"></line>
            </svg>
            Task Nou
          </button>
        </div>
      </div>

      {/* Main Task Body - KANBAN BOARD ONLY */}
      {loading ? (
        <div style={{ padding: '60px', textAlign: 'center', color: 'var(--color-text-muted)' }}>
          <div className="spinner large" style={{ margin: '0 auto 16px' }}></div>
          <p>Se încarcă sarcinile din sistem...</p>
        </div>
      ) : error ? (
        <div className="alert alert-danger" style={{ margin: '20px 0' }}>
          <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" strokeWidth="2">
            <circle cx="12" cy="12" r="10"></circle>
            <line x1="12" y1="8" x2="12" y2="12"></line>
            <line x1="12" y1="16" x2="12.01" y2="16"></line>
          </svg>
          <span>{error}</span>
        </div>
      ) : filteredTasks.length === 0 ? (
        <div className="task-empty-state glassmorphism">
          <div className="empty-icon">
            <svg viewBox="0 0 24 24" width="48" height="48" fill="none" stroke="var(--color-primary)" strokeWidth="1.5">
              <path d="M9 11l3 3L22 4"></path>
              <path d="M21 12v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11"></path>
            </svg>
          </div>
          <h3>Nu s-au găsit sarcini</h3>
          <p>Nu există niciun task corespunzător filtrelor selectate sau nu a fost creat încă niciun task.</p>
          <button
            type="button"
            className="btn btn-primary"
            onClick={() => {
              setSearchQuery('');
              setStatusFilter('all');
              setAssigneeFilter('all');
              setEditingTask(null);
              setIsEditModalOpen(true);
            }}
            style={{ width: 'auto', margin: '0 auto', padding: '10px 24px' }}
          >
            Creează Primul Task
          </button>
        </div>
      ) : (
        <div className="kanban-board">
          {kanbanColumns.map((col) => {
            const colTasks = filteredTasks.filter((t) => t.status === col.status);
            return (
              <div key={col.status} className="kanban-column glassmorphism">
                <div className="kanban-column-header">
                  <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                    <span style={{ color: col.color, display: 'flex', alignItems: 'center' }}>
                      {col.renderIcon()}
                    </span>
                    <span style={{ fontWeight: 600, fontSize: '0.95rem' }}>{col.title}</span>
                  </div>
                  <span
                    className="kanban-counter-badge"
                    style={{ backgroundColor: col.badgeBg, color: col.color }}
                  >
                    {colTasks.length}
                  </span>
                </div>

                <div className="kanban-task-list">
                  {colTasks.map((task) => {
                    const isTaskOverdue =
                      task.deadline &&
                      new Date(task.deadline) < new Date() &&
                      task.status !== TaskStatus.Completed;

                    return (
                      <div
                        key={task.id}
                        className={`kanban-task-card ${isTaskOverdue ? 'card-overdue' : ''}`}
                        onClick={() => setViewingTask(task)}
                      >
                        <div className="task-card-top">
                          <h4 className="task-card-title">{task.title}</h4>
                          <button
                            type="button"
                            className="task-card-menu-btn"
                            onClick={(e) => {
                              e.stopPropagation();
                              setEditingTask(task);
                              setIsEditModalOpen(true);
                            }}
                            title="Editează"
                          >
                            <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2">
                              <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path>
                              <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path>
                            </svg>
                          </button>
                        </div>

                        <p className="task-card-description">
                          {task.description}
                        </p>

                        {/* Deadline & Indicators */}
                        {task.deadline && (
                          <div style={{ marginTop: '8px' }}>
                            {formatDateBadge(task.deadline)}
                          </div>
                        )}

                        <div className="task-card-footer">
                          {/* Assignee Avatar Stack */}
                          <div className="task-avatar-group">
                            {task.assignees.slice(0, 3).map((a) => (
                              <div
                                key={a.id}
                                className="task-avatar"
                                title={`${a.firstName} ${a.lastName} (${a.email})`}
                              >
                                {a.firstName?.[0] || 'U'}
                                {a.lastName?.[0] || ''}
                              </div>
                            ))}
                            {task.assignees.length > 3 && (
                              <div className="task-avatar more">
                                +{task.assignees.length - 3}
                              </div>
                            )}
                            {task.assignees.length === 0 && (
                              <span style={{ fontSize: '0.75rem', color: 'var(--color-text-muted)', fontStyle: 'italic' }}>
                                Neasignat
                              </span>
                            )}
                          </div>

                          {/* Quick Status Select / Mover */}
                          <div
                            onClick={(e) => e.stopPropagation()}
                            style={{ position: 'relative' }}
                          >
                            <select
                              value={task.status}
                              onChange={(e) =>
                                handleStatusChange(task.id, Number(e.target.value) as TaskStatus)
                              }
                              className="kanban-quick-status"
                            >
                              <option value={TaskStatus.Pending}>În așteptare</option>
                              <option value={TaskStatus.InProgress}>În lucru</option>
                              <option value={TaskStatus.Completed}>Finalizat</option>
                            </select>
                          </div>
                        </div>
                      </div>
                    );
                  })}

                  {colTasks.length === 0 && (
                    <div className="kanban-empty-column">
                      Nicio sarcină în această etapă.
                    </div>
                  )}
                </div>
              </div>
            );
          })}
        </div>
      )}

      {/* Create / Edit Modal */}
      <TaskModal
        isOpen={isEditModalOpen}
        task={editingTask}
        availableUsers={availableUsers}
        onClose={() => {
          setIsEditModalOpen(false);
          setEditingTask(null);
        }}
        onSave={handleSaveTask}
      />

      {/* Task Detail Modal */}
      <TaskDetailModal
        task={viewingTask}
        onClose={() => setViewingTask(null)}
        onEdit={(task) => {
          setEditingTask(task);
          setIsEditModalOpen(true);
        }}
        onDelete={handleDeleteTask}
        onStatusChange={handleStatusChange}
      />
    </div>
  );
}
