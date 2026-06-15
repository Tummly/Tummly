import { useState } from "react"
import { zodResolver } from "@hookform/resolvers/zod"
import { useForm } from "react-hook-form"

import { AuthShell } from "@/components/auth/AuthShell"
import { ForgotPasswordEmailSentStep } from "@/components/auth/ForgotPasswordEmailSentStep"
import { ForgotPasswordRequestStep } from "@/components/auth/ForgotPasswordRequestStep"
import {
  FORGOT_PASSWORD_STEPS,
  requestPasswordReset,
  type ForgotPasswordStep,
} from "@/lib/forgotPasswordFlow"
import { defaultFormValidationOptions } from "@/lib/form"
import {
  signInEmailDefaultValues,
  signInEmailSchema,
  type SignInEmailValues,
} from "@/schemas/signIn"

function ForgotPasswordPage() {
  const [step, setStep] = useState<ForgotPasswordStep>(
    FORGOT_PASSWORD_STEPS.REQUEST_EMAIL
  )

  const form = useForm<SignInEmailValues>({
    resolver: zodResolver(signInEmailSchema),
    defaultValues: signInEmailDefaultValues,
    ...defaultFormValidationOptions,
  })

  const onSubmit = async (values: SignInEmailValues) => {
    form.clearErrors("root")

    try {
      await requestPasswordReset(values)
      setStep(FORGOT_PASSWORD_STEPS.EMAIL_SENT)
    } catch (error) {
      form.setError("root", {
        message:
          error instanceof Error ? error.message : "Request failed.",
      })
    }
  }

  return (
    <AuthShell>
      {step === FORGOT_PASSWORD_STEPS.REQUEST_EMAIL && (
        <ForgotPasswordRequestStep form={form} onSubmit={onSubmit} />
      )}

      {step === FORGOT_PASSWORD_STEPS.EMAIL_SENT && (
        <ForgotPasswordEmailSentStep />
      )}
    </AuthShell>
  )
}

export default ForgotPasswordPage
