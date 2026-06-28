import { useSyncExternalStore } from "react"

import type { CarouselApi } from "@/components/ui/carousel"

export function useCarouselSelectedIndex(api: CarouselApi | undefined) {
  return useSyncExternalStore(
    (onStoreChange) => {
      if (!api) return () => {}

      const handleUpdate = () => onStoreChange()

      api.on("reInit", handleUpdate)
      api.on("select", handleUpdate)

      return () => {
        api.off("reInit", handleUpdate)
        api.off("select", handleUpdate)
      }
    },
    () => api?.selectedScrollSnap() ?? 0,
    () => 0
  )
}
