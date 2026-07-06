import { useEffect, useState } from "react"
import { Link, Navigate, useParams } from "react-router-dom"
import { isAxiosError } from "axios"

import {
  getMyHelpCentreQuery,
  postMyHelpCentreReply,
} from "@/api/helpCentreApi"
import {
  helpCentreArticleSectionInner,
  helpCentreArticleSectionShell,
  helpCentreSectionPadding,
} from "@/components/help-centre/helpCentreLayout"
import { HelpCentreStatusBadge } from "@/components/help-centre/HelpCentreStatusBadge"
import { HelpCentreThreadMessage } from "@/components/help-centre/HelpCentreThreadMessage"
import Footer from "@/components/home/Footer"
import { Button } from "@/components/ui/button"
import { Textarea } from "@/components/ui/textarea"
import {
  HELP_CENTRE_CONTACT_URL,
  HELP_CENTRE_MY_QUERIES_URL,
} from "@/config/support"
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
    <>
      <section className="w-full bg-white">
        <div className={`${helpCentreArticleSectionShell} ${helpCentreSectionPadding}`}>
          <div className={`${helpCentreArticleSectionInner} flex flex-col gap-8`}>
            <Link
              to={HELP_CENTRE_MY_QUERIES_URL}
              className="w-fit text-base font-medium text-[#14a74a] underline-offset-4 hover:underline"
            >
              ← Back to My queries
            </Link>

            {state === "loading" && (
              <p className="m-0 text-base leading-[22px] text-[#6b6b6b]">
                Loading query...
              </p>
            )}

            {state === "error" && (
              <p className="m-0 text-base leading-[22px] text-destructive">
                Unable to load this query.
              </p>
            )}

            {query && (
              <>
                <header className="flex flex-col gap-3">
                  <div className="flex flex-wrap items-center gap-3">
                    <h1 className="m-0 text-[28px] font-bold leading-normal text-[#141414] lg:text-[32px]">
                      {query.topicLabel}
                    </h1>
                    <HelpCentreStatusBadge
                      status={query.status}
                      statusLabel={query.statusLabel}
                    />
                  </div>
                  <p className="m-0 text-sm leading-[22px] text-[#6b6b6b]">
                    Last updated {formatTimestamp(query.updatedAt)}
                  </p>
                </header>

                <div className="flex flex-col gap-4">
                  {query.messages.map((message) => (
                    <HelpCentreThreadMessage
                      key={message.id}
                      message={message}
                    />
                  ))}
                </div>

                {query.canReply ? (
                  <div className="flex flex-col gap-4 rounded-xl border border-[#e5e5e5] bg-[#f6f6f6] p-5">
                    <Textarea
                      value={reply}
                      onChange={(event) => setReply(event.target.value)}
                      rows={4}
                      placeholder="Write a reply to Tummly support"
                      className="border-[#e5e5e5] bg-white"
                    />
                    {submitError && (
                      <p className="m-0 text-sm text-destructive" role="alert">
                        {submitError}
                      </p>
                    )}
                    <Button
                      onClick={() => void handleReply()}
                      disabled={isSubmitting || !reply.trim()}
                      className="h-auto w-fit rounded-[84px] bg-[#14a74a] px-[17px] py-[9px] text-base font-medium leading-5 text-white hover:bg-[#129641]"
                    >
                      {isSubmitting ? "Sending..." : "Send reply"}
                    </Button>
                  </div>
                ) : (
                  <p className="m-0 text-base leading-[22px] text-[#6b6b6b]">
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
        </div>
      </section>

      <Footer />
    </>
  )
}
