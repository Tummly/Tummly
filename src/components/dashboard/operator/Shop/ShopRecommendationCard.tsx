import { useEffect, useState } from "react"
import { Minus, Plus } from "lucide-react"
import { Button } from "@/components/ui/button"
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
      className={cn(
        "group flex w-full min-w-[280px] max-w-[460px] shrink-0 cursor-pointer flex-col overflow-hidden rounded-[6px] border border-op-border-default bg-op-card-background transition-all hover:border-op-action-tertiary sm:flex-row",
        "shadow-[-5px_0px_10px_0px_rgba(0,0,0,0.03)] shadow-[-19px_0px_19px_0px_rgba(0,0,0,0.03)]",
        className
      )}
    >
      <div className="flex flex-1 items-center justify-center overflow-hidden bg-op-background-primary/70 p-0">
        <img
          src={imageSrc}
          alt={title}
          className="size-full max-h-[190px] scale-[1.22] object-contain transition-transform duration-200 group-hover:scale-[1.28]"
        />
      </div>

      <div className="flex w-full flex-col justify-between gap-3.5 p-4 sm:w-64">
        <div className="flex flex-col gap-1.5">
          <h4 className="text-sm font-semibold text-op-text-primary sm:text-base">
            {title}
          </h4>
          <p className="text-xs font-normal leading-snug text-op-text-secondary">
            {description}
          </p>
          <p className="text-xs font-normal leading-snug text-op-text-secondary">
            {allocationText}
          </p>
        </div>

        <div className="flex items-end justify-between">
          <div className="flex items-baseline gap-1">
            <span className="text-base font-semibold text-op-text-primary sm:text-lg">
              £{totalPrice}
            </span>
            <span className="text-[11px] font-normal text-op-text-muted">
              excluding VAT
            </span>
          </div>

          <div className="flex items-center gap-2">
            <button
              type="button"
              onClick={handleDecrease}
              aria-label="Decrease quantity"
              className="flex size-6 items-center justify-center rounded-[3px] border border-op-border-default bg-op-background-primary text-op-text-primary transition-colors hover:bg-op-surface-secondary"
            >
              <Minus className="size-3" />
            </button>
            <span className="min-w-5 text-center text-xs font-medium text-op-text-primary">
              {quantity}
            </span>
            <button
              type="button"
              onClick={handleIncrease}
              aria-label="Increase quantity"
              className="flex size-6 items-center justify-center rounded-[3px] border border-op-border-default bg-op-background-primary text-op-text-primary transition-colors hover:bg-op-surface-secondary"
            >
              <Plus className="size-3" />
            </button>
          </div>
        </div>

        <Button
          type="button"
          variant="op-secondary"
          disabled={purchaseDisabled}
          className="h-8.5 w-full rounded-[4px] text-xs font-medium"
          onClick={handleOrderClick}
        >
          Order now
        </Button>
      </div>
    </div>
  )
}
