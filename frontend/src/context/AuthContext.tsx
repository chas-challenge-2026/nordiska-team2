import { createContext, useContext, useState, useEffect } from 'react';
import type { ReactNode } from 'react';
import { setAuthToken, registerTokenChangeHandler } from '../client';

interface AuthContextType {
  accessToken: string | null;
  setAccessToken: (token: string | null) => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [accessToken, setAccessTokenState] = useState<string | null>(null);

  function setAccessToken(token: string | null) {
    setAccessTokenState(token);
    setAuthToken(token);
  }
  /*
    Lets client.ts's interceptor update React state when it silently
    refreshes a token (or clears one on refresh failure).
    
    Without this, a component reading accessToken from context could go stale
    relative to what's actually being sent on requests.
  */
  useEffect(() => {
    registerTokenChangeHandler(setAccessTokenState);
  }, []);

  return (
    <AuthContext.Provider value={{ accessToken, setAccessToken }}>
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