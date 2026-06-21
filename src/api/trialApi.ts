import axiosInstance from "./axiosInstance"
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

export const validateInviteToken = async (token: string): Promise<unknown> => {
  const response = await axiosInstance.get("/auth/validate-invite", {
    params: { token },
    skipAuthRedirect: true,
  })
  return response.data
}
