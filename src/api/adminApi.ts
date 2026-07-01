import axiosInstance from "./axiosInstance";
import type {
  AdminOperatorLocation,
  AdminTrialRequest,
  AdminTrialRequestsResponse,
  ExtendActivationPayload,
  UpdateTrialStatusPayload,
} from "../types/admin";

function normalizeOperatorLocations(
  value: unknown
): AdminOperatorLocation[] {
  if (!Array.isArray(value)) {
    return [];
  }

  return value.map((item) => {
    const location = item as Record<string, unknown>;

    return {
      locationName: String(location.locationName ?? location.LocationName ?? ""),
      address: String(location.address ?? location.Address ?? ""),
      postcode:
        (location.postcode ?? location.Postcode ?? null) as string | null,
      locationPhone:
        (location.locationPhone ?? location.LocationPhone ?? null) as
          | string
          | null,
      localContact:
        (location.localContact ?? location.LocalContact ?? null) as
          | string
          | null,
    };
  });
}

function normalizeTrialRequest(raw: Record<string, unknown>): AdminTrialRequest {
  const operatorLocations = normalizeOperatorLocations(
    raw.operatorLocations ?? raw.OperatorLocations
  );
  const primaryAddress =
    (raw.primaryAddress ?? raw.PrimaryAddress ?? null) as string | null;
  const primaryPostcode =
    (raw.primaryPostcode ?? raw.PrimaryPostcode ?? null) as string | null;
  const firstLocation = operatorLocations[0];

  return {
    ...(raw as AdminTrialRequest),
    mainLocation: String(raw.mainLocation ?? raw.MainLocation ?? ""),
    townCity: String(raw.townCity ?? raw.TownCity ?? ""),
    mainLocationPostcode: String(
      raw.mainLocationPostcode ?? raw.MainLocationPostcode ?? ""
    ),
    operatorLocations,
    primaryAddress:
      primaryAddress ?? (firstLocation?.address ? firstLocation.address : null),
    primaryPostcode:
      primaryPostcode ??
      (firstLocation?.postcode ? firstLocation.postcode : null),
    operatorUserId:
      (raw.operatorUserId ?? raw.OperatorUserId ?? null) as number | null,
    activationStatus:
      (raw.activationStatus ?? raw.ActivationStatus ?? null) as
        | AdminTrialRequest["activationStatus"]
        | undefined,
    activationStatusDetail:
      (raw.activationStatusDetail ?? raw.ActivationStatusDetail ?? null) as
        | AdminTrialRequest["activationStatusDetail"]
        | undefined,
    activationExpiresAt:
      (raw.activationExpiresAt ?? raw.ActivationExpiresAt ?? null) as
        | string
        | null,
    activationCode:
      (raw.activationCode ?? raw.ActivationCode ?? null) as string | null,
  };
}

export const getTrialRequests = async (): Promise<AdminTrialRequest[]> => {
  const response = await axiosInstance.get<AdminTrialRequestsResponse>(
    "/admin/trial-requests"
  );
  return response.data.data.map((item) =>
    normalizeTrialRequest(item as unknown as Record<string, unknown>)
  );
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

export const deleteTrialRequest = async (id: number): Promise<unknown> => {
  const response = await axiosInstance.delete(`/admin/trial-requests/${id}`);
  return response.data;
};

export const extendActivation = async (
  userId: number,
  payload: ExtendActivationPayload = {}
): Promise<AdminTrialRequest> => {
  const response = await axiosInstance.post(
    `/admin/operators/${userId}/extend-activation`,
    payload
  );

  return normalizeTrialRequest(
    response.data.data as Record<string, unknown>
  );
};

export const downloadActivationAsset = async (userId: number) => {
  const response = await axiosInstance.get(
    `/admin/operators/${userId}/activation-download`,
    { responseType: "blob" }
  );

  return response.data as Blob;
};
