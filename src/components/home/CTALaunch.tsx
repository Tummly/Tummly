import SignInLink from "@/components/auth/SignInLink"

import { ctaLaunchBgPicture } from "@/assets/marketing-images"
import { RequestTrialLink } from "@/components/navigation/RequestTrialLink"
import OptimizedImage from "@/components/media/OptimizedImage"
import { Button } from "@/components/ui/button"
import { PANORAMIC_BG_IMAGE_SIZES } from "@/lib/imagePresets"
import {
  marketingSectionBody,
  marketingSectionHeading,
  marketingSectionInset,
} from "@/lib/marketing-layout"
import { cn } from "@/lib/utils"

/** Figma Restaurant sign-up CTA frame `1809:37282`. */
const CTA_MOBILE_BG_GRADIENT =
  "linear-gradient(169.42deg, rgb(20, 20, 20) 28.698%, rgba(20, 20, 20, 0) 86.983%)"

const CTA_DESKTOP_BG_GRADIENT =
  "linear-gradient(140.54deg, rgb(20, 20, 20) 19.88%, rgba(20, 20, 20, 0) 89.61%)"

function CTALaunch() {
  return (
    <section className="relative isolate h-[576px] w-full overflow-hidden lg:h-[688px]">
      <div aria-hidden className="pointer-events-none absolute inset-0">
        <div className="absolute inset-0 overflow-hidden">
          <div className="absolute inset-0 overflow-hidden lg:hidden [&_picture]:contents">
            <OptimizedImage
              picture={ctaLaunchBgPicture}
              sizes={PANORAMIC_BG_IMAGE_SIZES}
              alt=""
              className="absolute top-0 left-[-170.38%] h-full w-[321.73%] max-w-none object-cover object-[center_42%]"
            />
          </div>
          <OptimizedImage
            picture={ctaLaunchBgPicture}
            sizes={PANORAMIC_BG_IMAGE_SIZES}
            alt=""
            className="hidden size-full object-cover object-left lg:block"
          />
        </div>
        <div
          className="absolute inset-0 lg:hidden"
          style={{ backgroundImage: CTA_MOBILE_BG_GRADIENT }}
        />
        <div
          className="absolute inset-0 hidden lg:block"
          style={{ backgroundImage: CTA_DESKTOP_BG_GRADIENT }}
        />
      </div>

      <div
        className={cn(
          "relative z-10 mx-auto flex h-full w-full flex-col items-start justify-start py-[60px] lg:py-22.5",
          marketingSectionInset
        )}
      >
        <div className="flex w-full max-w-187.5 flex-col gap-15">
          <div className="flex flex-col gap-3">
            <h2 className={cn("m-0", marketingSectionHeading, "text-white")}>
              Ready to set up your first Guest Loop?
            </h2>
            <p
              className={cn(
                "m-0 max-w-2xl text-white sm:max-w-xl lg:max-w-187.5",
                marketingSectionBody,
                "text-white"
              )}
            >
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
                <RequestTrialLink>Request guided trial</RequestTrialLink>
              </Button>

              <p className="m-0 text-sm font-medium leading-[normal] text-white lg:text-base">
                Already have an account?{" "}
                <Button
                  variant="link"
                  size="link-sm"
                  asChild
                  className="font-medium text-[#14a247] underline underline-offset-2 hover:text-[#129641]"
                >
                  <SignInLink to="/login">Sign in</SignInLink>
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
