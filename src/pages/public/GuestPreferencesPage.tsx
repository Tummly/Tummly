import { useEffect, useRef, useState } from "react"
import { Link, useSearchParams } from "react-router-dom"

import {
  fetchOverview,
  openSessionFromLink,
  verifySessionEmail,
  type GuestPreferencesOverview as GuestPreferencesOverviewData,
} from "@/api/publicGuestPreferencesApi"
import { GuestPreferencesEntry } from "@/components/guest-preferences/GuestPreferencesEntry"
import { GuestPreferencesHeader } from "@/components/guest-preferences/GuestPreferencesHeader"
import { GuestPreferencesLinkConfirm } from "@/components/guest-preferences/GuestPreferencesLinkConfirm"
import { GuestPreferencesOverview } from "@/components/guest-preferences/GuestPreferencesOverview"
import { GuestPreferencesRestaurants } from "@/components/guest-preferences/GuestPreferencesRestaurants"
import { GuestFeedbackShell } from "@/components/guest-feedback/GuestFeedbackShell"
import { Button } from "@/components/ui/button"
import { Spinner } from "@/components/ui/spinner"
import { LEGAL_ROUTES } from "@/constants/legalRoutes"
import { GUEST_PREFERENCES_COPY } from "@/content/guestPreferences/guestPreferencesCopy"
import {
  confirmLink,
  resolveLinkSession,
  routeGuestPreferencesRequestError,
} from "@/lib/guestPreferences/guestPreferencesRequestErrors"
import {
  resolveGuestPreferencesPageMode,
  type GuestPreferencesPageMode,
} from "@/lib/guestPreferences/guestPreferencesPageMode"
import { GUEST_PREFERENCES_LEGAL_LINK_CLASS } from "@/lib/guestPreferences/guestPreferencesPresentation"
import { readGuestPreferencesSession } from "@/lib/guestPreferences/guestPreferencesSession"

type PageView =
  | { kind: "loading" }
  /** `linkExpired` is set when the link that brought the guest here is invalid. */
  | { kind: "entry"; linkExpired: boolean }
  /** Text only, with no form. Used after an add-contact link is confirmed. */
  | { kind: "notice"; message: string }
  /** Add-contact link. Confirms only when the guest presses the button. */
  | { kind: "link-confirm"; v: string }
  | { kind: "overview"; overview: GuestPreferencesOverviewData }
  /** Manage restaurants sub-view. Back refetches the overview. */
  | { kind: "restaurants" }
  /** `retry` repeats the step that failed, so Try again does not skip it. */
  | { kind: "error"; message: string; retry: () => Promise<PageView> }

type LinkMode = Extract<
  GuestPreferencesPageMode,
  { kind: "link" | "verify-email" | "link-contact" }
>

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
      : { kind: "error", message: route.message, retry: loadOverviewView }
  }
}

/**
 * Resolves a link on the URL into the next view. The one-time param is
 * dropped once the link is answered. A failure that is not a 400 keeps the
 * param, so a refresh or Try again can check the same link again.
 */
async function resolveLinkMode(
  mode: LinkMode,
  dropParam: (name: string) => void
): Promise<PageView> {
  const retry = () => resolveLinkMode(mode, dropParam)
  switch (mode.kind) {
    case "link": {
      const outcome = await resolveLinkSession(() => openSessionFromLink(mode.token))
      if (outcome === "unavailable") {
        // The link may still be valid. `t` stays in the URL so Try again can check it.
        return { kind: "error", message: GUEST_PREFERENCES_COPY.loadFailed, retry }
      }
      // Strip the token before the branch, so a refresh never replays it.
      dropParam("t")
      return outcome === "valid" ? loadOverviewView() : EXPIRED_ENTRY_VIEW
    }
    case "verify-email": {
      const outcome = await confirmLink(() => verifySessionEmail(mode.v))
      if (outcome === "unavailable") {
        return { kind: "error", message: GUEST_PREFERENCES_COPY.loadFailed, retry }
      }
      dropParam("v")
      return outcome === "ok" ? loadOverviewView() : EXPIRED_ENTRY_VIEW
    }
    case "link-contact":
      // Nothing is sent on load. The guest presses Confirm, so a link preview or scanner
      // cannot add the address. The view drops `link` once the link is answered.
      return { kind: "link-confirm", v: mode.v }
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
  const [addingContact, setAddingContact] = useState(false)
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
      if (initialMode.kind === "entry") {
        return
      }
      if (initialMode.kind === "session") {
        setView(await loadOverviewView())
        return
      }
      setView(await resolveLinkMode(initialMode, dropParam))
    }

    void start()
  }, [initialMode, searchParams, setSearchParams])

  /** Shows the spinner, then the view that `load` returns. */
  function loadView(load: () => Promise<PageView>) {
    setView({ kind: "loading" })
    void load().then(setView)
  }

  function refreshOverview() {
    void loadOverviewView().then(setView)
  }

  function handleSessionExpired() {
    setAddingContact(false)
    setView(ENTRY_VIEW)
  }

  function handleContactAdded() {
    setAddingContact(false)
    refreshOverview()
  }

  /** Drops a one-time URL param. Used after the add-contact link is answered. */
  function dropUrlParam(name: string) {
    const next = new URLSearchParams(searchParams)
    next.delete(name)
    setSearchParams(next, { replace: true })
  }

  function handleContactLinkConfirmed() {
    dropUrlParam("link")
    setView({ kind: "notice", message: GUEST_PREFERENCES_COPY.contactLinked })
  }

  function handleContactLinkExpired() {
    dropUrlParam("link")
    setView(EXPIRED_ENTRY_VIEW)
  }

  return (
    <GuestFeedbackShell showPoweredBy={false} contentClassName="gap-[29px]">
      {renderView(view, {
        addingContact,
        onSessionExpired: handleSessionExpired,
        onRetry: (retry) => loadView(retry),
        onEntryVerified: () => loadView(loadOverviewView),
        onAddContact: () => setAddingContact(true),
        onContactAdded: handleContactAdded,
        onManageRestaurants: () => setView({ kind: "restaurants" }),
        onBackFromRestaurants: () => loadView(loadOverviewView),
        onContactLinkConfirmed: handleContactLinkConfirmed,
        onContactLinkExpired: handleContactLinkExpired,
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

type PageViewHandlers = {
  addingContact: boolean
  onSessionExpired: () => void
  onRetry: (retry: () => Promise<PageView>) => void
  onEntryVerified: () => void
  onAddContact: () => void
  onContactAdded: () => void
  onManageRestaurants: () => void
  onBackFromRestaurants: () => void
  onContactLinkConfirmed: () => void
  onContactLinkExpired: () => void
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
    case "link-confirm":
      return (
        <GuestPreferencesLinkConfirm
          v={view.v}
          onConfirmed={handlers.onContactLinkConfirmed}
          onExpired={handlers.onContactLinkExpired}
        />
      )
    case "overview":
      return (
        <GuestPreferencesOverview
          overview={view.overview}
          addingContact={handlers.addingContact}
          onSessionExpired={handlers.onSessionExpired}
          onAddContact={handlers.onAddContact}
          onContactAdded={handlers.onContactAdded}
          onManageRestaurants={handlers.onManageRestaurants}
        />
      )
    case "restaurants":
      return (
        <GuestPreferencesRestaurants
          onBack={handlers.onBackFromRestaurants}
          onSessionExpired={handlers.onSessionExpired}
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
              onClick={() => handlers.onRetry(view.retry)}
              className="font-sans font-medium text-guest-feedback-accent"
            >
              {GUEST_PREFERENCES_COPY.tryAgainLabel}
            </Button>
          </div>
        </>
      )
  }
}
