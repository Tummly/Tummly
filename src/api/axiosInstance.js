import axios from "axios";

const axiosInstance = axios.create({
  baseURL: "http://localhost:5204/api",

  headers: {
    "Content-Type": "application/json",
  },
});

/*
 =========================================
 REQUEST INTERCEPTOR
 =========================================
*/

axiosInstance.interceptors.request.use(
  (config) => {

    const token =
      localStorage.getItem("token");

    if (token) {
      config.headers.Authorization =
        `Bearer ${token}`;
    }

    return config;
  },

  (error) => {
    return Promise.reject(error);
  }
);

/*
 =========================================
 RESPONSE INTERCEPTOR
 =========================================
*/

axiosInstance.interceptors.response.use(

  (response) => response,

  (error) => {

    /*
     =========================================
     AUTO LOGOUT ON 401
     =========================================
    */

    if (error.response?.status === 401) {

      localStorage.removeItem("token");

      window.location.href = "/login";
    }

    return Promise.reject(error);
  }
);

export default axiosInstance;