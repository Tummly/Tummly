import "./axiosTypes"

import axios from "axios"

import { API_BASE_URL } from "../config/api"
import { getAuthToken, useAuthStore } from "@/stores/authStore"

const axiosInstance = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    "Content-Type": "application/json",
  },
})

axiosInstance.interceptors.request.use(
  (config) => {
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
  (error) => {
    const status = error.response?.status
    const data = error.response?.data as
      | { activationExpired?: boolean }
      | undefined

    if (
      status === 403
      && data?.activationRequired === true
      && !error.config?.skipAuthRedirect
    ) {
      window.location.href = "/login?step=activation-code"
      return Promise.reject(error)
    }

    if (
      status === 403
      && data?.activationExpired === true
      && !error.config?.skipAuthRedirect
    ) {
      useAuthStore.getState().clearSession()
      window.location.href = "/login"
    }

    if (
      status === 401
      && !error.config?.skipAuthRedirect
    ) {
      useAuthStore.getState().clearSession()
      window.location.href = "/login"
    }

    return Promise.reject(error)
  }
)

export default axiosInstance
