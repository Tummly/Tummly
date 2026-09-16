import type { OperatorGlobalSearchSnapshot } from "@/lib/operatorGlobalSearch/createOperatorGlobalSearchModule"

export type GlobalSearchSelectHandlers = {
  onSelectSuggestion: (suggestionId: string) => void
  onSelectGuestHit: (guestId: string) => void
  onSelectFeedbackHit: (feedbackId: string) => void
  onSelectCampaignHit: (campaignId: string) => void
  onSelectOfferHit: (offerId: string) => void
  onSelectQrCodeHit: (qrCodeId: string) => void
  onViewAllGuests: () => void
  onViewAllFeedback: () => void
  onViewAllCampaigns: () => void
  onViewAllOffers: () => void
  onViewAllQrCodes: () => void
}

export type GlobalSearchSelectable = {
  value: string
  activate: () => void
}

/** Flat list of cmdk item values in visual order for keyboard Move / Open. */
export function listGlobalSearchSelectables(
  snapshot: OperatorGlobalSearchSnapshot,
  handlers: GlobalSearchSelectHandlers
): GlobalSearchSelectable[] {
  const trimmed = snapshot.query.trim()
  const items: GlobalSearchSelectable[] = []

  if (trimmed.length === 0) {
    for (const suggestion of snapshot.emptySuggestions) {
      items.push({
        value: suggestion.id,
        activate: () => handlers.onSelectSuggestion(suggestion.id),
      })
    }
    return items
  }

  if (trimmed.length < 2) {
    return items
  }

  for (const hit of snapshot.guestHits) {
    items.push({
      value: `guest-${hit.id}`,
      activate: () => handlers.onSelectGuestHit(hit.id),
    })
  }
  if (snapshot.showViewAllGuests) {
    items.push({
      value: "view-all-guests",
      activate: () => handlers.onViewAllGuests(),
    })
  }

  for (const hit of snapshot.feedbackHits) {
    items.push({
      value: `feedback-${hit.id}`,
      activate: () => handlers.onSelectFeedbackHit(hit.id),
    })
  }
  if (snapshot.showViewAllFeedback) {
    items.push({
      value: "view-all-feedback",
      activate: () => handlers.onViewAllFeedback(),
    })
  }

  for (const hit of snapshot.campaignHits) {
    items.push({
      value: `campaign-${hit.id}`,
      activate: () => handlers.onSelectCampaignHit(hit.id),
    })
  }
  if (snapshot.showViewAllCampaigns) {
    items.push({
      value: "view-all-campaigns",
      activate: () => handlers.onViewAllCampaigns(),
    })
  }

  for (const hit of snapshot.offerHits) {
    items.push({
      value: `offer-${hit.id}`,
      activate: () => handlers.onSelectOfferHit(hit.id),
    })
  }
  if (snapshot.showViewAllOffers) {
    items.push({
      value: "view-all-offers",
      activate: () => handlers.onViewAllOffers(),
    })
  }

  for (const hit of snapshot.qrCodeHits) {
    items.push({
      value: `qr-code-${hit.id}`,
      activate: () => handlers.onSelectQrCodeHit(hit.id),
    })
  }
  if (snapshot.showViewAllQrCodes) {
    items.push({
      value: "view-all-qr-codes",
      activate: () => handlers.onViewAllQrCodes(),
    })
  }

  for (const suggestion of snapshot.typedSuggestions) {
    items.push({
      value: suggestion.id,
      activate: () => handlers.onSelectSuggestion(suggestion.id),
    })
  }

  return items
}

export function moveGlobalSearchSelection(
  values: readonly string[],
  current: string,
  delta: number
): string {
  if (values.length === 0) {
    return ""
  }
  const index = values.indexOf(current)
  if (index < 0) {
    return delta >= 0 ? values[0]! : values[values.length - 1]!
  }
  const next = (index + delta + values.length) % values.length
  return values[next]!
}

/**
 * Default keyboard highlight for the results list.
 * When entity hits first arrive, jump to the first product row even if an
 * Ask Tummly suggestion id is still a valid selectable.
 */
export function resolveGlobalSearchHighlight(
  selectableValues: readonly string[],
  current: string,
  options: { entityHitsJustArrived: boolean }
): string {
  if (selectableValues.length === 0) {
    return ""
  }
  if (options.entityHitsJustArrived) {
    return selectableValues[0]!
  }
  if (!selectableValues.includes(current)) {
    return selectableValues[0]!
  }
  return current
}

/** True when the snapshot has at least one product entity hit. */
export function globalSearchSnapshotHasEntityHits(
  snapshot: Pick<
    OperatorGlobalSearchSnapshot,
    | "guestHits"
    | "feedbackHits"
    | "campaignHits"
    | "offerHits"
    | "qrCodeHits"
  >
): boolean {
  return (
    snapshot.guestHits.length > 0 ||
    snapshot.feedbackHits.length > 0 ||
    snapshot.campaignHits.length > 0 ||
    snapshot.offerHits.length > 0 ||
    snapshot.qrCodeHits.length > 0
  )
}
