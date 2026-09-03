import { useEffect, useState } from "react"
import { Minus, Plus } from "lucide-react"
import { Button } from "@/components/ui/button"
import {
  SHOP_PRODUCT_CARD_RECOMMENDATION_BODY_CLASS,
  SHOP_PRODUCT_CARD_RECOMMENDATION_CLASS,
  SHOP_PRODUCT_CARD_RECOMMENDATION_IMAGE_CLASS,
} from "@/lib/operatorShop/shopSurfacePresentation"
import { cn } from "@/lib/utils"

export type ShopRecommendationCardProps = {
  title: string
  description: string
  allocationText: string
  unitPriceGbp: number
  initialQuantity?: number
  imageSrc: string
  className?: string
  purchaseDisabled?: boolean
  onOrderNow?: (quantity: number) => void
  onSelectCard?: () => void
}

/** Horizontal recommendation product card — Figma `4384:47538` (562×241). */
export function ShopRecommendationCard({
  title,
  description,
  allocationText,
  unitPriceGbp,
  initialQuantity = 1,
  imageSrc,
  className,
  purchaseDisabled = false,
  onOrderNow,
  onSelectCard,
}: ShopRecommendationCardProps) {
  const [quantity, setQuantity] = useState<number>(initialQuantity)

  useEffect(() => {
    setQuantity(initialQuantity)
  }, [initialQuantity])

  const handleDecrease = (e: React.MouseEvent) => {
    e.stopPropagation()
    setQuantity((prev) => Math.max(1, prev - 1))
  }

  const handleIncrease = (e: React.MouseEvent) => {
    e.stopPropagation()
    setQuantity((prev) => prev + 1)
  }

  const handleOrderClick = (e: React.MouseEvent) => {
    e.stopPropagation()
    onOrderNow?.(quantity)
  }

  const totalPrice = (unitPriceGbp * quantity).toFixed(2)

  return (
    <div
      onClick={onSelectCard}
      className={cn(SHOP_PRODUCT_CARD_RECOMMENDATION_CLASS, className)}
    >
      <div className={SHOP_PRODUCT_CARD_RECOMMENDATION_IMAGE_CLASS}>
        <img
          src={imageSrc}
          alt={title}
          className="absolute inset-0 size-full object-contain transition-transform duration-200 group-hover:scale-[1.03]"
        />
      </div>

      <div className={SHOP_PRODUCT_CARD_RECOMMENDATION_BODY_CLASS}>
        {/* Heading — Figma cards/header-gap 8px */}
        <div className="flex w-full flex-col gap-2">
          <h4 className="text-lg font-medium leading-normal whitespace-nowrap text-op-text-primary">
            {title}
          </h4>
          <p className="line-clamp-2 text-sm font-medium leading-5 text-[var(--op-color-gray-550)]">
            {description}
          </p>
          <p className="line-clamp-1 text-sm font-medium leading-5 text-[var(--op-color-gray-550)]">
            {allocationText}
          </p>
        </div>

        {/* Price + quantity — Figma Frame 5463 */}
        <div className="flex w-full items-end justify-between">
          <div className="flex items-end gap-1.5 whitespace-nowrap">
            <span className="text-[22px] font-medium leading-normal text-op-text-primary">
              £{totalPrice}
            </span>
            <span className="pb-px text-xs font-medium leading-normal text-[var(--op-color-gray-550)]">
              excluding VAT
            </span>
          </div>

          <div className="flex items-center gap-3">
            <button
              type="button"
              onClick={handleDecrease}
              aria-label="Decrease quantity"
              className="flex size-5 shrink-0 items-center justify-center text-op-text-primary transition-opacity hover:opacity-80"
            >
              <Minus className="size-5" strokeWidth={1.75} />
            </button>
            <span className="min-w-[21px] text-center text-base font-medium leading-none text-op-text-primary">
              {quantity}
            </span>
            <button
              type="button"
              onClick={handleIncrease}
              aria-label="Increase quantity"
              className="flex size-5 shrink-0 items-center justify-center text-op-text-primary transition-opacity hover:opacity-80"
            >
              <Plus className="size-5" strokeWidth={1.75} />
            </button>
          </div>
        </div>

        {/* Order now — Figma button h=36, py=8, fill #333 */}
        <Button
          type="button"
          variant="op-secondary"
          disabled={purchaseDisabled}
          className="h-9 w-full rounded-[2px] py-2"
          onClick={handleOrderClick}
        >
          Order now
        </Button>
      </div>
    </div>
  )
}
