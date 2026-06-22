import {
  Card,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import OptimizedImage, {
  type PictureOutput,
} from "@/components/media/OptimizedImage"
import { CARD_IMAGE_SIZES } from "@/lib/imagePresets"
import { cn } from "@/lib/utils"

type ImageWithCardTheme = "default" | "inverse"
type ImageWithCardSize = "default" | "compact" | "trial"

type ImageWithCardProps = {
  picture: PictureOutput
  imageAlt: string
  title: string
  description: string
  sizes?: string
  theme?: ImageWithCardTheme
  size?: ImageWithCardSize
  className?: string
}

function ImageWithCard({
  picture,
  imageAlt,
  title,
  description,
  sizes = CARD_IMAGE_SIZES,
  theme = "default",
  size = "default",
  className,
}: ImageWithCardProps) {
  const isInverse = theme === "inverse"
  const isCompact = size === "compact"
  const isTrial = size === "trial"

  return (
    <Card
      className={cn(
        "border-0 bg-transparent p-0 shadow-none ring-0",
        isTrial ? "gap-7.5" : isCompact ? "gap-6.5" : "gap-7.5",
        className
      )}
    >
      <div
        className={cn(
          "relative w-full shrink-0 overflow-hidden rounded-[6px]",
          isTrial ? "aspect-436/230" : "aspect-392/262"
        )}
      >
        <OptimizedImage
          picture={picture}
          sizes={sizes}
          alt={imageAlt}
          className="absolute inset-0 size-full rounded-[6px] object-cover"
        />
      </div>

      <CardHeader className="gap-3 p-0">
        <CardTitle
          className={cn(
            isTrial
              ? "text-lg leading-[normal] font-bold"
              : "text-xl leading-normal font-bold sm:text-[22px] lg:text-2xl",
            isInverse ? "text-white" : "text-[#232323]"
          )}
        >
          {title}
        </CardTitle>
        <CardDescription
          className={cn(
            isTrial || isCompact
              ? "text-sm leading-5"
              : "text-sm leading-5.5 sm:text-[15px] lg:text-base",
            isInverse ? "text-[#999999]" : "text-[#232323]"
          )}
        >
          {description}
        </CardDescription>
      </CardHeader>
    </Card>
  )
}

export default ImageWithCard
export type { ImageWithCardProps, ImageWithCardTheme, ImageWithCardSize }
