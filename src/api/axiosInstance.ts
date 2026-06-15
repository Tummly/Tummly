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
    if (error.response?.status === 401) {
      useAuthStore.getState().clearSession()
      window.location.href = "/login"
    }

    return Promise.reject(error)
  }
)

export default axiosInstance
