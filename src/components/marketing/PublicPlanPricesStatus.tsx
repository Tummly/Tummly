import { CustomPlanQuoteStatus } from "@/components/marketing/custom-plan/CustomPlanSummaryCard"
import { Skeleton } from "@/components/ui/skeleton"
import { CUSTOM_PLAN_QUOTE_COPY } from "@/content/marketing/customPlanQuoteCopy"
import type { PublicPlanPricesStatus } from "@/lib/marketingCustomPlan/usePublicPlanPrices"

/** Loading or failed copy for plan prices. Show once above each plan grid. */
export function PublicPlanPricesStatusMessage({
  status,
  onRetry,
}: {
  status: PublicPlanPricesStatus
  onRetry: () => void
}) {
  if (status === "ready") {
    return null
  }
  return (
    <CustomPlanQuoteStatus
      message={
        status === "loading"
          ? CUSTOM_PLAN_QUOTE_COPY.loading
          : CUSTOM_PLAN_QUOTE_COPY.failed
      }
      showRetry={status === "failed"}
      onRetry={onRetry}
    />
  )
}

/** Stands in for a paid plan price: a placeholder while loading, a dash when unavailable. */
export function PublicPlanPriceFallback({
  status,
}: {
  status: PublicPlanPricesStatus
}) {
  if (status === "loading") {
    return <Skeleton className="h-9 w-24" aria-hidden />
  }
  return (
    <p className="m-0 font-sans text-2xl leading-none text-[#141414]">—</p>
  )
}
