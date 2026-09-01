import { useEffect, useState } from "react"
import type { FormEvent } from "react"
import { zodResolver } from "@hookform/resolvers/zod"
import { useForm } from "react-hook-form"
import { Navigate, useSearchParams } from "react-router-dom"

import { AuthShell } from "@/components/auth/AuthShell"
import { AuthSessionLoading } from "@/components/auth/AuthSessionLoading"
import { SignInForm } from "@/components/auth/SignInForm"
import { SignInChooseMethodStep } from "@/components/auth/SignInChooseMethodStep"
import { SignInChooseWorkspaceStep } from "@/components/auth/SignInChooseWorkspaceStep"
import { SignInVerifyOtpStep } from "@/components/auth/SignInVerifyOtpStep"
import { SignInActivationCodeStep } from "@/components/auth/SignInActivationCodeStep"
import { activateAccount } from "@/api/authActivation"
import axiosInstance from "@/api/axiosInstance"
import { isAxiosError } from "axios"
import {
  mapResendApiMessage,
  mapVerifyApiMessage,
  MAX_VERIFY_ATTEMPTS,
  OTP_MESSAGES,
  RESEND_COOLDOWN_SECONDS,
  type OtpFeedback,
} from "@/components/home/hero-trial-otp"
import { useCountdown } from "@/hooks/use-countdown"
import {
  getOtpDestinationCopy,
  getOtpResentMessage,
  getOtpSentMessage,
  parseOtpChallengeResponse,
  requestOtpResend,
  requestSwitchToEmailOtp,
  requestSwitchToSmsOtp,
  type OtpChannel,
  type SendOtpApiResult,
} from "@/lib/signInOtp"
import { defaultFormValidationOptions } from "@/lib/form"
import {
  fetchCurrentUserRouting,
  getAuthenticatedLoginDestination,
  getFallbackLoginDestination,
  isAuthenticatedActivationCodeDestination,
} from "@/lib/sessionRouting"
import {
  fetchWorkspaceLocations,
  submitWorkspaceSelection,
  type WorkspaceLocation,
} from "@/lib/workspaceSetupFlow"
import {
  signInCredentialsDefaultValues,
  signInCredentialsSchema,
  toSignInPayload,
  type SignInCredentialsValues,
} from "@/schemas/signIn"
import {
  signInActivationCodeSchema,
  normalizeActivationCodeInput,
} from "@/schemas/signInActivation"
import { useAuthStore } from "@/stores/authStore"
import {
  completeUserSession,
  getDeviceToken,
  getPostLoginDestination,
  isActivationCodeDestination,
  isWorkspaceSetupDestination,
  parseTrustSkipLoginResponse,
  parseVerifyOtpResponse,
  persistActivationRequired,
  persistAuthSession,
  persistSelectedLocation,
  type UserSessionPayload,
} from "../utils/authHelpers"
import { getFetchErrorMessage, readBoolean, readNumber, readString, unwrapDataObject } from "@/lib/apiEnvelope"

const STEPS = {
  LOGIN: "LOGIN",
  VERIFY_OTP: "VERIFY_OTP",
  CHOOSE_SIGN_IN_METHOD: "CHOOSE_SIGN_IN_METHOD",
  ACTIVATION_CODE: "ACTIVATION_CODE",
  WORKSPACE_SETUP: "WORKSPACE_SETUP",
} as const

type LoginStep = (typeof STEPS)[keyof typeof STEPS]

function LoginPageContent() {
  const [searchParams, setSearchParams] = useSearchParams()
  const hasHydrated = useAuthStore((state) => state._hasHydrated)
  const token = useAuthStore((state) => state.token)

  const [step, setStep] = useState<LoginStep>(STEPS.LOGIN)

  const [otpEmail, setOtpEmail] = useState("")
  const [otpCode, setOtpCode] = useState("")
  const [otpChannel, setOtpChannel] = useState<OtpChannel>("email")
  const [hasVerifiedPhone, setHasVerifiedPhone] = useState(false)
  const [maskedPhone, setMaskedPhone] = useState<string | null>(null)
  const [otpFeedback, setOtpFeedback] = useState<OtpFeedback | null>(null)
  const [verifyAttempts, setVerifyAttempts] = useState(0)
  const [otpSubmitting, setOtpSubmitting] = useState(false)
  const [rememberDevice, setRememberDevice] = useState(true)

  const [workspaces, setWorkspaces] = useState<WorkspaceLocation[]>([])
  const [selectedLocationId, setSelectedLocationId] = useState<number | null>(
    null
  )
  const [workspaceLoading, setWorkspaceLoading] = useState(false)
  const [workspaceError, setWorkspaceError] = useState<string | null>(null)
  const [workspaceSubmitting, setWorkspaceSubmitting] = useState(false)

  const [activationCode, setActivationCode] = useState("")
  const [activationError, setActivationError] = useState<string | null>(null)
  const [activationSubmitting, setActivationSubmitting] = useState(false)

  const [authRedirectTarget, setAuthRedirectTarget] = useState<string | null>(
    null
  )
  const [authRedirectResolved, setAuthRedirectResolved] = useState(false)

  const otpStepActive =
    step === STEPS.VERIFY_OTP || step === STEPS.CHOOSE_SIGN_IN_METHOD

  const {
    secondsRemaining: resendSecondsRemaining,
    isComplete: canResend,
    restart: restartResendTimer,
  } = useCountdown(RESEND_COOLDOWN_SECONDS, otpStepActive)

  const loginForm = useForm<SignInCredentialsValues>({
    resolver: zodResolver(signInCredentialsSchema),
    defaultValues: signInCredentialsDefaultValues,
    ...defaultFormValidationOptions,
  })

  const navigateAfterSession = (
    session: UserSessionPayload,
    deviceToken?: string | null
  ) => {
    const destination = completeUserSession(session, deviceToken)

    if (isActivationCodeDestination(destination)) {
      setSearchParams({}, { replace: true })
      setActivationCode("")
      setActivationError(null)
      setStep(STEPS.ACTIVATION_CODE)
      return
    }

    if (isWorkspaceSetupDestination(destination)) {
      setSearchParams({}, { replace: true })
      setWorkspaces([])
      setSelectedLocationId(null)
      setWorkspaceError(null)
      setWorkspaceLoading(true)
      setStep(STEPS.WORKSPACE_SETUP)
      return
    }

    window.location.href = destination
  }

  useEffect(() => {
    if (!hasHydrated) {
      return
    }

    if (!token) {
      setAuthRedirectResolved(true)
      return
    }

    if (
      searchParams.get("step") === "workspace-setup" ||
      searchParams.get("step") === "activation-code"
    ) {
      setAuthRedirectResolved(true)
      return
    }

    let cancelled = false

    const resolveAuthenticatedSession = async () => {
      const role = useAuthStore.getState().role

      if (role === "ADMIN") {
        if (!cancelled) {
          setAuthRedirectTarget("/admin-dashboard")
        }
        return
      }

      if (role === "SUPPORT") {
        if (!cancelled) {
          setAuthRedirectTarget("/support-dashboard")
        }
        return
      }

      const routing = await fetchCurrentUserRouting()

      if (cancelled) {
        return
      }

      if (!routing) {
        const fallback = getFallbackLoginDestination()
        if (fallback) {
          setAuthRedirectTarget(fallback)
        } else {
          setAuthRedirectResolved(true)
        }
        return
      }

      const destination = getAuthenticatedLoginDestination(routing)

      if (isAuthenticatedActivationCodeDestination(destination)) {
        setActivationCode("")
        setActivationError(null)
        setStep(STEPS.ACTIVATION_CODE)
        setAuthRedirectResolved(true)
        return
      }

      if (isWorkspaceSetupDestination(destination)) {
        setWorkspaces([])
        setSelectedLocationId(null)
        setWorkspaceError(null)
        setWorkspaceLoading(true)
        setStep(STEPS.WORKSPACE_SETUP)
        setAuthRedirectResolved(true)
        return
      }

      if (routing.selectedLocationId != null) {
        persistSelectedLocation(routing.selectedLocationId)
      }

      setAuthRedirectTarget(destination)
    }

    void resolveAuthenticatedSession()

    return () => {
      cancelled = true
    }
  }, [hasHydrated, token, searchParams])

  useEffect(() => {
    if (
      !hasHydrated ||
      searchParams.get("step") !== "workspace-setup" ||
      !token
    ) {
      return
    }

    setWorkspaces([])
    setSelectedLocationId(null)
    setWorkspaceError(null)
    setWorkspaceLoading(true)
    setStep(STEPS.WORKSPACE_SETUP)
  }, [hasHydrated, token, searchParams])

  useEffect(() => {
    if (
      !hasHydrated ||
      searchParams.get("step") !== "activation-code" ||
      !token
    ) {
      return
    }

    setActivationCode("")
    setActivationError(null)
    setStep(STEPS.ACTIVATION_CODE)
  }, [hasHydrated, token, searchParams])

  useEffect(() => {
    if (step !== STEPS.WORKSPACE_SETUP) {
      return
    }

    let cancelled = false

    const loadWorkspaces = async () => {
      setWorkspaceLoading(true)
      setWorkspaceError(null)

      try {
        const locations = await fetchWorkspaceLocations()

        if (cancelled) {
          return
        }

        setWorkspaces(locations)
        setSelectedLocationId(null)
      } catch (error) {
        if (cancelled) {
          return
        }

        setWorkspaceError(
          error instanceof Error
            ? error.message
            : "Unable to load workspaces."
        )
      } finally {
        if (!cancelled) {
          setWorkspaceLoading(false)
        }
      }
    }

    void loadWorkspaces()

    return () => {
      cancelled = true
    }
  }, [step])

  const beginOtpChallenge = (
    email: string,
    remember: boolean,
    challenge: {
      otpChannel: OtpChannel
      hasVerifiedPhone: boolean
      maskedPhone: string | null
    }
  ) => {
    setRememberDevice(remember)
    setOtpEmail(email)
    setOtpChannel(challenge.otpChannel)
    setHasVerifiedPhone(challenge.hasVerifiedPhone)
    setMaskedPhone(challenge.maskedPhone)
    setOtpCode("")
    setVerifyAttempts(0)
    setOtpFeedback(null)
    restartResendTimer(RESEND_COOLDOWN_SECONDS)
    setStep(STEPS.VERIFY_OTP)
  }

  const onLoginSubmit = async (values: SignInCredentialsValues) => {
    loginForm.clearErrors("root")

    try {
      const payload = toSignInPayload(values)
      const response = await axiosInstance.post(
        "/auth/universal-login",
        payload,
        { skipAuthRedirect: true }
      )
      const result = response.data

      if (result.loginType === "ADMIN") {
        if (!result.token) {
          loginForm.setError("root", {
            message: "Login succeeded but no session token was returned.",
          })
          return
        }

        persistAuthSession(result.token, "ADMIN")
        window.location.href = "/admin-dashboard"
        return
      }

      if (result.loginType === "SUPPORT") {
        if (!result.token) {
          loginForm.setError("root", {
            message: "Login succeeded but no session token was returned.",
          })
          return
        }

        persistAuthSession(result.token, "SUPPORT")
        window.location.href = "/support-dashboard"
        return
      }

      if (result.loginType === "USER") {
        const trustSkip = parseTrustSkipLoginResponse(result)

        if (trustSkip) {
          navigateAfterSession(trustSkip)
          return
        }

        const challenge = parseOtpChallengeResponse(result) ?? {
          otpChannel: "email" as const,
          hasVerifiedPhone: false,
          maskedPhone: null,
        }

        beginOtpChallenge(payload.email, payload.rememberDevice, challenge)
        return
      }
    } catch (error) {
      if (isAxiosError(error)) {
        loginForm.setError("root", {
          message: getFetchErrorMessage(error.response?.data, "Login failed."),
        })
        return
      }
      loginForm.setError("root", {
        message: "Unable to connect server.",
      })
    }
  }

  const handleVerifyOtp = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setOtpFeedback(null)

    if (otpCode.trim().length !== 6) {
      setOtpFeedback({
        kind: "error",
        code: "invalid",
        message: OTP_MESSAGES.incomplete,
      })
      return
    }

    if (verifyAttempts >= MAX_VERIFY_ATTEMPTS) {
      setOtpFeedback({
        kind: "error",
        code: "too_many_attempts",
        message: OTP_MESSAGES.too_many_attempts,
      })
      return
    }

    try {
      setOtpSubmitting(true)

      const response = await axiosInstance.post(
        "/auth/verify-otp",
        {
          email: otpEmail,
          otpCode: otpCode.trim(),
          rememberDevice,
          ...(getDeviceToken() ? { deviceToken: getDeviceToken() } : {}),
        },
        { skipAuthRedirect: true }
      )

      const verified = parseVerifyOtpResponse(response.data)

      if (!verified) {
        setOtpFeedback({
          kind: "error",
          code: "invalid",
          message: "Verification succeeded but session data was missing.",
        })
        return
      }

      navigateAfterSession(verified, verified.deviceToken)
    } catch (error) {
      if (isAxiosError(error)) {
        const message = getFetchErrorMessage(
          error.response?.data,
          "OTP verification failed."
        )
        const feedback = mapVerifyApiMessage(message)
        const nextAttempts = verifyAttempts + 1

        setVerifyAttempts(nextAttempts)

        if (nextAttempts >= MAX_VERIFY_ATTEMPTS) {
          setOtpFeedback({
            kind: "error",
            code: "too_many_attempts",
            message: OTP_MESSAGES.too_many_attempts,
          })
        } else {
          setOtpFeedback(feedback)
        }
      } else {
        setOtpFeedback({
          kind: "error",
          code: "invalid",
          message: "Verification failed.",
        })
      }
    } finally {
      setOtpSubmitting(false)
    }
  }

  const navigateAfterActivation = (
    accountType: string,
    workspaceSetupRequired: boolean,
    selectedLocationId: number | null,
    activationRequired: boolean
  ) => {
    const destination = getPostLoginDestination(
      accountType,
      workspaceSetupRequired,
      selectedLocationId,
      activationRequired
    )

    if (isWorkspaceSetupDestination(destination)) {
      setWorkspaces([])
      setSelectedLocationId(null)
      setWorkspaceError(null)
      setWorkspaceLoading(true)
      setStep(STEPS.WORKSPACE_SETUP)
      return
    }

    if (selectedLocationId != null) {
      persistSelectedLocation(selectedLocationId)
    }

    persistActivationRequired(activationRequired)

    window.location.href = destination
  }

  const handleActivationSubmit = async (
    event: FormEvent<HTMLFormElement>
  ) => {
    event.preventDefault()
    setActivationError(null)

    const parsed = signInActivationCodeSchema.safeParse({
      activationCode: normalizeActivationCodeInput(activationCode),
    })

    if (!parsed.success) {
      setActivationError(
        parsed.error.issues[0]?.message ?? "Enter a valid activation code."
      )
      return
    }

    try {
      setActivationSubmitting(true)

      const response = await activateAccount(parsed.data.activationCode)
      const data = unwrapDataObject(response)

      if (!data) {
        setActivationError("Activation succeeded but session data was missing.")
        return
      }

      const accountType =
        readString(data, "accountType") ??
        useAuthStore.getState().accountType ??
        "Single"

      navigateAfterActivation(
        accountType,
        readBoolean(data, "workspaceSetupRequired") ?? false,
        readNumber(data, "selectedLocationId"),
        readBoolean(data, "activationRequired") ?? false
      )
    } catch (error) {
      if (isAxiosError(error)) {
        setActivationError(
          getFetchErrorMessage(
            error.response?.data,
            "Activation failed."
          )
        )
        return
      }

      setActivationError("Activation failed.")
    } finally {
      setActivationSubmitting(false)
    }
  }

  const applyOtpSendResult = (result: SendOtpApiResult) => {
    setOtpChannel(result.otpChannel)
    if (result.maskedPhone) {
      setMaskedPhone(result.maskedPhone)
    }
  }

  const handleResendOtp = async () => {
    if (!canResend || otpSubmitting) {
      return
    }

    setOtpFeedback(null)

    try {
      setOtpSubmitting(true)
      const result = await requestOtpResend(otpEmail)

      applyOtpSendResult(result)
      setOtpCode("")
      setVerifyAttempts(0)
      restartResendTimer(RESEND_COOLDOWN_SECONDS)
      setOtpFeedback({
        kind: "info",
        code: "code_resent",
        message: getOtpResentMessage(result.otpChannel),
      })
    } catch (error) {
      const message =
        error instanceof Error
          ? error.message
          : "We couldn't resend the code. Try again shortly."

      setOtpFeedback(mapResendApiMessage(message))
    } finally {
      setOtpSubmitting(false)
    }
  }

  const handleSendViaEmail = async () => {
    setOtpFeedback(null)

    try {
      setOtpSubmitting(true)
      const result = await requestSwitchToEmailOtp(otpEmail)

      applyOtpSendResult(result)
      setOtpCode("")
      setVerifyAttempts(0)

      if (!result.skipped) {
        restartResendTimer(RESEND_COOLDOWN_SECONDS)
        if (result.otpChannel === "sms") {
          setOtpFeedback({
            kind: "info",
            code: "code_resent",
            message: getOtpSentMessage(result.otpChannel),
          })
        }
      }

      setStep(STEPS.VERIFY_OTP)
    } catch (error) {
      const message =
        error instanceof Error
          ? error.message
          : "Unable to continue with email verification."

      setOtpFeedback({
        kind: "error",
        code: "invalid",
        message,
      })
    } finally {
      setOtpSubmitting(false)
    }
  }

  const handleSendViaSms = async () => {
    setOtpFeedback(null)

    try {
      setOtpSubmitting(true)
      const result = await requestSwitchToSmsOtp(otpEmail)

      applyOtpSendResult(result)
      setOtpCode("")
      setVerifyAttempts(0)
      restartResendTimer(RESEND_COOLDOWN_SECONDS)
      setOtpFeedback({
        kind: "info",
        code: "code_resent",
        message: getOtpSentMessage(result.otpChannel),
      })
      setStep(STEPS.VERIFY_OTP)
    } catch (error) {
      const message =
        error instanceof Error
          ? error.message
          : "Unable to send SMS verification code."

      setOtpFeedback({
        kind: "error",
        code: "invalid",
        message,
      })
    } finally {
      setOtpSubmitting(false)
    }
  }

  const handleOtpChange = (value: string) => {
    setOtpCode(value)
    if (otpFeedback?.kind === "error") {
      setOtpFeedback(null)
    }
  }

  const handleWorkspaceSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setWorkspaceError(null)

    if (selectedLocationId == null) {
      setWorkspaceError("Select a workspace to continue.")
      return
    }

    try {
      setWorkspaceSubmitting(true)
      const selected = await submitWorkspaceSelection(selectedLocationId)
      persistSelectedLocation(selected.locationId)
      const auth = useAuthStore.getState()
      if (auth.token == null) {
        throw new Error("You must be signed in to continue.")
      }
      const accountType =
        selected.accountType ?? auth.accountType ?? "Multi"
      persistAuthSession(
        auth.token,
        "USER",
        accountType,
        auth.refreshToken
      )
      window.location.href = getPostLoginDestination(
        accountType,
        false,
        selected.locationId,
        false
      )
    } catch (error) {
      setWorkspaceError(
        error instanceof Error
          ? error.message
          : "Unable to save workspace selection."
      )
    } finally {
      setWorkspaceSubmitting(false)
    }
  }

  const otpDestination = getOtpDestinationCopy(
    otpChannel,
    otpEmail,
    maskedPhone
  )

  const setupNotice = (() => {
    const setupState = searchParams.get("setup")

    if (setupState === "complete") {
      return {
        tone: "success" as const,
        message:
          "Your account is ready. Sign in with the email and password you just created.",
      }
    }

    if (setupState === "invalid") {
      return {
        tone: "error" as const,
        message:
          "That setup link is invalid or has expired. Sign in if you already completed setup, or request a new invite.",
      }
    }

    return null
  })()

  const awaitingSessionRedirect =
    token &&
    !authRedirectResolved &&
    authRedirectTarget === null &&
    searchParams.get("step") !== "workspace-setup" &&
    searchParams.get("step") !== "activation-code"

  if (!hasHydrated || awaitingSessionRedirect) {
    return (
      <AuthShell>
        <AuthSessionLoading />
      </AuthShell>
    )
  }

  if (authRedirectTarget) {
    return <Navigate to={authRedirectTarget} replace />
  }

  return (
    <AuthShell>
      {setupNotice ? (
        <div
          className={`mb-6 w-full max-w-[420px] rounded-[16px] border px-4 py-3 text-sm leading-relaxed ${
            setupNotice.tone === "success"
              ? "border-[#BBF7D0] bg-[#F0FDF4] text-[#166534]"
              : "border-[#FECACA] bg-[#FEF2F2] text-[#991B1B]"
          }`}
          role="status"
        >
          {setupNotice.message}
        </div>
      ) : null}

      {step === STEPS.LOGIN && (
        <SignInForm form={loginForm} onSubmit={onLoginSubmit} />
      )}

      {step === STEPS.VERIFY_OTP && (
        <SignInVerifyOtpStep
          destination={otpDestination}
          otpCode={otpCode}
          submitting={otpSubmitting}
          feedback={otpFeedback}
          resendSecondsRemaining={resendSecondsRemaining}
          canResend={canResend}
          onOtpChange={handleOtpChange}
          onVerify={handleVerifyOtp}
          onResend={handleResendOtp}
          onChooseSignInMethod={() => setStep(STEPS.CHOOSE_SIGN_IN_METHOD)}
        />
      )}

      {step === STEPS.CHOOSE_SIGN_IN_METHOD && (
        <SignInChooseMethodStep
          submitting={otpSubmitting}
          feedback={otpFeedback}
          hasVerifiedPhone={hasVerifiedPhone}
          onSendViaEmail={handleSendViaEmail}
          onSendViaSms={handleSendViaSms}
        />
      )}

      {step === STEPS.ACTIVATION_CODE && (
        <SignInActivationCodeStep
          activationCode={activationCode}
          submitting={activationSubmitting}
          error={activationError}
          onActivationCodeChange={(value) =>
            setActivationCode(normalizeActivationCodeInput(value))
          }
          onSubmit={handleActivationSubmit}
        />
      )}

      {step === STEPS.WORKSPACE_SETUP && (
        <SignInChooseWorkspaceStep
          workspaces={workspaces}
          selectedLocationId={selectedLocationId}
          loading={workspaceLoading}
          submitting={workspaceSubmitting}
          error={workspaceError}
          onSelect={setSelectedLocationId}
          onSubmit={handleWorkspaceSubmit}
        />
      )}
    </AuthShell>
  )
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
