import {
  ACCOUNT_WORKSPACE_PAGE_COPY,
  ACCOUNT_WORKSPACE_TAB_IDS,
  ACCOUNT_WORKSPACE_TAB_LABELS,
  isAccountWorkspaceFormTab,
  resolveAccountWorkspaceTabId,
  type AccountWorkspaceTabId,
} from "@/lib/operatorAccountWorkspace/accountWorkspacePresentation"

export {
  ACCOUNT_WORKSPACE_TAB_IDS,
  resolveAccountWorkspaceTabId,
} from "@/lib/operatorAccountWorkspace/accountWorkspacePresentation"

export type AccountWorkspaceStatus = {
  workspaceStatus: string
  planStatus: string
  billingStatus: string
  accountCreatedAt: string
  activeLocations: number
  teamMembers: number
  guestProfiles: number
  guestFormStatus: string
  lastAccountUpdateAt: string
}

export type AccountWorkspaceDetails = {
  workspaceName: string
  accountStructure: string
  businessCategory: string | null
  businessCategoryLabel: string | null
  mainOperatingCountry: string
  brandLogoOperatorUrl: string | null
  brandLogoPublicUrl: string | null
  lastSavedAt: string | null
  status: AccountWorkspaceStatus
}

export type AccountWorkspaceToast = {
  kind: "success" | "error"
  message: string
} | null

export type OperatorAccountWorkspacePageAdapters = {
  getDetails: () => Promise<AccountWorkspaceDetails>
  updateAccountDetails: (params: {
    name: string
    logo: File | null
  }) => Promise<AccountWorkspaceDetails>
}

export type OperatorAccountWorkspacePageOptions = {
  initialTabId?: string | null
}

type PendingLeave =
  | { kind: "tab"; tabId: AccountWorkspaceTabId }
  | { kind: "href"; href: string }
  | null

type AccountDetailsDraft = {
  workspaceName: string
  stagedLogo: File | null
  stagedLogoPreviewUrl: string | null
}

export type OperatorAccountWorkspacePageSnapshot = {
  loadStatus: "idle" | "loading" | "loaded" | "error"
  loadError: string | null
  activeTabId: AccountWorkspaceTabId
  tabs: ReadonlyArray<{ id: AccountWorkspaceTabId; label: string }>
  lastSavedAt: string | null
  isSaving: boolean
  isDirty: boolean
  saveEnabled: boolean
  accountDetails: {
    workspaceName: string
    accountStructure: string
    businessCategory: string | null
    businessCategoryLabel: string | null
    mainOperatingCountry: string
    brandLogoPreviewUrl: string | null
    status: AccountWorkspaceStatus | null
  }
  renameConfirmOpen: boolean
  leaveDirtyOpen: boolean
  pendingNavigationHref: string | null
  toast: AccountWorkspaceToast
}

export type OperatorAccountWorkspacePageModule = {
  getSnapshot: () => OperatorAccountWorkspacePageSnapshot
  subscribe: (listener: () => void) => () => void
  load: () => Promise<void>
  setActiveTabFromUrl: (raw: string | null | undefined) => void
  setWorkspaceName: (name: string) => void
  stageBrandLogo: (file: File | null) => void
  requestSave: () => Promise<void>
  confirmRename: () => Promise<void>
  cancelRenameConfirm: () => void
  closeRenameConfirm: () => void
  requestTabChange: (tabId: AccountWorkspaceTabId) => void
  requestNavigateAway: (href: string) => boolean
  confirmLeaveDirtySave: () => Promise<void>
  confirmLeaveDirtyCancel: () => Promise<void>
  closeLeaveDirty: () => void
  clearToast: () => void
  consumePendingNavigation: () => string | null
}

function emptyStatus(): AccountWorkspaceStatus {
  return {
    workspaceStatus: "Active",
    planStatus: "Pilot",
    billingStatus: "Active",
    accountCreatedAt: "",
    activeLocations: 0,
    teamMembers: 1,
    guestProfiles: 0,
    guestFormStatus: "Live",
    lastAccountUpdateAt: "",
  }
}

export function createOperatorAccountWorkspacePageModule(
  adapters: OperatorAccountWorkspacePageAdapters,
  options: OperatorAccountWorkspacePageOptions = {}
): OperatorAccountWorkspacePageModule {
  let loadStatus: OperatorAccountWorkspacePageSnapshot["loadStatus"] = "idle"
  let loadError: string | null = null
  let activeTabId = resolveAccountWorkspaceTabId(options.initialTabId)
  let persisted: AccountWorkspaceDetails | null = null
  let lastSavedAt: string | null = null
  let isSaving = false
  let renameConfirmOpen = false
  let leaveDirtyOpen = false
  let pendingLeave: PendingLeave = null
  let pendingNavigationHref: string | null = null
  let toast: AccountWorkspaceToast = null
  let draft: AccountDetailsDraft = {
    workspaceName: "",
    stagedLogo: null,
    stagedLogoPreviewUrl: null,
  }

  const listeners = new Set<() => void>()

  function emit() {
    for (const listener of listeners) {
      listener()
    }
  }

  function revokePreview() {
    if (draft.stagedLogoPreviewUrl?.startsWith("blob:")) {
      URL.revokeObjectURL(draft.stagedLogoPreviewUrl)
    }
  }

  function resetDraftFromPersisted() {
    revokePreview()
    draft = {
      workspaceName: persisted?.workspaceName ?? "",
      stagedLogo: null,
      stagedLogoPreviewUrl: null,
    }
  }

  function applyPersisted(details: AccountWorkspaceDetails) {
    persisted = details
    lastSavedAt = details.lastSavedAt
    resetDraftFromPersisted()
  }

  function isAccountDetailsDirty(): boolean {
    if (persisted == null) {
      return false
    }
    const nameChanged =
      draft.workspaceName.trim() !== persisted.workspaceName.trim()
    return nameChanged || draft.stagedLogo != null
  }

  function activeTabDirty(): boolean {
    if (!isAccountWorkspaceFormTab(activeTabId)) {
      return false
    }
    if (activeTabId === "account-details") {
      return isAccountDetailsDirty()
    }
    // Later tickets own other form tabs.
    return false
  }

  function nameChanged(): boolean {
    if (persisted == null) {
      return false
    }
    return draft.workspaceName.trim() !== persisted.workspaceName.trim()
  }

  function getSnapshot(): OperatorAccountWorkspacePageSnapshot {
    const dirty = activeTabDirty()
    const onControls = activeTabId === "account-controls"
    return {
      loadStatus,
      loadError,
      activeTabId,
      tabs: ACCOUNT_WORKSPACE_TAB_IDS.map((id) => ({
        id,
        label: ACCOUNT_WORKSPACE_TAB_LABELS[id],
      })),
      lastSavedAt,
      isSaving,
      isDirty: dirty,
      saveEnabled: !onControls && dirty && !isSaving,
      accountDetails: {
        workspaceName: draft.workspaceName,
        accountStructure: persisted?.accountStructure ?? "",
        businessCategory: persisted?.businessCategory ?? null,
        businessCategoryLabel: persisted?.businessCategoryLabel ?? null,
        mainOperatingCountry:
          persisted?.mainOperatingCountry ?? "United Kingdom",
        brandLogoPreviewUrl:
          draft.stagedLogoPreviewUrl
          ?? persisted?.brandLogoPublicUrl
          ?? null,
        status: persisted?.status ?? null,
      },
      renameConfirmOpen,
      leaveDirtyOpen,
      pendingNavigationHref,
      toast,
    }
  }

  async function persistAccountDetails(): Promise<boolean> {
    if (persisted == null) {
      return false
    }

    const name = draft.workspaceName.trim()
    if (name.length === 0 || name.length > 200) {
      toast = {
        kind: "error",
        message: ACCOUNT_WORKSPACE_PAGE_COPY.saveError,
      }
      emit()
      return false
    }

    isSaving = true
    toast = null
    emit()

    try {
      const result = await adapters.updateAccountDetails({
        name,
        logo: draft.stagedLogo,
      })
      applyPersisted(result)
      toast = {
        kind: "success",
        message: ACCOUNT_WORKSPACE_PAGE_COPY.saveSuccess,
      }
      isSaving = false
      emit()
      return true
    } catch {
      toast = {
        kind: "error",
        message: ACCOUNT_WORKSPACE_PAGE_COPY.saveError,
      }
      isSaving = false
      emit()
      return false
    }
  }

  async function runSaveFlow(): Promise<boolean> {
    if (!activeTabDirty() || isSaving) {
      return false
    }

    if (activeTabId === "account-details" && nameChanged()) {
      renameConfirmOpen = true
      emit()
      return false
    }

    return persistAccountDetails()
  }

  function continuePendingLeave() {
    if (pendingLeave == null) {
      return
    }

    if (pendingLeave.kind === "tab") {
      activeTabId = pendingLeave.tabId
    } else {
      pendingNavigationHref = pendingLeave.href
    }
    pendingLeave = null
  }

  return {
    getSnapshot,
    subscribe(listener) {
      listeners.add(listener)
      return () => {
        listeners.delete(listener)
      }
    },

    async load() {
      loadStatus = "loading"
      loadError = null
      emit()
      try {
        const details = await adapters.getDetails()
        applyPersisted(details)
        loadStatus = "loaded"
        emit()
      } catch {
        loadStatus = "error"
        loadError = "Could not load Account & workspace."
        emit()
      }
    },

    setActiveTabFromUrl(raw) {
      const next = resolveAccountWorkspaceTabId(raw)
      if (next === activeTabId) {
        return
      }
      if (activeTabDirty()) {
        return
      }
      activeTabId = next
      emit()
    },

    setWorkspaceName(name) {
      draft = { ...draft, workspaceName: name }
      emit()
    },

    stageBrandLogo(file) {
      revokePreview()
      draft = {
        ...draft,
        stagedLogo: file,
        stagedLogoPreviewUrl:
          file != null ? URL.createObjectURL(file) : null,
      }
      emit()
    },

    async requestSave() {
      await runSaveFlow()
    },

    async confirmRename() {
      if (!renameConfirmOpen) {
        return
      }
      renameConfirmOpen = false
      emit()
      const ok = await persistAccountDetails()
      if (ok) {
        continuePendingLeave()
        emit()
      }
    },

    cancelRenameConfirm() {
      renameConfirmOpen = false
      // Abort whole save; keep draft; do not continue leave.
      pendingLeave = null
      emit()
    },

    closeRenameConfirm() {
      renameConfirmOpen = false
      pendingLeave = null
      emit()
    },

    requestTabChange(tabId) {
      const next = resolveAccountWorkspaceTabId(tabId)
      if (next === activeTabId) {
        return
      }
      if (activeTabDirty()) {
        pendingLeave = { kind: "tab", tabId: next }
        leaveDirtyOpen = true
        emit()
        return
      }
      activeTabId = next
      emit()
    },

    requestNavigateAway(href) {
      if (!activeTabDirty()) {
        return true
      }
      pendingLeave = { kind: "href", href }
      leaveDirtyOpen = true
      emit()
      return false
    },

    async confirmLeaveDirtySave() {
      if (!leaveDirtyOpen) {
        return
      }
      leaveDirtyOpen = false
      emit()

      if (activeTabId === "account-details" && nameChanged()) {
        renameConfirmOpen = true
        emit()
        return
      }

      const ok = await persistAccountDetails()
      if (ok) {
        continuePendingLeave()
      } else {
        pendingLeave = null
      }
      emit()
    },

    async confirmLeaveDirtyCancel() {
      if (!leaveDirtyOpen) {
        return
      }
      leaveDirtyOpen = false
      resetDraftFromPersisted()
      continuePendingLeave()
      emit()
    },

    closeLeaveDirty() {
      leaveDirtyOpen = false
      pendingLeave = null
      emit()
    },

    clearToast() {
      toast = null
      emit()
    },

    consumePendingNavigation() {
      const href = pendingNavigationHref
      pendingNavigationHref = null
      return href
    },
  }
}
