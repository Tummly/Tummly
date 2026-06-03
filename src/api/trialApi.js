import axiosInstance from "./axiosInstance";

/*
 =========================================
 SUBMIT TRIAL REQUEST
 =========================================
*/

export const submitTrialRequest = async (
  data
) => {

  const response =
    await axiosInstance.post(
      "/Trial/request-trial",
      data
    );

  return response.data;
};

/*
 =========================================
 VERIFY OTP
 =========================================
*/

export const verifyOtpRequest = async (
  data
) => {

  const response =
    await axiosInstance.post(
      "/Trial/verify-otp",
      data
    );

  return response.data;
};

/*
 =========================================
 RESEND OTP
 =========================================
*/

export const resendOtpRequest = async (
  email
) => {

  const response =
    await axiosInstance.post(
      "/Trial/resend-otp",
      {
        email
      }
    );

  return response.data;
};

/*
 =========================================
 VALIDATE SETUP TOKEN
 =========================================
*/

export const validateSetupToken = async (
  token
) => {

  const response =
    await axiosInstance.get(
      `/Trial/validate-setup-token?token=${token}`
    );

  return response.data;
};

/*
 =========================================
 COMPLETE ACCOUNT SETUP
 =========================================
*/

export const completeSetupRequest = async (
  data
) => {

  const response =
    await axiosInstance.post(
      "/Trial/complete-setup",
      data
    );

  return response.data;
};