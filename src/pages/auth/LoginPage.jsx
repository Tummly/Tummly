// LoginSystem.jsx

import React, { useEffect, useState } from "react";
import {
  Eye,
  EyeOff,
  Lock,
  AlertCircle,
} from "lucide-react";


const API_BASE_URL = "http://localhost:5204/api/auth";

const LoginSystem = () => {
  /*
  =========================================================
  STEPS
  =========================================================
  */

  const STEPS = {
    LOGIN: "LOGIN",
    FORGOT_EMAIL: "FORGOT_EMAIL",
    FORGOT_OTP: "FORGOT_OTP",
    RESET_EMAIL: "RESET_EMAIL",
    RESET_PASSWORD: "RESET_PASSWORD",
    PASSWORD_SUCCESS: "PASSWORD_SUCCESS",
  };

  const [step, setStep] = useState(STEPS.LOGIN);

  /*
  =========================================================
  GLOBAL STATE
  =========================================================
  */

  const [loading, setLoading] = useState(false);
  const [globalError, setGlobalError] =
    useState("");
  const [globalSuccess, setGlobalSuccess] =
    useState("");

  /*
  =========================================================
  LOGIN
  =========================================================
  */

  const [loginData, setLoginData] = useState({
    email: "",
    password: "",
    rememberDevice: false,
  });

  const [showPassword, setShowPassword] =
    useState(false);

  /*
  =========================================================
  OTP
  =========================================================
  */

  const [forgotEmail, setForgotEmail] =
    useState("");

  const [otpCode, setOtpCode] = useState("");

  /*
  =========================================================
  RESET
  =========================================================
  */

  const [resetEmail, setResetEmail] =
    useState("");

  const [newPassword, setNewPassword] =
    useState("");

  const [confirmPassword, setConfirmPassword] =
    useState("");

  const [showNewPassword, setShowNewPassword] =
    useState(false);

  const [
    showConfirmPassword,
    setShowConfirmPassword,
  ] = useState(false);

  const [resetToken, setResetToken] =
    useState("");

  /*
  =========================================================
  TOKEN
  =========================================================
  */

  useEffect(() => {
    const params = new URLSearchParams(
      window.location.search
    );

    const token = params.get("token");

    if (token) {
      setResetToken(token);
      setStep(STEPS.RESET_PASSWORD);
    }
  }, []);

  /*
  =========================================================
  VALIDATIONS
  =========================================================
  */

  const validateEmail = (email) => {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(
      email
    );
  };

  const validatePassword = (password) => {
    return password.length >= 8;
  };

  const clearMessages = () => {
    setGlobalError("");
    setGlobalSuccess("");
  };

  /*
  =========================================================
  LOGIN API
  =========================================================
  */

  const handleLogin = async () => {
    clearMessages();

    if (!validateEmail(loginData.email)) {
      return setGlobalError(
        "Please enter a valid email."
      );
    }

    if (!validatePassword(loginData.password)) {
      return setGlobalError(
        "Password must be at least 8 characters."
      );
    }

    try {
      setLoading(true);

      const response = await fetch(
  `${API_BASE_URL}/universal-login`,  
        {
          method: "POST",
          headers: {
            "Content-Type":
              "application/json",
          },
          body: JSON.stringify({
            email: loginData.email,
            password: loginData.password,
            rememberDevice:
              loginData.rememberDevice,
          }),
        }
      );

      const result = await response.json();

      if (!response.ok) {
        return setGlobalError(
          result.message || "Login failed."
        );
      }

     // ADMIN LOGIN

if (result.loginType === "ADMIN")
{
    localStorage.setItem(
        "token",
        result.token
    );

    window.location.href =
        "/admin-dashboard";

    return;
}

// USER LOGIN

if (result.loginType === "USER")
{
    setForgotEmail(
        loginData.email
    );

    setStep(
        STEPS.FORGOT_OTP
    );

    setGlobalSuccess(
        "Verification code sent to your email."
    );

    return;
}
    } catch {
      setGlobalError(
        "Unable to connect server."
      );
    } finally {
      setLoading(false);
    }
  };

  /*
  =========================================================
  SEND OTP
  =========================================================
  */

  const handleSendOtp = async () => {
    clearMessages();

    if (!validateEmail(forgotEmail)) {
      return setGlobalError(
        "Enter valid email."
      );
    }

    try {
      setLoading(true);

      const response = await fetch(
        `${API_BASE_URL}/send-otp`,
        {
          method: "POST",
          headers: {
            "Content-Type":
              "application/json",
          },
          body: JSON.stringify({
            email: forgotEmail,
          }),
        }
      );

      const result = await response.json();

      if (!response.ok) {
        return setGlobalError(
          result.message ||
          "Unable to send OTP."
        );
      }

      setStep(STEPS.FORGOT_OTP);

      setGlobalSuccess(
        "OTP sent successfully."
      );
    } catch {
      setGlobalError("Network error.");
    } finally {
      setLoading(false);
    }
  };

  /*
  =========================================================
  VERIFY OTP
  =========================================================
  */

  const handleVerifyOtp = async () => {
  clearMessages();

  if (otpCode.length !== 6) {
    return setGlobalError("OTP must be 6 digits.");
  }

  try {
    setLoading(true);

    const response = await fetch(
      `${API_BASE_URL}/verify-otp`,
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          email: forgotEmail,
          otpCode: otpCode,   // ✅ FIXED (IMPORTANT)
        }),
      }
    );

    const result = await response.json();

    if (!response.ok) {
      return setGlobalError(
        result.message || "OTP verification failed."
      );
    }

    localStorage.setItem("token", result.token);
localStorage.setItem("role", result.loginType);

// redirect properly
if (result.loginType === "ADMIN") {
  window.location.href = "/admin-dashboard";
} else {
  window.location.href = "/multi-dashboard";
}
  } catch (error) {
    console.error(error);
    setGlobalError("Verification failed.");
  } finally {
    setLoading(false);
  }
};
  /*
  =========================================================
  SEND RESET LINK
  =========================================================
  */

  const handleSendResetLink = async () => {
  clearMessages();

  if (!validateEmail(resetEmail)) {
    return setGlobalError("Enter valid email.");
  }

  try {
    setLoading(true);

    const response = await fetch(
      `${API_BASE_URL}/forgot-password`,
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          email: resetEmail,
        }),
      }
    );

    const result = await response.json();

    if (!response.ok) {
      return setGlobalError(result.message || "Unable to send reset link.");
    }

    setGlobalSuccess("Reset link sent successfully.");
  } catch {
    setGlobalError("Request failed.");
  } finally {
    setLoading(false);
  }
};
  /*
  =========================================================
  UPDATE PASSWORD
  =========================================================
  */

  const handleUpdatePassword = async () => {
  clearMessages();

  if (!validatePassword(newPassword)) {
    return setGlobalError("Password must be at least 8 characters.");
  }

  if (newPassword !== confirmPassword) {
    return setGlobalError("Passwords do not match.");
  }

  try {
    setLoading(true);

    const response = await fetch(
      `${API_BASE_URL}/reset-password`,
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          token: resetToken,
          password: newPassword,
          confirmPassword,
        }),
      }
    );

    const result = await response.json();

    if (!response.ok) {
      return setGlobalError(result.message || "Unable to update password.");
    }

    setStep(STEPS.PASSWORD_SUCCESS);
  } catch {
    setGlobalError("Password update failed.");
  } finally {
    setLoading(false);
  }
};

  /*
  =========================================================
  INPUT FIELD
  =========================================================
  */

  const InputField = ({
    type = "text",
    placeholder,
    value,
    onChange,
    passwordToggle,
    showPassword,
    togglePassword,
  }) => {
    return (
      <div className="relative">
        <input
          type={
            passwordToggle
              ? showPassword
                ? "text"
                : "password"
              : type
          }
          placeholder={placeholder}
          value={value}
          onChange={onChange}
          className="
            w-full
            h-[64px]
            rounded-[4px]
            border
            border-[#D6D6D6]
            bg-white
            px-[18px]
            pr-[52px]
            text-[16px]
            font-[400]
            text-[#222222]
            outline-none
            transition-all
            duration-200
            placeholder:text-[#9B9B9B]
            hover:border-[#BEBEBE]
            focus:border-[#17A34A]
            focus:shadow-[0_0_0_3px_rgba(22,163,74,0.08)]
          "
        />

        {passwordToggle && (
          <button
            type="button"
            onClick={togglePassword}
            className="
              absolute
              right-[18px]
              top-[50%]
              translate-y-[-50%]
              text-[#8C8C8C]
              hover:text-[#4F4F4F]
            "
          >
            {showPassword ? (
              <EyeOff size={19} />
            ) : (
              <Eye size={19} />
            )}
          </button>
        )}
      </div>
    );
  };

  /*
  =========================================================
  BUTTON
  =========================================================
  */

  const PrimaryButton = ({
    title,
    onClick,
  }) => {
    return (
      <button
        onClick={onClick}
        disabled={loading}
        className="
          w-full
          h-[58px]
          rounded-[4px]
          bg-[#16A34A]
          text-white
          text-[17px]
          font-[500]
          transition-all
          duration-200
          hover:bg-[#14863E]
          active:scale-[0.995]
          disabled:bg-[#93D4AB]
        "
      >
        {loading ? "Please wait..." : title}
      </button>
    );
  };

  /*
  =========================================================
  FORM CARD
  =========================================================
  */

  const FormCard = ({
    title,
    subtitle,
    children,
  }) => {
    return (
      <div
        className="
    w-full
    max-w-[620px]
    min-h-[820px]
    bg-white
    border
    border-[#DCDCDC]
    rounded-[24px]
    px-[52px]
    py-[56px]
    shadow-[0_20px_60px_rgba(0,0,0,0.10)]
    flex
    flex-col
    justify-center
  "
      >
        <h2
          className="
    text-[42px]
    leading-[100%]
    font-[700]
    text-[#1F1F1F]
    mb-[56px]
    pl-[8px]
    mt-[-10px]
  "
        >
          {title}
        </h2>

        {subtitle && (
          <p
            className="
              text-[15px]
              text-[#666]
              leading-[24px]
              mb-[40px]
            "
          >
            {subtitle}
          </p>
        )}

        {children}
      </div>
    );
  };

  /*
  =========================================================
  MAIN UI
  =========================================================
  */

  return (
    <div
      className="
    min-h-screen
    bg-gradient-to-br
from-[#F8FAFC]
via-[#F4F7F9]
to-[#EEF4F6]
    flex
    flex-col
    lg:flex-row
  "
    >
      {/* =====================================================
      LEFT SIDE
      ===================================================== */}

      <div
        className="
          hidden
          lg:flex
          lg:w-[52%]
          relative
          overflow-hidden
        "
      >
        <div className="hidden lg:block lg:w-2/1 h-screen overflow-hidden">
          <img
            src="src/assets/images/login-food.png"
            alt="food"
            className="
      w-full
      h-full
      object-cover
      object-center
    "
          />
        </div>

        <div className="absolute inset-0 bg-black/65" />

        {/* CONTENT */}

        <div
          className="
            absolute
            top-[80px]
            left-[120px]
            z-20
            max-w-[620px]
          "
        >
          <h1
            className="
              text-[#12C95A]
              text-[78px]
drop-shadow-[0_10px_20px_rgba(18,201,90,0.30)]
              leading-[100%]
              font-[800]
              italic
              tracking-[-2px]
            "
          >
            tummly
          </h1>

          <h2
            className="
              mt-[42px]
              text-white
              text-[66px]
              leading-[72px]
              font-[700]
              tracking-[-2px]
            "
          >
            Turn every visit into a guest relationship.
          </h2>

          <p
            className="
              mt-[34px]
              text-[#F2F2F2]
              text-[28px]
              leading-[42px]
              font-[400]
            "
          >
            Manage QR capture, private
            feedback, offers and campaigns
            for your restaurant.
          </p>
        </div>
      </div>

      {/* =====================================================
      RIGHT SIDE
      ===================================================== */}

      <div
        className="
    lg:w-[48%]
    min-h-screen
    flex
    flex-col
    items-center
    justify-center
    px-[24px]
    py-[60px]
    relative
    overflow-y-auto
  "
      >
        {/* BACKGROUND PATTERN */}

        <div
          className="
            absolute
            top-0
            right-0
            w-[320px]
            h-[220px]
            opacity-[0.06]
            pointer-events-none
            bg-repeat
          "
          style={{
            backgroundImage:
              "url('/assets/pattern.png')",
          }}
        />

        {/* ALERT */}

        {(globalError ||
          globalSuccess) && (
            <div
              className={`
              w-full
              max-w-[560px]
              mb-[22px]
              rounded-[8px]
              px-[26px]
              py-[16px]
              text-[15px]
              flex
              items-center
              gap-[12px]
              ${globalError
                  ? "bg-red-50 text-red-600 border border-red-200"
                  : "bg-green-50 text-green-700 border border-green-200"
                }
            `}
            >
              <AlertCircle size={20} />
              {globalError || globalSuccess}
            </div>
          )}

        {/* =====================================================
        LOGIN
        ===================================================== */}

        {step === STEPS.LOGIN && (
          <div
            className="
    w-full
    max-w-[520px]
    mb-[50px]
    min-h-[760px]
    bg-white
    rounded-[24px]
    border border-[#EAEAEA]
    bg-white
    shadow-[0_25px_80px_rgba(0,0,0,0.12)]
    hover:-translate-y-[4px]
transition-all
duration-500
    backdrop-blur-[20px]
    px-[52px]
    py-[56px]
    flex
    flex-col
    justify-between
  "
          >
            {/* TITLE */}

            <h1
              className="
    text-[44px]
    font-[700]
    text-[#111827]
    tracking-[-1px]
    mb-[40px]

    relative
    left-[40px]
    top-[20px]
  "
            >
              Login
            </h1>

            {/* FORM */}

            <div
              className="
flex
flex-col
gap-[20px]
px-[22px]
"
            >
              {/* EMAIL */}

<div className="relative">
  <input
    type="email"
    value={loginData.email}
    onChange={(e) =>
      setLoginData({
        ...loginData,
        email: e.target.value,
      })
    }
    className="
      w-full
      h-[58px]

      px-[22px]

      border
      border-[#D5D5D5]

      rounded-[8px]

      bg-white

      text-[15px]
      font-[400]
      text-[#222]

      outline-none

      transition-all
      duration-300

      hover:border-[#BBBBBB]

      focus:border-[#00A86B]
      focus:shadow-[0_0_0_4px_rgba(0,168,107,0.10)]
    "
  />

  {!loginData.email && (
    <span
      className="
        absolute
        left-[30px]
        top-1/2
        -translate-y-1/2

        text-[#999]
        text-[15px]
        font-[400]

        pointer-events-none
      "
    >
      Email
    </span>
  )}
</div>
              {/* PASSWORD */}

<div className="relative">
  <input
    type={showPassword ? "text" : "password"}
    value={loginData.password}
    onChange={(e) =>
      setLoginData({
        ...loginData,
        password: e.target.value,
      })
    }
    className="
      w-full
      h-[58px]

      px-[22px]
      pr-[55px]

      border
      border-[#D5D5D5]

      rounded-[8px]

      bg-white

      text-[15px]
      font-[400]
      text-[#222]

      outline-none

      transition-all
      duration-300

      hover:border-[#BBBBBB]

      focus:border-[#00A86B]
      focus:shadow-[0_0_0_4px_rgba(0,168,107,0.10)]
    "
  />

  {!loginData.password && (
    <span
      className="
        absolute
        left-[40px]
        top-1/2
        -translate-y-1/2

        text-[#999]
        text-[15px]
        font-[400]

        pointer-events-none
      "
    >
      Password
    </span>
  )}

  <button
    type="button"
    onClick={() => setShowPassword(!showPassword)}
    className="
      absolute
      right-[18px]
      top-1/2
      -translate-y-1/2
      text-[#999]
    "
  >
    {showPassword ? (
      <EyeOff size={18} />
    ) : (
      <Eye size={18} />
    )}
  </button>
</div>

             {/* FORGOT */}

<div
  className="
    text-[13px]
    text-[#444]
    relative
    left-[35px]
  "
>
  Forgot password?{" "}
  <button
    onClick={() =>
      setStep(STEPS.RESET_EMAIL)
    }
    className="
      text-[#00A86B]
      hover:underline
    "
  >
    Reset password
  </button>
</div>

{/* REMEMBER */}

<label
  className="
    flex
    items-center
    gap-[10px]

    text-[14px]
    text-[#444]

    pt-[4px]

    relative
    left-[30px]
  "
>
  <input
    type="checkbox"
    checked={
      loginData.rememberDevice
    }
    onChange={(e) =>
      setLoginData({
        ...loginData,
        rememberDevice:
          e.target.checked,
      })
    }
  />

  Remember this device for 30 days
</label>

              {/* LOGIN BUTTON */}

              <div className="mt-[12px]">
                <button
                  onClick={handleLogin}
                  disabled={loading}
                  className="
w-full
h-[56px]
bg-[#08B56A]
hover:bg-[#06995A]
rounded-[8px]
text-white
text-[16px]
font-[600]
transition-all
duration-300
shadow-[0_10px_25px_rgba(8,181,106,0.20)]
"
                >
                  {loading
                    ? "Please wait..."
                    : "Login"}
                </button>
              </div>
            </div>

            {/* DIVIDER */}

            <div
              className="
        flex
        items-center
        gap-[14px]
        my-[28px]
px-[22px]
      "
            >
              <div className="flex-1 h-[1px] bg-gradient-to-r from-transparent via-[#DADADA] to-transparent" />

              <span
                className="
          text-[14px]
          text-[#777]
        "
              >
                or
              </span>

              <div className="flex-1 h-[1px] bg-gradient-to-r from-transparent via-[#DADADA] to-transparent" />
            </div>

            {/* GOOGLE */}

            <button
              className="
        w-full
        h-[56px]
        border
        border-[#D0D0D0]
        rounded-[12px]
bg-[#FFFFFF]
hover:bg-[#F8F8F8]
hover:border-[#CFCFCF]
transition-all
duration-
shadow-[0_2px_12px_rgba(0,0,0,0.04)]
        bg-white
        flex
        items-center
        justify-center
        gap-[12px]
        text-[15px]
        font-[500]
        hover:bg-[#FAFAFA]
        transition-all
      "
            >
              <img
                src="https://upload.wikimedia.org/wikipedia/commons/c/c1/Google_%22G%22_logo.svg"
                alt="Google"
                className="w-[18px] h-[18px]"
              />

              Continue with Google
            </button>

            {/* SECURITY */}

            <p
              className="
text-[13px]
text-[#555]
leading-[22px]
mt-[28px]
px-[22px]
"
            >
              We may send a verification
              code by email or SMS to help
              keep your account secure.
              Message and data rates may
              apply.
            </p>

            {/* FOOTER LINKS */}

            <div className="mt-[28px] space-y-[14px]">
              <div className="text-[15px] text-[#222]">
                New to Tummly?{" "}
                <button
                  className="
            text-[#00A86B]
            hover:underline
          "
                >
                  Start setup
                </button>
              </div>

              <div className="text-[15px] text-[#222]">
                Need help?{" "}
                <button
                  className="
            text-[#00A86B]
            hover:underline
          "
                >
                  Visit help centre
                </button>
              </div>
            </div>
          </div>
        )}

        {/* =====================================================
        FORGOT PASSWORD
        ===================================================== */}

        {step === STEPS.FORGOT_EMAIL && (
          <FormCard
            title="Reset your password"
            subtitle="Enter the email linked to your account."
          >
            <div className="space-y-[24px]">
              <InputField
                type="email"
                placeholder="Email"
                value={forgotEmail}
                onChange={(e) =>
                  setForgotEmail(
                    e.target.value
                  )
                }
              />

              <PrimaryButton
                title="Send OTP"
                onClick={handleSendOtp}
              />

              <button
                onClick={() =>
                  setStep(STEPS.LOGIN)
                }
                className="
                  w-full
                  text-center
                  text-[#16A34A]
                  text-[16px]
                  font-[500]
                "
              >
                Back to sign in
              </button>
            </div>
          </FormCard>
        )}

        {/* =====================================================
        OTP
        ===================================================== */}

        {step === STEPS.FORGOT_OTP && (
          <div
            className="
    w-full
    max-w-[520px]

    min-h-[560px]

    bg-white

    border
    border-[#ECECEC]

    rounded-[24px]

    px-[58px]
    py-[60px]

    shadow-[0_30px_80px_rgba(15,23,42,0.10)]

    hover:shadow-[0_35px_90px_rgba(15,23,42,0.14)]

    transition-all
    duration-500

    flex
    flex-col
    justify-center

    relative
    overflow-hidden
  "
          >
            {/* HEADING */}

            <h2
              className="
    text-[36px]
    leading-[42px]
    font-[700]
    text-[#202124]
    tracking-[-1px]

    mb-[26px]

    relative
    top-[-50px]
    left-[20px]
  "
            >
              Verify it’s you
            </h2>

            {/* DESCRIPTION */}

            <p
              className="
    text-[16px]
    leading-[28px]
    text-[#444]

    mb-[36px]

    relative
    top-[-40px]
    left-[15px]
  "
            >
              We sent a 6-digit code to
              m••••@restaurant.com.
              <br />
              Enter it below to continue.
            </p>

            {/* OTP INPUT */}

            <input
              type="text"
              placeholder="Enter the 6-digit code"
              value={otpCode}
              onChange={(e) =>
                setOtpCode(e.target.value)
              }
              className="
    w-full
    h-[64px]

    px-[26px]

    border
    border-[#BDBDBD]

    rounded-[6px]

    bg-white

    text-[17px]
    font-[400]
    text-[#202124]

    placeholder:text-[#8A8A8A]
    placeholder:font-[400]

    outline-none

    transition-all
    duration-200

    mb-[38px]
    relative
    top-[-15px]
    hover:border-[#AFAFAF]

    focus:border-[#16A34A]
    focus:shadow-[0_0_0_3px_rgba(22,163,74,0.08)]
  "
            />

            {/* VERIFY BUTTON */}

            <button
              onClick={handleVerifyOtp}
              disabled={loading}
              className="
        w-full
        h-[62px]

        rounded-[5px]

        bg-[#19B54A]
        hover:bg-[#15963D]

        text-white
        text-[18px]
        font-[500]

        transition-all
        duration-200
      "
            >
              {loading ? "Please wait..." : "Verify"}
            </button>

            {/* LINKS */}

            <div
              className="
    relative
    top-[40px]
    left-[30px]
  "
            >
              <div
                className="
      flex
      items-center
      gap-[10px]
      text-[15px]
      text-[#444]
      mb-[20px]
    "
              >
                <span>Didn’t get a code?</span>

                <button
                  onClick={handleSendOtp}
                  className="
        text-[#16A34A]
        hover:underline
        font-[500]
      "
                >
                  Resend code
                </button>
              </div>

              <button
                onClick={() =>
                  setStep(STEPS.LOGIN)
                }
                className="
      text-[15px]
      text-[#16A34A]
      hover:underline
      font-[500]
    "
              >
                Use a different sign-in method
              </button>
            </div>
          </div>
        )}

      {step === STEPS.RESET_EMAIL && (
  <div
    style={{
      width: "100%",
      maxWidth: "450px",
      background: "#fff",
      border: "1px solid #d9d9d9",
      borderRadius: "6px",
      boxShadow: "0 15px 40px rgba(0,0,0,0.08)",
      padding: "40px 48px",
      boxSizing: "border-box",
    }}
  >
    {/* TITLE */}
    <div style={{ marginBottom: "20px" }}>
      <h2
        style={{
          margin: 0,
          fontSize: "30px",
          fontWeight: 700,
          lineHeight: "38px",
          color: "#222",
        }}
      >
        Reset your password
      </h2>
    </div>

    {/* DESCRIPTION */}
    <div style={{ marginBottom: "35px" }}>
      <p
        style={{
          margin: 0,
          fontSize: "15px",
          lineHeight: "28px",
          color: "#555",
        }}
      >
        Enter the email address linked to your Tummly account.
        If an account exists, we'll send instructions to reset
        your password.
      </p>
    </div>

    {/* INPUT */}
    <div style={{ marginBottom: "30px" }}>
      <input
        type="email"
        placeholder="Email"
        value={resetEmail}
        onChange={(e) => setResetEmail(e.target.value)}
        style={{
          width: "100%",
          height: "46px",
          border: "1px solid #cfcfcf",
          borderRadius: "3px",
          padding: "0 14px",
          fontSize: "14px",
          outline: "none",
          boxSizing: "border-box",
        }}
      />
    </div>

    {/* BUTTON */}
    <div style={{ marginBottom: "35px" }}>
      <button
        onClick={handleSendResetLink}
        disabled={loading}
        style={{
          width: "100%",
          height: "46px",
          border: "none",
          borderRadius: "3px",
          background: "#18AE47",
          color: "#fff",
          fontSize: "14px",
          fontWeight: 500,
          cursor: "pointer",
        }}
      >
        {loading ? "Please wait..." : "Send reset link"}
      </button>
    </div>

    {/* BACK LINK */}
    <div style={{ textAlign: "center" }}>
      <button
        onClick={() => setStep(STEPS.LOGIN)}
        style={{
          background: "transparent",
          border: "none",
          color: "#18AE47",
          textDecoration: "underline",
          fontSize: "14px",
          cursor: "pointer",
        }}
      >
        Back to sign in
      </button>
    </div>
  </div>
)}

       {/* =====================================================
RESET PASSWORD
===================================================== */}

{step === STEPS.RESET_PASSWORD && (
  <div
    className="
      w-full
      max-w-[520px]

      bg-white

      border
      border-[#ECECEC]

      rounded-[28px]

      px-[56px]
      py-[58px]

      shadow-[0_30px_80px_rgba(15,23,42,0.10)]

      transition-all
      duration-500

      flex
      flex-col
      justify-start
    "
  >
    {/* HEADING */}

    <h2
      className="
        text-[38px]
        leading-[44px]
        font-[700]
        text-[#202124]

        tracking-[-1px]

        mb-[18px]

        relative
        left-[6px]
        top-[-18px]
      "
    >
      Reset your password
    </h2>

    {/* DESCRIPTION */}

    <p
      className="
        text-[16px]
        leading-[28px]
        text-[#5F6368]

        mb-[42px]

        relative
        left-[8px]
        top-[-10px]
      "
    >
      Create a strong new password
      for your Tummly account.
    </p>

    {/* NEW PASSWORD */}

    <div className="relative mb-[24px]">
      <input
        type={
          showNewPassword
            ? "text"
            : "password"
        }
        placeholder="New password"
        value={newPassword}
        onChange={(e) =>
          setNewPassword(
            e.target.value
          )
        }
        className="
          w-full
          h-[64px]

          px-[24px]
          pr-[58px]

          border
          border-[#D6DCE2]

          rounded-[12px]

          bg-white

          text-[16px]
          font-[400]
          text-[#202124]

          placeholder:text-[#8A8A8A]

          outline-none

          transition-all
          duration-200

          hover:border-[#B8C0C7]

          focus:border-[#16A34A]
          focus:shadow-[0_0_0_4px_rgba(22,163,74,0.08)]
        "
      />

      <button
        type="button"
        onClick={() =>
          setShowNewPassword(
            !showNewPassword
          )
        }
        className="
          absolute
          right-[18px]
          top-1/2
          translate-y-[-50%]

          text-[#8A8A8A]
        "
      >
        {showNewPassword ? (
          <EyeOff size={20} />
        ) : (
          <Eye size={20} />
        )}
      </button>
    </div>

    {/* CONFIRM PASSWORD */}

    <div className="relative mb-[34px]">
      <input
        type={
          showConfirmPassword
            ? "text"
            : "password"
        }
        placeholder="Confirm password"
        value={confirmPassword}
        onChange={(e) =>
          setConfirmPassword(
            e.target.value
          )
        }
        className="
          w-full
          h-[64px]

          px-[24px]
          pr-[58px]

          border
          border-[#D6DCE2]

          rounded-[12px]

          bg-white

          text-[16px]
          font-[400]
          text-[#202124]

          placeholder:text-[#8A8A8A]

          outline-none

          transition-all
          duration-200

          hover:border-[#B8C0C7]

          focus:border-[#16A34A]
          focus:shadow-[0_0_0_4px_rgba(22,163,74,0.08)]
        "
      />

      <button
        type="button"
        onClick={() =>
          setShowConfirmPassword(
            !showConfirmPassword
          )
        }
        className="
          absolute
          right-[18px]
          top-1/2
          translate-y-[-50%]

          text-[#8A8A8A]
        "
      >
        {showConfirmPassword ? (
          <EyeOff size={20} />
        ) : (
          <Eye size={20} />
        )}
      </button>
    </div>

    {/* UPDATE BUTTON */}

    <button
      onClick={handleUpdatePassword}
      disabled={loading}
      className="
        w-full
        h-[62px]

        rounded-[12px]

        bg-[#16A34A]
        hover:bg-[#14863E]

        text-white
        text-[17px]
        font-[600]

        transition-all
        duration-200

        shadow-[0_12px_30px_rgba(22,163,74,0.18)]
      "
    >
      {loading
        ? "Please wait..."
        : "Update password"}
    </button>

    {/* FOOTER LINKS */}

    <div
      className="
        mt-[42px]

        pl-[4px]
      "
    >
      <button
        onClick={() =>
          setStep(STEPS.LOGIN)
        }
        className="
          text-[15px]
          text-[#16A34A]

          hover:underline

          font-[500]
        "
      >
        Back to sign in
      </button>
    </div>
  </div>
)}

        {/* =====================================================
        SUCCESS
        ===================================================== */}

        {step ===
          STEPS.PASSWORD_SUCCESS && (
            <FormCard title="Password updated">
              <div className="space-y-[24px]">
                <p
                  className="
                  text-[16px]
                  text-[#555]
                  leading-[28px]
                "
                >
                  Your password has been
                  updated successfully.
                </p>

                <PrimaryButton
                  title="Back to Login"
                  onClick={() =>
                    setStep(STEPS.LOGIN)
                  }
                />
              </div>
            </FormCard>
          )}

        {/* =====================================================
        FOOTER
        ===================================================== */}

        <div
          className="
    absolute
    bottom-[28px]
    left-1/2
    translate-x-[-50%]
    w-full
    text-center
    text-[13px]
    text-[#777]
  "
        >
          <div
            className="
              flex
              justify-center
              gap-[28px]
              flex-wrap
            "
          >
            <span>© 2026 Tummly</span>
            <span>Help Centre</span>
            <span>Terms</span>
            <span>Privacy</span>
            <span>Cookie settings</span>
          </div>

          <div
            className="
              mt-[14px]
              flex
              items-center
              justify-center
              gap-[7px]
            "
          >
            <Lock size={14} />
            Secure restaurant access
          </div>
        </div>
      </div>
    </div>
  );
};

export default LoginSystem;