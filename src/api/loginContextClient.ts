import axios from "axios"

import { API_BASE_URL } from "@/config/api"
import { getAuthToken } from "@/stores/authStore"

/**
 * Authenticated auth endpoints used during the login wizard.
 * No 401 redirect — callers clear stale sessions explicitly.
 */
const loginContextClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    "Content-Type": "application/json",
  },
})

loginContextClient.interceptors.request.use((config) => {
  const token = getAuthToken()

  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }

  return config
})

export async function fetchCurrentUser(): Promise<unknown> {
  const response = await loginContextClient.get("/auth/me")
  return response.data
}

export async function fetchWorkspaces(): Promise<unknown> {
  const response = await loginContextClient.get("/auth/workspaces")
  return response.data
}

export async function selectWorkspace(
  locationId: number
): Promise<unknown> {
  const response = await loginContextClient.post("/auth/select-workspace", {
    locationId,
  })
  return response.data
}
