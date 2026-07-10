import { cn } from "@/lib/utils";

/** 25px horizontal inset below `lg`; desktop padding unchanged at `lg+`. */
export const marketingSectionInset = "px-[25px] lg:px-16 xl:px-45";

export const marketingSectionInsetLeft =
  "pl-[25px] lg:pl-16 xl:pl-45";

export const marketingSectionInsetRight =
  "pr-[25px] lg:pr-16 xl:pr-45";

/** Last-slide end inset — Embla reads margin-right (not container padding) for scroll bounds. */
export const marketingCarouselEndMargin =
  "mr-[25px] lg:mr-16 xl:mr-45";

/** Track inset — viewport stays full bleed; slides scroll off the left edge naturally. */
export const marketingCarouselTrackInset = marketingSectionInsetLeft;

/** Carousel track: left inset on the scroll container, not the viewport. */
export const marketingCarouselContentClass = cn("-ml-0", marketingCarouselTrackInset);

/** Embla options for marketing peek carousels. */
export function marketingCarouselOptions() {
  return {
    align: "start",
    containScroll: "trimSnaps",
    loop: false,
  } as const;
}

/** Slide width: one full slide below `lg`; desktop peek via fixed width at `lg+`.
 * Gap uses margin (not padding) so border-box basis keeps equal content/image sizes. */
export function marketingCarouselItemClass(index: number, slideCount: number) {
  return cn(
    "!pl-0 max-lg:basis-[calc(100%-1.5625rem)] lg:basis-109",
    index > 0 && "ml-7.5",
    index === slideCount - 1 && marketingCarouselEndMargin,
  );
}

export const marketingCarouselContentClassCompact = marketingCarouselContentClass;

export function marketingCarouselItemClassCompact(index: number, slideCount: number) {
  return cn(
    "!pl-0 basis-[calc(100%-3.125rem)]",
    index > 0 && "ml-4",
    index === slideCount - 1 && marketingCarouselEndMargin,
  );
}

/** Shared vertical section padding for marketing homepage blocks. */
export const marketingSectionPadding = "py-12 sm:py-16 lg:py-22.5";

/** Section H2: 34px below `lg`; fluid scale at `lg+`. */
export const marketingSectionHeading =
  "text-[34px] font-bold leading-[normal] text-[#232323] lg:text-[clamp(1.75rem,4vw,2.625rem)]";

/** Section body: 16px / 22px below `lg`; desktop scale at `lg+`. */
export const marketingSectionBody =
  "text-base font-medium leading-[22px] text-[#232323] lg:text-[17px] lg:leading-6.5 lg:text-lg";

/** Hero H1 on marketing homepage below `lg`. */
export const marketingHeroHeading =
  "text-[36px] font-bold leading-[normal] lg:text-[46px]";

/** Hero body copy below `lg`. */
export const marketingHeroBody =
  "text-base font-medium leading-[22px] lg:text-[18px] lg:leading-[24px]";

export function marketingSectionShell(...extra: Array<string | undefined>) {
  return cn(
    "mx-auto flex w-full flex-col gap-12 sm:gap-14 lg:gap-15",
    marketingSectionInset,
    marketingSectionPadding,
    ...extra,
  );
}
