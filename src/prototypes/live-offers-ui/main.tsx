/**
 * PROTOTYPE — throwaway ungated UI review for Home Live offers.
 * Question: Is the Live offers section UI ready (empty / loading / error / cards)?
 * Run: npm run prototype:live-offers → http://localhost:5174/prototype-live-offers.html
 */
import { StrictMode, useMemo, useState } from "react"
import { createRoot } from "react-dom/client"

import { HomeLiveOffersSection } from "@/components/dashboard/operator/Home/HomeLiveOffersSection"
import type { OperatorHomeLiveCard } from "@/lib/operatorHome/buildLiveOffersSectionCards"
import type { LiveOffersEmptyActionId } from "@/lib/operatorHome/liveOffersSectionPresentation"
import "@/index.css"

type PrototypeStateId = "populated" | "empty" | "loading" | "error"

const STATE_ORDER: PrototypeStateId[] = [
  "populated",
  "empty",
  "loading",
  "error",
]

const STATE_LABELS: Record<PrototypeStateId, string> = {
  populated: "Populated (campaign + offer)",
  empty: "Empty",
  loading: "Loading",
  error: "Error",
}

const STUB_CARDS: OperatorHomeLiveCard[] = [
  {
    kind: "campaign",
    id: 101,
    title: "Thank-you campaign",
    status: "sending",
    statusLabel: "Sending",
    rowVersion: "AAAA",
    channel: "email",
    messageSubject: "Thanks for visiting",
    messageBody:
      "Hi Sarah,\n\nThanks for visiting Burger House.\nWe’d love to see you again this week. Here’s a small thank-you from us.",
    metricParts: [
      "Sent to 42 guests",
      "96% delivered",
      "8 offer claims",
    ],
  },
  {
    kind: "offer",
    id: 55,
    title: "10% off your next visit",
    status: "active",
    statusLabel: "Active",
    description:
      "Show this code to the team on your next visit. This offer is from Burger House and is subject to the terms below.",
    expiryDate: "2026-07-31",
    metricParts: ["12 claims", "5 redemptions", "Expires 31 Jul 2026"],
  },
]

function readStateFromUrl(): PrototypeStateId {
  const raw = new URLSearchParams(window.location.search).get("state")
  if (
    raw === "populated"
    || raw === "empty"
    || raw === "loading"
    || raw === "error"
  ) {
    return raw
  }
  return "populated"
}

function setStateInUrl(state: PrototypeStateId) {
  const url = new URL(window.location.href)
  url.searchParams.set("state", state)
  window.history.replaceState({}, "", url)
}

function logAction(label: string, detail?: unknown) {
  // Prototype surface — intentional console for review feedback.
  console.info(`[live-offers-prototype] ${label}`, detail ?? "")
}

function LiveOffersUiPrototypePage() {
  const [stateId, setStateId] = useState<PrototypeStateId>(readStateFromUrl)
  const [lastAction, setLastAction] = useState<string>("(none yet)")

  const loadStatus = useMemo(() => {
    if (stateId === "loading") {
      return "loading" as const
    }
    if (stateId === "error") {
      return "error" as const
    }
    return "loaded" as const
  }, [stateId])

  const cards = stateId === "populated" ? STUB_CARDS : []

  const cycle = (delta: number) => {
    const index = STATE_ORDER.indexOf(stateId)
    const next =
      STATE_ORDER[(index + delta + STATE_ORDER.length) % STATE_ORDER.length]
      ?? "populated"
    setStateId(next)
    setStateInUrl(next)
  }

  const record = (label: string, detail?: unknown) => {
    setLastAction(label)
    logAction(label, detail)
  }

  return (
    <div className="mx-auto flex min-h-screen max-w-[1600px] flex-col gap-6 p-6 pb-28">
      <header className="flex flex-col gap-2 border-b border-border pb-4">
        <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
          PROTOTYPE — no auth · stub data · port 5174
        </p>
        <h1 className="text-2xl font-bold text-foreground">
          Live offers and campaigns — UI readiness
        </h1>
        <p className="text-sm text-muted-foreground">
          Uses the real <code>HomeLiveOffersSection</code>. Switch states with
          the bar below or <code>?state=</code> in the URL. Clicks only log —
          they do not navigate.
        </p>
        <p className="text-sm text-foreground">
          Last action: <span className="font-medium">{lastAction}</span>
        </p>
      </header>

      <HomeLiveOffersSection
        loadStatus={loadStatus}
        cards={cards}
        errorMessage="Could not load live offers or campaigns. Please try again."
        brandName="Burger House"
        locationName="Camden High Street"
        locationAddress="12 High Street, London"
        onEmptyAction={(actionId: LiveOffersEmptyActionId) => {
          record(`Empty CTA: ${actionId}`)
        }}
        onRetry={() => {
          record("Retry")
          setStateId("populated")
          setStateInUrl("populated")
        }}
        onPreview={(card) => {
          record(`Preview ${card.kind} #${card.id}`, card.title)
        }}
        onViewCampaign={(id) => {
          record(`View campaign #${id}`)
        }}
        onViewOffer={(id) => {
          record(`View offer #${id}`)
        }}
        onViewRedemptions={(id) => {
          record(`View redemptions #${id}`)
        }}
        onPauseCampaign={(id) => {
          const ok = window.confirm(`Pause campaign #${id}? (stub)`)
          record(ok ? `Pause confirmed #${id}` : `Pause cancelled #${id}`)
        }}
      />

      {import.meta.env.PROD ? null : (
        <div
          className="fixed bottom-4 left-1/2 z-50 flex -translate-x-1/2 items-center gap-3 rounded-full border border-border bg-background px-3 py-2 shadow-lg"
          role="group"
          aria-label="Prototype state switcher"
        >
          <button
            type="button"
            className="rounded-full px-3 py-1 text-sm font-medium hover:bg-muted"
            onClick={() => {
              cycle(-1)
            }}
            aria-label="Previous state"
          >
            ←
          </button>
          <span className="min-w-[220px] text-center text-sm font-medium">
            {STATE_LABELS[stateId]}
          </span>
          <button
            type="button"
            className="rounded-full px-3 py-1 text-sm font-medium hover:bg-muted"
            onClick={() => {
              cycle(1)
            }}
            aria-label="Next state"
          >
            →
          </button>
        </div>
      )}
    </div>
  )
}

const rootElement = document.getElementById("root")
if (!rootElement) {
  throw new Error("Root element not found")
}

createRoot(rootElement).render(
  <StrictMode>
    <LiveOffersUiPrototypePage />
  </StrictMode>
)
