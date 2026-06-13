import axiosInstance from "./axiosInstance";
import type {
  AdminTrialRequest,
  AdminTrialRequestsResponse,
  UpdateTrialStatusPayload,
} from "../types/admin";

export const getTrialRequests = async (): Promise<AdminTrialRequest[]> => {
  const response = await axiosInstance.get<AdminTrialRequestsResponse>(
    "/admin/trial-requests"
  );
  return response.data.data;
};

export const approveTrialRequest = async (id: number): Promise<unknown> => {
  const response = await axiosInstance.post(`/admin/approve/${id}`);
  return response.data;
};

export const resendInvite = async (id: number): Promise<unknown> => {
  const response = await axiosInstance.post(`/admin/resend-invite/${id}`);
  return response.data;
};

export const declineTrialRequest = async (id: number): Promise<unknown> => {
  const response = await axiosInstance.post(`/admin/decline/${id}`);
  return response.data;
};

export const requestMoreInfo = async (id: number): Promise<unknown> => {
  const response = await axiosInstance.post(`/admin/request-more-info/${id}`);
  return response.data;
};

export const updateStatus = async (
  data: UpdateTrialStatusPayload
): Promise<unknown> => {
  const response = await axiosInstance.put("/admin/update-status", data);
  return response.data;
};
