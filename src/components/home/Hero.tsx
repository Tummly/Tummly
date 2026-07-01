import { heroBgMobilePicture, heroBgPicture } from "@/assets/critical-images";
import HeroTrialForm from "@/components/home/HeroTrialForm";
import OptimizedImage from "@/components/media/OptimizedImage";
import {
  marketingHeroBody,
  marketingHeroHeading,
  marketingSectionInset,
} from "@/lib/marketing-layout";
import { cn } from "@/lib/utils";

/** Desktop hero photo height — fixed so address-field reveal does not rescale the image. */
const DESKTOP_HERO_IMAGE_HEIGHT_PX = 1220;

const heroCopy = (
  <div className="flex w-full min-w-0 max-w-[643px] shrink-0 flex-col items-start lg:flex-1 lg:pt-[110px] xl:max-w-[643px]">
    <div className="flex w-full max-w-xl flex-col items-start gap-[22px] text-[#141414] lg:text-white">
      <h1 className={cn("m-0 w-full", marketingHeroHeading)}>
        Turn every order into a direct guest relationship.
      </h1>
      <p className={cn("m-0 w-full", marketingHeroBody)}>
        Use QR prompts to collect private feedback, grow your guest list, send
        return offers and see what&apos;s working each week.
      </p>
    </div>
  </div>
);

function Hero() {
  return (
    <section
      id="request-trial"
      className="relative isolate w-full scroll-mt-[77px] overflow-hidden bg-white lg:scroll-mt-[78px] lg:bg-[#141414]"
    >
      <div className="flex w-full flex-col lg:hidden">
        <div
          className={cn(
            "mx-auto flex w-full flex-col pt-[50px] pb-[50px]",
            marketingSectionInset
          )}
        >
          {heroCopy}
        </div>

        <div className="relative h-[241px] w-full" aria-hidden>
          <OptimizedImage
            picture={heroBgMobilePicture}
            sizes="100vw"
            priority
            alt=""
            className="size-full object-cover"
          />
        </div>

        <div className="w-full bg-white">
          <HeroTrialForm />
        </div>
      </div>

      <div className="relative hidden w-full lg:block">
        <div
          aria-hidden
          className="pointer-events-none absolute inset-x-0 top-0"
          style={{ height: DESKTOP_HERO_IMAGE_HEIGHT_PX }}
        >
          <OptimizedImage
            picture={heroBgPicture}
            sizes="100vw"
            priority
            alt=""
            className="size-full object-cover object-[center_30%]"
          />
          <div
            className="absolute inset-0"
            style={{
              backgroundImage:
                "linear-gradient(147.04deg, #141414 7.53%, rgba(20, 20, 20, 0) 73.71%)",
            }}
          />
          <div
            className="absolute inset-x-0 bottom-0 h-24"
            style={{
              backgroundImage:
                "linear-gradient(180deg, rgba(20, 20, 20, 0) 0%, #141414 100%)",
            }}
          />
        </div>

        <div className="relative z-10 mx-auto flex w-full min-h-[1010px] flex-row items-start justify-between gap-10 px-16 pb-20 pt-0 xl:gap-12 xl:px-45">
          {heroCopy}
          <div className="w-[615px] shrink-0">
            <HeroTrialForm />
          </div>
        </div>
      </div>
    </section>
  );
}

export default Hero;
