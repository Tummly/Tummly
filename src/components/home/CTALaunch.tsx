import { Link } from "react-router-dom"

import ctaBg from "@/assets/images/cta-launch-bg.png"
import { Button } from "@/components/ui/button"

function CTALaunch() {
  return (
    <section className="relative isolate h-[688px] w-full overflow-hidden">
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 bg-cover bg-no-repeat bg-[center_40%] sm:bg-left"
        style={{
          backgroundImage: `linear-gradient(140.54deg, rgb(20, 20, 20) 19.88%, rgba(20, 20, 20, 0) 89.61%), url(${ctaBg})`,
        }}
      />
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 bg-[#141414]/30 sm:bg-transparent"
      />

      <div className="relative z-10 mx-auto flex h-full w-full max-w-360 flex-col items-start justify-start px-4 py-12 sm:px-6 sm:py-16 md:px-10 lg:px-16 lg:py-22.5 xl:px-45">
        <div className="flex w-full max-w-187.5 flex-col gap-15">
          <div className="flex flex-col gap-3">
            <h2 className="m-0 text-[clamp(1.75rem,4vw,2.625rem)] font-bold leading-[normal] text-white">
              Ready to set up your first Guest Loop?
            </h2>
            <p className="m-0 max-w-2xl text-base font-medium leading-5.5 text-white sm:max-w-xl lg:max-w-187.5">
              Request guided access and we&apos;ll help you prepare the guest
              prompts, feedback form and first return offer for your restaurant.
            </p>
          </div>

          <div className="flex flex-col gap-3">
            <div className="flex flex-col items-start gap-4 sm:flex-row sm:flex-wrap sm:items-center">
              <Button
                variant="cta"
                size="responsive"
                asChild
                className="shadow-none hover:bg-white/95"
              >
                <Link to="/request-trial">Request guided trial</Link>
              </Button>

              <p className="m-0 text-base font-medium leading-[normal] text-white">
                Already have an account?{" "}
                <Button
                  variant="link"
                  size="link-sm"
                  asChild
                  className="font-medium text-[#14a247] underline underline-offset-2 hover:text-[#129641]"
                >
                  <Link to="/login">Sign in</Link>
                </Button>
              </p>
            </div>

            <p className="m-0 text-sm font-medium leading-[normal] text-white">
              No payment is taken when you request access.
            </p>
          </div>
        </div>
      </div>
    </section>
  )
}

export default CTALaunch
