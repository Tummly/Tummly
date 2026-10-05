import type { CreateCatalogOfferRequestBody } from "@/lib/operatorOffers/offerCatalogPresentation"
import {
  canConfirmCampaignCatalogOfferDetails,
  emptyCampaignCatalogOfferDetailsDraft,
  mergeCampaignCatalogOfferDraftPatch,
  toCreateCatalogOfferRequestBody,
  type CampaignCatalogOfferDetailsDraft,
  type CampaignCatalogOfferTypeId,
} from "@/lib/operatorOffers/offerCatalogPresentation"
import type { ConfirmCatalogOfferWriteResult } from "@/lib/operatorOffers/createEditOfferDrawerPresentation"
import { SHOP_OFFER_CARD_SETUP_COPY } from "@/lib/operatorShop/shopOfferCardSetupPresentation"
import type { ShopOfferCardOfferFact } from "@/api/shopOfferCardOfferApi"

export type ShopOfferCardSetupSnapshot = {
  isOpen: boolean
  loadStatus: "idle" | "loading" | "ready" | "error"
  hasAttach: boolean
  attached: ShopOfferCardOfferFact
  draft: CampaignCatalogOfferDetailsDraft
  saveStatus: "idle" | "saving" | "error"
  saveError: string | null
  canConfirm: boolean
}

export type ShopOfferCardSetupAdapters = {
  locationId: () => number | null
  getAttached: () => Promise<ShopOfferCardOfferFact>
  createCatalogOffer: (
    body: CreateCatalogOfferRequestBody
  ) => Promise<{ id: number; title: string }>
  putOfferCardOffer: (
    locationId: number,
    offerId: number
  ) => Promise<ShopOfferCardOfferFact>
  onError?: (message: string) => void
  onSuccess?: (message: string) => void
}

export type ShopOfferCardSetupModule = {
  getSnapshot: () => ShopOfferCardSetupSnapshot
  subscribe: (listener: () => void) => () => void
  loadForLocation: () => Promise<void>
  openIfNeeded: () => "opened" | "noop"
  open: () => "opened" | "noop"
  close: () => void
  patchDraft: (patch: Partial<CampaignCatalogOfferDetailsDraft>) => void
  setOfferType: (offerType: CampaignCatalogOfferTypeId | null) => void
  confirm: () => Promise<ConfirmCatalogOfferWriteResult>
}

function emptyAttached(): ShopOfferCardOfferFact {
  return { offerId: null, title: null, live: false }
}

function withSetupDefaults(
  draft: CampaignCatalogOfferDetailsDraft
): CampaignCatalogOfferDetailsDraft {
  const next = { ...draft }
  if (next.description.trim() === "") {
    next.description = SHOP_OFFER_CARD_SETUP_COPY.defaultDescription
  }
  if (
    next.offerType === "percentage_discount"
    && next.discountPercentage.trim() === ""
  ) {
    next.discountPercentage = "20"
  }
  if (
    next.offerType === "free_item"
    && next.purchaseRequirement == null
  ) {
    next.purchaseRequirement = "with_any_purchase"
  }
  return next
}

function initialDraft(): CampaignCatalogOfferDetailsDraft {
  return mergeCampaignCatalogOfferDraftPatch(
    emptyCampaignCatalogOfferDetailsDraft(),
    {
      offerType: null,
      title: SHOP_OFFER_CARD_SETUP_COPY.titlePlaceholder,
      titleTouched: true,
      description: SHOP_OFFER_CARD_SETUP_COPY.defaultDescription,
      discountPercentage: "20",
    }
  )
}

export function createShopOfferCardSetupModule(
  adapters: ShopOfferCardSetupAdapters
): ShopOfferCardSetupModule {
  type InternalState = {
    isOpen: boolean
    loadStatus: ShopOfferCardSetupSnapshot["loadStatus"]
    attached: ShopOfferCardOfferFact
    draft: CampaignCatalogOfferDetailsDraft
    saveStatus: ShopOfferCardSetupSnapshot["saveStatus"]
    saveError: string | null
  }

  let state: InternalState = {
    isOpen: false,
    loadStatus: "idle",
    attached: emptyAttached(),
    draft: initialDraft(),
    saveStatus: "idle",
    saveError: null,
  }

  const listeners = new Set<() => void>()
  let loadGeneration = 0

  const buildSnapshot = (): ShopOfferCardSetupSnapshot => {
    const draftForConfirm = withSetupDefaults(state.draft)
    return {
      isOpen: state.isOpen,
      loadStatus: state.loadStatus,
      hasAttach: state.attached.offerId != null,
      attached: state.attached,
      draft: state.draft,
      saveStatus: state.saveStatus,
      saveError: state.saveError,
      canConfirm: canConfirmCampaignCatalogOfferDetails(draftForConfirm),
    }
  }

  let snapshot = buildSnapshot()

  const publish = () => {
    snapshot = buildSnapshot()
    for (const listener of listeners) {
      listener()
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
    async loadForLocation() {
      const locationId = adapters.locationId()
      const generation = ++loadGeneration
      if (locationId == null) {
        state = {
          ...state,
          loadStatus: "error",
          attached: emptyAttached(),
          isOpen: false,
        }
        publish()
        return
      }

      state = {
        ...state,
        loadStatus: "loading",
        saveStatus: "idle",
        saveError: null,
      }
      publish()

      try {
        const attached = await adapters.getAttached()
        if (generation !== loadGeneration) {
          return
        }
        const hasAttach = attached.offerId != null
        state = {
          ...state,
          loadStatus: "ready",
          attached,
          isOpen: !hasAttach,
          draft: hasAttach ? state.draft : initialDraft(),
        }
        publish()
      } catch {
        if (generation !== loadGeneration) {
          return
        }
        state = {
          ...state,
          loadStatus: "error",
          attached: emptyAttached(),
          isOpen: false,
        }
        publish()
        adapters.onError?.(SHOP_OFFER_CARD_SETUP_COPY.loadError)
      }
    },
    openIfNeeded() {
      if (state.attached.offerId != null || adapters.locationId() == null) {
        return "noop"
      }
      state = {
        ...state,
        isOpen: true,
        draft: initialDraft(),
        saveStatus: "idle",
        saveError: null,
      }
      publish()
      return "opened"
    },
    open() {
      if (adapters.locationId() == null) {
        return "noop"
      }
      state = {
        ...state,
        isOpen: true,
        draft: initialDraft(),
        saveStatus: "idle",
        saveError: null,
      }
      publish()
      return "opened"
    },
    close() {
      state = {
        ...state,
        isOpen: false,
        saveStatus: "idle",
        saveError: null,
      }
      publish()
    },
    patchDraft(patch) {
      if (!state.isOpen) {
        return
      }
      state = {
        ...state,
        draft: mergeCampaignCatalogOfferDraftPatch(state.draft, {
          ...patch,
          titleTouched:
            patch.title != null ? true : (patch.titleTouched ?? state.draft.titleTouched),
        }),
        saveStatus: state.saveStatus === "error" ? "idle" : state.saveStatus,
        saveError: state.saveStatus === "error" ? null : state.saveError,
      }
      publish()
    },
    setOfferType(offerType) {
      if (!state.isOpen) {
        return
      }
      const patch: Partial<CampaignCatalogOfferDetailsDraft> = {
        offerType,
      }
      if (offerType === "percentage_discount") {
        patch.discountPercentage =
          state.draft.discountPercentage.trim() === ""
            ? "20"
            : state.draft.discountPercentage
      }
      if (offerType === "free_item" && state.draft.purchaseRequirement == null) {
        patch.purchaseRequirement = "with_any_purchase"
      }
      state = {
        ...state,
        draft: mergeCampaignCatalogOfferDraftPatch(state.draft, patch),
        saveStatus: state.saveStatus === "error" ? "idle" : state.saveStatus,
        saveError: state.saveStatus === "error" ? null : state.saveError,
      }
      publish()
    },
    async confirm() {
      const locationId = adapters.locationId()
      const draft = withSetupDefaults(state.draft)
      if (
        !state.isOpen
        || locationId == null
        || !canConfirmCampaignCatalogOfferDetails(draft)
      ) {
        return "noop"
      }

      const body = toCreateCatalogOfferRequestBody({
        locationId,
        draft,
      })
      if (body == null) {
        return "noop"
      }

      state = {
        ...state,
        draft,
        saveStatus: "saving",
        saveError: null,
      }
      publish()

      try {
        const created = await adapters.createCatalogOffer(body)
        try {
          const attached = await adapters.putOfferCardOffer(
            locationId,
            created.id
          )
          state = {
            ...state,
            isOpen: false,
            attached,
            saveStatus: "idle",
            saveError: null,
            draft: initialDraft(),
          }
          publish()
          adapters.onSuccess?.("Card offer saved.")
          return "created"
        } catch {
          state = {
            ...state,
            saveStatus: "error",
            saveError: SHOP_OFFER_CARD_SETUP_COPY.createThenAttachError,
          }
          publish()
          adapters.onError?.(SHOP_OFFER_CARD_SETUP_COPY.createThenAttachError)
          return "error"
        }
      } catch {
        state = {
          ...state,
          saveStatus: "error",
          saveError: SHOP_OFFER_CARD_SETUP_COPY.createError,
        }
        publish()
        adapters.onError?.(SHOP_OFFER_CARD_SETUP_COPY.createError)
        return "error"
      }
    },
  }
}
