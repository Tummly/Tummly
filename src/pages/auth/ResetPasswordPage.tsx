import { useState } from "react"
import { zodResolver } from "@hookform/resolvers/zod"
import { useForm } from "react-hook-form"
import { Link, useSearchParams } from "react-router-dom"

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

const cardClassName =
  "flex w-full max-w-[490px] shrink-0 flex-col gap-6 rounded-[6px] border border-[#d2d2d2] bg-white px-[clamp(1.25rem,4vw,1.875rem)] py-[clamp(1.25rem,4vw,2.375rem)] shadow-[2px_6px_14px_rgba(0,0,0,0.04),9px_25px_26px_rgba(0,0,0,0.03),20px_55px_35px_rgba(0,0,0,0.02)]"

const headingClassName =
  "m-0 text-[clamp(1.625rem,4vw,2rem)] font-bold leading-normal tracking-[-0.64px] text-[#232323]"

const bodyClassName = "m-0 text-sm leading-normal text-[#232323]"

const linkButtonClassName =
  "self-start font-medium text-primary underline underline-offset-2"

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
        <div className={cardClassName}>
          <h1 className={headingClassName}>Link expired or invalid</h1>

          <p className={bodyClassName} role="alert">
            {tokenErrorMessage}
          </p>

          <p className={bodyClassName}>
            Request a new reset link or return to sign in to continue.
          </p>

          <div className="flex flex-col gap-3">
            <Button
              type="button"
              variant="link"
              size="link-sm"
              asChild
              className={linkButtonClassName}
            >
              <Link to="/forgot-password">Request a new reset link</Link>
            </Button>

            <Button
              type="button"
              variant="link"
              size="link-sm"
              asChild
              className={linkButtonClassName}
            >
              <Link to="/login">Back to sign in</Link>
            </Button>
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
