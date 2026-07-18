const DEFAULT_API_BASE_URL = "https://api.qa.tummly.com/api";

const apiBaseUrl =
  import.meta.env.VITE_API_BASE_URL ?? DEFAULT_API_BASE_URL;

export const API_BASE_URL = apiBaseUrl.replace(/\/$/, "");
export const AUTH_API_BASE_URL = `${API_BASE_URL}/auth`;
