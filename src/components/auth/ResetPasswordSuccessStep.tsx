import { Link } from "react-router-dom"

import { Button } from "@/components/ui/button"

const cardShadow =
  "shadow-[2px_6px_14px_rgba(0,0,0,0.04),9px_25px_26px_rgba(0,0,0,0.03),20px_55px_35px_rgba(0,0,0,0.02)]"

export function ResetPasswordSuccessStep() {
  return (
    <div
      className={`flex w-full max-w-[490px] shrink-0 flex-col gap-6 rounded-[6px] border border-[#d2d2d2] bg-white px-[clamp(1.25rem,4vw,1.875rem)] py-[clamp(1.25rem,4vw,2.375rem)] sm:gap-7 lg:gap-[34px] ${cardShadow}`}
    >
      <header className="flex flex-col gap-4 text-[#232323]">
        <h1 className="m-0 text-[clamp(1.625rem,4vw,2rem)] font-bold leading-normal tracking-[-0.64px]">
          Password updated
        </h1>
        <p className="m-0 text-sm leading-normal" role="status">
          Your password has been changed. You can now sign in to your Tummly
          workspace.
        </p>
      </header>

      <Button
        type="button"
        asChild
        className="h-auto min-h-0 w-full rounded-[4px] bg-[#14a74a] px-[17px] py-[15px] text-base font-medium leading-5 text-white hover:bg-[#129641]"
      >
        <Link to="/login">Sign in</Link>
      </Button>
    </div>
  )
}
