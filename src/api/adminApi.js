import axiosInstance from "./axiosInstance";

/*
=========================
GET ALL
=========================
*/
export const getTrialRequests = async () => {
  const response = await axiosInstance.get("/admin/trial-requests");
  return response.data;
};

/*
=========================
APPROVE
=========================
*/
export const approveTrialRequest = async (id) => {
  const response = await axiosInstance.post(`/admin/approve/${id}`);
  return response.data;
};

/*
=========================
RESEND INVITE
=========================
*/
export const resendInvite = async (id) => {
  const response = await axiosInstance.post(`/admin/resend-invite/${id}`);
  return response.data;
};

/*
=========================
DECLINE
=========================
*/
export const declineTrialRequest = async (id) => {
  const response = await axiosInstance.post(`/admin/decline/${id}`);
  return response.data;
};

/*
=========================
MORE INFO
=========================
*/
export const requestMoreInfo = async (id) => {
  const response = await axiosInstance.post(`/admin/request-more-info/${id}`);
  return response.data;
};

/*
=========================
UPDATE STATUS (OPTIONAL)
=========================
*/
export const updateStatus = async (data) => {
  const response = await axiosInstance.put("/admin/update-status", data);
  return response.data;
};