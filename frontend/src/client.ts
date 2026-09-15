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

type TokenChangeHandler = (token: string | null) => void;
let onTokenChange: TokenChangeHandler | null = null;

export function registerTokenChangeHandler(handler: TokenChangeHandler) {
  onTokenChange = handler;
}

let redirectToLogin: () => void = () => {
  window.location.href = '/login';
};

export function setRedirectHandler(handler: () => void) {
  redirectToLogin = handler;
}

let refreshPromise: Promise<string> | null = null;

async function refreshAccessToken(): Promise<string> {
  if (!refreshPromise) {
    refreshPromise = apiClient
      .post('/auth/refresh')
      .then((response) => {
        const newToken = response.data.accessToken;
        setAuthToken(newToken);
        onTokenChange?.(newToken);
        return newToken;
      })
      .finally(() => {
        refreshPromise = null;
      });
  }
  return refreshPromise;
}

apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;
    const status = error.response?.status;
    const isAuthEndpoint = originalRequest?.url?.includes('/auth/');

    // No response at all -- network failure, CORS block, backend down.
    // Not a status code case at all, so handle it first and bail out
    // before touching anything status-specific below.
    if (!error.response) {
      console.error("Network error (no response received):", error.message);
      return Promise.reject(error);
    }

    switch (status) {
      case 401: {
        // A 401 from an auth endpoint itself is NOT "my token expired" --
        // login's 401 means wrong credentials, refresh's own 401 means
        // no valid session. Neither should trigger another refresh
        // attempt (this is what caused the repeated-refresh-calls bug).
        if (isAuthEndpoint || originalRequest._retry) {
          console.error("401 from auth endpoint or already retried:", originalRequest?.url);
          return Promise.reject(error);
        }

        originalRequest._retry = true;

        try {
          const newToken = await refreshAccessToken();
          originalRequest.headers['Authorization'] = `Bearer ${newToken}`;
          return apiClient(originalRequest);
        } catch (refreshError) {
          setAuthToken(null);
          onTokenChange?.(null);
          redirectToLogin();
          return Promise.reject(refreshError);
        }
      }

      case 403: {
        // Authenticated, but not allowed -- e.g. trying to access an
        // account that isn't the caller's. Refreshing won't help here;
        // a new token for the same customer would fail the same way.
        console.error("403 Forbidden:", originalRequest?.url);
        return Promise.reject(error);
      }

      case 404: {
        console.error("404 Not Found:", originalRequest?.url);
        return Promise.reject(error);
      }

      case 429: {
        // Rate limited -- specifically on SensitiveEndpoints (login,
        // deposit, withdraw). Retrying immediately would just get
        // rate-limited again; this needs a real backoff/user message,
        // not automatic retry logic.
        console.error("429 Too Many Requests -- rate limited:", originalRequest?.url);
        return Promise.reject(error);
      }

      case 500: {
        console.error("500 Internal Server Error:", originalRequest?.url);
        return Promise.reject(error);
      }

      default: {
        console.error("API error:", status, error.message, originalRequest?.url);
        return Promise.reject(error);
      }
    }
  }
);