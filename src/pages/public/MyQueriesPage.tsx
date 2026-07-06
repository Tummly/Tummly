import { useEffect, useState } from "react"
import { Link } from "react-router-dom"

import { getMyHelpCentreQueries } from "@/api/helpCentreApi"
import { HelpCentrePageShell } from "@/components/help-centre/HelpCentrePageShell"
import { HelpCentreStatusBadge } from "@/components/help-centre/HelpCentreStatusBadge"
import {
  HELP_CENTRE_CONTACT_URL,
  HELP_CENTRE_URL,
  helpCentreMyQueryUrl,
} from "@/config/support"
import type { HelpCentreQueryListItem } from "@/types/helpCentre"

function formatUpdatedAt(value: string) {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) {
    return value
  }

  return date.toLocaleString("en-GB", {
    day: "numeric",
    month: "short",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  })
}

export default function MyQueriesPage() {
  const [queries, setQueries] = useState<HelpCentreQueryListItem[]>([])
  const [state, setState] = useState<"loading" | "loaded" | "error">("loading")

  useEffect(() => {
    let active = true

    void (async () => {
      try {
        const result = await getMyHelpCentreQueries()
        if (active) {
          setQueries(result)
          setState("loaded")
        }
      } catch {
        if (active) {
          setState("error")
        }
      }
    })()

    return () => {
      active = false
    }
  }, [])

  return (
    <HelpCentrePageShell>
      <div className="flex flex-col gap-6">
        <div className="flex flex-col gap-2">
          <Link
            to={HELP_CENTRE_URL}
            className="text-sm font-medium text-[#14a74a] underline-offset-4 hover:underline"
          >
            ← Back to Help Centre
          </Link>
          <h1 className="text-3xl font-bold text-[#232323]">My queries</h1>
          <p className="text-muted-foreground">
            Track your support requests and continue conversations with Tummly
            support.
          </p>
        </div>

        {state === "loading" && (
          <p className="text-sm text-muted-foreground">Loading queries...</p>
        )}

        {state === "error" && (
          <p className="text-sm text-destructive">
            Unable to load your queries right now.
          </p>
        )}

        {state === "loaded" && queries.length === 0 && (
          <div className="rounded-xl border border-dashed border-border px-6 py-10 text-center">
            <p className="text-muted-foreground">
              You have not submitted any queries yet.
            </p>
            <Link
              to={HELP_CENTRE_CONTACT_URL}
              className="mt-3 inline-block text-sm font-medium text-[#14a74a] underline"
            >
              Contact us
            </Link>
          </div>
        )}

        {queries.length > 0 && (
          <ul className="divide-y divide-border rounded-xl border border-border bg-white">
            {queries.map((query) => (
              <li key={query.id}>
                <Link
                  to={helpCentreMyQueryUrl(query.id)}
                  className="flex flex-col gap-3 px-5 py-4 transition-colors hover:bg-muted/40 sm:flex-row sm:items-center sm:justify-between"
                >
                  <div className="flex flex-col gap-1">
                    <span className="font-semibold text-[#232323]">
                      {query.topicLabel}
                    </span>
                    {query.preview && (
                      <span className="line-clamp-1 text-sm text-muted-foreground">
                        {query.preview}
                      </span>
                    )}
                  </div>
                  <div className="flex flex-col items-start gap-2 sm:items-end">
                    <HelpCentreStatusBadge
                      status={query.status}
                      statusLabel={query.statusLabel}
                    />
                    <span className="text-xs text-muted-foreground">
                      Updated {formatUpdatedAt(query.updatedAt)}
                    </span>
                  </div>
                </Link>
              </li>
            ))}
          </ul>
        )}
      </div>
    </HelpCentrePageShell>
  )
}
