import { Link } from "react-router-dom"

import heroFormAccent from "@/assets/svg/hero-form-accent.svg"
import SignInLink from "@/components/auth/SignInLink"
import { Button } from "@/components/ui/button"

/**
 * Home hero card: Signup CTA (replaces the retired Request Trial form).
 * Entry goes to `/signup`; OTP verify is `/signup/verify` (not `/verify-email`).
 */
function HeroTrialForm() {
  return (
    <div className="relative w-full shrink-0 overflow-hidden px-[22px] py-[38px] max-lg:shadow-none lg:mx-auto lg:max-w-[615px] lg:px-[38px] lg:pb-[38px] lg:pt-[68px]">
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 z-0 bg-white"
      />

      <div
        aria-hidden
        className="pointer-events-none absolute left-0 top-[-5px] z-[1] h-[210px] w-[367px] overflow-hidden "
      >
        <div className="absolute left-[3.67px] top-[-5px]">
          <div className="absolute left-0 top-0 flex h-[209.635px] w-[363.027px] items-center justify-center">
            <div className="-scale-y-100 flex-none rotate-180">
              <div className="relative h-[209.635px] w-[363.027px]">
                <img
                  src={heroFormAccent}
                  alt=""
                  className="absolute inset-0 block size-full max-w-none"
                />
              </div>
            </div>
          </div>
        </div>
        <div className="absolute left-0 top-[-5px] flex h-[210px] w-[367px] items-center justify-center">
          <div className="-scale-y-100 flex-none rotate-180">
            <div
              className="h-[210px] w-[367px]"
              style={{
                backgroundImage:
                  "linear-gradient(10.784231689007541deg, rgb(255, 255, 255) 27.237%, rgba(255, 255, 255, 0.2) 71.441%), linear-gradient(87.63101003628996deg, rgb(255, 255, 255) 1.4701%, rgba(255, 255, 255, 0.2) 48.114%)",
              }}
            />
          </div>
        </div>
      </div>

      <div className="relative z-[2] flex w-full flex-col">
        <header className="mb-7 flex flex-col gap-3 text-[#232323] sm:mb-8 lg:mb-[34px] lg:gap-3">
          <h2 className="m-0 text-[26px] font-bold leading-[normal] tracking-[-0.56px] lg:text-[clamp(1.375rem,3vw,1.75rem)]">
            Create your Tummly account
          </h2>
          <p className="m-0 text-base font-medium leading-[21px] tracking-[-0.32px]">
            Sign up with your work email, verify, set up your restaurant, then
            choose Essential or Pro — no payment on this step.
          </p>
        </header>

        <div className="mt-auto flex flex-col items-center gap-5 pt-2 lg:gap-[22px]">
          <Button
            asChild
            className="h-auto min-h-0 w-full rounded-[54px] border border-[rgba(20,162,71,0)] bg-[#14a247] px-[17px] py-[13px] text-base leading-5 text-white hover:bg-[#129641]"
          >
            <Link to="/signup">Sign up</Link>
          </Button>

          <p className="m-0 flex flex-wrap items-center justify-center gap-2.5 text-sm font-medium tracking-[0.4px] text-[#232323]">
            <span>Already have an account?</span>
            <Button
              variant="link"
              size="link-sm"
              asChild
              className="text-[#14a74a] underline underline-offset-2"
            >
              <SignInLink to="/login">Sign in</SignInLink>
            </Button>
          </p>

          <p className="m-0 max-w-[313px] text-center text-sm font-medium leading-5 text-[#232323]">
            For restaurants and hospitality operators only.
          </p>
        </div>
      </div>
    </div>
  )
}

export default HeroTrialForm
