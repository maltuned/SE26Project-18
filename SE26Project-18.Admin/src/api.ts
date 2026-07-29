import axios from 'axios';
import { API_BASE } from './config';

const api = axios.create({
  baseURL: `${API_BASE}/admin`,
});

// Request interceptor: attach JWT token
api.interceptors.request.use(config => {
  const token = localStorage.getItem('admin_token');
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

// Image API proxy with auth
export const imageApi = axios.create({
  baseURL: API_BASE,
});

imageApi.interceptors.request.use(config => {
  const token = localStorage.getItem('admin_token');
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

imageApi.interceptors.response.use(
  res => res,
  err => {
    if (err.response?.status === 401) {
      localStorage.removeItem('admin_token');
      localStorage.removeItem('admin_info');
      window.location.href = '/admin/login';
    }
    return Promise.reject(err);
  }
);
api.interceptors.response.use(
  res => res,
  err => {
    if (err.response?.status === 401) {
      localStorage.removeItem('admin_token');
      localStorage.removeItem('admin_info');
      window.location.href = '/admin/login';
    }
    return Promise.reject(err);
  }
);

// Auth
export const adminLogin = (username: string, password: string) =>
  api.post('/login', { username, password }).then(r => r.data);

export const getPendingCount = () =>
  api.get('/pending-count').then(r => r.data);

// Reports
export const getReports = (status?: string) =>
  api.get('/reports', { params: { status } }).then(r => r.data);

export const handleReport = (id: number, status: string) =>
  api.put(`/reports/${id}`, { status }).then(r => r.data);

export const getReportTarget = (reportId: number) =>
  api.get(`/reports/${reportId}/target`).then(r => r.data);

// Feedbacks
export const getFeedbacks = (status?: string) =>
  api.get('/feedbacks', { params: { status } }).then(r => r.data);

export const handleFeedback = (id: number, status: string) =>
  api.put(`/feedbacks/${id}`, { status }).then(r => r.data);

// Users
export const searchUsers = (query: string) =>
  api.get('/users', { params: { search: query } }).then(r => r.data);

export const updateUser = (id: number, data: Record<string, string>) =>
  api.put(`/users/${id}`, data).then(r => r.data);

export const updateUserStatus = (id: number, status: string) =>
  api.put(`/users/${id}/status`, { status }).then(r => r.data);

export const clearUserProfile = (id: number) =>
  api.put(`/users/${id}/clear`).then(r => r.data);

// Recruitments
export const searchRecruitments = (query: string) =>
  api.get('/recruitments', { params: { search: query } }).then(r => r.data);

export const closeRecruitment = (id: number) =>
  api.put(`/recruitments/${id}/status`, { status: '已关闭' }).then(r => r.data);

export const deleteRecruitment = (id: number) =>
  api.delete(`/recruitments/${id}`).then(r => r.data);

// Games
export const searchGames = (query: string) =>
  api.get('/games', { params: { search: query } }).then(r => r.data);

export const updateGame = (id: number, data: Record<string, unknown>) =>
  api.put(`/games/${id}`, data).then(r => r.data);

export const createGame = (data: Record<string, unknown>) =>
  api.post('/games', data).then(r => r.data);

export const deleteGame = (id: number) =>
  api.delete(`/games/${id}`).then(r => r.data);

export const uploadImage = (file: File, folder: string, name?: string) => {
  const form = new FormData();
  form.append('file', file);
  form.append('folder', folder);
  if (name) form.append('name', name);
  return imageApi.post('/Image/upload', form, {
    headers: { 'Content-Type': 'multipart/form-data' },
  }).then(r => r.data);
};

// Notifications
export const sendNotification = (data: { userId?: number; title: string; body: string }) =>
  api.post('/notifications', data).then(r => r.data);