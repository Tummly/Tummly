import { useEffect, useState, type FormEvent, type ReactNode } from "react"
import { useSearchParams } from "react-router-dom"

import {
  acceptTeamInvitationInPlace,
  previewTeamInvitation,
  submitTeamInvitationCredentials,
  submitTeamInvitationSignIn,
  verifyTeamInvitationOtp,
  type TeamInvitationPreview,
  type TeamInvitationSession,
} from "@/api/teamInvitationAcceptApi"
import { AuthSessionLoading } from "@/components/auth/AuthSessionLoading"
import { AuthShell } from "@/components/auth/AuthShell"
import { PasswordStrengthMeter } from "@/components/auth/PasswordStrengthMeter"
import { SignInChooseWorkspaceStep } from "@/components/auth/SignInChooseWorkspaceStep"
import { SignInVerifyOtpStep } from "@/components/auth/SignInVerifyOtpStep"
import { type OtpFeedback } from "@/components/home/hero-trial-otp"
import { Button } from "@/components/ui/button"
import { FieldErrorSlot } from "@/components/ui/field"
import { FloatingLabelInput } from "@/components/ui/floating-label-input"
import { PASSWORD_REQUIREMENTS_HINT } from "@/constants/passwordCopy"
import { isPasswordAtLeastGood } from "@/lib/passwordStrength"
import {
  fetchWorkspaceLocations,
  submitWorkspaceSelection,
  type WorkspaceLocation,
} from "@/lib/workspaceSetupFlow"
import {
  clearAuthSession,
  getMultiDashboardPath,
  persistActivationRequired,
  persistAuthSession,
  persistSelectedLocation,
} from "@/pages/utils/authHelpers"
import { useAuthStore } from "@/stores/authStore"

const cardShadow =
  "shadow-[2px_6px_14px_rgba(0,0,0,0.04),9px_25px_26px_rgba(0,0,0,0.03),20px_55px_35px_rgba(0,0,0,0.02)]"

type Step =
  | "loading"
  | "error"
  | "form"
  | "otp"
  | "wait"
  | "expired"
  | "workspace"
  | "wrong-email"

const INVALID_INVITE = "This invitation is not valid."
const OWNER_EXPIRED = "Your 30 day free trial is over"
const OWNER_WAIT =
  "This workspace is waiting for the Account owner to activate."

function applySession(session: TeamInvitationSession) {
  persistAuthSession(
    session.token,
    "USER",
    session.accountType,
    session.refreshToken
  )
  persistActivationRequired(false)
  if (session.selectedLocationId != null) {
    persistSelectedLocation(session.selectedLocationId)
  }
}

function dashboardHref(session: TeamInvitationSession): string {
  if (session.accountType === "Single") {
    return "/single-dashboard"
  }
  return getMultiDashboardPath(session.selectedLocationId)
}

function finishSession(session: TeamInvitationSession): Step | "go" {
  if (session.ownerActivation === "pending") {
    return "wait"
  }
  if (session.ownerActivation === "expired") {
    return "expired"
  }
  if (session.workspaceCount > 1) {
    return "workspace"
  }
  return "go"
}

function TeamInvitationAcceptPage() {
  const [searchParams] = useSearchParams()
  const invite = (searchParams.get("invite") ?? "").trim()
  const hasHydrated = useAuthStore((state) => state._hasHydrated)

  const [step, setStep] = useState<Step>("loading")
  const [preview, setPreview] = useState<TeamInvitationPreview | null>(null)
  const [error, setError] = useState(INVALID_INVITE)
  const [fullName, setFullName] = useState("")
  const [password, setPassword] = useState("")
  const [formError, setFormError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const [otpCode, setOtpCode] = useState("")
  const [otpFeedback, setOtpFeedback] = useState<OtpFeedback | null>(null)
  const [session, setSession] = useState<TeamInvitationSession | null>(null)
  const [workspaces, setWorkspaces] = useState<WorkspaceLocation[]>([])
  const [selectedLocationId, setSelectedLocationId] = useState<number | null>(
    null
  )
  const [workspaceLoading, setWorkspaceLoading] = useState(false)
  const [workspaceError, setWorkspaceError] = useState<string | null>(null)
  const [workspaceSubmitting, setWorkspaceSubmitting] = useState(false)

  useEffect(() => {
    if (!hasHydrated) {
      return
    }
    if (invite === "") {
      setError(INVALID_INVITE)
      setStep("error")
      return
    }

    let cancelled = false
    const load = async () => {
      setStep("loading")
      try {
        const next = await previewTeamInvitation(invite)
        if (cancelled) {
          return
        }
        setPreview(next)
        setFullName(next.fullName)
        if (next.session === "wrong-email") {
          setStep("wrong-email")
          return
        }
        setStep("form")
      } catch (caught) {
        if (cancelled) {
          return
        }
        setError(
          caught instanceof Error ? caught.message : INVALID_INVITE
        )
        setStep("error")
      }
    }
    void load()
    return () => {
      cancelled = true
    }
  }, [hasHydrated, invite])

  useEffect(() => {
    if (step !== "workspace") {
      return
    }
    let cancelled = false
    const load = async () => {
      setWorkspaceLoading(true)
      setWorkspaceError(null)
      try {
        const locations = await fetchWorkspaceLocations()
        if (cancelled) {
          return
        }
        setWorkspaces(locations)
        setSelectedLocationId(null)
      } catch (caught) {
        if (cancelled) {
          return
        }
        setWorkspaceError(
          caught instanceof Error
            ? caught.message
            : "Unable to load workspaces."
        )
      } finally {
        if (!cancelled) {
          setWorkspaceLoading(false)
        }
      }
    }
    void load()
    return () => {
      cancelled = true
    }
  }, [step])

  const completeAccept = (next: TeamInvitationSession) => {
    applySession(next)
    setSession(next)
    const outcome = finishSession(next)
    if (outcome === "go") {
      window.location.href = dashboardHref(next)
      return
    }
    setStep(outcome)
  }

  const sendOtp = async () => {
    if (preview == null) {
      return
    }
    if (preview.existingUser) {
      await submitTeamInvitationSignIn({ invite, password })
      return
    }
    await submitTeamInvitationCredentials({
      invite,
      fullName: fullName.trim(),
      password,
    })
  }

  const onFormSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (preview == null || submitting) {
      return
    }
    setFormError(null)
    setSubmitting(true)
    try {
      if (preview.session === "invited-email") {
        completeAccept(await acceptTeamInvitationInPlace(invite))
        return
      }
      if (!preview.existingUser) {
        if (fullName.trim() === "") {
          setFormError("Full name is required.")
          return
        }
        if (!isPasswordAtLeastGood(password)) {
          setFormError(PASSWORD_REQUIREMENTS_HINT)
          return
        }
      } else if (password.trim() === "") {
        setFormError("Password is required.")
        return
      }
      await sendOtp()
      setOtpCode("")
      setOtpFeedback({
        kind: "info",
        code: "code_resent",
        message: `We sent a 6 digit code to ${preview.email}.`,
      })
      setStep("otp")
    } catch (caught) {
      setFormError(
        caught instanceof Error ? caught.message : "Could not continue."
      )
    } finally {
      setSubmitting(false)
    }
  }

  const onVerifyOtp = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (preview == null || submitting) {
      return
    }
    setSubmitting(true)
    setOtpFeedback(null)
    try {
      completeAccept(
        await verifyTeamInvitationOtp({
          invite,
          email: preview.email,
          otpCode,
        })
      )
    } catch (caught) {
      setOtpFeedback({
        kind: "error",
        code: "invalid",
        message: caught instanceof Error ? caught.message : "Invalid OTP.",
      })
    } finally {
      setSubmitting(false)
    }
  }

  const onResendOtp = async () => {
    if (preview == null || submitting) {
      return
    }
    setSubmitting(true)
    try {
      await sendOtp()
      setOtpFeedback({
        kind: "info",
        code: "code_resent",
        message: `We sent a new code to ${preview.email}.`,
      })
    } catch (caught) {
      setOtpFeedback({
        kind: "error",
        code: "invalid",
        message:
          caught instanceof Error ? caught.message : "Could not resend code.",
      })
    } finally {
      setSubmitting(false)
    }
  }

  const onSignOut = () => {
    clearAuthSession()
    setStep("loading")
    void previewTeamInvitation(invite)
      .then((next) => {
        setPreview(next)
        setFullName(next.fullName)
        setStep(next.session === "wrong-email" ? "wrong-email" : "form")
      })
      .catch((caught: unknown) => {
        setError(caught instanceof Error ? caught.message : INVALID_INVITE)
        setStep("error")
      })
  }

  const onWorkspaceSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (selectedLocationId == null || session == null) {
      return
    }
    setWorkspaceSubmitting(true)
    setWorkspaceError(null)
    try {
      const selected = await submitWorkspaceSelection(selectedLocationId)
      persistSelectedLocation(selected.locationId)
      const accountType = selected.accountType ?? session.accountType
      persistAuthSession(
        session.token,
        "USER",
        accountType,
        session.refreshToken
      )
      window.location.href = dashboardHref({
        ...session,
        accountType,
        selectedLocationId: selected.locationId,
      })
    } catch (caught) {
      setWorkspaceError(
        caught instanceof Error
          ? caught.message
          : "Unable to save workspace selection."
      )
    } finally {
      setWorkspaceSubmitting(false)
    }
  }

  if (!hasHydrated || step === "loading") {
    return <AuthSessionLoading />
  }

  return (
    <AuthShell>
      {step === "error" ? (
        <StatusCard title="Invitation not valid" body={error} />
      ) : null}
      {step === "wait" ? (
        <StatusCard title="Waiting for activation" body={OWNER_WAIT} />
      ) : null}
      {step === "expired" ? (
        <StatusCard title="Trial ended" body={OWNER_EXPIRED} />
      ) : null}
      {step === "wrong-email" && preview != null ? (
        <StatusCard
          title="Wrong account"
          body={`You are signed in with a different email. Sign out to continue as ${preview.email}.`}
        >
          <Button
            type="button"
            className="h-auto min-h-0 w-full rounded-[4px] bg-[#14a74a] px-[17px] py-[15px] text-base font-medium leading-5 text-white hover:bg-[#129641]"
            onClick={onSignOut}
          >
            Sign out
          </Button>
        </StatusCard>
      ) : null}
      {step === "form" && preview != null ? (
        <form
          onSubmit={(event) => {
            void onFormSubmit(event)
          }}
          noValidate
          className={`flex w-full max-w-[490px] shrink-0 flex-col gap-6 rounded-[6px] border border-[#d2d2d2] bg-white px-[clamp(1.25rem,4vw,1.875rem)] py-[clamp(1.25rem,4vw,2.375rem)] ${cardShadow}`}
        >
          <header className="flex flex-col gap-4 text-[#232323]">
            <h1 className="m-0 text-[clamp(1.625rem,4vw,2rem)] font-bold leading-normal tracking-[-0.64px]">
              Join {preview.workspaceName}
            </h1>
            <p className="m-0 text-sm leading-normal">
              {preview.session === "invited-email"
                ? `Accept this invitation as ${preview.email} to join as ${preview.roleName} (${preview.locationScope}).`
                : preview.existingUser
                  ? `Sign in as ${preview.email} to join as ${preview.roleName} (${preview.locationScope}).`
                  : `Create your Tummly account to join as ${preview.roleName} (${preview.locationScope}).`}
            </p>
          </header>
          {preview.session === "invited-email" ? null : (
            <div className="flex flex-col gap-5">
              {preview.existingUser ? null : (
                <FloatingLabelInput
                  label="Full name"
                  value={fullName}
                  autoComplete="name"
                  onChange={(event) => setFullName(event.target.value)}
                  disabled={submitting}
                />
              )}
              <div className="flex flex-col gap-3">
                <FloatingLabelInput
                  label="Password"
                  type="password"
                  value={password}
                  autoComplete={
                    preview.existingUser ? "current-password" : "new-password"
                  }
                  onChange={(event) => setPassword(event.target.value)}
                  disabled={submitting}
                />
                {preview.existingUser ? null : (
                  <PasswordStrengthMeter
                    password={password}
                    hideWhenEmpty
                  />
                )}
              </div>
              {preview.existingUser ? null : (
                <p className="m-0 text-sm leading-5 text-[#232323]">
                  {PASSWORD_REQUIREMENTS_HINT}
                </p>
              )}
            </div>
          )}
          <FieldErrorSlot error={formError ?? undefined} />
          <Button
            type="submit"
            disabled={submitting}
            className="h-auto min-h-0 w-full rounded-[4px] bg-[#14a74a] px-[17px] py-[15px] text-base font-medium leading-5 text-white hover:bg-[#129641]"
          >
            {submitting
              ? "Please wait..."
              : preview.session === "invited-email"
                ? "Accept invitation"
                : preview.existingUser
                  ? "Continue"
                  : "Create account"}
          </Button>
        </form>
      ) : null}
      {step === "otp" && preview != null ? (
        <SignInVerifyOtpStep
          destination={preview.email}
          otpCode={otpCode}
          submitting={submitting}
          feedback={otpFeedback}
          resendSecondsRemaining={0}
          canResend
          onOtpChange={setOtpCode}
          onVerify={(event) => {
            void onVerifyOtp(event)
          }}
          onResend={() => {
            void onResendOtp()
          }}
        />
      ) : null}
      {step === "workspace" ? (
        <SignInChooseWorkspaceStep
          workspaces={workspaces}
          selectedLocationId={selectedLocationId}
          loading={workspaceLoading}
          submitting={workspaceSubmitting}
          error={workspaceError}
          onSelect={setSelectedLocationId}
          onSubmit={(event) => {
            void onWorkspaceSubmit(event)
          }}
        />
      ) : null}
    </AuthShell>
  )
}

function StatusCard({
  title,
  body,
  children,
}: {
  title: string
  body: string
  children?: ReactNode
}) {
  return (
    <div
      className={`flex w-full max-w-[490px] shrink-0 flex-col gap-6 rounded-[6px] border border-[#d2d2d2] bg-white px-[clamp(1.25rem,4vw,1.875rem)] py-[clamp(1.25rem,4vw,2.375rem)] ${cardShadow}`}
    >
      <header className="flex flex-col gap-4 text-[#232323]">
        <h1 className="m-0 text-[clamp(1.625rem,4vw,2rem)] font-bold leading-normal tracking-[-0.64px]">
          {title}
        </h1>
        <p className="m-0 text-sm leading-normal">{body}</p>
      </header>
      {children}
    </div>
  )
}

export default TeamInvitationAcceptPage
