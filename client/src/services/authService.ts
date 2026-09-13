export interface UserProfile {
  id: number;
  email: string;
  firstName: string;
  lastName: string;
  roleId: number;
  appRoleId?: number;
  jobTitleId: number;
  isActive: boolean;
}

export interface AuthResponse {
  token: string;
  user: UserProfile;
  message?: string;
}

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5110/api';
const TOKEN_KEY = 'erp_auth_token';
const USER_KEY = 'erp_auth_user';

export const authService = {
  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  },

  getStoredUser(): UserProfile | null {
    const raw = localStorage.getItem(USER_KEY);
    if (!raw) return null;
    try {
      return JSON.parse(raw);
    } catch {
      return null;
    }
  },

  setSession(token: string, user: UserProfile) {
    localStorage.setItem(TOKEN_KEY, token);
    const normalizedUser: UserProfile = {
      ...user,
      roleId: user.appRoleId || user.roleId || 1
    };
    localStorage.setItem(USER_KEY, JSON.stringify(normalizedUser));
  },

  logout() {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
  },

  async login(email: string, password: string): Promise<AuthResponse> {
    const response = await fetch(`${API_URL}/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password })
    });

    let data: any = null;
    try {
      data = await response.json();
    } catch {
      // fallback if response body is empty or not JSON
    }

    if (!response.ok) {
      throw new Error(data?.message || `Autentificare eșuată (${response.status}). Verificați credențialele.`);
    }

    const normalizedUser: UserProfile = {
      id: data.user.id,
      email: data.user.email,
      firstName: data.user.firstName,
      lastName: data.user.lastName,
      roleId: data.user.appRoleId || data.user.roleId || 1,
      appRoleId: data.user.appRoleId,
      jobTitleId: data.user.jobTitleId || 1,
      isActive: data.user.isActive ?? true
    };

    authService.setSession(data.token, normalizedUser);

    return {
      token: data.token,
      user: normalizedUser
    };
  },

  async register(data: {
    email: string;
    password: string;
    firstName: string;
    lastName: string;
    appRoleId: number;
    jobTitleId: number;
  }): Promise<AuthResponse> {
    const response = await fetch(`${API_URL}/auth/register`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data)
    });

    const resData = await response.json();

    if (!response.ok) {
      throw new Error(resData.message || 'Înregistrarea a eșuat.');
    }

    const normalizedUser: UserProfile = {
      id: resData.user.id,
      email: resData.user.email,
      firstName: resData.user.firstName,
      lastName: resData.user.lastName,
      roleId: resData.user.appRoleId || resData.user.roleId || 1,
      appRoleId: resData.user.appRoleId,
      jobTitleId: resData.user.jobTitleId || 1,
      isActive: resData.user.isActive ?? true
    };

    authService.setSession(resData.token, normalizedUser);

    return {
      token: resData.token,
      user: normalizedUser
    };
  },

  async setPassword(email: string, token: string, newPassword: string): Promise<AuthResponse> {
    const response = await fetch(`${API_URL}/auth/set-password`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, token, newPassword })
    });

    const data = await response.json();

    if (!response.ok) {
      throw new Error(data.message || 'Nu s-a putut salva noua parolă.');
    }

    if (data.token && data.user) {
      const normalizedUser: UserProfile = {
        id: data.user.id,
        email: data.user.email,
        firstName: data.user.firstName,
        lastName: data.user.lastName,
        roleId: data.user.appRoleId || data.user.roleId || 1,
        appRoleId: data.user.appRoleId,
        jobTitleId: data.user.jobTitleId || 1,
        isActive: data.user.isActive ?? true
      };
      authService.setSession(data.token, normalizedUser);
      return {
        token: data.token,
        user: normalizedUser,
        message: data.message
      };
    }

    return data;
  },

  async forgotPassword(email: string): Promise<{ message: string }> {
    const response = await fetch(`${API_URL}/auth/forgot-password`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        email,
        redirectUrl: window.location.origin
      })
    });

    const data = await response.json();

    if (!response.ok) {
      throw new Error(data.message || 'Eroare la trimiterea link-ului de resetare.');
    }

    return data;
  },

  async getMe(): Promise<UserProfile | null> {
    const token = authService.getToken();
    if (!token) return null;

    try {
      const response = await fetch(`${API_URL}/auth/me`, {
        headers: {
          Authorization: `Bearer ${token}`
        }
      });

      if (!response.ok) {
        authService.logout();
        return null;
      }

      const user = await response.json();
      const normalizedUser: UserProfile = {
        id: user.id,
        email: user.email,
        firstName: user.firstName,
        lastName: user.lastName,
        roleId: user.appRoleId || user.roleId || 1,
        appRoleId: user.appRoleId,
        jobTitleId: user.jobTitleId || 1,
        isActive: user.isActive ?? true
      };

      localStorage.setItem(USER_KEY, JSON.stringify(normalizedUser));
      return normalizedUser;
    } catch {
      return authService.getStoredUser();
    }
  },

  async authFetch(url: string, options: RequestInit = {}): Promise<Response> {
    const token = authService.getToken();
    const headers = new Headers(options.headers || {});
    
    if (token && !headers.has('Authorization')) {
      headers.set('Authorization', `Bearer ${token}`);
    }

    return fetch(url, {
      ...options,
      headers
    });
  }
};
