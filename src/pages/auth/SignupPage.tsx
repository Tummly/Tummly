import { zodResolver } from "@hookform/resolvers/zod"
import { isAxiosError } from "axios"
import { useForm } from "react-hook-form"
import { useNavigate } from "react-router-dom"

import { startSignup } from "@/api/signupApi"
import { SignupForm } from "@/components/signup/SignupForm"
import { SignupModalShell } from "@/components/signup/SignupModalShell"
import { OTP_MESSAGES } from "@/components/home/hero-trial-otp"
import { defaultFormValidationOptions } from "@/lib/form"
import {
  buildSignupVerifyPath,
  saveSignupSessionToken,
} from "@/lib/signupSession"
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

  const form = useForm<SignupStartValues>({
    resolver: zodResolver(signupStartSchema),
    defaultValues: signupStartDefaultValues,
    ...defaultFormValidationOptions,
  })

  const onSubmit = async (values: SignupStartValues) => {
    form.clearErrors("root")
    form.clearErrors("email")

    try {
      const payload = toSignupStartPayload(values)
      const session = await startSignup(payload)
      saveSignupSessionToken(session.sessionToken)
      navigate(buildSignupVerifyPath(session.email))
    } catch (error) {
      const message = getApiErrorMessage(
        error,
        "We couldn't start signup. Please try again."
      )
      const normalized = message.trim().toLowerCase()

      if (normalized.includes("email already in use")) {
        form.setError("email", {
          message: OTP_MESSAGES.email_in_use,
        })
        return
      }

      form.setError("root", { message })
    }
  }

  return (
    <SignupModalShell>
      <SignupForm form={form} onSubmit={onSubmit} />
    </SignupModalShell>
  )
}

export default SignupPage
