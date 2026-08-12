/**
 * Capture Guest form thank-you attach dialog state machine (ticket 07).
 */

import {
  filterExistingOfferPickerItems,
  mapCatalogOfferToExistingPickerCard,
  CAMPAIGN_EXISTING_OFFER_PICKER_COPY,
  type CampaignExistingOfferPickerCard,
} from "@/lib/operatorCampaigns/campaignExistingOfferPickerPresentation"
import type { CampaignExistingOfferPickerLoadStatus } from "@/lib/operatorCampaigns/createCampaignWizardModule"
import {
  CAPTURE_THANK_YOU_OFFER_COPY,
  type CaptureThankYouOfferFact,
} from "@/lib/operatorCapture/captureThankYouOfferPresentation"
import {
  canConfirmCampaignCatalogOfferDetails,
  emptyCampaignCatalogOfferDetailsDraft,
  mergeCampaignCatalogOfferDraftPatch,
  toCreateCatalogOfferRequestBody,
  type CampaignCatalogOfferDetailsDraft,
} from "@/lib/operatorOffers/offerCatalogPresentation"
import type { ConfirmCatalogOfferWriteResult } from "@/lib/operatorOffers/createEditOfferDrawerPresentation"
import type { CreateCatalogOfferRequestBody } from "@/types/operatorCampaigns"
import type { CatalogOffersListItem } from "@/types/operatorCampaigns"
import type { CatalogOffersListQueryParams } from "@/types/operatorCampaigns"

export type CaptureThankYouOfferStanceId =
  | "create-new-offer"
  | "existing-offer"
  | "clear-offer"

export type CaptureThankYouOfferDialogPanel =
  | "stances"
  | "existing"
  | "create"

export type CaptureThankYouOfferDialogSnapshot = {
  isOpen: boolean
  panel: CaptureThankYouOfferDialogPanel
  attached: CaptureThankYouOfferFact
  createOfferDraft: CampaignCatalogOfferDetailsDraft
  createOfferStatus: "idle" | "saving" | "error"
  createOfferError: string | null
  canConfirmCreateOffer: boolean
  existingPickerLoadStatus: CampaignExistingOfferPickerLoadStatus
  existingPickerError: string | null
  existingPickerSearchQuery: string
  existingPickerCards: CampaignExistingOfferPickerCard[]
  existingPickerIsEmpty: boolean
  existingPickerEmptyHelper: string | null
}

export type CaptureThankYouOfferModuleAdapters = {
  locationId: () => number | null
  locationName: () => string
  getAttached: () => CaptureThankYouOfferFact
  setAttached: (next: CaptureThankYouOfferFact) => void
  createCatalogOffer: (
    body: CreateCatalogOfferRequestBody
  ) => Promise<{ id: number; title: string }>
  putThankYouOffer: (
    locationId: number,
    offerId: number | null
  ) => Promise<{
    thankYouOfferId: number | null
    thankYouOfferTitle: string | null
    thankYouOfferLive: boolean
  }>
  listCatalogOffers: (
    params: CatalogOffersListQueryParams
  ) => Promise<{ items: CatalogOffersListItem[] }>
  onAttachError?: (message: string) => void
  onAttachSuccess?: (message: string) => void
}

export type CaptureThankYouOfferModule = {
  getSnapshot: () => CaptureThankYouOfferDialogSnapshot
  subscribe: (listener: () => void) => () => void
  open: () => "opened" | "noop"
  close: () => void
  selectStance: (stanceId: CaptureThankYouOfferStanceId) => Promise<"ok" | "failed" | "noop">
  patchCreateDraft: (patch: Partial<CampaignCatalogOfferDetailsDraft>) => void
  confirmCreate: () => Promise<ConfirmCatalogOfferWriteResult>
  setExistingSearchQuery: (query: string) => void
  selectExistingOffer: (offerId: number) => Promise<"ok" | "failed" | "noop">
  retryExistingPicker: () => void
  backToStances: () => void
}

function emptyAttached(): CaptureThankYouOfferFact {
  return { offerId: null, title: null, live: false }
}

function closedSnapshot(
  attached: CaptureThankYouOfferFact
): CaptureThankYouOfferDialogSnapshot {
  return {
    isOpen: false,
    panel: "stances",
    attached,
    createOfferDraft: emptyCampaignCatalogOfferDetailsDraft(),
    createOfferStatus: "idle",
    createOfferError: null,
    canConfirmCreateOffer: false,
    existingPickerLoadStatus: "idle",
    existingPickerError: null,
    existingPickerSearchQuery: "",
    existingPickerCards: [],
    existingPickerIsEmpty: false,
    existingPickerEmptyHelper: null,
  }
}

export function createCaptureThankYouOfferModule(
  adapters: CaptureThankYouOfferModuleAdapters
): CaptureThankYouOfferModule {
  type InternalState = {
    isOpen: boolean
    panel: CaptureThankYouOfferDialogPanel
    createOfferDraft: CampaignCatalogOfferDetailsDraft
    createOfferStatus: "idle" | "saving" | "error"
    createOfferError: string | null
    existingPickerLoadStatus: CampaignExistingOfferPickerLoadStatus
    existingPickerError: string | null
    existingPickerSearchQuery: string
    existingPickerItems: CatalogOffersListItem[]
  }

  let state: InternalState = {
    isOpen: false,
    panel: "stances",
    createOfferDraft: emptyCampaignCatalogOfferDetailsDraft(),
    createOfferStatus: "idle",
    createOfferError: null,
    existingPickerLoadStatus: "idle",
    existingPickerError: null,
    existingPickerSearchQuery: "",
    existingPickerItems: [],
  }

  let loadGeneration = 0
  const listeners = new Set<() => void>()

  const buildSnapshot = (): CaptureThankYouOfferDialogSnapshot => {
    const attached = adapters.getAttached()
    const filtered = filterExistingOfferPickerItems(
      state.existingPickerItems,
      state.existingPickerSearchQuery
    )
    const isEmpty =
      state.existingPickerLoadStatus === "ready"
      && state.existingPickerItems.length === 0
    return {
      isOpen: state.isOpen,
      panel: state.panel,
      attached,
      createOfferDraft: state.createOfferDraft,
      createOfferStatus: state.createOfferStatus,
      createOfferError: state.createOfferError,
      canConfirmCreateOffer: canConfirmCampaignCatalogOfferDetails(
        state.createOfferDraft
      ),
      existingPickerLoadStatus: state.existingPickerLoadStatus,
      existingPickerError: state.existingPickerError,
      existingPickerSearchQuery: state.existingPickerSearchQuery,
      existingPickerCards: filtered.map(mapCatalogOfferToExistingPickerCard),
      existingPickerIsEmpty: isEmpty,
      existingPickerEmptyHelper: isEmpty
        ? CAMPAIGN_EXISTING_OFFER_PICKER_COPY.emptyHelper
        : state.existingPickerLoadStatus === "ready"
          && filtered.length === 0
          && state.existingPickerSearchQuery.trim().length > 0
          ? CAMPAIGN_EXISTING_OFFER_PICKER_COPY.searchMissHelper
          : null,
    }
  }

  let snapshot = closedSnapshot(adapters.getAttached())

  const publish = () => {
    snapshot = buildSnapshot()
    for (const listener of listeners) {
      listener()
    }
  }

  const applyAttachResult = (result: {
    thankYouOfferId: number | null
    thankYouOfferTitle: string | null
    thankYouOfferLive: boolean
  }) => {
    adapters.setAttached({
      offerId: result.thankYouOfferId,
      title: result.thankYouOfferTitle,
      live: result.thankYouOfferLive,
    })
  }

  const loadExistingPicker = async () => {
    const locationId = adapters.locationId()
    const generation = ++loadGeneration
    if (locationId == null) {
      state = {
        ...state,
        existingPickerLoadStatus: "error",
        existingPickerError: CAMPAIGN_EXISTING_OFFER_PICKER_COPY.loadError,
        existingPickerItems: [],
      }
      publish()
      return
    }

    state = {
      ...state,
      existingPickerLoadStatus: "loading",
      existingPickerError: null,
    }
    publish()

    try {
      const response = await adapters.listCatalogOffers({
        locationId,
        view: "all",
        page: 1,
        pageSize: 25,
        status: ["active"],
      })
      if (generation !== loadGeneration) {
        return
      }
      const items = (response.items ?? []).filter(
        (item) => item.status === "active"
      )
      state = {
        ...state,
        existingPickerLoadStatus: "ready",
        existingPickerError: null,
        existingPickerItems: items,
      }
      publish()
    } catch {
      if (generation !== loadGeneration) {
        return
      }
      state = {
        ...state,
        existingPickerLoadStatus: "error",
        existingPickerError: CAMPAIGN_EXISTING_OFFER_PICKER_COPY.loadError,
        existingPickerItems: [],
      }
      publish()
    }
  }

  return {
    getSnapshot: () => snapshot,
    subscribe(listener) {
      listeners.add(listener)
      return () => {
        listeners.delete(listener)
      }
    },
    open() {
      if (adapters.locationId() == null) {
        return "noop"
      }
      state = {
        isOpen: true,
        panel: "stances",
        createOfferDraft: emptyCampaignCatalogOfferDetailsDraft(),
        createOfferStatus: "idle",
        createOfferError: null,
        existingPickerLoadStatus: "idle",
        existingPickerError: null,
        existingPickerSearchQuery: "",
        existingPickerItems: [],
      }
      publish()
      return "opened"
    },
    close() {
      state = {
        ...state,
        isOpen: false,
        panel: "stances",
        createOfferStatus: "idle",
        createOfferError: null,
      }
      publish()
    },
    async selectStance(stanceId) {
      if (!state.isOpen) {
        return "noop"
      }
      if (stanceId === "create-new-offer") {
        state = {
          ...state,
          panel: "create",
          createOfferDraft: emptyCampaignCatalogOfferDetailsDraft(),
          createOfferStatus: "idle",
          createOfferError: null,
        }
        publish()
        return "ok"
      }
      if (stanceId === "existing-offer") {
        state = {
          ...state,
          panel: "existing",
          existingPickerSearchQuery: "",
        }
        publish()
        void loadExistingPicker()
        return "ok"
      }

      const locationId = adapters.locationId()
      if (locationId == null) {
        return "noop"
      }
      try {
        const result = await adapters.putThankYouOffer(locationId, null)
        applyAttachResult(result)
        state = { ...state, isOpen: false, panel: "stances" }
        publish()
        adapters.onAttachSuccess?.(
          CAPTURE_THANK_YOU_OFFER_COPY.clearSuccessToast
        )
        return "ok"
      } catch {
        adapters.onAttachError?.(CAPTURE_THANK_YOU_OFFER_COPY.attachError)
        return "failed"
      }
    },
    patchCreateDraft(patch) {
      if (!state.isOpen || state.panel !== "create") {
        return
      }
      state = {
        ...state,
        createOfferDraft: mergeCampaignCatalogOfferDraftPatch(
          state.createOfferDraft,
          patch
        ),
        createOfferStatus:
          state.createOfferStatus === "error" ? "idle" : state.createOfferStatus,
        createOfferError:
          state.createOfferStatus === "error" ? null : state.createOfferError,
      }
      publish()
    },
    async confirmCreate() {
      const locationId = adapters.locationId()
      if (
        !state.isOpen
        || state.panel !== "create"
        || locationId == null
        || !canConfirmCampaignCatalogOfferDetails(state.createOfferDraft)
      ) {
        return "noop"
      }

      const body = toCreateCatalogOfferRequestBody({
        locationId,
        draft: state.createOfferDraft,
      })
      if (body == null) {
        return "noop"
      }

      state = {
        ...state,
        createOfferStatus: "saving",
        createOfferError: null,
      }
      publish()

      try {
        const created = await adapters.createCatalogOffer(body)
        try {
          const attached = await adapters.putThankYouOffer(
            locationId,
            created.id
          )
          applyAttachResult(attached)
          state = {
            ...state,
            isOpen: false,
            panel: "stances",
            createOfferStatus: "idle",
            createOfferError: null,
            createOfferDraft: emptyCampaignCatalogOfferDetailsDraft(),
          }
          publish()
          adapters.onAttachSuccess?.(
            CAPTURE_THANK_YOU_OFFER_COPY.attachSuccessToast
          )
          return "created"
        } catch {
          state = {
            ...state,
            createOfferStatus: "error",
            createOfferError:
              CAPTURE_THANK_YOU_OFFER_COPY.createThenAttachError,
          }
          publish()
          adapters.onAttachError?.(
            CAPTURE_THANK_YOU_OFFER_COPY.createThenAttachError
          )
          return "error"
        }
      } catch {
        state = {
          ...state,
          createOfferStatus: "error",
          createOfferError: CAPTURE_THANK_YOU_OFFER_COPY.attachError,
        }
        publish()
        adapters.onAttachError?.(CAPTURE_THANK_YOU_OFFER_COPY.attachError)
        return "error"
      }
    },
    setExistingSearchQuery(query) {
      if (!state.isOpen || state.panel !== "existing") {
        return
      }
      state = { ...state, existingPickerSearchQuery: query }
      publish()
    },
    async selectExistingOffer(offerId) {
      const locationId = adapters.locationId()
      if (!state.isOpen || state.panel !== "existing" || locationId == null) {
        return "noop"
      }
      try {
        const result = await adapters.putThankYouOffer(locationId, offerId)
        applyAttachResult(result)
        state = { ...state, isOpen: false, panel: "stances" }
        publish()
        adapters.onAttachSuccess?.(
          CAPTURE_THANK_YOU_OFFER_COPY.attachSuccessToast
        )
        return "ok"
      } catch {
        adapters.onAttachError?.(CAPTURE_THANK_YOU_OFFER_COPY.attachError)
        return "failed"
      }
    },
    retryExistingPicker() {
      if (!state.isOpen || state.panel !== "existing") {
        return
      }
      void loadExistingPicker()
    },
    backToStances() {
      if (!state.isOpen) {
        return
      }
      state = {
        ...state,
        panel: "stances",
        createOfferStatus: "idle",
        createOfferError: null,
      }
      publish()
    },
  }
}

export { emptyAttached as emptyCaptureThankYouOfferFact }
