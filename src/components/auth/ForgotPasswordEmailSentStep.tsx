import { useState } from "react"

import { AuthBackToLogin } from "@/components/auth/AuthBackToLogin"
import { AuthFormHeader } from "@/components/auth/AuthFormHeader"
import { Button } from "@/components/ui/button"
import { FieldErrorSlot } from "@/components/ui/field"

type ForgotPasswordEmailSentStepProps = {
  email: string
  onResend: () => Promise<void>
  resending?: boolean
}

export function ForgotPasswordEmailSentStep({
  email,
  onResend,
  resending = false,
}: ForgotPasswordEmailSentStepProps) {
  const [resendError, setResendError] = useState<string | undefined>()
  const [isResending, setIsResending] = useState(false)
  const busy = resending || isResending

  const handleResend = async () => {
    setResendError(undefined)
    setIsResending(true)

    try {
      await onResend()
    } catch (error) {
      setResendError(
        error instanceof Error ? error.message : "Unable to send reset link."
      )
    } finally {
      setIsResending(false)
    }
  }

  return (
    <div className="flex w-full flex-col gap-10">
      <AuthFormHeader
        title="Check your inbox"
        description={`If a Tummly account exists for ${email}, we've sent a password reset link.`}
      />

      <div className="flex flex-col gap-9">
        <div className="flex flex-col gap-1.5">
          <p className="m-0 flex flex-wrap items-center gap-2.5 text-sm font-medium tracking-[0.4px] text-[#232323]">
            <span>Didn&apos;t get it?</span>
            <Button
              type="button"
              variant="link"
              size="link-sm"
              disabled={busy}
              onClick={() => {
                void handleResend()
              }}
              className="font-medium text-primary underline underline-offset-2"
            >
              {busy ? "Sending..." : "Send another link"}
            </Button>
          </p>

          <FieldErrorSlot error={resendError} />
        </div>

        <div className="h-px w-full bg-[#d2d2d2]" />

        <AuthBackToLogin />
      </div>
    </div>
  )
}
