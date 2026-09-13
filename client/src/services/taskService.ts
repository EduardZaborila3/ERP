import { authService } from './authService';

export const TaskStatus = {
  Pending: 0,
  InProgress: 1,
  Completed: 2
} as const;

export type TaskStatus = (typeof TaskStatus)[keyof typeof TaskStatus];

export interface TaskAssignee {
  id: number;
  email: string;
  firstName: string;
  lastName: string;
}

export interface TaskOwner {
  id: number;
  email: string;
  firstName: string;
  lastName: string;
}

export interface AppTask {
  id: number;
  ownerId: number;
  owner?: TaskOwner | null;
  title: string;
  description: string;
  status: TaskStatus;
  createdAt: string;
  deadline?: string | null;
  assignees: TaskAssignee[];
}

export interface CreateTaskInput {
  title: string;
  description: string;
  status?: TaskStatus;
  deadline?: string | null;
  assigneeIds?: number[];
}

export interface UpdateTaskInput {
  title: string;
  description: string;
  status: TaskStatus;
  deadline?: string | null;
  assigneeIds?: number[];
}

export interface TaskSummary {
  totalTasks: number;
  pendingTasks: number;
  inProgressTasks: number;
  completedTasks: number;
  overdueTasks: number;
}

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5110/api';

export const taskService = {
  async getTasks(params?: {
    status?: TaskStatus;
    assigneeId?: number | string;
    ownerId?: number | string;
    search?: string;
  }): Promise<AppTask[]> {
    const query = new URLSearchParams();
    if (params?.status !== undefined) {
      query.append('status', params.status.toString());
    }
    if (params?.assigneeId) {
      query.append('assigneeId', params.assigneeId.toString());
    }
    if (params?.ownerId) {
      query.append('ownerId', params.ownerId.toString());
    }
    if (params?.search) {
      query.append('search', params.search);
    }

    const url = `${API_URL}/tasks${query.toString() ? `?${query.toString()}` : ''}`;
    const response = await authService.authFetch(url);

    if (!response.ok) {
      throw new Error('Eroare la încărcarea listei de sarcini.');
    }

    return response.json();
  },

  async getTaskById(id: number): Promise<AppTask> {
    const response = await authService.authFetch(`${API_URL}/tasks/${id}`);
    if (!response.ok) {
      throw new Error(`Sarcina cu ID ${id} nu a fost găsită.`);
    }
    return response.json();
  },

  async getTaskSummary(): Promise<TaskSummary> {
    const response = await authService.authFetch(`${API_URL}/tasks/summary`);
    if (!response.ok) {
      throw new Error('Eroare la încărcarea sumarului de sarcini.');
    }
    return response.json();
  },

  async createTask(data: CreateTaskInput): Promise<AppTask> {
    const response = await authService.authFetch(`${API_URL}/tasks`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data)
    });

    if (!response.ok) {
      const err = await response.text();
      throw new Error(err || 'Eroare la crearea sarcinii.');
    }

    return response.json();
  },

  async updateTask(id: number, data: UpdateTaskInput): Promise<AppTask> {
    const response = await authService.authFetch(`${API_URL}/tasks/${id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data)
    });

    if (!response.ok) {
      const err = await response.text();
      throw new Error(err || 'Eroare la actualizarea sarcinii.');
    }

    return response.json();
  },

  async updateTaskStatus(id: number, status: TaskStatus): Promise<AppTask> {
    const response = await authService.authFetch(`${API_URL}/tasks/${id}/status`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ status })
    });

    if (!response.ok) {
      const err = await response.text();
      throw new Error(err || 'Eroare la schimbarea stării sarcinii.');
    }

    return response.json();
  },

  async deleteTask(id: number): Promise<void> {
    const response = await authService.authFetch(`${API_URL}/tasks/${id}`, {
      method: 'DELETE'
    });

    if (!response.ok) {
      const err = await response.text();
      throw new Error(err || 'Eroare la ștergerea sarcinii.');
    }
  }
};
