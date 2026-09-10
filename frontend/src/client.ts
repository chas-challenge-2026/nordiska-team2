import axios from "axios";

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  withCredentials: true,
  headers: {
    "Content-Type": "application/json",
  },
});

export function setAuthToken(token: string | null) {
  if (token) {
    apiClient.defaults.headers.common['Authorization'] = `Bearer ${token}`;
  } else {
    delete apiClient.defaults.headers.common['Authorization'];
  }
}

// Bridges token changes happening OUTSIDE React (the interceptor below)
// back into React state (AuthContext).
type TokenChangeHandler = (token: string | null) => void;
let onTokenChange: TokenChangeHandler | null = null;

export function registerTokenChangeHandler(handler: TokenChangeHandler) {
  onTokenChange = handler;
}

// Same bridge pattern for the redirect-on-session-end behavior. Default
// is a real browser redirect; tests substitute their own handler so
// this is verifiable without fighting jsdom's window.location object.
let redirectToLogin: () => void = () => {
  window.location.href = '/login';
};

export function setRedirectHandler(handler: () => void) {
  redirectToLogin = handler;
}

// Enkel global felhantering — byggs ut senare med JWT.
// Interceptor körs automatiskt varje gång ett API-anrop görs.
apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;

      try {
        const refreshResponse = await apiClient.post('/api/auth/refresh');
        const newToken = refreshResponse.data.accessToken;

        setAuthToken(newToken);
        onTokenChange?.(newToken);

        originalRequest.headers['Authorization'] = `Bearer ${newToken}`;
        return apiClient(originalRequest);
      } catch (refreshError) {
        setAuthToken(null);
        onTokenChange?.(null);
        redirectToLogin();
        return Promise.reject(refreshError);
      }
    }

    console.error("API error:", error.response?.status, error.message);
    return Promise.reject(error);
  }
);