import { Button } from "@/components/ui/button"
import {
  SHOP_PRODUCT_CARD_PRIMARY_CLASS,
  SHOP_PRODUCT_CARD_PRIMARY_IMAGE_CLASS,
  SHOP_PRODUCT_CARD_SECONDARY_CLASS,
  SHOP_PRODUCT_CARD_SECONDARY_IMAGE_CLASS,
} from "@/lib/operatorShop/shopSurfacePresentation"
import { cn } from "@/lib/utils"

export type ShopCatalogItemCardVariant = "primary" | "secondary"

export type ShopCatalogItemCardProps = {
  title: string
  description: string
  price: number
  imageSrc: string
  /** `primary` = catalog grid; `secondary` = “You may also need”. */
  variant?: ShopCatalogItemCardVariant
  isPlanIncluded?: boolean
  popularBadge?: string
  className?: string
  onViewMaterial?: () => void
}

export function ShopCatalogItemCard({
  title,
  description,
  price,
  imageSrc,
  variant = "primary",
  isPlanIncluded,
  popularBadge,
  className,
  onViewMaterial,
}: ShopCatalogItemCardProps) {
  const isSecondary = variant === "secondary"

  return (
    <div
      onClick={onViewMaterial}
      className={cn(
        isSecondary
          ? SHOP_PRODUCT_CARD_SECONDARY_CLASS
          : SHOP_PRODUCT_CARD_PRIMARY_CLASS,
        className
      )}
    >
      <div
        className={
          isSecondary
            ? SHOP_PRODUCT_CARD_SECONDARY_IMAGE_CLASS
            : SHOP_PRODUCT_CARD_PRIMARY_IMAGE_CLASS
        }
      >
        {(isPlanIncluded || popularBadge) && (
          <div className="absolute left-3 top-3 z-10 flex w-fit flex-col items-start gap-1">
            {isPlanIncluded && (
              <span className="w-fit whitespace-nowrap rounded-[4px] bg-op-action-primary px-2 py-0.5 text-[10px] font-medium text-white">
                Included with your plan
              </span>
            )}
            {popularBadge && (
              <span className="w-fit whitespace-nowrap rounded-[4px] bg-op-surface-secondary px-2 py-0.5 text-[10px] font-medium text-op-text-primary">
                {popularBadge}
              </span>
            )}
          </div>
        )}
        <img
          src={imageSrc}
          alt={title}
          className="size-full object-contain transition-transform duration-200 group-hover:scale-[1.04]"
        />
      </div>

      <div className="flex flex-1 flex-col justify-between gap-4 p-5">
        <div className="flex flex-col gap-2">
          <h4 className="text-lg font-medium leading-normal text-op-text-primary">
            {title}
          </h4>
          <p className="line-clamp-3 text-sm font-medium leading-5 text-[var(--op-color-gray-550)]">
            {description}
          </p>
        </div>

        <div className="flex flex-col gap-3">
          <p className="m-0 text-[0px] leading-none">
            <span className="text-sm font-medium text-[var(--op-color-gray-550)]">
              From{" "}
            </span>
            <span className="text-[22px] font-medium text-op-text-primary">
              £{price.toFixed(0)}
            </span>
          </p>

          <Button
            type="button"
            variant="op-secondary"
            className="w-full"
            onClick={(event) => {
              event.stopPropagation()
              onViewMaterial?.()
            }}
          >
            View material
          </Button>
        </div>
      </div>
    </div>
  )
}
