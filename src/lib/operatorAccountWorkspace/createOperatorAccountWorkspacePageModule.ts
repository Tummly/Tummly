import { isValidUkPostcode } from "@/lib/addressLookup"
import {
  ACCOUNT_WORKSPACE_DEFAULT_COUNTRY,
  ACCOUNT_WORKSPACE_PAGE_COPY,
  ACCOUNT_WORKSPACE_TAB_IDS,
  ACCOUNT_WORKSPACE_TAB_LABELS,
  defaultAccountWorkspaceCountry,
  isAccountWorkspaceFormTab,
  isUnitedKingdomCountry,
  resolveAccountWorkspaceTabId,
  type AccountWorkspaceTabId,
  type DefaultReportingPeriodValue,
  type LegalStructureValue,
  type WeekStartsOnValue,
} from "@/lib/operatorAccountWorkspace/accountWorkspacePresentation"
import { bumpRecommendedNextStepSoftCaches } from "@/lib/operatorRecommendations/recommendationSoftCacheBust"

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

export type AccountWorkspaceBusinessDetails = {
  legalStructure: LegalStructureValue | ""
  legalBusinessName: string
  tradingName: string
  companyNumber: string
  vatNumber: string
  countryOfRegistration: string
  addressLine1: string
  addressLine2: string
  townCity: string
  county: string
  postcode: string
  country: string
}

export type TeamMemberPickerItem = {
  userId: number
  fullName: string
  email: string
}

export type AccountWorkspaceKeyContacts = {
  accountOwner: TeamMemberPickerItem
  billingContactUserId: number
  privacyContactUserId: number
  supportContactUserId: number
  eligibleMembers: TeamMemberPickerItem[]
}

export type AccountWorkspaceWorkspaceDefaults = {
  weekStartsOn: WeekStartsOnValue
  defaultReportingPeriod: DefaultReportingPeriodValue
  defaultCampaignSenderName: string
  defaultTimezone: string
  defaultCurrency: string
  defaultLanguage: string
  dateFormat: string
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
  businessDetails: AccountWorkspaceBusinessDetails
  keyContacts: AccountWorkspaceKeyContacts
  workspaceDefaults: AccountWorkspaceWorkspaceDefaults
}

export type UpdateBusinessDetailsPayload = {
  legalStructure: LegalStructureValue | ""
  legalBusinessName: string
  tradingName: string
  sameAsLegalBusinessName: boolean
  companyNumber: string
  vatNumber: string
  countryOfRegistration: string
  addressLine1: string
  addressLine2: string
  townCity: string
  county: string
  postcode: string
  country: string
}

export type UpdateKeyContactsPayload = {
  billingContactUserId: number
  privacyContactUserId: number
  supportContactUserId: number
}

export type UpdateWorkspaceDefaultsPayload = {
  weekStartsOn: WeekStartsOnValue
  defaultReportingPeriod: DefaultReportingPeriodValue
  defaultCampaignSenderName: string
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
  updateBusinessDetails: (
    payload: UpdateBusinessDetailsPayload
  ) => Promise<AccountWorkspaceDetails>
  updateKeyContacts: (
    payload: UpdateKeyContactsPayload
  ) => Promise<AccountWorkspaceDetails>
  updateWorkspaceDefaults: (
    payload: UpdateWorkspaceDefaultsPayload
  ) => Promise<AccountWorkspaceDetails>
  /** Refresh shell readers of Restaurant.Name / Brand logo after persist. */
  onIdentityPersisted?: (details: AccountWorkspaceDetails) => void
  /** Bust Home / Campaigns Recommended next step soft caches after period save. */
  onWorkspaceDefaultsPersisted?: (details: AccountWorkspaceDetails) => void
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

type BusinessDetailsDraft = AccountWorkspaceBusinessDetails & {
  sameAsLegalBusinessName: boolean
}

type KeyContactsDraft = {
  billingContactUserId: number
  privacyContactUserId: number
  supportContactUserId: number
}

type WorkspaceDefaultsDraft = {
  weekStartsOn: WeekStartsOnValue
  defaultReportingPeriod: DefaultReportingPeriodValue
  defaultCampaignSenderName: string
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
    workspaceNameError: string | null
    accountStructure: string
    businessCategory: string | null
    businessCategoryLabel: string | null
    mainOperatingCountry: string
    brandLogoPreviewUrl: string | null
    status: AccountWorkspaceStatus | null
  }
  businessDetails: BusinessDetailsDraft & {
    postcodeError: string | null
  }
  keyContacts: KeyContactsDraft & {
    accountOwner: TeamMemberPickerItem | null
    eligibleMembers: TeamMemberPickerItem[]
  }
  workspaceDefaults: WorkspaceDefaultsDraft & {
    defaultTimezone: string
    defaultCurrency: string
    defaultLanguage: string
    dateFormat: string
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
  setLegalStructure: (value: string) => void
  setLegalBusinessName: (value: string) => void
  setTradingName: (value: string) => void
  setSameAsLegalBusinessName: (checked: boolean) => void
  setCompanyNumber: (value: string) => void
  setVatNumber: (value: string) => void
  setCountryOfRegistration: (value: string) => void
  setAddressLine1: (value: string) => void
  setAddressLine2: (value: string) => void
  setTownCity: (value: string) => void
  setCounty: (value: string) => void
  setPostcode: (value: string) => void
  setCountry: (value: string) => void
  setBillingContactUserId: (userId: number) => void
  setPrivacyContactUserId: (userId: number) => void
  setSupportContactUserId: (userId: number) => void
  setWeekStartsOn: (value: WeekStartsOnValue) => void
  setDefaultReportingPeriod: (value: DefaultReportingPeriodValue) => void
  setDefaultCampaignSenderName: (value: string) => void
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

const WORKSPACE_NAME_REQUIRED_ERROR = "Workspace name is required."
const WORKSPACE_NAME_MAX_ERROR = "Workspace name must be 200 characters or fewer."

function emptyBusinessDetails(): AccountWorkspaceBusinessDetails {
  return {
    legalStructure: "",
    legalBusinessName: "",
    tradingName: "",
    companyNumber: "",
    vatNumber: "",
    countryOfRegistration: ACCOUNT_WORKSPACE_DEFAULT_COUNTRY,
    addressLine1: "",
    addressLine2: "",
    townCity: "",
    county: "",
    postcode: "",
    country: ACCOUNT_WORKSPACE_DEFAULT_COUNTRY,
  }
}

function emptyKeyContacts(): AccountWorkspaceKeyContacts {
  return {
    accountOwner: { userId: 0, fullName: "", email: "" },
    billingContactUserId: 0,
    privacyContactUserId: 0,
    supportContactUserId: 0,
    eligibleMembers: [],
  }
}

function emptyWorkspaceDefaults(): AccountWorkspaceWorkspaceDefaults {
  return {
    weekStartsOn: "monday",
    defaultReportingPeriod: "7days",
    defaultCampaignSenderName: "",
    defaultTimezone: "Europe/London",
    defaultCurrency: "GBP",
    defaultLanguage: "English",
    dateFormat: "DD/MM/YYYY",
  }
}

function normalizeWeekStartsOn(value: string | null | undefined): WeekStartsOnValue {
  const allowed: WeekStartsOnValue[] = [
    "monday",
    "tuesday",
    "wednesday",
    "thursday",
    "friday",
    "saturday",
    "sunday",
  ]
  const trimmed = (value ?? "").trim().toLowerCase()
  return (allowed.find((item) => item === trimmed) ?? "monday") as WeekStartsOnValue
}

function normalizeReportingPeriod(
  value: string | null | undefined
): DefaultReportingPeriodValue {
  const allowed: DefaultReportingPeriodValue[] = [
    "7days",
    "30days",
    "thisMonth",
  ]
  const trimmed = (value ?? "").trim()
  return (
    allowed.find((item) => item.toLowerCase() === trimmed.toLowerCase())
    ?? "7days"
  )
}

function normalizeWorkspaceDefaults(
  details: AccountWorkspaceWorkspaceDefaults | null | undefined
): AccountWorkspaceWorkspaceDefaults {
  const base = details ?? emptyWorkspaceDefaults()
  return {
    weekStartsOn: normalizeWeekStartsOn(base.weekStartsOn),
    defaultReportingPeriod: normalizeReportingPeriod(base.defaultReportingPeriod),
    defaultCampaignSenderName: (base.defaultCampaignSenderName ?? "").trim(),
    defaultTimezone: base.defaultTimezone || "Europe/London",
    defaultCurrency: base.defaultCurrency || "GBP",
    defaultLanguage: base.defaultLanguage || "English",
    dateFormat: base.dateFormat || "DD/MM/YYYY",
  }
}

function normalizeBusinessDetails(
  details: AccountWorkspaceBusinessDetails | null | undefined
): AccountWorkspaceBusinessDetails {
  const base = details ?? emptyBusinessDetails()
  return {
    legalStructure: base.legalStructure ?? "",
    legalBusinessName: base.legalBusinessName ?? "",
    tradingName: base.tradingName ?? "",
    companyNumber: base.companyNumber ?? "",
    vatNumber: base.vatNumber ?? "",
    countryOfRegistration: defaultAccountWorkspaceCountry(
      base.countryOfRegistration
    ),
    addressLine1: base.addressLine1 ?? "",
    addressLine2: base.addressLine2 ?? "",
    townCity: base.townCity ?? "",
    county: base.county ?? "",
    postcode: base.postcode ?? "",
    country: defaultAccountWorkspaceCountry(base.country),
  }
}

function normalizeKeyContacts(
  details: AccountWorkspaceKeyContacts | null | undefined
): AccountWorkspaceKeyContacts {
  const base = details ?? emptyKeyContacts()
  const owner = base.accountOwner ?? emptyKeyContacts().accountOwner
  const ownerId = owner.userId
  return {
    accountOwner: {
      userId: owner.userId,
      fullName: owner.fullName ?? "",
      email: owner.email ?? "",
    },
    billingContactUserId: base.billingContactUserId || ownerId,
    privacyContactUserId: base.privacyContactUserId || ownerId,
    supportContactUserId: base.supportContactUserId || ownerId,
    eligibleMembers: (base.eligibleMembers ?? []).map((member) => ({
      userId: member.userId,
      fullName: member.fullName ?? "",
      email: member.email ?? "",
    })),
  }
}

function deriveSameAsLegalBusinessName(
  details: AccountWorkspaceBusinessDetails
): boolean {
  const legal = details.legalBusinessName.trim()
  const trading = details.tradingName.trim()
  return legal !== "" && legal === trading
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
  let workspaceNameError: string | null = null
  let postcodeError: string | null = null
  let draft: AccountDetailsDraft = {
    workspaceName: "",
    stagedLogo: null,
    stagedLogoPreviewUrl: null,
  }
  let businessDraft: BusinessDetailsDraft = {
    ...emptyBusinessDetails(),
    sameAsLegalBusinessName: false,
  }
  let keyContactsDraft: KeyContactsDraft = {
    billingContactUserId: 0,
    privacyContactUserId: 0,
    supportContactUserId: 0,
  }
  let workspaceDefaultsDraft: WorkspaceDefaultsDraft = {
    weekStartsOn: "monday",
    defaultReportingPeriod: "7days",
    defaultCampaignSenderName: "",
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
    const business = normalizeBusinessDetails(persisted?.businessDetails)
    businessDraft = {
      ...business,
      sameAsLegalBusinessName: deriveSameAsLegalBusinessName(business),
    }
    const contacts = normalizeKeyContacts(persisted?.keyContacts)
    keyContactsDraft = {
      billingContactUserId: contacts.billingContactUserId,
      privacyContactUserId: contacts.privacyContactUserId,
      supportContactUserId: contacts.supportContactUserId,
    }
    const defaults = normalizeWorkspaceDefaults(persisted?.workspaceDefaults)
    workspaceDefaultsDraft = {
      weekStartsOn: defaults.weekStartsOn,
      defaultReportingPeriod: defaults.defaultReportingPeriod,
      defaultCampaignSenderName: defaults.defaultCampaignSenderName,
    }
    postcodeError = null
  }

  function applyPersisted(details: AccountWorkspaceDetails) {
    persisted = {
      ...details,
      businessDetails: normalizeBusinessDetails(details.businessDetails),
      keyContacts: normalizeKeyContacts(details.keyContacts),
      workspaceDefaults: normalizeWorkspaceDefaults(details.workspaceDefaults),
    }
    // Header Last saved shares the form-save clock; CreatedAt until first save.
    lastSavedAt =
      details.lastSavedAt ?? details.status.lastAccountUpdateAt
    workspaceNameError = null
    resetDraftFromPersisted()
  }

  function validateAccountDetailsDraft(): boolean {
    const name = draft.workspaceName.trim()
    if (name.length === 0) {
      workspaceNameError = WORKSPACE_NAME_REQUIRED_ERROR
      return false
    }
    if (name.length > 200) {
      workspaceNameError = WORKSPACE_NAME_MAX_ERROR
      return false
    }
    workspaceNameError = null
    return true
  }

  function validateBusinessDetailsDraft(): boolean {
    const postcode = businessDraft.postcode.trim()
    if (
      isUnitedKingdomCountry(businessDraft.country)
      && postcode.length > 0
      && !isValidUkPostcode(postcode)
    ) {
      postcodeError = ACCOUNT_WORKSPACE_PAGE_COPY.ukPostcodeError
      return false
    }
    postcodeError = null
    return true
  }

  function isAccountDetailsDirty(): boolean {
    if (persisted == null) {
      return false
    }
    const nameChanged =
      draft.workspaceName.trim() !== persisted.workspaceName.trim()
    return nameChanged || draft.stagedLogo != null
  }

  function isBusinessDetailsDirty(): boolean {
    if (persisted == null) {
      return false
    }
    const saved = normalizeBusinessDetails(persisted.businessDetails)
    const savedSameAs = deriveSameAsLegalBusinessName(saved)
    return (
      businessDraft.legalStructure !== saved.legalStructure
      || businessDraft.legalBusinessName !== saved.legalBusinessName
      || businessDraft.tradingName !== saved.tradingName
      || businessDraft.sameAsLegalBusinessName !== savedSameAs
      || businessDraft.companyNumber !== saved.companyNumber
      || businessDraft.vatNumber !== saved.vatNumber
      || businessDraft.countryOfRegistration !== saved.countryOfRegistration
      || businessDraft.addressLine1 !== saved.addressLine1
      || businessDraft.addressLine2 !== saved.addressLine2
      || businessDraft.townCity !== saved.townCity
      || businessDraft.county !== saved.county
      || businessDraft.postcode !== saved.postcode
      || businessDraft.country !== saved.country
    )
  }

  function isKeyContactsDirty(): boolean {
    if (persisted == null) {
      return false
    }
    const saved = normalizeKeyContacts(persisted.keyContacts)
    return (
      keyContactsDraft.billingContactUserId !== saved.billingContactUserId
      || keyContactsDraft.privacyContactUserId !== saved.privacyContactUserId
      || keyContactsDraft.supportContactUserId !== saved.supportContactUserId
    )
  }

  function isWorkspaceDefaultsDirty(): boolean {
    if (persisted == null) {
      return false
    }
    const saved = normalizeWorkspaceDefaults(persisted.workspaceDefaults)
    return (
      workspaceDefaultsDraft.weekStartsOn !== saved.weekStartsOn
      || workspaceDefaultsDraft.defaultReportingPeriod
        !== saved.defaultReportingPeriod
      || workspaceDefaultsDraft.defaultCampaignSenderName.trim()
        !== saved.defaultCampaignSenderName
    )
  }

  function activeTabDirty(): boolean {
    if (!isAccountWorkspaceFormTab(activeTabId)) {
      return false
    }
    if (activeTabId === "account-details") {
      return isAccountDetailsDirty()
    }
    if (activeTabId === "business-details") {
      return isBusinessDetailsDirty()
    }
    if (activeTabId === "key-contacts") {
      return isKeyContactsDirty()
    }
    if (activeTabId === "workspace-defaults") {
      return isWorkspaceDefaultsDirty()
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

  function patchBusinessDraft(
    patch: Partial<BusinessDetailsDraft>
  ): void {
    businessDraft = { ...businessDraft, ...patch }
    postcodeError = null
    emit()
  }

  function patchKeyContactsDraft(patch: Partial<KeyContactsDraft>): void {
    keyContactsDraft = { ...keyContactsDraft, ...patch }
    emit()
  }

  function patchWorkspaceDefaultsDraft(
    patch: Partial<WorkspaceDefaultsDraft>
  ): void {
    workspaceDefaultsDraft = { ...workspaceDefaultsDraft, ...patch }
    emit()
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
        workspaceNameError,
        accountStructure: persisted?.accountStructure ?? "",
        businessCategory: persisted?.businessCategory ?? null,
        businessCategoryLabel: persisted?.businessCategoryLabel ?? null,
        mainOperatingCountry:
          persisted?.mainOperatingCountry ?? ACCOUNT_WORKSPACE_DEFAULT_COUNTRY,
        brandLogoPreviewUrl:
          draft.stagedLogoPreviewUrl
          ?? persisted?.brandLogoPublicUrl
          ?? persisted?.brandLogoOperatorUrl
          ?? null,
        status: persisted?.status ?? null,
      },
      businessDetails: {
        ...businessDraft,
        // Preview only — persist still owns the same-as copy.
        tradingName: businessDraft.sameAsLegalBusinessName
          ? businessDraft.legalBusinessName
          : businessDraft.tradingName,
        postcodeError,
      },
      keyContacts: {
        ...keyContactsDraft,
        accountOwner: persisted?.keyContacts.accountOwner ?? null,
        eligibleMembers: persisted?.keyContacts.eligibleMembers ?? [],
      },
      workspaceDefaults: {
        ...workspaceDefaultsDraft,
        defaultTimezone:
          persisted?.workspaceDefaults.defaultTimezone ?? "Europe/London",
        defaultCurrency:
          persisted?.workspaceDefaults.defaultCurrency ?? "GBP",
        defaultLanguage:
          persisted?.workspaceDefaults.defaultLanguage ?? "English",
        dateFormat:
          persisted?.workspaceDefaults.dateFormat ?? "DD/MM/YYYY",
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

    if (!validateAccountDetailsDraft()) {
      emit()
      return false
    }

    const name = draft.workspaceName.trim()

    isSaving = true
    toast = null
    emit()

    try {
      const result = await adapters.updateAccountDetails({
        name,
        logo: draft.stagedLogo,
      })
      applyPersisted(result)
      adapters.onIdentityPersisted?.(result)
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

  async function persistBusinessDetails(): Promise<boolean> {
    if (persisted == null) {
      return false
    }

    if (!validateBusinessDetailsDraft()) {
      emit()
      return false
    }

    const tradingName = businessDraft.sameAsLegalBusinessName
      ? businessDraft.legalBusinessName
      : businessDraft.tradingName

    const payload: UpdateBusinessDetailsPayload = {
      legalStructure: businessDraft.legalStructure,
      legalBusinessName: businessDraft.legalBusinessName,
      tradingName,
      sameAsLegalBusinessName: businessDraft.sameAsLegalBusinessName,
      companyNumber: businessDraft.companyNumber,
      vatNumber: businessDraft.vatNumber,
      countryOfRegistration: businessDraft.countryOfRegistration,
      addressLine1: businessDraft.addressLine1,
      addressLine2: businessDraft.addressLine2,
      townCity: businessDraft.townCity,
      county: businessDraft.county,
      postcode: businessDraft.postcode,
      country: businessDraft.country,
    }

    isSaving = true
    toast = null
    emit()

    try {
      const result = await adapters.updateBusinessDetails(payload)
      applyPersisted(result)
      toast = {
        kind: "success",
        message: ACCOUNT_WORKSPACE_PAGE_COPY.businessDetailsSaveSuccess,
      }
      isSaving = false
      emit()
      return true
    } catch {
      toast = {
        kind: "error",
        message: ACCOUNT_WORKSPACE_PAGE_COPY.businessDetailsSaveError,
      }
      isSaving = false
      emit()
      return false
    }
  }

  async function persistKeyContacts(): Promise<boolean> {
    if (persisted == null) {
      return false
    }

    const payload: UpdateKeyContactsPayload = {
      billingContactUserId: keyContactsDraft.billingContactUserId,
      privacyContactUserId: keyContactsDraft.privacyContactUserId,
      supportContactUserId: keyContactsDraft.supportContactUserId,
    }

    isSaving = true
    toast = null
    emit()

    try {
      const result = await adapters.updateKeyContacts(payload)
      applyPersisted(result)
      toast = {
        kind: "success",
        message: ACCOUNT_WORKSPACE_PAGE_COPY.keyContactsSaveSuccess,
      }
      isSaving = false
      emit()
      return true
    } catch {
      toast = {
        kind: "error",
        message: ACCOUNT_WORKSPACE_PAGE_COPY.keyContactsSaveError,
      }
      isSaving = false
      emit()
      return false
    }
  }

  async function persistWorkspaceDefaults(): Promise<boolean> {
    if (persisted == null) {
      return false
    }

    const previousPeriod = persisted.workspaceDefaults.defaultReportingPeriod
    const payload: UpdateWorkspaceDefaultsPayload = {
      weekStartsOn: workspaceDefaultsDraft.weekStartsOn,
      defaultReportingPeriod: workspaceDefaultsDraft.defaultReportingPeriod,
      defaultCampaignSenderName: workspaceDefaultsDraft.defaultCampaignSenderName,
    }

    isSaving = true
    toast = null
    emit()

    try {
      const result = await adapters.updateWorkspaceDefaults(payload)
      applyPersisted(result)
      if (
        previousPeriod !== result.workspaceDefaults.defaultReportingPeriod
      ) {
        bumpRecommendedNextStepSoftCaches()
        adapters.onWorkspaceDefaultsPersisted?.(result)
      }
      toast = {
        kind: "success",
        message: ACCOUNT_WORKSPACE_PAGE_COPY.workspaceDefaultsSaveSuccess,
      }
      isSaving = false
      emit()
      return true
    } catch {
      toast = {
        kind: "error",
        message: ACCOUNT_WORKSPACE_PAGE_COPY.workspaceDefaultsSaveError,
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

    if (activeTabId === "business-details") {
      return persistBusinessDetails()
    }

    if (activeTabId === "key-contacts") {
      return persistKeyContacts()
    }

    if (activeTabId === "workspace-defaults") {
      return persistWorkspaceDefaults()
    }

    if (activeTabId === "account-details" && !validateAccountDetailsDraft()) {
      emit()
      return false
    }

    if (activeTabId === "account-details" && nameChanged()) {
      renameConfirmOpen = true
      emit()
      return false
    }

    if (activeTabId === "account-details") {
      return persistAccountDetails()
    }

    return false
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
      workspaceNameError = null
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

    setLegalStructure(value) {
      patchBusinessDraft({ legalStructure: value })
    },

    setLegalBusinessName(value) {
      patchBusinessDraft({ legalBusinessName: value })
    },

    setTradingName(value) {
      patchBusinessDraft({
        tradingName: value,
        sameAsLegalBusinessName: false,
      })
    },

    setSameAsLegalBusinessName(checked) {
      // Checkbox is UI-only; Trading name copies on persist, not here.
      patchBusinessDraft({ sameAsLegalBusinessName: checked })
    },

    setCompanyNumber(value) {
      patchBusinessDraft({ companyNumber: value })
    },

    setVatNumber(value) {
      patchBusinessDraft({ vatNumber: value })
    },

    setCountryOfRegistration(value) {
      patchBusinessDraft({ countryOfRegistration: value })
    },

    setAddressLine1(value) {
      patchBusinessDraft({ addressLine1: value })
    },

    setAddressLine2(value) {
      patchBusinessDraft({ addressLine2: value })
    },

    setTownCity(value) {
      patchBusinessDraft({ townCity: value })
    },

    setCounty(value) {
      patchBusinessDraft({ county: value })
    },

    setPostcode(value) {
      patchBusinessDraft({ postcode: value })
    },

    setCountry(value) {
      patchBusinessDraft({ country: value })
    },

    setBillingContactUserId(userId) {
      patchKeyContactsDraft({ billingContactUserId: userId })
    },

    setPrivacyContactUserId(userId) {
      patchKeyContactsDraft({ privacyContactUserId: userId })
    },

    setSupportContactUserId(userId) {
      patchKeyContactsDraft({ supportContactUserId: userId })
    },

    setWeekStartsOn(value) {
      patchWorkspaceDefaultsDraft({ weekStartsOn: value })
    },

    setDefaultReportingPeriod(value) {
      patchWorkspaceDefaultsDraft({ defaultReportingPeriod: value })
    },

    setDefaultCampaignSenderName(value) {
      patchWorkspaceDefaultsDraft({ defaultCampaignSenderName: value })
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

      if (activeTabId === "business-details") {
        const ok = await persistBusinessDetails()
        if (ok) {
          continuePendingLeave()
        } else {
          pendingLeave = null
        }
        emit()
        return
      }

      if (activeTabId === "key-contacts") {
        const ok = await persistKeyContacts()
        if (ok) {
          continuePendingLeave()
        } else {
          pendingLeave = null
        }
        emit()
        return
      }

      if (activeTabId === "workspace-defaults") {
        const ok = await persistWorkspaceDefaults()
        if (ok) {
          continuePendingLeave()
        } else {
          pendingLeave = null
        }
        emit()
        return
      }

      if (activeTabId === "account-details" && !validateAccountDetailsDraft()) {
        pendingLeave = null
        emit()
        return
      }

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
