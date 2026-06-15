import { useState } from "react";
import type { FormEvent, ReactNode } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { Navigate, useSearchParams } from "react-router-dom";

import { AuthShell } from "@/components/auth/AuthShell";
import { SignInForm } from "@/components/auth/SignInForm";
import { AUTH_API_BASE_URL as API_BASE_URL } from "../../config/api";
import { FormFloatingInput } from "@/components/form/FormFloatingInput";
import { FloatingLabelInput } from "@/components/ui/floating-label-input";
import {
  mapResendApiMessage,
  mapVerifyApiMessage,
  MAX_VERIFY_ATTEMPTS,
  OTP_MESSAGES,
  RESEND_COOLDOWN_SECONDS,
  type OtpFeedback,
} from "@/components/home/hero-trial-otp";
import { Button } from "@/components/ui/button";
import { FieldErrorSlot } from "@/components/ui/field";
import { Form } from "@/components/ui/form";
import { useCountdown } from "@/hooks/use-countdown";
import { defaultFormValidationOptions } from "@/lib/form";
import {
  resetPasswordDefaultValues,
  resetPasswordFormSchema,
  toResetPasswordPayload,
  type ResetPasswordFormValues,
} from "@/schemas/resetPassword";
import {
  signInCredentialsDefaultValues,
  signInCredentialsSchema,
  signInEmailDefaultValues,
  signInEmailSchema,
  toSignInEmailPayload,
  toSignInPayload,
  type SignInCredentialsValues,
  type SignInEmailValues,
} from "@/schemas/signIn";
import { cn } from "@/lib/utils";
import {
  getPostVerifyDashboardPath,
  parseVerifyOtpResponse,
  persistAuthSession,
} from "../utils/authHelpers";

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

function getFetchErrorMessage(
  result: { message?: string },
  fallback: string
) {
  return result.message?.trim() || fallback;
}

function maskEmail(email: string) {
  const [local, domain] = email.split("@");
  if (!local || !domain) {
    return email;
  }

  const maskedLocal = local.length <= 1 ? "ΓÇó" : `${local[0]}ΓÇóΓÇóΓÇóΓÇó`;
  return `${maskedLocal}@${domain}`;
}

function OtpFeedbackMessage({ feedback }: { feedback: OtpFeedback | null }) {
  if (!feedback) {
    return null;
  }

  return (
    <p
      role={feedback.kind === "error" ? "alert" : "status"}
      className={cn(
        "m-0 text-[14px] font-medium leading-[22px]",
        feedback.kind === "error" ? "text-destructive" : "text-[#14a247]"
      )}
    >
      {feedback.message}
    </p>
  );
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

function LoginPageContent() {
  const [step, setStep] = useState<LoginStep>(getInitialStep);
  const [resetToken] = useState(getResetTokenFromUrl);

  const [otpEmail, setOtpEmail] = useState("");
  const [otpCode, setOtpCode] = useState("");
  const [otpFeedback, setOtpFeedback] = useState<OtpFeedback | null>(null);
  const [verifyAttempts, setVerifyAttempts] = useState(0);
  const [otpSubmitting, setOtpSubmitting] = useState(false);
  const [resetEmailSuccess, setResetEmailSuccess] = useState<string | null>(null);

  const {
    secondsRemaining: resendSecondsRemaining,
    isComplete: canResend,
    restart: restartResendTimer,
  } = useCountdown(RESEND_COOLDOWN_SECONDS, step === STEPS.FORGOT_OTP);

  const loginForm = useForm<SignInCredentialsValues>({
    resolver: zodResolver(signInCredentialsSchema),
    defaultValues: signInCredentialsDefaultValues,
    ...defaultFormValidationOptions,
  });

  const forgotEmailForm = useForm<SignInEmailValues>({
    resolver: zodResolver(signInEmailSchema),
    defaultValues: signInEmailDefaultValues,
    ...defaultFormValidationOptions,
  });

  const resetEmailForm = useForm<SignInEmailValues>({
    resolver: zodResolver(signInEmailSchema),
    defaultValues: signInEmailDefaultValues,
    ...defaultFormValidationOptions,
  });

  const resetPasswordForm = useForm<ResetPasswordFormValues>({
    resolver: zodResolver(resetPasswordFormSchema),
    defaultValues: resetPasswordDefaultValues,
    ...defaultFormValidationOptions,
  });

  const forgotEmailRootError = forgotEmailForm.formState.errors.root?.message;
  const resetEmailRootError = resetEmailForm.formState.errors.root?.message;
  const resetPasswordRootError = resetPasswordForm.formState.errors.root?.message;

  const onLoginSubmit = async (values: SignInCredentialsValues) => {
    loginForm.clearErrors("root");

    try {
      const payload = toSignInPayload(values);
      const response = await fetch(`${API_BASE_URL}/universal-login`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(payload),
      });

      const result = await response.json();

      if (!response.ok) {
        loginForm.setError("root", {
          message: getFetchErrorMessage(result, "Login failed."),
        });
        return;
      }

      if (result.loginType === "ADMIN") {
        if (!result.token) {
          loginForm.setError("root", {
            message: "Login succeeded but no session token was returned.",
          });
          return;
        }

        persistAuthSession(result.token, "ADMIN");
        window.location.href = "/admin-dashboard";
        return;
      }

      if (result.loginType === "USER") {
        setOtpEmail(payload.email);
        setOtpCode("");
        setVerifyAttempts(0);
        setOtpFeedback({
          kind: "info",
          code: "code_resent",
          message: "Verification code sent to your email.",
        });
        restartResendTimer(RESEND_COOLDOWN_SECONDS);
        setStep(STEPS.FORGOT_OTP);
        return;
      }
    } catch {
      loginForm.setError("root", {
        message: "Unable to connect server.",
      });
    }
  };

  const onForgotEmailSubmit = async (values: SignInEmailValues) => {
    forgotEmailForm.clearErrors("root");

    try {
      const payload = toSignInEmailPayload(values);
      const response = await fetch(`${API_BASE_URL}/send-otp`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(payload),
      });

      const result = await response.json();

      if (!response.ok) {
        forgotEmailForm.setError("root", {
          message: getFetchErrorMessage(result, "Unable to send OTP."),
        });
        return;
      }

      setOtpEmail(payload.email);
      setOtpCode("");
      setVerifyAttempts(0);
      setOtpFeedback({
        kind: "info",
        code: "code_resent",
        message: "OTP sent successfully.",
      });
      restartResendTimer(RESEND_COOLDOWN_SECONDS);
      setStep(STEPS.FORGOT_OTP);
    } catch {
      forgotEmailForm.setError("root", {
        message: "Network error.",
      });
    }
  };

  const handleVerifyOtp = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setOtpFeedback(null);

    if (otpCode.trim().length !== 6) {
      setOtpFeedback({
        kind: "error",
        code: "invalid",
        message: OTP_MESSAGES.incomplete,
      });
      return;
    }

    if (verifyAttempts >= MAX_VERIFY_ATTEMPTS) {
      setOtpFeedback({
        kind: "error",
        code: "too_many_attempts",
        message: OTP_MESSAGES.too_many_attempts,
      });
      return;
    }

    try {
      setOtpSubmitting(true);

      const response = await fetch(`${API_BASE_URL}/verify-otp`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          email: otpEmail,
          otpCode: otpCode.trim(),
        }),
      });

      const result = await response.json();

      if (!response.ok) {
        const message = getFetchErrorMessage(result, "OTP verification failed.");
        const feedback = mapVerifyApiMessage(message);
        const nextAttempts = verifyAttempts + 1;

        setVerifyAttempts(nextAttempts);

        if (nextAttempts >= MAX_VERIFY_ATTEMPTS) {
          setOtpFeedback({
            kind: "error",
            code: "too_many_attempts",
            message: OTP_MESSAGES.too_many_attempts,
          });
        } else {
          setOtpFeedback(feedback);
        }
        return;
      }

      const verified = parseVerifyOtpResponse(result);

      if (!verified) {
        setOtpFeedback({
          kind: "error",
          code: "invalid",
          message: "Verification succeeded but session data was missing.",
        });
        return;
      }

      persistAuthSession(verified.token, "USER");
      window.location.href = getPostVerifyDashboardPath(verified.accountType);
    } catch {
      setOtpFeedback({
        kind: "error",
        code: "invalid",
        message: "Verification failed.",
      });
    } finally {
      setOtpSubmitting(false);
    }
  };

  const handleResendOtp = async () => {
    if (!canResend || otpSubmitting) {
      return;
    }

    setOtpFeedback(null);

    try {
      setOtpSubmitting(true);

      const response = await fetch(`${API_BASE_URL}/send-otp`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          email: otpEmail,
        }),
      });

      const result = await response.json();

      if (!response.ok) {
        setOtpFeedback(
          mapResendApiMessage(
            getFetchErrorMessage(result, "We couldn't resend the code. Try again shortly.")
          )
        );
        return;
      }

      setOtpCode("");
      setVerifyAttempts(0);
      restartResendTimer(RESEND_COOLDOWN_SECONDS);
      setOtpFeedback({
        kind: "info",
        code: "code_resent",
        message: OTP_MESSAGES.code_resent,
      });
    } catch {
      setOtpFeedback(
        mapResendApiMessage("We couldn't resend the code. Try again shortly.")
      );
    } finally {
      setOtpSubmitting(false);
    }
  };

  const handleOtpChange = (value: string) => {
    setOtpCode(value);
    if (otpFeedback?.kind === "error") {
      setOtpFeedback(null);
    }
  };

  const onResetEmailSubmit = async (values: SignInEmailValues) => {
    resetEmailForm.clearErrors("root");
    setResetEmailSuccess(null);

    try {
      const payload = toSignInEmailPayload(values);
      const response = await fetch(`${API_BASE_URL}/forgot-password`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(payload),
      });

      const result = await response.json();

      if (!response.ok) {
        resetEmailForm.setError("root", {
          message: getFetchErrorMessage(result, "Unable to send reset link."),
        });
        return;
      }

      setResetEmailSuccess("Reset link sent successfully.");
    } catch {
      resetEmailForm.setError("root", {
        message: "Request failed.",
      });
    }
  };

  const onResetPasswordSubmit = async (values: ResetPasswordFormValues) => {
    if (!resetToken) {
      resetPasswordForm.setError("root", {
        message: "Invalid or missing token",
      });
      return;
    }

    resetPasswordForm.clearErrors("root");

    try {
      const response = await fetch(`${API_BASE_URL}/reset-password`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(toResetPasswordPayload(values, resetToken)),
      });

      const result = await response.json();

      if (!response.ok) {
        resetPasswordForm.setError("root", {
          message: getFetchErrorMessage(result, "Unable to update password."),
        });
        return;
      }

      setStep(STEPS.PASSWORD_SUCCESS);
    } catch {
      resetPasswordForm.setError("root", {
        message: "Password update failed.",
      });
    }
  };

  const handleBackToLogin = () => {
    setStep(STEPS.LOGIN);
    setOtpCode("");
    setOtpFeedback(null);
    setVerifyAttempts(0);
    setResetEmailSuccess(null);
    loginForm.clearErrors("root");
    forgotEmailForm.clearErrors("root");
    resetEmailForm.clearErrors("root");
    resetPasswordForm.clearErrors("root");
  };

    /*
  =========================================================
  MAIN UI
  =========================================================
  */

  return (
    <AuthShell>
      {step === STEPS.LOGIN && (
        <SignInForm
          form={loginForm}
          onSubmit={onLoginSubmit}
          onResetPassword={() => {
            setResetEmailSuccess(null);
            resetEmailForm.clearErrors("root");
            setStep(STEPS.RESET_EMAIL);
          }}
        />
      )}

        {/* =====================================================
        FORGOT PASSWORD
        ===================================================== */}

        {step === STEPS.FORGOT_EMAIL && (
          <FormCard
            title="Reset your password"
            subtitle="Enter the email linked to your account."
          >
            <Form {...forgotEmailForm}>
              <form
                onSubmit={forgotEmailForm.handleSubmit(onForgotEmailSubmit)}
                noValidate
                className="space-y-[24px]"
              >
                <FormFloatingInput
                  control={forgotEmailForm.control}
                  name="email"
                  type="email"
                  label="Email"
                  required
                />

                <FieldErrorSlot
                  error={forgotEmailRootError}
                  reserveClassName="min-h-0"
                />

                <Button
                  type="submit"
                  disabled={forgotEmailForm.formState.isSubmitting}
                  size="auth-lg"
                >
                  {forgotEmailForm.formState.isSubmitting
                    ? "Please wait..."
                    : "Send OTP"}
                </Button>

                <Button
                  type="button"
                  variant="link"
                  size="link-block"
                  onClick={handleBackToLogin}
                >
                  Back to sign in
                </Button>
              </form>
            </Form>
          </FormCard>
        )}

        {/* =====================================================
        OTP
        ===================================================== */}

        {step === STEPS.FORGOT_OTP && (
          <form
            onSubmit={handleVerifyOtp}
            noValidate
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
              Verify itΓÇÖs you
            </h2>

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
              We sent a 6-digit code to{" "}
              {maskEmail(otpEmail)}.
              <br />
              Enter it below to continue.
            </p>

            <FloatingLabelInput
              label="Enter the 6-digit code"
              value={otpCode}
              onChange={(event) => handleOtpChange(event.target.value)}
              inputMode="numeric"
              autoComplete="one-time-code"
              maxLength={6}
              className="mb-[16px] relative top-[-15px]"
              error={
                otpFeedback?.kind === "error" ? otpFeedback.message : undefined
              }
            />

            {otpFeedback?.kind === "info" ? (
              <div className="mb-[22px] relative top-[-10px]">
                <OtpFeedbackMessage feedback={otpFeedback} />
              </div>
            ) : null}

            <Button
              type="submit"
              disabled={otpSubmitting || otpCode.trim().length !== 6}
              size="auth-xl"
            >
              {otpSubmitting ? "Please wait..." : "Verify"}
            </Button>

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
                <span>DidnΓÇÖt get a code?</span>

                {canResend ? (
                  <Button
                    type="button"
                    variant="link"
                    disabled={otpSubmitting}
                    onClick={handleResendOtp}
                  >
                    Resend code
                  </Button>
                ) : (
                  <span>
                    Resend code in {resendSecondsRemaining} seconds
                  </span>
                )}
              </div>

              <Button
                type="button"
                variant="link"
                size="link-sm"
                onClick={handleBackToLogin}
              >
                Use a different sign-in method
              </Button>
            </div>
          </form>
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

    <Form {...resetEmailForm}>
      <form
        onSubmit={resetEmailForm.handleSubmit(onResetEmailSubmit)}
        noValidate
      >
        <div style={{ marginBottom: "30px" }}>
          <FormFloatingInput
            control={resetEmailForm.control}
            name="email"
            type="email"
            label="Email"
            required
          />
        </div>

        {resetEmailSuccess ? (
          <p
            role="status"
            style={{
              margin: "0 0 20px",
              fontSize: "14px",
              fontWeight: 500,
              color: "#14a247",
            }}
          >
            {resetEmailSuccess}
          </p>
        ) : (
          <FieldErrorSlot
            error={resetEmailRootError}
            reserveClassName="min-h-0 mb-5"
          />
        )}

        <div style={{ marginBottom: "35px" }}>
          <Button
            type="submit"
            disabled={resetEmailForm.formState.isSubmitting}
            size="auth-sm"
          >
            {resetEmailForm.formState.isSubmitting
              ? "Please wait..."
              : "Send reset link"}
          </Button>
        </div>

        <div style={{ textAlign: "center" }}>
          <Button
            type="button"
            variant="link"
            size="link-sm"
            onClick={handleBackToLogin}
          >
            Back to sign in
          </Button>
        </div>
      </form>
    </Form>
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

    <Form {...resetPasswordForm}>
      <form
        onSubmit={resetPasswordForm.handleSubmit(onResetPasswordSubmit)}
        noValidate
      >
        <FormFloatingInput
          control={resetPasswordForm.control}
          name="newPassword"
          type="password"
          label="New password"
          className="mb-[24px]"
          required
        />

        <FormFloatingInput
          control={resetPasswordForm.control}
          name="confirmPassword"
          type="password"
          label="Confirm password"
          className="mb-[24px]"
          required
        />

        <FieldErrorSlot
          error={resetPasswordRootError}
          reserveClassName="min-h-0 mb-[10px]"
        />

        <Button
          type="submit"
          disabled={resetPasswordForm.formState.isSubmitting}
          variant="default"
          size="auth-xl"
        >
          {resetPasswordForm.formState.isSubmitting
            ? "Please wait..."
            : "Update password"}
        </Button>

        <div
          className="
        mt-[42px]

        pl-[4px]
      "
        >
          <Button
            type="button"
            variant="link"
            size="link-sm"
            onClick={handleBackToLogin}
          >
            Back to sign in
          </Button>
        </div>
      </form>
    </Form>
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
                  onClick={handleBackToLogin}
                  loading={false}
                />
              </div>
            </FormCard>
          )}

    </AuthShell>
  );
}

function LoginPage() {
  const [searchParams] = useSearchParams()
  const resetToken = searchParams.get("token")

  if (resetToken) {
    return (
      <Navigate
        to={`/reset-password?token=${encodeURIComponent(resetToken)}`}
        replace
      />
    )
  }

  return <LoginPageContent />
}

export default LoginPage
