import {
  channelHasContact,
  formatMarketingPreferenceRecordedOn,
  isMarketingPreferenceSaveDirty,
  MANAGE_MARKETING_PREFERENCES_COPY,
  MARKETING_PREFERENCE_STATUS_CARDS,
  marketingPreferenceConsequenceHelper,
  marketingPreferencePermissionSource,
  operatorMaySelectMarketingPreference,
  resolveMarketingPreferenceConsentAt,
} from "@/lib/operatorGuests/manageMarketingPreferencesPresentation"
import type {
  GuestProfileResponse,
  LocationGuestMarketingPreference,
  PatchGuestMarketingPreferenceRequest,
  PatchGuestMarketingPreferenceResponse,
} from "@/types/dashboard"

export type ManageMarketingPreferencesAdapters = {
  getGuestProfile: (params: {
    guestId: number
    locationId: number
  }) => Promise<GuestProfileResponse>
  patchMarketingPreference: (params: {
    guestId: number
    locationId: number
    body: PatchGuestMarketingPreferenceRequest
  }) => Promise<PatchGuestMarketingPreferenceResponse>
}

export type ManageMarketingPreferencesStatusCard = {
  id: LocationGuestMarketingPreference
  label: string
  helper: string
  selected: boolean
  disabled: boolean
}

export type ManageMarketingPreferencesSnapshot = {
  isOpen: boolean
  loadStatus: "idle" | "loading" | "loaded" | "error"
  loadError: string | null
  guestId: number | null
  locationId: number | null
  guestName: string
  subtitle: string
  currentPreference: LocationGuestMarketingPreference | null
  draftPreference: LocationGuestMarketingPreference | null
  draftNote: string
  emailAvailable: boolean
  smsAvailable: boolean
  permissionSourceDisplay: string
  recordedOnDisplay: string
  consequenceHelper: string | null
  statusCards: readonly ManageMarketingPreferencesStatusCard[]
  canSave: boolean
  saveStatus: "idle" | "saving" | "error"
  saveError: string | null
}

export type ManageMarketingPreferencesSaveResult =
  | { status: "saved" }
  | { status: "saved_with_note_error"; message: string }
  | { status: "error"; message: string }
  | { status: "noop" }

export type ManageMarketingPreferencesSessionModule = {
  subscribe: (listener: () => void) => () => void
  getSnapshot: () => ManageMarketingPreferencesSnapshot
  openFromLoaded: (profile: GuestProfileResponse) => void
  openFromList: (params: {
    guestId: number
    locationId: number
  }) => Promise<void>
  close: () => void
  setDraftPreference: (preference: LocationGuestMarketingPreference) => void
  setDraftNote: (note: string) => void
  save: () => Promise<ManageMarketingPreferencesSaveResult>
}

type SessionState = {
  isOpen: boolean
  loadStatus: ManageMarketingPreferencesSnapshot["loadStatus"]
  loadError: string | null
  loadGeneration: number
  guestId: number | null
  locationId: number | null
  guestName: string
  currentPreference: LocationGuestMarketingPreference | null
  draftPreference: LocationGuestMarketingPreference | null
  draftNote: string
  emailAvailable: boolean
  smsAvailable: boolean
  consentAt: string | null
  saveStatus: ManageMarketingPreferencesSnapshot["saveStatus"]
  saveError: string | null
  saveGeneration: number
}

const closedState = (): SessionState => ({
  isOpen: false,
  loadStatus: "idle",
  loadError: null,
  loadGeneration: 0,
  guestId: null,
  locationId: null,
  guestName: "",
  currentPreference: null,
  draftPreference: null,
  draftNote: "",
  emailAvailable: false,
  smsAvailable: false,
  consentAt: null,
  saveStatus: "idle",
  saveError: null,
  saveGeneration: 0,
})

function hydrateFromProfile(profile: GuestProfileResponse): Partial<SessionState> {
  const consentAt = resolveMarketingPreferenceConsentAt(
    profile.contactEligibility
  )
  return {
    isOpen: true,
    loadStatus: "loaded",
    loadError: null,
    guestId: profile.id,
    locationId: profile.locationId,
    guestName: profile.name,
    currentPreference: profile.marketingPreference,
    draftPreference: profile.marketingPreference,
    draftNote: "",
    emailAvailable: channelHasContact(profile.profileSummary.email),
    smsAvailable: channelHasContact(profile.profileSummary.mobile),
    consentAt,
    saveStatus: "idle",
    saveError: null,
  }
}

function toSnapshot(state: SessionState): ManageMarketingPreferencesSnapshot {
  const current = state.currentPreference
  const draft = state.draftPreference
  const canSave =
    state.loadStatus === "loaded" &&
    current != null &&
    draft != null &&
    state.saveStatus !== "saving" &&
    isMarketingPreferenceSaveDirty(current, draft, state.draftNote)

  return {
    isOpen: state.isOpen,
    loadStatus: state.loadStatus,
    loadError: state.loadError,
    guestId: state.guestId,
    locationId: state.locationId,
    guestName: state.guestName,
    subtitle: MANAGE_MARKETING_PREFERENCES_COPY.subtitle(state.guestName),
    currentPreference: current,
    draftPreference: draft,
    draftNote: state.draftNote,
    emailAvailable: state.emailAvailable,
    smsAvailable: state.smsAvailable,
    permissionSourceDisplay: marketingPreferencePermissionSource(
      state.consentAt
    ),
    recordedOnDisplay: formatMarketingPreferenceRecordedOn(state.consentAt),
    consequenceHelper:
      draft == null ? null : marketingPreferenceConsequenceHelper(draft),
    statusCards: MARKETING_PREFERENCE_STATUS_CARDS.map((card) => ({
      id: card.id,
      label: card.label,
      helper: card.helper,
      selected: draft === card.id,
      disabled:
        current == null ||
        !operatorMaySelectMarketingPreference(current, card.id),
    })),
    canSave,
    saveStatus: state.saveStatus,
    saveError: state.saveError,
  }
}

export function createManageMarketingPreferencesSessionModule(
  adapters: ManageMarketingPreferencesAdapters
): ManageMarketingPreferencesSessionModule {
  let state = closedState()
  let snapshot = toSnapshot(state)
  const listeners = new Set<() => void>()

  const publish = () => {
    snapshot = toSnapshot(state)
    for (const listener of listeners) {
      listener()
    }
  }

  const setState = (patch: Partial<SessionState>) => {
    state = { ...state, ...patch }
    publish()
  }

  const loadFromList = async (guestId: number, locationId: number) => {
    const generation = state.loadGeneration + 1
    setState({
      ...closedState(),
      isOpen: true,
      loadStatus: "loading",
      loadGeneration: generation,
      guestId,
      locationId,
    })

    try {
      const profile = await adapters.getGuestProfile({ guestId, locationId })
      if (generation !== state.loadGeneration) {
        return
      }
      setState({
        ...hydrateFromProfile(profile),
        loadGeneration: generation,
      })
    } catch {
      if (generation !== state.loadGeneration) {
        return
      }
      setState({
        loadStatus: "error",
        loadError: MANAGE_MARKETING_PREFERENCES_COPY.loadError,
      })
    }
  }

  return {
    subscribe: (listener) => {
      listeners.add(listener)
      return () => {
        listeners.delete(listener)
      }
    },
    getSnapshot: () => snapshot,
    openFromLoaded: (profile) => {
      setState({
        ...hydrateFromProfile(profile),
        loadGeneration: state.loadGeneration + 1,
      })
    },
    openFromList: ({ guestId, locationId }) => loadFromList(guestId, locationId),
    close: () => {
      setState({
        ...closedState(),
        loadGeneration: state.loadGeneration + 1,
        saveGeneration: state.saveGeneration + 1,
      })
    },
    setDraftPreference: (preference) => {
      if (state.currentPreference == null) {
        return
      }
      if (
        !operatorMaySelectMarketingPreference(
          state.currentPreference,
          preference
        )
      ) {
        return
      }
      setState({
        draftPreference: preference,
        saveError: state.saveStatus === "error" ? null : state.saveError,
        saveStatus: state.saveStatus === "error" ? "idle" : state.saveStatus,
      })
    },
    setDraftNote: (note) => {
      setState({
        draftNote: note,
        saveError: state.saveStatus === "error" ? null : state.saveError,
        saveStatus: state.saveStatus === "error" ? "idle" : state.saveStatus,
      })
    },
    save: async () => {
      const current = state.currentPreference
      const draft = state.draftPreference
      if (
        !state.isOpen ||
        state.loadStatus !== "loaded" ||
        current == null ||
        draft == null ||
        state.guestId == null ||
        state.locationId == null ||
        state.saveStatus === "saving" ||
        !isMarketingPreferenceSaveDirty(current, draft, state.draftNote)
      ) {
        return { status: "noop" }
      }

      const guestId = state.guestId
      const locationId = state.locationId
      const note = state.draftNote.trim()
      const generation = state.saveGeneration + 1
      setState({
        saveGeneration: generation,
        saveStatus: "saving",
        saveError: null,
      })

      try {
        const result = await adapters.patchMarketingPreference({
          guestId,
          locationId,
          body:
            note.length > 0
              ? { preference: draft, note }
              : { preference: draft },
        })
        const noteError = (result.noteError ?? "").trim()
        const noteFailed = noteError.length > 0

        if (generation !== state.saveGeneration) {
          if (noteFailed && result.preferenceChanged) {
            return {
              status: "saved_with_note_error",
              message: noteError,
            }
          }
          if (noteFailed) {
            return { status: "error", message: noteError }
          }
          return { status: "saved" }
        }

        if (noteFailed && !result.preferenceChanged) {
          setState({
            saveStatus: "error",
            saveError: noteError,
          })
          return { status: "error", message: noteError }
        }

        setState({
          ...closedState(),
          loadGeneration: state.loadGeneration + 1,
          saveGeneration: generation,
        })

        if (noteFailed) {
          return {
            status: "saved_with_note_error",
            message: noteError,
          }
        }
        return { status: "saved" }
      } catch {
        if (generation !== state.saveGeneration) {
          return { status: "noop" }
        }
        setState({
          saveStatus: "error",
          saveError: MANAGE_MARKETING_PREFERENCES_COPY.saveError,
        })
        return {
          status: "error",
          message: MANAGE_MARKETING_PREFERENCES_COPY.saveError,
        }
      }
    },
  }
}
