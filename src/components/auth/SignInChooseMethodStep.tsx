import type { OtpFeedback } from "@/components/home/hero-trial-otp"
import { Button } from "@/components/ui/button"
import { cn } from "@/lib/utils"

const cardShadow =
  "shadow-[2px_6px_14px_rgba(0,0,0,0.04),9px_25px_26px_rgba(0,0,0,0.03),20px_55px_35px_rgba(0,0,0,0.02)]"

const SUPPORT_EMAIL = "support@tummly.com"

type SignInChooseMethodStepProps = {
  submitting: boolean
  feedback: OtpFeedback | null
  hasVerifiedPhone: boolean
  onSendViaEmail: () => void
  onSendViaSms: () => void
}

function OtpFeedbackMessage({ feedback }: { feedback: OtpFeedback }) {
  return (
    <p
      role={feedback.kind === "error" ? "alert" : "status"}
      className={cn(
        "m-0 text-sm font-medium leading-5",
        feedback.kind === "error" ? "text-destructive" : "text-[#14a247]"
      )}
    >
      {feedback.message}
    </p>
  )
}

export function SignInChooseMethodStep({
  submitting,
  feedback,
  hasVerifiedPhone,
  onSendViaEmail,
  onSendViaSms,
}: SignInChooseMethodStepProps) {
  return (
    <div
      className={`flex w-full max-w-[490px] shrink-0 flex-col gap-6 rounded-[6px] border border-[#d2d2d2] bg-white px-[clamp(1.25rem,4vw,1.875rem)] py-[clamp(1.25rem,4vw,2.375rem)] sm:gap-7 lg:gap-16 ${cardShadow}`}
    >
      <header className="flex flex-col gap-4 text-[#232323]">
        <h1 className="m-0 text-[clamp(1.625rem,4vw,2rem)] font-bold leading-normal tracking-[-0.64px]">
          Choose another way to
          <br />
          sign in
        </h1>
        <p className="m-0 text-sm leading-normal">
          Select a method linked to your Tummly account.
        </p>
      </header>

      {feedback ? <OtpFeedbackMessage feedback={feedback} /> : null}

      <div className="flex flex-col gap-4">
        <Button
          type="button"
          disabled={submitting}
          onClick={onSendViaEmail}
          className="h-auto min-h-0 w-full rounded-[4px] bg-[#14a74a] px-[17px] py-[15px] text-base font-medium leading-5 text-white hover:bg-[#129641]"
        >
          {submitting ? "Please wait..." : "Send code to email"}
        </Button>

        {hasVerifiedPhone ? (
          <Button
            type="button"
            disabled={submitting}
            onClick={onSendViaSms}
            className="h-auto min-h-0 w-full rounded-[4px] border border-transparent bg-[#e8e8e8] px-[17px] py-[15px] text-base font-medium leading-5 text-[#232323] hover:bg-[#dedede]"
          >
            {submitting ? "Please wait..." : "Send code by SMS"}
          </Button>
        ) : null}
      </div>

      <p className="m-0 flex flex-wrap items-center gap-2.5 text-sm font-medium tracking-[0.4px] text-[#232323]">
        <span>Can&apos;t access these methods?</span>
        <Button
          variant="link"
          size="link-sm"
          asChild
          className="font-medium text-primary underline underline-offset-2"
        >
          <a href={`mailto:${SUPPORT_EMAIL}`}>Contact support</a>
        </Button>
      </p>
    </div>
  )
}
