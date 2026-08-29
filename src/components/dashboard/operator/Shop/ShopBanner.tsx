import shopBannerImg from "@/assets/images/shop/shop-banner.png"
import { Button } from "@/components/ui/button"

type ShopBannerProps = {
  onReviewStarterKit: () => void
  onSeeWhatsIncluded: () => void
}

export function ShopBanner({
  onReviewStarterKit,
  onSeeWhatsIncluded,
}: ShopBannerProps) {
  return (
    <div
      className="relative overflow-hidden rounded-md border border-op-border-default bg-[#ece6de] bg-cover bg-right shadow-sm sm:bg-center"
      style={{
        backgroundImage: `url(${shopBannerImg})`,
        minHeight: "280px",
      }}
    >
      {/* Semi-transparent soft gradient overlay for text readability on small screens */}
      <div className="absolute inset-0 bg-gradient-to-r from-[#ece6de] via-[#ece6de]/95 to-transparent sm:via-[#ece6de]/75 md:to-transparent" />

      <div className="relative z-10 flex flex-col justify-center p-6 sm:max-w-md md:max-w-lg md:p-10">
        <span className="text-xs font-semibold uppercase tracking-wider text-neutral-600">
          Included with your plan
        </span>
        <h2 className="mt-2 text-2xl font-bold tracking-tight text-neutral-900 sm:text-3xl">
          Your QR starter kit is ready
        </h2>
        <p className="mt-2 text-sm leading-relaxed text-neutral-700">
          Get the essential materials you need to start collecting private feedback at this location. We&apos;ll recommend quantities based on how the location operates.
        </p>

        <div className="mt-6 flex flex-wrap items-center gap-3">
          <Button
            type="button"
            variant="op-primary"
            className="h-10 rounded-md px-4 text-sm font-medium"
            onClick={onReviewStarterKit}
          >
            Review starter kit
          </Button>
          <Button
            type="button"
            variant="op-secondary"
            className="h-10 rounded-md px-4 text-sm font-medium bg-[#27272a] text-white hover:bg-[#3f3f46]"
            onClick={onSeeWhatsIncluded}
          >
            See what&apos;s included
          </Button>
        </div>
      </div>
    </div>
  )
}
