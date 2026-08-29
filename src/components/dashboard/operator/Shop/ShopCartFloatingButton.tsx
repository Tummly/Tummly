import { ShoppingBag } from "lucide-react"
import { Button } from "@/components/ui/button"

type ShopCartFloatingButtonProps = {
  itemCount: number
  onClick: () => void
}

export function ShopCartFloatingButton({
  itemCount,
  onClick,
}: ShopCartFloatingButtonProps) {
  return (
    <div className="fixed bottom-6 right-6 z-40">
      <Button
        type="button"
        onClick={onClick}
        className="group relative flex h-11 items-center gap-2 rounded-full border border-op-border-default bg-op-surface-secondary px-5 py-2.5 text-sm font-semibold text-foreground shadow-xl transition-all hover:scale-105 hover:bg-op-action-secondary-hover"
      >
        <ShoppingBag className="size-4.5 text-foreground transition-transform group-hover:-rotate-6" />
        <span>Cart</span>
        {itemCount > 0 && (
          <span className="flex size-5 items-center justify-center rounded-full bg-op-action-primary text-xs font-bold text-white">
            {itemCount}
          </span>
        )}
      </Button>
    </div>
  )
}
