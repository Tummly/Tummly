import { useEffect, useState } from "react"
import { Link, Navigate, useParams } from "react-router-dom"
import { isAxiosError } from "axios"

import {
  getMyHelpCentreQuery,
  postMyHelpCentreReply,
} from "@/api/helpCentreApi"
import { HelpCentrePageShell } from "@/components/help-centre/HelpCentrePageShell"
import { HelpCentreStatusBadge } from "@/components/help-centre/HelpCentreStatusBadge"
import {
  HELP_CENTRE_CONTACT_URL,
  HELP_CENTRE_MY_QUERIES_URL,
} from "@/config/support"
import { Button } from "@/components/ui/button"
import { Textarea } from "@/components/ui/textarea"
import { getFetchErrorMessage } from "@/lib/apiEnvelope"
import type { HelpCentreQueryDetail } from "@/types/helpCentre"

function formatTimestamp(value: string) {
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

function authorLabel(kind: HelpCentreQueryDetail["messages"][number]["authorKind"]) {
  switch (kind) {
    case "SUPPORT":
      return "Tummly Support"
    case "OPERATOR":
    case "SUBMITTER":
      return "You"
    default:
      return "You"
  }
}

export default function MyQueryThreadPage() {
  const { id } = useParams<{ id: string }>()
  const queryId = Number(id)

  const [query, setQuery] = useState<HelpCentreQueryDetail | null>(null)
  const [state, setState] = useState<"loading" | "loaded" | "error">("loading")
  const [reply, setReply] = useState("")
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  useEffect(() => {
    if (!Number.isFinite(queryId)) {
      return
    }

    let active = true

    void (async () => {
      try {
        const result = await getMyHelpCentreQuery(queryId)
        if (active) {
          setQuery(result)
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
  }, [queryId])

  if (!Number.isFinite(queryId)) {
    return <Navigate to={HELP_CENTRE_MY_QUERIES_URL} replace />
  }

  const handleReply = async () => {
    if (!reply.trim() || !query) {
      return
    }

    setSubmitError(null)
    setIsSubmitting(true)

    try {
      const updated = await postMyHelpCentreReply(queryId, reply.trim())
      setQuery(updated)
      setReply("")
    } catch (error) {
      setSubmitError(
        isAxiosError(error)
          ? getFetchErrorMessage(error.response?.data, "Unable to send reply.")
          : "Unable to send reply."
      )
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <HelpCentrePageShell>
      <div className="flex flex-col gap-6">
        <Link
          to={HELP_CENTRE_MY_QUERIES_URL}
          className="text-sm font-medium text-[#14a74a] underline-offset-4 hover:underline"
        >
          ← Back to My queries
        </Link>

        {state === "loading" && (
          <p className="text-sm text-muted-foreground">Loading query...</p>
        )}

        {state === "error" && (
          <p className="text-sm text-destructive">Unable to load this query.</p>
        )}

        {query && (
          <>
            <header className="flex flex-col gap-3">
              <div className="flex flex-wrap items-center gap-3">
                <h1 className="text-2xl font-bold text-[#232323]">
                  {query.topicLabel}
                </h1>
                <HelpCentreStatusBadge
                  status={query.status}
                  statusLabel={query.statusLabel}
                />
              </div>
              <p className="text-sm text-muted-foreground">
                Last updated {formatTimestamp(query.updatedAt)}
              </p>
            </header>

            <div className="flex flex-col gap-4">
              {query.messages.map((message) => (
                <article
                  key={message.id}
                  className="rounded-xl border border-border bg-white px-4 py-4"
                >
                  <div className="mb-2 flex items-center justify-between gap-3">
                    <span className="text-sm font-semibold text-[#232323]">
                      {authorLabel(message.authorKind)}
                    </span>
                    <span className="text-xs text-muted-foreground">
                      {formatTimestamp(message.createdAt)}
                    </span>
                  </div>
                  <p className="text-sm leading-6 whitespace-pre-wrap text-foreground">
                    {message.body}
                  </p>
                </article>
              ))}
            </div>

            {query.canReply ? (
              <div className="flex flex-col gap-3 rounded-xl border border-border bg-muted/20 p-4">
                <Textarea
                  value={reply}
                  onChange={(event) => setReply(event.target.value)}
                  rows={4}
                  placeholder="Write a reply to Tummly support"
                />
                {submitError && (
                  <p className="text-sm text-destructive" role="alert">
                    {submitError}
                  </p>
                )}
                <Button
                  onClick={() => void handleReply()}
                  disabled={isSubmitting || !reply.trim()}
                  className="w-fit bg-[#14a74a] hover:bg-[#129641]"
                >
                  {isSubmitting ? "Sending..." : "Send reply"}
                </Button>
              </div>
            ) : (
              <p className="text-sm text-muted-foreground">
                This query is closed.{" "}
                <Link
                  to={HELP_CENTRE_CONTACT_URL}
                  className="font-medium text-[#14a74a] underline underline-offset-2"
                >
                  Contact us again
                </Link>{" "}
                if you need more help.
              </p>
            )}
          </>
        )}
      </div>
    </HelpCentrePageShell>
  )
}
