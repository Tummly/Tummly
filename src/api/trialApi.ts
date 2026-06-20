import axios from "axios"

import axiosInstance from "./axiosInstance"
import { API_BASE_URL } from "../config/api"
import type {
  TrialRequestPayload,
  VerifyOtpPayload,
} from "../types/trial"

export const submitTrialRequest = async (
  data: TrialRequestPayload
): Promise<unknown> => {
  const response = await axiosInstance.post("/Trial/request-trial", data);
  return response.data;
};

export const verifyOtpRequest = async (
  data: VerifyOtpPayload
): Promise<unknown> => {
  const response = await axiosInstance.post("/Trial/verify-otp", data);
  return response.data;
};

export const resendOtpRequest = async (email: string): Promise<unknown> => {
  const response = await axiosInstance.post("/Trial/resend-otp", { email });
  return response.data;
};

export const validateSetupToken = async (token: string): Promise<unknown> => {
  const response = await axios.get(
    `${API_BASE_URL}/Trial/validate-setup-token`,
    {
      params: { token },
    }
  )
  return response.data
}
