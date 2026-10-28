import { useEffect, useRef, useState } from "react"
import { Link, useSearchParams } from "react-router-dom"

import {
  fetchOverview,
  openSessionFromLink,
  verifyContactEmail,
  verifySessionEmail,
  type GuestPreferencesOverview as GuestPreferencesOverviewData,
} from "@/api/publicGuestPreferencesApi"
import { GuestPreferencesAddContactDialog } from "@/components/guest-preferences/GuestPreferencesAddContactDialog"
import { GuestPreferencesEntry } from "@/components/guest-preferences/GuestPreferencesEntry"
import { GuestPreferencesHeader } from "@/components/guest-preferences/GuestPreferencesHeader"
import { GuestPreferencesOverview } from "@/components/guest-preferences/GuestPreferencesOverview"
import { GuestFeedbackShell } from "@/components/guest-feedback/GuestFeedbackShell"
import { Button } from "@/components/ui/button"
import { Spinner } from "@/components/ui/spinner"
import { LEGAL_ROUTES } from "@/constants/legalRoutes"
import { GUEST_PREFERENCES_COPY } from "@/content/guestPreferences/guestPreferencesCopy"
import { routeGuestPreferencesRequestError } from "@/lib/guestPreferences/guestPreferencesRequestErrors"
import { resolveGuestPreferencesPageMode } from "@/lib/guestPreferences/guestPreferencesPageMode"
import { GUEST_PREFERENCES_LEGAL_LINK_CLASS } from "@/lib/guestPreferences/guestPreferencesPresentation"
import { readGuestPreferencesSession } from "@/lib/guestPreferences/guestPreferencesSession"

type PageView =
  | { kind: "loading" }
  /** `linkExpired` is set when the link that brought the guest here failed. */
  | { kind: "entry"; linkExpired: boolean }
  /** Text only, with no form. Used after an add-contact link is confirmed. */
  | { kind: "notice"; message: string }
  | { kind: "overview"; overview: GuestPreferencesOverviewData }
  | { kind: "error"; message: string }

const ENTRY_VIEW: PageView = { kind: "entry", linkExpired: false }
const EXPIRED_ENTRY_VIEW: PageView = { kind: "entry", linkExpired: true }

async function loadOverviewView(): Promise<PageView> {
  try {
    return { kind: "overview", overview: await fetchOverview() }
  } catch (error) {
    const route = routeGuestPreferencesRequestError(
      error,
      GUEST_PREFERENCES_COPY.loadFailed
    )
    return route.view === "entry"
      ? ENTRY_VIEW
      : { kind: "error", message: route.message }
  }
}

/** Any failure (bad, used or expired token) counts as invalid. */
async function succeeds(action: () => Promise<unknown>): Promise<boolean> {
  try {
    await action()
    return true
  } catch {
    return false
  }
}

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
    initialMode.kind === "entry" ? ENTRY_VIEW : { kind: "loading" }
  )
  const [addContactOpen, setAddContactOpen] = useState(false)
  const startedRef = useRef(false)

  useEffect(() => {
    // Once per mount. StrictMode re-runs effects, and link tokens are single use.
    if (startedRef.current) {
      return
    }
    startedRef.current = true

    /** Drops a one-time param so a refresh does not replay the link. */
    function dropParam(name: string) {
      const next = new URLSearchParams(searchParams)
      next.delete(name)
      setSearchParams(next, { replace: true })
    }

    async function start() {
      switch (initialMode.kind) {
        case "link": {
          const valid = await openLinkSession(initialMode.token)
          if (!valid) {
            setView(EXPIRED_ENTRY_VIEW)
            return
          }
          // Drop the one-time token so a refresh uses the stored session.
          dropParam("t")
          setView(await loadOverviewView())
          return
        }
        case "verify-email": {
          const valid = await succeeds(() => verifySessionEmail(initialMode.v))
          dropParam("v")
          setView(valid ? await loadOverviewView() : EXPIRED_ENTRY_VIEW)
          return
        }
        case "link-contact": {
          // No session is written here. The link may be opened in another browser.
          const valid = await succeeds(() => verifyContactEmail(initialMode.v))
          dropParam("link")
          setView(
            valid
              ? { kind: "notice", message: GUEST_PREFERENCES_COPY.contactLinked }
              : EXPIRED_ENTRY_VIEW
          )
          return
        }
        case "session":
          setView(await loadOverviewView())
          return
        case "entry":
          return
      }
    }

    void start()
  }, [initialMode, searchParams, setSearchParams])

  function reloadOverview() {
    setView({ kind: "loading" })
    void loadOverviewView().then(setView)
  }

  function refreshOverview() {
    void loadOverviewView().then(setView)
  }

  function handleSessionExpired() {
    setAddContactOpen(false)
    setView(ENTRY_VIEW)
  }

  function handleContactAdded() {
    setAddContactOpen(false)
    refreshOverview()
  }

  return (
    <GuestFeedbackShell showPoweredBy={false} contentClassName="gap-[29px]">
      {renderView(view, {
        onSessionExpired: handleSessionExpired,
        onRetry: reloadOverview,
        onEntryVerified: reloadOverview,
        onAddContact: () => setAddContactOpen(true),
      })}

      {view.kind === "overview" && addContactOpen ? (
        <GuestPreferencesAddContactDialog
          baselineContactCount={view.overview.contacts.length}
          onClose={() => setAddContactOpen(false)}
          onVerified={handleContactAdded}
          onSessionExpired={handleSessionExpired}
        />
      ) : null}

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

type PageViewHandlers = {
  onSessionExpired: () => void
  onRetry: () => void
  onEntryVerified: () => void
  onAddContact: () => void
}

function renderView(view: PageView, handlers: PageViewHandlers) {
  switch (view.kind) {
    case "loading":
      return (
        <div className="flex justify-center py-16">
          <Spinner />
        </div>
      )
    case "entry":
      return (
        <GuestPreferencesEntry
          linkExpired={view.linkExpired}
          onVerified={handlers.onEntryVerified}
        />
      )
    case "notice":
      return <GuestPreferencesHeader lead={view.message} />
    case "overview":
      return (
        <GuestPreferencesOverview
          overview={view.overview}
          onSessionExpired={handlers.onSessionExpired}
          onAddContact={handlers.onAddContact}
          // Task 12 (restaurants) wires this.
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
