import axiosInstance from "./axiosInstance";
import type {
  CompleteSetupPayload,
  TrialRequestPayload,
  VerifyOtpPayload,
} from "../types/trial";

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
  const response = await axiosInstance.get(
    `/Trial/validate-setup-token?token=${token}`
  );
  return response.data;
};

export const completeSetupRequest = async (
  data: CompleteSetupPayload
): Promise<unknown> => {
  const response = await axiosInstance.post("/Trial/complete-setup", data);
  return response.data;
};
