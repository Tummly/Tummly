import { useState } from "react";
import { useSearchParams } from "react-router-dom";
import axios, { isAxiosError } from "axios";
import { AUTH_API_BASE_URL } from "../../config/api";

interface ResetPasswordResponse {
  message: string;
}

const ResetPasswordPage = () => {
  const [searchParams] = useSearchParams();
  const token = searchParams.get("token");

  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [loading, setLoading] = useState(false);

  const handleResetPassword = async () => {
    try {
      if (!token) {
        alert("Invalid or missing token");
        return;
      }

      if (!newPassword || !confirmPassword) {
        alert("Please fill all fields");
        return;
      }

      if (newPassword !== confirmPassword) {
        alert("Passwords do not match");
        return;
      }

      setLoading(true);
      console.log("TOKEN:", token);

      const response = await axios.post<ResetPasswordResponse>(
        `${AUTH_API_BASE_URL}/reset-password`,
        {
          token,
          newPassword,
          confirmPassword,
        }
      );

      alert(response.data.message);
      window.location.href = "/login";
    } catch (error: unknown) {
      console.log("RESET PASSWORD ERROR:", isAxiosError(error) ? error.response : error);

      if (isAxiosError<ResetPasswordResponse>(error)) {
        alert(error.response?.data?.message || "Something went wrong");
      } else {
        alert("Something went wrong");
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div
      style={{
        width: "100%",
        minHeight: "100vh",
        display: "flex",
        justifyContent: "center",
        alignItems: "center",
        background: "#f5f5f5",
      }}
    >
      <div
        style={{
          width: "420px",
          background: "#fff",
          padding: "40px",
          borderRadius: "12px",
          boxShadow: "0 10px 30px rgba(0,0,0,0.08)",
        }}
      >
        <h2 style={{ margin: 0, marginBottom: "10px" }}>Reset Password</h2>

        <p
          style={{
            color: "#666",
            fontSize: "14px",
            marginBottom: "25px",
          }}
        >
          Enter your new password below.
        </p>

        <input
          type="password"
          placeholder="New Password"
          value={newPassword}
          onChange={(e) => setNewPassword(e.target.value)}
          style={{
            width: "100%",
            height: "48px",
            padding: "0 14px",
            marginBottom: "15px",
            border: "1px solid #ccc",
            borderRadius: "6px",
            boxSizing: "border-box",
          }}
        />

        <input
          type="password"
          placeholder="Confirm Password"
          value={confirmPassword}
          onChange={(e) => setConfirmPassword(e.target.value)}
          style={{
            width: "100%",
            height: "48px",
            padding: "0 14px",
            marginBottom: "20px",
            border: "1px solid #ccc",
            borderRadius: "6px",
            boxSizing: "border-box",
          }}
        />

        <button
          onClick={handleResetPassword}
          disabled={loading}
          style={{
            width: "100%",
            height: "48px",
            border: "none",
            borderRadius: "6px",
            background: "#18AE47",
            color: "#fff",
            fontSize: "15px",
            fontWeight: 600,
            cursor: "pointer",
          }}
        >
          {loading ? "Please wait..." : "Update Password"}
        </button>
      </div>
    </div>
  );
};

export default ResetPasswordPage;
