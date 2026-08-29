import { Button } from "@/components/ui/button"
import { cn } from "@/lib/utils"

export type ShopCatalogItemCardProps = {
  title: string
  description: string
  price: number
  imageSrc: string
  className?: string
  onViewMaterial?: () => void
}

export function ShopCatalogItemCard({
  title,
  description,
  price,
  imageSrc,
  className,
  onViewMaterial,
}: ShopCatalogItemCardProps) {
  return (
    <div
      onClick={onViewMaterial}
      className={cn(
        "group flex w-full cursor-pointer flex-col justify-between overflow-hidden rounded-[6px] border border-op-border-default bg-op-card-background transition-all hover:border-op-action-tertiary",
        "shadow-[-5px_0px_10px_0px_rgba(0,0,0,0.03)] shadow-[-19px_0px_19px_0px_rgba(0,0,0,0.03)]",
        className
      )}
    >
      <div className="flex h-44 w-full items-center justify-center overflow-hidden bg-op-background-primary/80 sm:h-48">
        <img
          src={imageSrc}
          alt={title}
          className="size-full scale-[1.08] object-contain transition-transform duration-200 group-hover:scale-[1.14]"
        />
      </div>

      <div className="flex flex-1 flex-col justify-between gap-4 p-4 sm:p-5">
        <div className="flex flex-col gap-1.5">
          <h4 className="text-sm font-semibold text-op-text-primary sm:text-base">
            {title}
          </h4>
          <p className="line-clamp-2 min-h-[32px] text-xs font-normal leading-normal text-op-text-secondary">
            {description}
          </p>
        </div>

        <div className="flex flex-col gap-3">
          <div className="flex items-baseline gap-1">
            <span className="text-xs font-normal text-op-text-muted">
              From{" "}
            </span>
            <span className="text-base font-semibold text-op-text-primary sm:text-lg">
              £{price.toFixed(0)}
            </span>
          </div>

          <Button
            type="button"
            variant="op-secondary"
            className="h-8.5 w-full rounded-[4px] text-xs font-medium"
            onClick={onViewMaterial}
          >
            View material
          </Button>
        </div>
      </div>
    </div>
  )
}
