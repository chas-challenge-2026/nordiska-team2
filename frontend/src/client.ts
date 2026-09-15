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

    if (!error.response) {
      console.error("Network error (no response received):", error.message);
      return Promise.reject(error);
    }

    switch (status) {
      case 401: {
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
        console.error("403 Forbidden:", originalRequest?.url);
        return Promise.reject(error);
      }

      case 404: {
        console.error("404 Not Found:", originalRequest?.url);
        return Promise.reject(error);
      }

      case 429: {
        console.error("429 Too Many Requests:", originalRequest?.url);
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