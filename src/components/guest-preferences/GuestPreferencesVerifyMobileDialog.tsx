import { useState, type FormEvent } from "react"
import { REGEXP_ONLY_DIGITS } from "input-otp"

import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import {
  InputOTP,
  InputOTPGroup,
  InputOTPSlot,
} from "@/components/ui/input-otp"
import { GUEST_PREFERENCES_COPY } from "@/content/guestPreferences/guestPreferencesCopy"
import {
  GUEST_PREFERENCES_DIALOG_CLASS,
  GUEST_PREFERENCES_DIALOG_DESCRIPTION_CLASS,
  GUEST_PREFERENCES_DIALOG_TITLE_CLASS,
  GUEST_PREFERENCES_LINK_CLASS,
  GUEST_PREFERENCES_PILL_BUTTON_CLASS,
} from "@/lib/guestPreferences/guestPreferencesPresentation"
import {
  CODE_LENGTH,
  isCompleteCode,
} from "@/lib/guestPreferences/contactVerifyFlow"
import { cn } from "@/lib/utils"

type GuestPreferencesVerifyMobileFormProps = {
  busy: boolean
  error: string | null
  resendSeconds: number
  onVerify: (code: string) => void
  onResend: () => void
  onChangeNumber: () => void
}

/** Six-digit code, Verify, Resend code and Change number. */
export function GuestPreferencesVerifyMobileForm({
  busy,
  error,
  resendSeconds,
  onVerify,
  onResend,
  onChangeNumber,
}: GuestPreferencesVerifyMobileFormProps) {
  const [code, setCode] = useState("")
  const canVerify = !busy && isCompleteCode(code)

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (canVerify) {
      onVerify(code)
    }
  }

  return (
    <form
      className="flex w-full flex-col items-center gap-[14px]"
      noValidate
      onSubmit={handleSubmit}
    >
      <InputOTP
        autoFocus
        maxLength={CODE_LENGTH}
        pattern={REGEXP_ONLY_DIGITS}
        inputMode="numeric"
        autoComplete="one-time-code"
        aria-label={GUEST_PREFERENCES_COPY.verificationCodeLabel}
        aria-invalid={error ? true : undefined}
        value={code}
        disabled={busy}
        onChange={setCode}
        containerClassName="w-full"
      >
        <InputOTPGroup className="gap-2">
          {Array.from({ length: CODE_LENGTH }, (_, index) => (
            <InputOTPSlot
              key={index}
              index={index}
              className="h-[46px] rounded-[8px] border-guest-feedback-border bg-transparent font-sans text-base text-guest-feedback-text data-[active=true]:border-guest-feedback-accent data-[active=true]:ring-guest-feedback-accent/20"
            />
          ))}
        </InputOTPGroup>
      </InputOTP>

      {error ? (
        <p role="alert" className="m-0 text-center font-sans text-sm leading-5 text-destructive">
          {error}
        </p>
      ) : null}

      <Button
        type="submit"
        disabled={!canVerify}
        className={GUEST_PREFERENCES_PILL_BUTTON_CLASS}
      >
        {GUEST_PREFERENCES_COPY.verifyLabel}
      </Button>

      <div className="flex items-center justify-center gap-7">
        <Button
          type="button"
          variant="link"
          size="link-sm"
          disabled={busy || resendSeconds > 0}
          onClick={onResend}
          className={cn(GUEST_PREFERENCES_LINK_CLASS, "text-xs")}
        >
          {GUEST_PREFERENCES_COPY.resendCodeLabel}
        </Button>
        <Button
          type="button"
          variant="link"
          size="link-sm"
          disabled={busy}
          onClick={onChangeNumber}
          className={cn(GUEST_PREFERENCES_LINK_CLASS, "text-xs")}
        >
          {GUEST_PREFERENCES_COPY.changeNumberLabel}
        </Button>
      </div>
    </form>
  )
}

type GuestPreferencesVerifyMobileDialogProps = Omit<
  GuestPreferencesVerifyMobileFormProps,
  "onChangeNumber"
> & {
  open: boolean
  /** Entry and add-contact use different headings. */
  title: string
  onOpenChange: (open: boolean) => void
  onChangeNumber: () => void
}

/** SMS code dialog. Figma 7196:1226. */
export function GuestPreferencesVerifyMobileDialog({
  open,
  title,
  onOpenChange,
  ...form
}: GuestPreferencesVerifyMobileDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        showCloseButton={false}
        overlayClassName="bg-black/50"
        className={GUEST_PREFERENCES_DIALOG_CLASS}
      >
        <DialogHeader className="items-center gap-3 text-center">
          <DialogTitle className={GUEST_PREFERENCES_DIALOG_TITLE_CLASS}>
            {title}
          </DialogTitle>
          <DialogDescription className={GUEST_PREFERENCES_DIALOG_DESCRIPTION_CLASS}>
            {GUEST_PREFERENCES_COPY.verifyMobileBody}
          </DialogDescription>
        </DialogHeader>
        <GuestPreferencesVerifyMobileForm {...form} />
      </DialogContent>
    </Dialog>
  )
}
