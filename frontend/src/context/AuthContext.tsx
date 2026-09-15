import { createContext, useContext, useState, useEffect } from 'react';
import type { ReactNode } from 'react';
import { setAuthToken, registerTokenChangeHandler } from '../client';

interface AuthContextType {
  accessToken: string | null;
  isInitializing: boolean;
  setAccessToken: (token: string | null) => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [accessToken, setAccessTokenState] = useState<string | null>(null);
  // Starts true: we don't yet know if the user has a valid session
  // (the refresh cookie) until the silent-refresh attempt below
  // finishes. Anything checking "is this user logged in" needs to
  // wait for this to become false before trusting accessToken's value.
  const [isInitializing, setIsInitializing] = useState(true);
 
  function setAccessToken(token: string | null) {
    setAccessTokenState(token);
    setAuthToken(token);
  }
 
  useEffect(() => {
    registerTokenChangeHandler(setAccessTokenState);
 
    // On app load, the access token is always gone (memory-only,
    // doesn't survive a reload) even if the user is genuinely still
    // logged in via their HttpOnly refresh cookie. Try once, silently,
    // to restore the session before anything decides the user is
    // logged out.
    async function attemptSilentRefresh() {
      try {
        const response = await apiClient.post('/api/auth/refresh');
        setAccessToken(response.data.accessToken);
      } catch {
        // No valid refresh cookie (never logged in, or it expired) --
        // that's a normal, expected outcome, not an error to surface.
      } finally {
        setIsInitializing(false);
      }
    }
 
    attemptSilentRefresh();
  }, []);
 
  return (
    <AuthContext.Provider value={{ accessToken, isInitializing, setAccessToken }}>
      {children}
    </AuthContext.Provider>
  );
}
 
export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}