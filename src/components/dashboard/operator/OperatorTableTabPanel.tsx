import type { ReactNode } from "react"

import { Spinner } from "@/components/ui/spinner"

export type OperatorTabContentStatus = "loading" | "ready" | "refreshing"

function OperatorTableTabPanel({
  status,
  loadingLabel,
  children,
}: {
  status: OperatorTabContentStatus
  loadingLabel: string
  children: ReactNode
}) {
  return (
    <div role="tabpanel" aria-busy={status !== "ready"}>
      {status === "loading" ? (
        <div className="flex min-h-[291px] items-center justify-center">
          <Spinner aria-label={loadingLabel} />
        </div>
      ) : (
        children
      )}
    </div>
  )
}

export { OperatorTableTabPanel }
