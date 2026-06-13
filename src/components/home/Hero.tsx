import heroBg from "@/assets/images/hero-bg.png";
import HeroTrialForm from "@/components/home/HeroTrialForm";

function Hero() {
  return (
    <section
      id="request-trial"
      className="relative isolate w-full overflow-hidden"
    >
      <div aria-hidden className="pointer-events-none absolute inset-0">
        <img
          src={heroBg}
          alt=""
          className="size-full object-cover object-[center_35%] sm:object-[center_32%] lg:object-[center_30%]"
        />
        <div
          className="absolute inset-0"
          style={{
            backgroundImage:
              "linear-gradient(147.04deg, #141414 7.53%, rgba(20, 20, 20, 0) 73.71%)",
          }}
        />
        <div className="absolute inset-0 bg-[#141414]/25 lg:bg-transparent" />
      </div>

      <div className="relative z-10 mx-auto flex w-full flex-col gap-8 px-4 pb-12 pt-20 sm:gap-10 sm:px-6 sm:pb-16 sm:pt-24 md:px-10 lg:min-h-[1010px] lg:flex-row lg:items-start lg:justify-between lg:gap-10 lg:px-16 lg:pb-20 lg:pt-0 xl:gap-12 xl:px-45">
        <div className="flex w-full min-w-0 max-w-[643px] shrink-0 flex-col items-start lg:flex-1 lg:pt-[110px] xl:max-w-[643px]">
          <div className="flex w-full flex-col items-start gap-[22px] text-white">
            <h1 className="m-0 w-full max-w-[585px] text-[clamp(1.75rem,4.2vw,2.875rem)] font-bold leading-[normal] lg:text-[46px]">
              Turn every order into a direct guest relationship.
            </h1>
            <p className="m-0 w-full max-w-[572px] text-base font-medium leading-[1.333] lg:text-[18px] lg:leading-[24px]">
              Use QR prompts to collect private feedback, grow your guest list,
              send return offers and see what&apos;s working each week.
            </p>
          </div>
        </div>

        <div className="w-full min-w-0 lg:w-[615px] lg:shrink-0">
          <HeroTrialForm />
        </div>
      </div>
    </section>
  );
}

export default Hero;
