const RAILWAY_API_BASE_URL =
  "https://tummly-backend-production.up.railway.app/api";

const apiBaseUrl =
  import.meta.env.VITE_API_BASE_URL ?? RAILWAY_API_BASE_URL;

export const API_BASE_URL = apiBaseUrl.replace(/\/$/, "");
export const AUTH_API_BASE_URL = `${API_BASE_URL}/auth`;
