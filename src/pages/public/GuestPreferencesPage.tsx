import { useEffect, useRef, useState } from "react"
import { Link, useSearchParams } from "react-router-dom"

import {
  fetchOverview,
  GuestPreferencesSessionExpiredError,
  openSessionFromLink,
  type GuestPreferencesOverview as GuestPreferencesOverviewData,
} from "@/api/publicGuestPreferencesApi"
import { GuestPreferencesEntryPlaceholder } from "@/components/guest-preferences/GuestPreferencesEntryPlaceholder"
import { GuestPreferencesHeader } from "@/components/guest-preferences/GuestPreferencesHeader"
import { GuestPreferencesOverview } from "@/components/guest-preferences/GuestPreferencesOverview"
import { GuestFeedbackShell } from "@/components/guest-feedback/GuestFeedbackShell"
import { Button } from "@/components/ui/button"
import { Spinner } from "@/components/ui/spinner"
import { LEGAL_ROUTES } from "@/constants/legalRoutes"
import { GUEST_PREFERENCES_COPY } from "@/content/guestPreferences/guestPreferencesCopy"
import { guestPreferencesErrorMessage } from "@/lib/guestPreferences/guestPreferencesOverviewState"
import { resolveGuestPreferencesPageMode } from "@/lib/guestPreferences/guestPreferencesPageMode"
import { GUEST_PREFERENCES_LEGAL_LINK_CLASS } from "@/lib/guestPreferences/guestPreferencesPresentation"
import { readGuestPreferencesSession } from "@/lib/guestPreferences/guestPreferencesSession"

type PageView =
  | { kind: "loading" }
  | { kind: "entry" }
  | { kind: "overview"; overview: GuestPreferencesOverviewData }
  | { kind: "error"; message: string }

async function loadOverviewView(): Promise<PageView> {
  try {
    return { kind: "overview", overview: await fetchOverview() }
  } catch (error) {
    if (error instanceof GuestPreferencesSessionExpiredError) {
      return { kind: "entry" }
    }
    return {
      kind: "error",
      message: guestPreferencesErrorMessage(
        error,
        GUEST_PREFERENCES_COPY.loadFailed
      ),
    }
  }
}

/** Any failure to open the link (bad, used or expired token) counts as invalid. */
async function openLinkSession(token: string): Promise<boolean> {
  try {
    const { valid } = await openSessionFromLink(token)
    return valid
  } catch {
    return false
  }
}

export default function GuestPreferencesPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const [initialMode] = useState(() =>
    resolveGuestPreferencesPageMode(
      searchParams,
      readGuestPreferencesSession() !== null
    )
  )
  const [view, setView] = useState<PageView>(() =>
    initialMode.kind === "link" || initialMode.kind === "session"
      ? { kind: "loading" }
      : { kind: "entry" }
  )
  const startedRef = useRef(false)

  useEffect(() => {
    // Once per mount. StrictMode re-runs effects, and a link token is single use.
    if (startedRef.current) {
      return
    }
    startedRef.current = true

    async function start() {
      if (initialMode.kind === "link") {
        const valid = await openLinkSession(initialMode.token)
        if (!valid) {
          setView({ kind: "entry" })
          return
        }
        // Drop the one-time token so a refresh uses the stored session.
        const next = new URLSearchParams(searchParams)
        next.delete("t")
        setSearchParams(next, { replace: true })
        setView(await loadOverviewView())
        return
      }
      if (initialMode.kind === "session") {
        setView(await loadOverviewView())
      }
    }

    void start()
  }, [initialMode, searchParams, setSearchParams])

  function retryLoad() {
    setView({ kind: "loading" })
    void loadOverviewView().then(setView)
  }

  return (
    <GuestFeedbackShell showPoweredBy={false} contentClassName="gap-[29px]">
      {renderView(view, {
        onSessionExpired: () => setView({ kind: "entry" }),
        onRetry: retryLoad,
      })}

      <nav
        aria-label={GUEST_PREFERENCES_COPY.legalNavLabel}
        className="flex flex-col items-center gap-2 font-sans text-xs text-guest-feedback-muted-soft"
      >
        <div className="flex items-center justify-center gap-1.5">
          <Link to={LEGAL_ROUTES.terms} className={GUEST_PREFERENCES_LEGAL_LINK_CLASS}>
            {GUEST_PREFERENCES_COPY.termsLabel}
          </Link>
          <span aria-hidden>·</span>
          <Link to={LEGAL_ROUTES.privacy} className={GUEST_PREFERENCES_LEGAL_LINK_CLASS}>
            {GUEST_PREFERENCES_COPY.privacyLabel}
          </Link>
        </div>
      </nav>
    </GuestFeedbackShell>
  )
}

function renderView(
  view: PageView,
  handlers: { onSessionExpired: () => void; onRetry: () => void }
) {
  switch (view.kind) {
    case "loading":
      return (
        <div className="flex justify-center py-16">
          <Spinner />
        </div>
      )
    case "entry":
      return <GuestPreferencesEntryPlaceholder />
    case "overview":
      return (
        <GuestPreferencesOverview
          overview={view.overview}
          onSessionExpired={handlers.onSessionExpired}
          // Task 11 (add contact) and Task 12 (restaurants) wire these.
          onAddContact={() => undefined}
          onManageRestaurants={() => undefined}
        />
      )
    case "error":
      return (
        <>
          <GuestPreferencesHeader lead={view.message} />
          <div className="flex justify-center px-5">
            <Button
              type="button"
              variant="link"
              size="link-sm"
              onClick={handlers.onRetry}
              className="font-sans font-medium text-guest-feedback-accent"
            >
              {GUEST_PREFERENCES_COPY.tryAgainLabel}
            </Button>
          </div>
        </>
      )
  }
}
