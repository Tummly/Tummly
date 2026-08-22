import axios from "axios"

import "./axiosTypes"

import { API_BASE_URL } from "@/config/api"
import { ensureFreshAccessToken } from "@/api/sessionRefresh"
import { getAuthToken, getRefreshToken, useAuthStore } from "@/stores/authStore"

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

loginContextClient.interceptors.request.use(async (config) => {
  await ensureFreshAccessToken()
  const token = getAuthToken()

  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }

  return config
})

loginContextClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const status = error.response?.status
    const config = error.config

    if (status === 401 && !config?._authRetried && getRefreshToken()) {
      const refreshed = await ensureFreshAccessToken({ force: true })

      if (refreshed && config) {
        config._authRetried = true
        config.headers = config.headers ?? {}
        config.headers.Authorization = `Bearer ${refreshed}`
        return loginContextClient.request(config)
      }

      useAuthStore.getState().clearSession()
    }

    return Promise.reject(error)
  }
)

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
