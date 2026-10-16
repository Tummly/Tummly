import { useState } from "react"
import { toast } from "sonner"

import {
  registerShopInterest,
  type ShopInterestProduct,
} from "@/api/shopInterestApi"
import { Button } from "@/components/ui/button"
import { SHOP_INTEREST_THANKS_MESSAGE } from "@/lib/operatorShop/shopInterestPresentation"
import {
  SHOP_PRODUCT_CARD_PRIMARY_CLASS,
  SHOP_PRODUCT_CARD_PRIMARY_IMAGE_CLASS,
} from "@/lib/operatorShop/shopSurfacePresentation"
import { cn } from "@/lib/utils"

export type ShopInterestSectionProps = {
  products: ShopInterestProduct[]
  locationId: number
  searchQuery?: string
  onRegisterInterest?: (input: {
    productKey: string
    locationId: number
  }) => Promise<string>
}

export function ShopInterestSection({
  products,
  locationId,
  searchQuery = "",
  onRegisterInterest = registerShopInterest,
}: ShopInterestSectionProps) {
  const [confirmedKeys, setConfirmedKeys] = useState<Set<string>>(
    () => new Set()
  )
  const [busyKey, setBusyKey] = useState<string | null>(null)

  const filtered = products.filter((product) => {
    const q = searchQuery.trim().toLowerCase()
    if (!q) return true
    return (
      product.title.toLowerCase().includes(q) ||
      product.description.toLowerCase().includes(q)
    )
  })

  if (filtered.length === 0) {
    return null
  }

  const handleRegister = async (product: ShopInterestProduct) => {
    if (busyKey) return
    setBusyKey(product.productKey)
    try {
      const message = await onRegisterInterest({
        productKey: product.productKey,
        locationId,
      })
      setConfirmedKeys((prev) => new Set(prev).add(product.productKey))
      toast.success(message)
    } catch {
      toast.error("Could not register interest.")
    } finally {
      setBusyKey(null)
    }
  }

  return (
    <div className="flex flex-col gap-6" data-testid="shop-interest-section">
      <div className="flex flex-col gap-1">
        <h3 className="font-jakarta text-xl font-bold tracking-tight text-foreground sm:text-2xl">
          Register interest
        </h3>
        <p className="text-xs font-normal text-muted-foreground sm:text-sm">
          Wider media concepts are not for purchase yet. Register interest so we
          can prioritise what to build next. This does not add to cart or start
          checkout.
        </p>
      </div>

      <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4">
        {filtered.map((product) => {
          const confirmed = confirmedKeys.has(product.productKey)
          return (
            <div
              key={product.productKey}
              className={cn(SHOP_PRODUCT_CARD_PRIMARY_CLASS)}
              data-testid={`shop-interest-card-${product.productKey}`}
            >
              <div className={SHOP_PRODUCT_CARD_PRIMARY_IMAGE_CLASS}>
                {product.imageUrl ? (
                  <img
                    src={product.imageUrl}
                    alt={product.title}
                    className="size-full object-contain"
                  />
                ) : (
                  <div className="flex size-full items-center justify-center bg-op-surface-secondary text-sm text-op-text-muted">
                    Coming later
                  </div>
                )}
              </div>

              <div className="flex flex-1 flex-col justify-between gap-4 p-5">
                <div className="flex flex-col gap-2">
                  <h4 className="text-lg font-medium leading-normal text-op-text-primary">
                    {product.title}
                  </h4>
                  <p className="line-clamp-3 text-sm font-medium leading-5 text-[var(--op-color-gray-550)]">
                    {product.description}
                  </p>
                </div>

                {confirmed ? (
                  <p
                    className="text-sm font-medium text-op-text-success"
                    data-testid={`shop-interest-thanks-${product.productKey}`}
                  >
                    {SHOP_INTEREST_THANKS_MESSAGE}
                  </p>
                ) : (
                  <Button
                    type="button"
                    variant="op-secondary"
                    className="w-full"
                    disabled={busyKey === product.productKey}
                    onClick={() => void handleRegister(product)}
                  >
                    Register interest
                  </Button>
                )}
              </div>
            </div>
          )
        })}
      </div>
    </div>
  )
}
