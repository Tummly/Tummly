import { cn } from "@/lib/utils";

/** 25px horizontal inset below `lg`; desktop padding unchanged at `lg+`. */
export const marketingSectionInset = "px-[25px] lg:px-16 xl:px-45";

export const marketingSectionInsetLeft =
  "pl-[25px] lg:pl-16 xl:pl-45";

export const marketingSectionInsetRight =
  "pr-[25px] lg:pr-16 xl:pr-45";

/** Track inset — viewport stays full bleed; slides scroll off the left edge naturally. */
export const marketingCarouselTrackInset = marketingSectionInsetLeft;

/** Carousel track: left inset on the scroll container, not the viewport. */
export const marketingCarouselContentClass = cn("-ml-0", marketingCarouselTrackInset);

const MARKETING_CAROUSEL_END_INSET_PX = {
  default: 25,
  lg: 64,
  xl: 180,
} as const;

function getMarketingCarouselEndInsetPx(): number {
  if (typeof window === "undefined") {
    return MARKETING_CAROUSEL_END_INSET_PX.default;
  }

  if (window.matchMedia("(min-width: 1280px)").matches) {
    return MARKETING_CAROUSEL_END_INSET_PX.xl;
  }

  if (window.matchMedia("(min-width: 1024px)").matches) {
    return MARKETING_CAROUSEL_END_INSET_PX.lg;
  }

  return MARKETING_CAROUSEL_END_INSET_PX.default;
}

/** Embla options for marketing peek carousels. */
export function marketingCarouselOptions(slideCount: number) {
  return {
    align: (viewSize: number, snapSize: number, index: number) => {
      if (index === slideCount - 1) {
        return Math.max(0, viewSize - snapSize - getMarketingCarouselEndInsetPx());
      }

      return 0;
    },
    loop: false,
  } as const;
}

/** Slide width leaves room for the left inset plus a right peek (25px each on mobile). */
export function marketingCarouselItemClass(index: number) {
  return cn(
    "basis-[calc(100%-3.125rem)] sm:basis-109",
    index === 0 ? "!pl-0" : "pl-7.5",
  );
}

export const marketingCarouselContentClassCompact = marketingCarouselContentClass;

export function marketingCarouselItemClassCompact(index: number) {
  return cn(
    "basis-[calc(100%-3.125rem)]",
    index === 0 ? "!pl-0" : "pl-4",
  );
}

/** Shared vertical section padding for marketing homepage blocks. */
export const marketingSectionPadding = "py-12 sm:py-16 lg:py-22.5";

/** Section H2: 36px below `lg`; fluid scale at `lg+`. */
export const marketingSectionHeading =
  "text-[36px] font-bold leading-[normal] text-[#232323] lg:text-[clamp(1.75rem,4vw,2.625rem)]";

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
