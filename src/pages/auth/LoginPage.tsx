// LoginSystem.tsx

import { useState } from "react";
import type { ChangeEvent, ReactNode } from "react";
import {
  Eye,
  EyeOff,
  Lock,
  AlertCircle,
} from "lucide-react";
import loginFood from "../../assets/images/login-food.png";
import googleLogo from "../../assets/images/google-logo.png";
import { AUTH_API_BASE_URL as API_BASE_URL } from "../../config/api";
import { Button } from "@/components/ui/button";

const STEPS = {
  LOGIN: "LOGIN",
  FORGOT_EMAIL: "FORGOT_EMAIL",
  FORGOT_OTP: "FORGOT_OTP",
  RESET_EMAIL: "RESET_EMAIL",
  RESET_PASSWORD: "RESET_PASSWORD",
  PASSWORD_SUCCESS: "PASSWORD_SUCCESS",
} as const;

type LoginStep = (typeof STEPS)[keyof typeof STEPS];

function getResetTokenFromUrl(): string {
  return new URLSearchParams(window.location.search).get("token") ?? "";
}

function getInitialStep(): LoginStep {
  return getResetTokenFromUrl()
    ? STEPS.RESET_PASSWORD
    : STEPS.LOGIN;
}

interface InputFieldProps {
  type?: string;
  placeholder: string;
  value: string;
  onChange: (e: ChangeEvent<HTMLInputElement>) => void;
  passwordToggle?: boolean;
  showPassword?: boolean;
  togglePassword?: () => void;
}

interface PrimaryButtonProps {
  title: string;
  onClick: () => void;
  loading: boolean;
}

interface FormCardProps {
  title: string;
  subtitle?: string;
  children: ReactNode;
}

function InputField({
  type = "text",
  placeholder,
  value,
  onChange,
  passwordToggle,
  showPassword,
  togglePassword,
}: InputFieldProps) {
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
        <Button
          type="button"
          variant="input-toggle"
          onClick={togglePassword}
        >
          {showPassword ? <EyeOff size={19} /> : <Eye size={19} />}
        </Button>
      )}
    </div>
  );
}

function PrimaryButton({ title, onClick, loading }: PrimaryButtonProps) {
  return (
    <Button onClick={onClick} disabled={loading} size="auth-lg">
      {loading ? "Please wait..." : title}
    </Button>
  );
}

function FormCard({ title, subtitle, children }: FormCardProps) {
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
}

const LoginSystem = () => {
  const [step, setStep] = useState<LoginStep>(getInitialStep);

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

  const [resetToken] = useState(getResetTokenFromUrl);

  /*
  =========================================================
  VALIDATIONS
  =========================================================
  */

  const validateEmail = (email: string) => {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(
      email
    );
  };

  const validatePassword = (password: string) => {
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
  MAIN UI
  =========================================================
  */

  return (
      <div
  className="
    min-h-screen
    flex
    flex-col
    lg:flex-row
    relative
  "
>
  {/* MOBILE & TABLET BACKGROUND IMAGE */}

<div
  className="
    lg:hidden
    absolute
    inset-0
    z-0
  "
>
  <img
    src={loginFood}
    alt="Background"
    className="
      w-full
      h-full
      object-cover
    "
  />

  <div className="absolute inset-0 bg-black/65" />
</div>
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
           src={loginFood} alt="food"
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
    w-full
    min-h-screen
    flex
    flex-col
    items-center
    justify-center
    px-[20px]
    sm:px-[24px]
    py-[40px]
    pb-[80px]
    relative
    z-10
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

  <Button
    type="button"
    variant="input-toggle"
    onClick={() => setShowPassword(!showPassword)}
  >
    {showPassword ? (
      <EyeOff size={18} />
    ) : (
      <Eye size={18} />
    )}
  </Button>
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
  <Button variant="link" onClick={() => setStep(STEPS.RESET_EMAIL)}>
    Reset password
  </Button>
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
                <Button
                  onClick={handleLogin}
                  disabled={loading}
                  size="auth-md"
                >
                  {loading ? "Please wait..." : "Login"}
                </Button>
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

            <Button variant="outline" size="auth-md">
                <img
    src={googleLogo}
    alt="Google"
    className="w-[18px] h-[18px]"
  />
              Continue with Google
            </Button>

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
                <Button variant="link">Start setup</Button>
              </div>

              <div className="text-[15px] text-[#222]">
                Need help?{" "}
                <Button variant="link">Visit help centre</Button>
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
                loading={loading}
              />

              <Button
                variant="link"
                size="link-block"
                onClick={() => setStep(STEPS.LOGIN)}
              >
                Back to sign in
              </Button>
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

            <Button
              onClick={handleVerifyOtp}
              disabled={loading}
              size="auth-xl"
            >
              {loading ? "Please wait..." : "Verify"}
            </Button>

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

                <Button variant="link" onClick={handleSendOtp}>
                  Resend code
                </Button>
              </div>

              <Button
                variant="link"
                size="link-sm"
                onClick={() => setStep(STEPS.LOGIN)}
              >
                Use a different sign-in method
              </Button>
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
      <Button
        onClick={handleSendResetLink}
        disabled={loading}
        size="auth-sm"
      >
        {loading ? "Please wait..." : "Send reset link"}
      </Button>
    </div>

    {/* BACK LINK */}
    <div style={{ textAlign: "center" }}>
      <Button variant="link" size="link-sm" onClick={() => setStep(STEPS.LOGIN)}>
        Back to sign in
      </Button>
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

      <Button
        type="button"
        variant="input-toggle"
        onClick={() => setShowNewPassword(!showNewPassword)}
      >
        {showNewPassword ? (
          <EyeOff size={20} />
        ) : (
          <Eye size={20} />
        )}
      </Button>
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

      <Button
        type="button"
        variant="input-toggle"
        onClick={() => setShowConfirmPassword(!showConfirmPassword)}
      >
        {showConfirmPassword ? (
          <EyeOff size={20} />
        ) : (
          <Eye size={20} />
        )}
      </Button>
    </div>

    {/* UPDATE BUTTON */}

    <Button
      onClick={handleUpdatePassword}
      disabled={loading}
      variant="default"
      size="auth-xl"
    >
      {loading ? "Please wait..." : "Update password"}
    </Button>

    {/* FOOTER LINKS */}

    <div
      className="
        mt-[42px]

        pl-[4px]
      "
    >
      <Button
        variant="link"
        size="link-sm"
        onClick={() => setStep(STEPS.LOGIN)}
      >
        Back to sign in
      </Button>
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
                  loading={loading}
                />
              </div>
            </FormCard>
          )}

        {/* =====================================================
        FOOTER
        ===================================================== */}

        <div
  className="
    w-full
    mt-[40px]
    pb-[20px]
    text-center
    text-[12px]
    sm:text-[13px]
    text-[#777]
    flex-shrink-0
  "
>
          <div
  className="
    flex
    flex-wrap
    justify-center
    items-center
    gap-x-[16px]
    gap-y-[8px]
    px-[16px]
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
    mt-[12px]
    flex
    items-center
    justify-center
    gap-[6px]
    px-[16px]
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