import axios from 'axios';

const baseURL = import.meta.env.VITE_API_URL || 'http://localhost:5080/api';
export const api = axios.create({ baseURL, withCredentials: true });
let accessToken = null;
let refreshPromise = null;
let unauthorizedHandler = () => {};

export function setAccessToken(token) {
  accessToken = token || null;
  if (accessToken) api.defaults.headers.common.Authorization = `Bearer ${accessToken}`;
  else delete api.defaults.headers.common.Authorization;
}
export function setUnauthorizedHandler(handler) { unauthorizedHandler = handler || (() => {}); }

export async function refreshSession() {
  if (!refreshPromise) {
    refreshPromise = axios.post(`${baseURL}/auth/refresh`, {}, { withCredentials: true })
      .then(({ data }) => { setAccessToken(data.accessToken); return data; })
      .finally(() => { refreshPromise = null; });
  }
  return refreshPromise;
}

api.interceptors.request.use(config => {
  if (accessToken) config.headers.Authorization = `Bearer ${accessToken}`;
  return config;
});
api.interceptors.response.use(response => response, async error => {
  const request = error.config;
  if (error.response?.status === 401 && request && !request._retried && !request.url?.startsWith('/auth/')) {
    request._retried = true;
    try { await refreshSession(); return api(request); }
    catch { setAccessToken(null); unauthorizedHandler(); }
  }
  return Promise.reject(error);
});

export async function getPage(path, params = {}) { const { data } = await api.get(path, { params }); return data; }
