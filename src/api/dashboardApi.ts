import axiosInstance from "./axiosInstance";
import type {
  LocationsResponse,
  FeedbackResponse,
  FeedbackDetailsResponse,
  HomeLatestActivityResponse,
  HomePerformanceResponse,
  GuestsResponse,
  CorrectFeedbackClassificationRequest,
  CorrectFeedbackClassificationResponse,
  ChecklistAcksResponse,
  UpdateChecklistAcksRequest,
  FeedbackSentiment,
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

export const getHomeLatestActivity = async (
  locationId: number
): Promise<HomeLatestActivityResponse> => {
  const response = await axiosInstance.get<HomeLatestActivityResponse>(
    "/home/latest-activity",
    { params: { locationId } }
  );
  return response.data;
};

export const getGuests = async (params: {
  locationId: number;
  smartGroup?: string;
  q?: string;
  sort?: string;
  page?: number;
  pageSize?: number;
}): Promise<GuestsResponse> => {
  const response = await axiosInstance.get<GuestsResponse>("/guests", {
    params,
  });
  return response.data;
};

export const getHomePerformance = async (
  locationId: number,
  from: string,
  to: string
): Promise<HomePerformanceResponse> => {
  const response = await axiosInstance.get<HomePerformanceResponse>(
    "/home/performance",
    { params: { locationId, from, to } }
  );
  return response.data;
};

export const getFeedbackDetails = async (
  feedbackId: number
): Promise<FeedbackDetailsResponse> => {
  const response = await axiosInstance.get<FeedbackDetailsResponse>(
    `/feedback/${feedbackId}`
  );
  return response.data;
};

export const correctFeedbackClassification = async (
  feedbackId: number,
  sentiment: FeedbackSentiment
): Promise<CorrectFeedbackClassificationResponse> => {
  const body: CorrectFeedbackClassificationRequest = { sentiment };
  const response =
    await axiosInstance.put<CorrectFeedbackClassificationResponse>(
      `/feedback/${feedbackId}/classification`,
      body
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
