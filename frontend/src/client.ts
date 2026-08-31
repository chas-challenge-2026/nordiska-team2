import axios from "axios";

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  headers: {
    "Content-Type": "application/json",
  },
});

// Enkel global felhantering — byggs ut senare med JWT. 
// Interceptor körs automatiskt varje gång ett API-anrop görs.
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    console.error("API error:", error.response?.status, error.message);
    return Promise.reject(error);
  }
);