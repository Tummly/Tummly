import "./axiosTypes"

import axios from "axios"

import { API_BASE_URL } from "../config/api"
import { ensureFreshAccessToken } from "@/api/sessionRefresh"
import { getAuthToken, getRefreshToken, useAuthStore } from "@/stores/authStore"

const axiosInstance = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    "Content-Type": "application/json",
  },
})

function redirectToLogin() {
  useAuthStore.getState().clearSession()
  window.location.href = "/login"
}

axiosInstance.interceptors.request.use(
  async (config) => {
    if (!config.skipAuthRedirect) {
      await ensureFreshAccessToken()
    }

    const token = getAuthToken()

    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }

    return config
  },
  (error) => Promise.reject(error)
)

axiosInstance.interceptors.response.use(
  (response) => response,
  async (error) => {
    const status = error.response?.status
    const data = error.response?.data as
      | { activationExpired?: boolean; activationRequired?: boolean }
      | undefined
    const config = error.config

    if (
      status === 403
      && data?.activationRequired === true
      && !config?.skipAuthRedirect
    ) {
      window.location.href = "/login?step=activation-code"
      return Promise.reject(error)
    }

    if (
      status === 403
      && data?.activationExpired === true
      && !config?.skipAuthRedirect
    ) {
      redirectToLogin()
      return Promise.reject(error)
    }

    if (
      status === 401
      && !config?.skipAuthRedirect
      && !config?._authRetried
    ) {
      if (getRefreshToken()) {
        const refreshed = await ensureFreshAccessToken({ force: true })

        if (refreshed && config) {
          config._authRetried = true
          config.headers = config.headers ?? {}
          config.headers.Authorization = `Bearer ${refreshed}`
          return axiosInstance.request(config)
        }
      }

      redirectToLogin()
    }

    return Promise.reject(error)
  }
)

export default axiosInstance
