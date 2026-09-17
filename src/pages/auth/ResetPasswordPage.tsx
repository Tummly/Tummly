import { useState } from "react"
import { zodResolver } from "@hookform/resolvers/zod"
import { useForm } from "react-hook-form"
import { Link, useSearchParams } from "react-router-dom"

import { AuthBackToLogin } from "@/components/auth/AuthBackToLogin"
import { AuthFormHeader } from "@/components/auth/AuthFormHeader"
import { AuthShell } from "@/components/auth/AuthShell"
import { ResetPasswordCreateStep } from "@/components/auth/ResetPasswordCreateStep"
import { ResetPasswordSuccessStep } from "@/components/auth/ResetPasswordSuccessStep"
import { Button } from "@/components/ui/button"
import { defaultFormValidationOptions } from "@/lib/form"
import {
  isResetTokenError,
  RESET_PASSWORD_STEPS,
  submitPasswordReset,
  type ResetPasswordStep,
} from "@/lib/resetPasswordFlow"
import {
  resetPasswordDefaultValues,
  resetPasswordFormSchema,
  type ResetPasswordFormValues,
} from "@/schemas/resetPassword"

function ResetPasswordPage() {
  const [searchParams] = useSearchParams()
  const token = searchParams.get("token")

  const [step, setStep] = useState<ResetPasswordStep>(() =>
    token ? RESET_PASSWORD_STEPS.CREATE_PASSWORD : RESET_PASSWORD_STEPS.INVALID_TOKEN
  )
  const [tokenErrorMessage, setTokenErrorMessage] = useState(
    "This password reset link is invalid or has expired."
  )

  const form = useForm<ResetPasswordFormValues>({
    resolver: zodResolver(resetPasswordFormSchema),
    defaultValues: resetPasswordDefaultValues,
    ...defaultFormValidationOptions,
  })

  const onSubmit = async (values: ResetPasswordFormValues) => {
    if (!token) {
      setTokenErrorMessage("This password reset link is invalid or has expired.")
      setStep(RESET_PASSWORD_STEPS.INVALID_TOKEN)
      return
    }

    form.clearErrors("root")

    try {
      await submitPasswordReset(values, token)
      setStep(RESET_PASSWORD_STEPS.SUCCESS)
    } catch (error) {
      const message =
        error instanceof Error ? error.message : "Unable to reset password."

      if (isResetTokenError(message)) {
        setTokenErrorMessage(message)
        setStep(RESET_PASSWORD_STEPS.INVALID_TOKEN)
        return
      }

      form.setError("root", { message })
    }
  }

  return (
    <AuthShell>
      {step === RESET_PASSWORD_STEPS.INVALID_TOKEN && (
        <div className="flex w-full flex-col gap-10">
          <AuthFormHeader
            title="Link expired or invalid"
            description={
              <span role="alert">{tokenErrorMessage}</span>
            }
          />

          <div className="flex flex-col gap-9">
            <p className="m-0 text-base leading-[22px] text-[#141414]">
              Request a new reset link or return to sign in to continue.
            </p>

            <div className="flex flex-col gap-3">
              <Button
                type="button"
                variant="link"
                size="link-sm"
                asChild
                className="self-start font-medium text-primary underline underline-offset-2"
              >
                <Link to="/forgot-password">Request a new reset link</Link>
              </Button>

              <AuthBackToLogin />
            </div>
          </div>
        </div>
      )}

      {step === RESET_PASSWORD_STEPS.SUCCESS && <ResetPasswordSuccessStep />}

      {step === RESET_PASSWORD_STEPS.CREATE_PASSWORD && (
        <ResetPasswordCreateStep form={form} onSubmit={onSubmit} />
      )}
    </AuthShell>
  )
}

export default ResetPasswordPage
