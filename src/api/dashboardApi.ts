import axiosInstance from "./axiosInstance";
import type {
  LocationsResponse,
  FeedbackResponse,
  ChecklistAcksResponse,
  UpdateChecklistAcksRequest,
} from "../types/dashboard";

export const getLocations = async (): Promise<LocationsResponse> => {
  const response = await axiosInstance.get<LocationsResponse>(
    "/restaurant/locations"
  );
  return response.data;
};

export const getFeedback = async (
  locationId: number
): Promise<FeedbackResponse> => {
  const response = await axiosInstance.get<FeedbackResponse>(
    "/feedback",
    { params: { locationId } }
  );
  return response.data;
};

export const getChecklistAcks = async (
  locationId: number
): Promise<ChecklistAcksResponse> => {
  const response = await axiosInstance.get<ChecklistAcksResponse>(
    "/operator-home/checklist-acks",
    { params: { locationId } }
  );
  return response.data;
};

export const setChecklistAcks = async (
  locationId: number,
  body: UpdateChecklistAcksRequest
): Promise<ChecklistAcksResponse> => {
  const response = await axiosInstance.post<ChecklistAcksResponse>(
    "/operator-home/checklist-acks",
    body,
    { params: { locationId } }
  );
  return response.data;
};

export const downloadQrCode = async (
  locationId: number
): Promise<Blob> => {
  const response = await axiosInstance.get("/qr/download", {
    params: { locationId },
    responseType: "blob",
  });
  return response.data;
};
