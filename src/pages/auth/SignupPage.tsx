import { useEffect } from "react"
import { zodResolver } from "@hookform/resolvers/zod"
import { isAxiosError } from "axios"
import { useForm } from "react-hook-form"
import { useLocation, useNavigate, useSearchParams } from "react-router-dom"

import { startSignup } from "@/api/signupApi"
import {
  SIGNUP_RECOVERY_ERROR_TYPE,
  SignupForm,
} from "@/components/signup/SignupForm"
import { SignupModalShell } from "@/components/signup/SignupModalShell"
import { defaultFormValidationOptions } from "@/lib/form"
import { mapSignupStartError } from "@/lib/signupOtpFeedback"
import {
  clearOAuthErrorMessage,
  peekOAuthErrorMessage,
  resolveOAuthErrorBanner,
  stashOAuthErrorMessage,
} from "@/lib/oauthErrorCopy"
import {
  buildSignupVerifyPath,
  SIGNUP_SESSION_EXPIRED_MESSAGE,
  isSignupSessionExpiredState,
} from "@/lib/signupSession"
import { captureSignupPlanIntentFromSearch } from "@/lib/signupPlanIntent"
import {
  signupStartDefaultValues,
  signupStartSchema,
  toSignupStartPayload,
  type SignupStartValues,
} from "@/schemas/signup"

function getApiErrorMessage(error: unknown, fallback: string) {
  if (isAxiosError<{ message?: string }>(error)) {
    return error.response?.data?.message ?? fallback
  }

  if (error instanceof Error && error.message) {
    return error.message
  }

  return fallback
}

function SignupPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const [searchParams, setSearchParams] = useSearchParams()

  const form = useForm<SignupStartValues>({
    resolver: zodResolver(signupStartSchema),
    defaultValues: signupStartDefaultValues,
    ...defaultFormValidationOptions,
  })

  useEffect(() => {
    captureSignupPlanIntentFromSearch(searchParams)
  }, [searchParams])

  useEffect(() => {
    // A user mid-signup at deploy is sent here with the expired state.
    if (isSignupSessionExpiredState(location.state)) {
      form.setError("root", { message: SIGNUP_SESSION_EXPIRED_MESSAGE })
    }
    // Intentional: show once for the navigation that carried the state.
    // eslint-disable-next-line react-hooks/exhaustive-deps -- one-shot banner
  }, [])

  useEffect(() => {
    const emailFromQuery = searchParams.get("email")?.trim()
    if (emailFromQuery == null || emailFromQuery === "") {
      return
    }

    form.setValue("email", emailFromQuery, {
      shouldDirty: true,
      shouldTouch: true,
    })

    const next = new URLSearchParams(searchParams)
    next.delete("email")
    setSearchParams(next, { replace: true })
    // Intentional: prefill once from marketing CTA, then strip email from the URL.
    // eslint-disable-next-line react-hooks/exhaustive-deps -- one-shot prefill
  }, [])

  useEffect(() => {
    const oauthError = searchParams.get("oauthError")
    const oauthMessage = searchParams.get("oauthMessage")
    const stashed = peekOAuthErrorMessage("signup")

    const message = resolveOAuthErrorBanner("signup", {
      oauthError,
      oauthMessage,
      stashed,
    })

    if (message) {
      stashOAuthErrorMessage("signup", message)
      form.setError("root", { message })
    }

    if (oauthError || oauthMessage) {
      const next = new URLSearchParams(searchParams)
      next.delete("oauthError")
      next.delete("oauthMessage")
      setSearchParams(next, { replace: true })
    }
    // Intentional: surface oauthError once then strip from the URL.
    // sessionStorage stash survives Strict Mode remount.
    // eslint-disable-next-line react-hooks/exhaustive-deps -- one-shot banner
  }, [])

  const onSubmit = async (values: SignupStartValues) => {
    clearOAuthErrorMessage()
    form.clearErrors("root")
    form.clearErrors("email")

    try {
      const payload = toSignupStartPayload(values)
      const result = await startSignup(payload)
      navigate(buildSignupVerifyPath(result.email))
    } catch (error) {
      // A rate-limit or send failure uses the fixed signup copy and, when the user
      // is stuck, the Sign in / Forgot password / Contact us links.
      const feedback = mapSignupStartError(
        error,
        getApiErrorMessage(error, "We couldn't start signup. Please try again.")
      )
      form.setError("root", {
        type: feedback.showRecoveryLinks ? SIGNUP_RECOVERY_ERROR_TYPE : undefined,
        message: feedback.message,
      })
    }
  }

  return (
    <SignupModalShell>
      <SignupForm form={form} onSubmit={onSubmit} />
    </SignupModalShell>
  )
}

export default SignupPage
