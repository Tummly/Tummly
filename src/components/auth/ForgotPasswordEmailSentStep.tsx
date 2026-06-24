import { Link } from "react-router-dom"

import { Button } from "@/components/ui/button"

const cardShadow =
  "shadow-[2px_6px_14px_rgba(0,0,0,0.04),9px_25px_26px_rgba(0,0,0,0.03),20px_55px_35px_rgba(0,0,0,0.02)]"

export function ForgotPasswordEmailSentStep() {
  return (
    <div
      className={`flex w-full max-w-[490px] shrink-0 flex-col gap-6 rounded-[6px] border border-[#d2d2d2] bg-white px-[clamp(1.25rem,4vw,1.875rem)] py-[clamp(1.25rem,4vw,2.375rem)] sm:gap-7 lg:gap-16 ${cardShadow}`}
    >
      <header className="flex flex-col gap-4 text-[#232323]">
        <h1 className="m-0 text-[clamp(1.625rem,4vw,2rem)] font-bold leading-normal tracking-[-0.64px]">
          Check your email
        </h1>
        <p className="m-0 text-sm leading-normal">
          If an account exists for that email, we&apos;ve sent password reset
          instructions.
        </p>
      </header>

      <Button
        type="button"
        variant="link"
        size="link-sm"
        asChild
        className="self-start font-medium text-primary underline underline-offset-2"
      >
        <Link to="/login">Back to sign in</Link>
      </Button>
    </div>
  )
}
