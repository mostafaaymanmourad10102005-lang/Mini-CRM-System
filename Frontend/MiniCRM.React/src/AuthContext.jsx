import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import { api, refreshSession, setAccessToken, setUnauthorizedHandler } from './api';

const AuthContext = createContext(null);
export function AuthProvider({ children }) {
  const [user, setUser] = useState(null);
  const [ready, setReady] = useState(false);
  const accept = useCallback(data => { setAccessToken(data.accessToken); setUser(data.user); return data.user; }, []);
  useEffect(() => {
    let active = true;
    setUnauthorizedHandler(() => { if (active) setUser(null); });
    refreshSession().then(data => { if (active) setUser(data.user); }).catch(() => { if (active) { setAccessToken(null); setUser(null); } }).finally(() => { if (active) setReady(true); });
    return () => { active = false; setUnauthorizedHandler(null); };
  }, []);
  const login = useCallback(async credentials => accept((await api.post('/auth/login', credentials)).data), [accept]);
  const register = useCallback(async details => accept((await api.post('/auth/register', details)).data), [accept]);
  const logout = useCallback(async () => { try { await api.post('/auth/logout'); } finally { setAccessToken(null); setUser(null); } }, []);
  const value = useMemo(() => ({ user, ready, login, register, logout, setUser }), [user, ready, login, register, logout]);
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
export function useAuth() { const value = useContext(AuthContext); if (!value) throw new Error('useAuth must be used inside AuthProvider'); return value; }
