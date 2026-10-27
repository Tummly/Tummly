import { useEffect, useState } from "react"
import { toast } from "sonner"

import {
  registerShopInterest,
  unregisterShopInterest,
  type ShopInterestProduct,
} from "@/api/shopInterestApi"
import interestRegisteredCheck from "@/assets/images/shop/interest/interest-registered-check.svg"
import { Button } from "@/components/ui/button"
import { resolveShopCatalogImageSrc } from "@/lib/operatorShop/resolveShopCatalogImageSrc"
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
  onUnregisterInterest?: (input: {
    productKey: string
    locationId: number
  }) => Promise<void>
}

export function ShopInterestSection({
  products,
  locationId,
  searchQuery = "",
  onRegisterInterest = registerShopInterest,
  onUnregisterInterest = unregisterShopInterest,
}: ShopInterestSectionProps) {
  const [registeredKeys, setRegisteredKeys] = useState<Set<string>>(() => {
    return new Set(
      products
        .filter((product) => product.interestRegistered)
        .map((product) => product.productKey)
    )
  })
  const [busyKey, setBusyKey] = useState<string | null>(null)

  useEffect(() => {
    setRegisteredKeys(
      new Set(
        products
          .filter((product) => product.interestRegistered)
          .map((product) => product.productKey)
      )
    )
  }, [products])

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
      await onRegisterInterest({
        productKey: product.productKey,
        locationId,
      })
      setRegisteredKeys((prev) => new Set(prev).add(product.productKey))
    } catch {
      toast.error("Could not register interest.")
    } finally {
      setBusyKey(null)
    }
  }

  const handleUnregister = async (product: ShopInterestProduct) => {
    if (busyKey) return
    setBusyKey(product.productKey)
    try {
      await onUnregisterInterest({
        productKey: product.productKey,
        locationId,
      })
      setRegisteredKeys((prev) => {
        const next = new Set(prev)
        next.delete(product.productKey)
        return next
      })
    } catch {
      toast.error("Could not remove interest.")
    } finally {
      setBusyKey(null)
    }
  }

  return (
    <div className="flex flex-col gap-6" data-testid="shop-interest-section">
      <div className="flex flex-col gap-1">
        <h3 className="font-jakarta text-xl font-bold text-foreground sm:text-2xl">
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
          const registered = registeredKeys.has(product.productKey)
          return (
            <div
              key={product.productKey}
              className={cn(SHOP_PRODUCT_CARD_PRIMARY_CLASS, "cursor-default")}
              data-testid={`shop-interest-card-${product.productKey}`}
            >
              <div className={SHOP_PRODUCT_CARD_PRIMARY_IMAGE_CLASS}>
                {product.imageUrl ? (
                  <img
                    src={resolveShopCatalogImageSrc(product.imageUrl)}
                    alt={product.title}
                    className="size-full object-contain"
                  />
                ) : (
                  <div className="flex size-full items-center justify-center bg-[var(--op-color-gray-985)] text-sm text-op-text-muted">
                    Coming later
                  </div>
                )}
              </div>

              <div className="flex flex-1 flex-col justify-between gap-4 p-5">
                <div className="flex flex-col gap-2">
                  <h4 className="text-lg font-medium leading-normal text-op-text-primary">
                    {product.title}
                  </h4>
                  <p className="line-clamp-3 text-sm font-medium leading-body text-op-text-body">
                    {product.description}
                  </p>
                </div>

                {registered ? (
                  <div
                    className="flex flex-col items-center gap-3"
                    data-testid={`shop-interest-registered-${product.productKey}`}
                  >
                    <div className="flex items-center justify-center gap-2.5">
                      <img
                        src={interestRegisteredCheck}
                        alt=""
                        className="size-[18px]"
                      />
                      <span className="text-sm font-medium text-[#14a946]">
                        Interest registered
                      </span>
                    </div>
                    <div className="h-px w-full bg-op-border-default" />
                    <button
                      type="button"
                      className="text-xs font-medium text-op-text-body hover:underline"
                      disabled={busyKey === product.productKey}
                      onClick={() => void handleUnregister(product)}
                    >
                      Remove interest
                    </button>
                  </div>
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
