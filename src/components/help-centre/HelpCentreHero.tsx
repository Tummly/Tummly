import { Search, X } from "lucide-react"

import { helpCenterBgPicture } from "@/assets/critical-images"
import { FULL_BLEED_IMAGE_SIZES } from "@/lib/imagePresets";
import OptimizedImage from "@/components/media/OptimizedImage"
import { marketingSectionInset } from "@/lib/marketing-layout"
import { cn } from "@/lib/utils"

import { helpCentreHubSectionInner } from "./helpCentreLayout"

type HelpCentreHeroProps = {
  search: string
  onSearchChange: (value: string) => void
}

export function HelpCentreHero({ search, onSearchChange }: HelpCentreHeroProps) {
  return (
    <section className="relative isolate flex min-h-[420px] items-center justify-center overflow-hidden rounded-b-[40px] lg:min-h-[640px]">
      <div aria-hidden className="pointer-events-none absolute inset-0">
        <OptimizedImage
          picture={helpCenterBgPicture}
          sizes={FULL_BLEED_IMAGE_SIZES}
          priority
          alt=""
          className="size-full object-cover object-[center_35%]"
        />
        <div className="absolute inset-0 bg-gradient-to-b from-[#141414] to-[rgba(20,20,20,0)]" />
      </div>

      <div
        className={cn(
          "relative z-10 w-full pb-16 pt-[calc(77px+4rem)] lg:pb-20 lg:pt-[calc(78px+5rem)]",
          marketingSectionInset
        )}
      >
        <div
          className={cn(
            helpCentreHubSectionInner,
            "flex flex-col items-center gap-[42px] text-center text-white"
          )}
        >
          <div className="flex max-w-[730px] flex-col gap-[22px]">
            <h1 className="m-0 text-[36px] font-bold leading-normal lg:text-[46px]">
              How can we help?
            </h1>
            <p className="m-0 text-base font-medium leading-6 text-white lg:text-lg lg:leading-6">
              Find quick answers on setting up Tummly, creating QR codes,
              collecting private feedback, managing guests, sending offers and
              understanding your weekly insights.
            </p>
          </div>

          <label className="relative flex w-full max-w-[622px] items-center gap-[14px] rounded-[40px] bg-[rgba(239,239,239,0.2)] px-6 py-4 backdrop-blur-[22px]">
            <Search className="size-5 shrink-0 text-[#efefef]" aria-hidden />
            <input
              type="search"
              value={search}
              onChange={(event) => onSearchChange(event.target.value)}
              placeholder="Search setup, QR codes, offers, feedback, billing…"
              aria-label="Search help articles"
              className="help-centre-search w-full border-0 bg-transparent text-base font-medium text-white placeholder:text-sm placeholder:font-medium placeholder:text-[#efefef] focus:outline-none lg:placeholder:text-base"
            />
            {search.length > 0 && (
              <button
                type="button"
                onClick={() => onSearchChange("")}
                aria-label="Clear search"
                className="flex shrink-0 items-center justify-center rounded-full p-0.5 text-white hover:text-white/80"
              >
                <X className="size-4" aria-hidden />
              </button>
            )}
          </label>
        </div>
      </div>
    </section>
  )
}
