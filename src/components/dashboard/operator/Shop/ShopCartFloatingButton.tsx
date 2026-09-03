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
        variant="op-secondary"
        onClick={onClick}
        className="relative gap-2 shadow-xl"
      >
        <ShoppingBag className="size-4" />
        <span>Cart</span>
        {itemCount > 0 ? (
          <span className="flex size-5 items-center justify-center rounded-full bg-op-button-primary-background text-xs font-bold text-white">
            {itemCount}
          </span>
        ) : null}
      </Button>
    </div>
  )
}
