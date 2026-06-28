import { useSyncExternalStore } from "react"

import type { CarouselApi } from "@/components/ui/carousel"

export function useCarouselScrollProgress(api: CarouselApi | undefined) {
  const scrollProgress = useSyncExternalStore(
    (onStoreChange) => {
      if (!api) return () => {}

      const handleUpdate = () => onStoreChange()

      api.on("reInit", handleUpdate)
      api.on("scroll", handleUpdate)
      api.on("select", handleUpdate)

      return () => {
        api.off("reInit", handleUpdate)
        api.off("scroll", handleUpdate)
        api.off("select", handleUpdate)
      }
    },
    () => api?.scrollProgress() ?? 0,
    () => 0
  )

  const snapCount = useSyncExternalStore(
    (onStoreChange) => {
      if (!api) return () => {}

      const handleUpdate = () => onStoreChange()

      api.on("reInit", handleUpdate)

      return () => {
        api.off("reInit", handleUpdate)
      }
    },
    () => api?.scrollSnapList().length ?? 1,
    () => 1
  )

  const minProgress = snapCount <= 1 ? 1 : 1 / snapCount
  const progress =
    scrollProgress === 0
      ? minProgress
      : Math.max(scrollProgress, minProgress)

  return {
    progress,
    progressPercent: progress * 100,
    snapCount,
  }
}
