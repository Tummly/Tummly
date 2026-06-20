import axiosInstance from "./axiosInstance";
import type {
  LocationsResponse,
  FeedbackResponse,
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

export const downloadQrCode = async (
  locationId: number
): Promise<Blob> => {
  const response = await axiosInstance.get("/qr/download", {
    params: { locationId },
    responseType: "blob",
  });
  return response.data;
};
