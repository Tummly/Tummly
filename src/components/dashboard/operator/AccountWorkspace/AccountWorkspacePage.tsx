import {
  useEffect,
  useRef,
  useSyncExternalStore,
  type ReactNode,
} from "react"
import { Link, useNavigate, useOutletContext, useSearchParams } from "react-router-dom"

import { BrandLogoMark } from "@/components/brand/BrandLogoMark"
import { AccountWorkspaceConfirmDialog } from "@/components/dashboard/operator/AccountWorkspace/AccountWorkspaceConfirmDialog"
import { GuestDataExportDialog } from "@/components/dashboard/operator/AccountWorkspace/GuestDataExportDialog"
import { useAccountWorkspacePageModuleApi } from "@/components/dashboard/operator/AccountWorkspace/utils/accountWorkspacePageModuleContext"
import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { CheckboxLabel } from "@/components/ui/checkbox-label"
import { Input } from "@/components/ui/input"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import {
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
} from "@/components/ui/tabs"
import {
  ACCOUNT_STRUCTURE_OPTIONS,
  ACCOUNT_WORKSPACE_FIELD_HELPER_CLASS,
  ACCOUNT_WORKSPACE_FIELD_LABEL_CLASS,
  ACCOUNT_WORKSPACE_FULL_BLEED_BOTTOM,
  ACCOUNT_WORKSPACE_FULL_BLEED_X,
  ACCOUNT_WORKSPACE_IDENTITY_CARD_CLASS,
  ACCOUNT_WORKSPACE_IDENTITY_DIVIDER_CLASS,
  ACCOUNT_WORKSPACE_IDENTITY_SUBTITLE_CLASS,
  ACCOUNT_WORKSPACE_IDENTITY_TITLE_CLASS,
  ACCOUNT_WORKSPACE_PAGE_COPY,
  ACCOUNT_WORKSPACE_PAGE_STACK_CLASS,
  ACCOUNT_WORKSPACE_PAGE_SUBTITLE_CLASS,
  ACCOUNT_WORKSPACE_SELECT_ITEM_CLASS,
  ACCOUNT_WORKSPACE_SELECT_MENU_CLASS,
  ACCOUNT_WORKSPACE_SELECT_TRIGGER_CLASS,
  ACCOUNT_WORKSPACE_SHELL_PAD_BOTTOM,
  ACCOUNT_WORKSPACE_SHELL_PAD_X,
  ACCOUNT_WORKSPACE_TAB_BODY_CLASS,
  ACCOUNT_WORKSPACE_TAB_LIST_CLASS,
  ACCOUNT_WORKSPACE_TAB_TRIGGER_CLASS,
  ACCOUNT_WORKSPACE_TABS_RULE_CLASS,
  ACCOUNT_WORKSPACE_TEXT_INPUT_CLASS,
  DEFAULT_REPORTING_PERIOD_OPTIONS,
  LEGAL_STRUCTURE_OPTIONS,
  MAIN_OPERATING_COUNTRY_OPTIONS,
  WEEK_STARTS_ON_OPTIONS,
  accountRequestConfirmLabels,
  formatAccountWorkspaceLastSaved,
  resolveAccountWorkspacePlanStatusPresentation,
  resolveCampaignSenderDisplayName,
  type AccountWorkspacePlanStatusPresentation,
} from "@/lib/operatorAccountWorkspace/accountWorkspacePresentation"
import type { AccountWorkspaceTabId } from "@/lib/operatorAccountWorkspace/accountWorkspacePresentation"
import { operatorDashboardBillingCreditsPath } from "@/lib/operatorBillingCredits/billingCreditsPresentation"
import { BUSINESS_CATEGORY_OPTIONS } from "@/components/home/hero-trial-options"
import {
  BROWSER_BACK_HREF,
  registerLeaveDirtyGuard,
} from "@/lib/operatorNavigation/leaveDirtyGuard"
import {
  GUESTS_PAGE_HEADER_COPY_CLASS,
  GUESTS_PAGE_HEADER_ROW_CLASS,
  GUESTS_PAGE_PRIMARY_BUTTON_CLASS,
  GUESTS_PAGE_SECONDARY_BUTTON_CLASS,
  GUESTS_PAGE_TITLE_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import { cn } from "@/lib/utils"

const ACCOUNT_CONTROLS_STACK_CLASS = "flex flex-col gap-5"
const ACCOUNT_CONTROLS_ACTIONS_CLASS = "flex flex-wrap gap-3"
const ACCOUNT_CONTROLS_BODY_CLASS =
  "m-0 max-w-[818px] text-sm font-medium leading-normal text-[var(--op-color-gray-550)]"
const ACCOUNT_STATUS_COLUMNS_CLASS =
  "flex w-full flex-col gap-[18px] lg:flex-row"
const ACCOUNT_STATUS_COLUMN_CLASS =
  "flex min-w-0 flex-1 flex-col gap-[18px]"
const ACCOUNT_STATUS_ROW_CLASS = "flex items-center gap-3"
const ACCOUNT_STATUS_LABEL_CLASS =
  "m-0 shrink-0 text-sm font-medium leading-normal text-[var(--op-color-gray-550)]"
const ACCOUNT_STATUS_VALUE_CLASS =
  "m-0 text-sm font-medium leading-normal text-op-text-primary"

const FIELD_STACK_CLASS = "flex flex-col gap-2"
const FIELD_GRID_CLASS = "grid grid-cols-1 gap-10 lg:grid-cols-2"

function AccountStatusLabel({ children }: { children: string }) {
  return <p className={ACCOUNT_STATUS_LABEL_CLASS}>{children}</p>
}

function AccountStatusTextValue({ value }: { value: string }) {
  return <p className={ACCOUNT_STATUS_VALUE_CLASS}>{value}</p>
}

function AccountStatusMetricRow({
  label,
  children,
}: {
  label: string
  children: ReactNode
}) {
  return (
    <div className={ACCOUNT_STATUS_ROW_CLASS}>
      <AccountStatusLabel>{label}</AccountStatusLabel>
      {children}
    </div>
  )
}

function AccountStatusPlanValue({
  presentation,
}: {
  presentation: AccountWorkspacePlanStatusPresentation
}) {
  if (presentation.kind === "link") {
    return (
      <Link
        to={presentation.href}
        className={cn(
          ACCOUNT_STATUS_VALUE_CLASS,
          "underline decoration-solid underline-offset-2"
        )}
      >
        {presentation.label}
      </Link>
    )
  }

  return <AccountStatusTextValue value={presentation.label} />
}

function ContactMemberSelect({
  id,
  label,
  value,
  members,
  disabled = false,
  onValueChange,
}: {
  id: string
  label: string
  value: number
  members: ReadonlyArray<{ userId: number; fullName: string; email: string }>
  disabled?: boolean
  onValueChange?: (userId: number) => void
}) {
  const selected = members.find((member) => member.userId === value)
  const selectValue = value > 0 ? String(value) : undefined

  return (
    <div className={FIELD_STACK_CLASS}>
      <label htmlFor={id} className={ACCOUNT_WORKSPACE_FIELD_LABEL_CLASS}>
        {label}
      </label>
      <Select
        value={selectValue}
        disabled={disabled}
        onValueChange={(next) => {
          const parsed = Number(next)
          if (!Number.isFinite(parsed)) {
            return
          }
          onValueChange?.(parsed)
        }}
      >
        <SelectTrigger
          id={id}
          className={ACCOUNT_WORKSPACE_SELECT_TRIGGER_CLASS}
        >
          <SelectValue
            placeholder={ACCOUNT_WORKSPACE_PAGE_COPY.selectUserPlaceholder}
          >
            {selected?.fullName
              ?? ACCOUNT_WORKSPACE_PAGE_COPY.selectUserPlaceholder}
          </SelectValue>
        </SelectTrigger>
        <SelectContent
          position="popper"
          align="start"
          className={ACCOUNT_WORKSPACE_SELECT_MENU_CLASS}
        >
          {members.map((member) => (
            <SelectItem
              className={ACCOUNT_WORKSPACE_SELECT_ITEM_CLASS}
              key={member.userId}
              value={String(member.userId)}
            >
              <span className="flex flex-col gap-0.5 text-left">
                <span>{member.fullName}</span>
                <span className="text-xs font-normal text-[var(--op-color-gray-550)]">
                  {member.email}
                </span>
              </span>
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
    </div>
  )
}

function formatDateOnly(iso: string): string {
  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) {
    return "—"
  }
  return new Intl.DateTimeFormat("en-GB", {
    day: "numeric",
    month: "long",
    year: "numeric",
    timeZone: "Europe/London",
  }).format(date)
}

export function AccountWorkspacePage() {
  const pageModule = useAccountWorkspacePageModuleApi()
  const { billingCreditsAccess, selectedLocationId, locations, mode } =
    useOutletContext<DashboardOutletContext>()
  const snap = useSyncExternalStore(
    pageModule.subscribe,
    pageModule.getSnapshot,
    pageModule.getSnapshot
  )
  const accountRequestConfirmCopy =
    snap.accountRequestConfirm != null
      ? accountRequestConfirmLabels(snap.accountRequestConfirm)
      : null
  const [searchParams, setSearchParams] = useSearchParams()
  const navigate = useNavigate()
  const fileInputRef = useRef<HTMLInputElement>(null)

  const selectedLocationName =
    locations.find((location) => location.id === selectedLocationId)
      ?.locationName
      ?? null
  const firstLocationName = locations[0]?.locationName ?? null
  const guestFacingBusinessName =
    snap.accountDetails.guestFacingBusinessName.trim()
    || selectedLocationName?.trim()
    || firstLocationName?.trim()
    || ""

  useEffect(() => {
    const current = searchParams.get("tab")
    if (current === snap.activeTabId) {
      return
    }
    const next = new URLSearchParams(searchParams)
    next.set("tab", snap.activeTabId)
    setSearchParams(next, { replace: true })
  }, [snap.activeTabId, searchParams, setSearchParams])

  useEffect(() => {
    if (snap.pendingNavigationHref == null) {
      return
    }
    const href = pageModule.consumePendingNavigation()
    if (href == null) {
      return
    }
    if (href === BROWSER_BACK_HREF) {
      navigate(-1)
      return
    }
    navigate(href)
  }, [snap.pendingNavigationHref, pageModule, navigate])

  useEffect(() => {
    registerLeaveDirtyGuard({
      isBlocked: () => pageModule.getSnapshot().isDirty,
      requestLeave: (href) => pageModule.requestNavigateAway(href),
    })
    return () => {
      registerLeaveDirtyGuard(null)
    }
  }, [pageModule])

  useEffect(() => {
    if (!snap.isDirty) {
      return
    }

    const onPopState = () => {
      if (!pageModule.getSnapshot().isDirty) {
        return
      }
      // Stay on this URL until Unsaved changes resolves.
      window.history.pushState(null, "", window.location.href)
      pageModule.requestNavigateAway(BROWSER_BACK_HREF)
    }

    window.history.pushState(null, "", window.location.href)
    window.addEventListener("popstate", onPopState)
    return () => {
      window.removeEventListener("popstate", onPopState)
    }
  }, [snap.isDirty, pageModule])

  const status = snap.accountDetails.status
  const billingCreditsHref = operatorDashboardBillingCreditsPath(
    mode,
    selectedLocationId
  )
  const billingActivityHref = operatorDashboardBillingCreditsPath(
    mode,
    selectedLocationId,
    { tab: "activity" }
  )
  const canOpenBillingCredits =
    billingCreditsAccess === "view" || billingCreditsAccess === "manage"
  const planStatusPresentation =
    status != null
      ? resolveAccountWorkspacePlanStatusPresentation({
          planStatus: status.planStatus,
          billingCreditsAccess,
          planSubscriptionHref: billingCreditsHref,
        })
      : null
  const workspaceNameError = snap.accountDetails.workspaceNameError
  const business = snap.businessDetails
  const keyContacts = snap.keyContacts
  const workspaceDefaults = snap.workspaceDefaults
  const eligibleMembers = keyContacts.eligibleMembers
  const ownerMember =
    keyContacts.accountOwner != null
      ? [keyContacts.accountOwner]
      : eligibleMembers
  const isPaused = status?.workspaceStatus.toLowerCase() === "paused"
  const dangerZoneDisabled = !snap.isAccountOwner || snap.isSaving
  const campaignSenderDisplayName = resolveCampaignSenderDisplayName({
    storedSenderName: workspaceDefaults.defaultCampaignSenderName,
    workspaceName: snap.accountDetails.workspaceName,
    locationName: selectedLocationName ?? firstLocationName,
  })

  return (
    <div className={ACCOUNT_WORKSPACE_PAGE_STACK_CLASS}>
      <div className={GUESTS_PAGE_HEADER_ROW_CLASS}>
        <div className={GUESTS_PAGE_HEADER_COPY_CLASS}>
          <h1 className={GUESTS_PAGE_TITLE_CLASS}>
            {ACCOUNT_WORKSPACE_PAGE_COPY.title}
          </h1>
          <p className={ACCOUNT_WORKSPACE_PAGE_SUBTITLE_CLASS}>
            {ACCOUNT_WORKSPACE_PAGE_COPY.subtitle}
          </p>
          <p className="m-0 text-sm font-medium text-[var(--op-color-gray-550)]">
            {formatAccountWorkspaceLastSaved(snap.lastSavedAt)}
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-3">
          <Button
            type="button"
            variant="op-secondary"
            className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
            disabled
          >
            {ACCOUNT_WORKSPACE_PAGE_COPY.viewAccountActivity}
          </Button>
          <Button
            type="button"
            variant="op-primary"
            className={GUESTS_PAGE_PRIMARY_BUTTON_CLASS}
            disabled={!snap.saveEnabled}
            onClick={() => {
              void pageModule.requestSave()
            }}
          >
            {ACCOUNT_WORKSPACE_PAGE_COPY.saveChanges}
          </Button>
        </div>
      </div>

      <Tabs
        value={snap.activeTabId}
        onValueChange={(value) => {
          pageModule.requestTabChange(value as AccountWorkspaceTabId)
        }}
        className="flex min-h-0 flex-1 flex-col gap-0"
      >
        <div
          className={cn(
            ACCOUNT_WORKSPACE_FULL_BLEED_X,
            ACCOUNT_WORKSPACE_TABS_RULE_CLASS,
            "shrink-0"
          )}
        >
          <div className={ACCOUNT_WORKSPACE_SHELL_PAD_X}>
            <TabsList
              variant="line"
              className={ACCOUNT_WORKSPACE_TAB_LIST_CLASS}
            >
              {snap.tabs.map((tab) => (
                <TabsTrigger
                  key={tab.id}
                  value={tab.id}
                  className={ACCOUNT_WORKSPACE_TAB_TRIGGER_CLASS}
                >
                  {tab.label}
                </TabsTrigger>
              ))}
            </TabsList>
          </div>
        </div>

        <div
          className={cn(
            ACCOUNT_WORKSPACE_FULL_BLEED_X,
            ACCOUNT_WORKSPACE_FULL_BLEED_BOTTOM,
            ACCOUNT_WORKSPACE_SHELL_PAD_X,
            ACCOUNT_WORKSPACE_SHELL_PAD_BOTTOM,
            ACCOUNT_WORKSPACE_TAB_BODY_CLASS
          )}
        >
        <TabsContent value="account-details" className="mt-0">
          <section className={ACCOUNT_WORKSPACE_IDENTITY_CARD_CLASS}>
              <div className="flex flex-col gap-2">
                <h2 className={ACCOUNT_WORKSPACE_IDENTITY_TITLE_CLASS}>
                  {ACCOUNT_WORKSPACE_PAGE_COPY.workspaceIdentityTitle}
                </h2>
                <p className={ACCOUNT_WORKSPACE_IDENTITY_SUBTITLE_CLASS}>
                  {ACCOUNT_WORKSPACE_PAGE_COPY.workspaceIdentitySubtitle}
                </p>
              </div>

              <div className="flex flex-col gap-[30px]">
                <div className="grid grid-cols-1 gap-10 lg:grid-cols-2">
                  <div className="flex flex-col gap-2.5">
                    <div className={FIELD_STACK_CLASS}>
                      <label
                        htmlFor="workspace-name"
                        className={ACCOUNT_WORKSPACE_FIELD_LABEL_CLASS}
                      >
                        {ACCOUNT_WORKSPACE_PAGE_COPY.workspaceName}
                      </label>
                      <Input
                        id="workspace-name"
                        value={snap.accountDetails.workspaceName}
                        maxLength={200}
                        aria-invalid={workspaceNameError != null}
                        aria-describedby={
                          workspaceNameError != null
                            ? "workspace-name-error"
                            : "workspace-name-helper"
                        }
                        onChange={(event) => {
                          pageModule.setWorkspaceName(event.target.value)
                        }}
                        className={ACCOUNT_WORKSPACE_TEXT_INPUT_CLASS}
                      />
                      {workspaceNameError != null ? (
                        <p
                          id="workspace-name-error"
                          className="m-0 text-sm text-destructive"
                        >
                          {workspaceNameError}
                        </p>
                      ) : (
                        <p
                          id="workspace-name-helper"
                          className={ACCOUNT_WORKSPACE_FIELD_HELPER_CLASS}
                        >
                          {ACCOUNT_WORKSPACE_PAGE_COPY.workspaceNameHelper}
                        </p>
                      )}
                    </div>
                  </div>
                  <div className="flex flex-col gap-2.5">
                    <div className={FIELD_STACK_CLASS}>
                      <label
                        htmlFor="guest-facing-business-name"
                        className={ACCOUNT_WORKSPACE_FIELD_LABEL_CLASS}
                      >
                        {ACCOUNT_WORKSPACE_PAGE_COPY.guestFacingBusinessName}
                      </label>
                      <Input
                        id="guest-facing-business-name"
                        value={guestFacingBusinessName}
                        readOnly
                        disabled
                        aria-describedby="guest-facing-business-name-helper"
                        className={ACCOUNT_WORKSPACE_TEXT_INPUT_CLASS}
                      />
                      <p
                        id="guest-facing-business-name-helper"
                        className={ACCOUNT_WORKSPACE_FIELD_HELPER_CLASS}
                      >
                        {
                          ACCOUNT_WORKSPACE_PAGE_COPY.guestFacingBusinessNameHelper
                        }
                      </p>
                    </div>
                  </div>
                </div>

                <hr className={ACCOUNT_WORKSPACE_IDENTITY_DIVIDER_CLASS} />

                <div className="grid grid-cols-1 gap-10 lg:grid-cols-2">
                  <div className={FIELD_STACK_CLASS}>
                    <label
                      htmlFor="account-structure"
                      className={ACCOUNT_WORKSPACE_FIELD_LABEL_CLASS}
                    >
                      {ACCOUNT_WORKSPACE_PAGE_COPY.accountStructure}
                    </label>
                    <Select
                      value={
                        snap.accountDetails.accountStructure === ""
                          ? undefined
                          : snap.accountDetails.accountStructure
                      }
                      disabled
                    >
                      <SelectTrigger
                        id="account-structure"
                        className={ACCOUNT_WORKSPACE_SELECT_TRIGGER_CLASS}
                      >
                        <SelectValue
                          placeholder={
                            ACCOUNT_WORKSPACE_PAGE_COPY.legalStructurePlaceholder
                          }
                        />
                      </SelectTrigger>
                      <SelectContent
                        position="popper"
                        align="start"
                        className={ACCOUNT_WORKSPACE_SELECT_MENU_CLASS}
                      >
                        {ACCOUNT_STRUCTURE_OPTIONS.map((option) => (
                          <SelectItem
                              className={ACCOUNT_WORKSPACE_SELECT_ITEM_CLASS} key={option.value} value={option.value}>
                            {option.label}
                          </SelectItem>
                        ))}
                        {snap.accountDetails.accountStructure !== ""
                        && !ACCOUNT_STRUCTURE_OPTIONS.some(
                          (option) =>
                            option.value
                            === snap.accountDetails.accountStructure
                        ) ? (
                          <SelectItem
                              className={ACCOUNT_WORKSPACE_SELECT_ITEM_CLASS}
                            value={snap.accountDetails.accountStructure}
                          >
                            {snap.accountDetails.accountStructure}
                          </SelectItem>
                        ) : null}
                      </SelectContent>
                    </Select>
                  </div>
                  <div className="flex flex-col gap-3">
                    <div className={FIELD_STACK_CLASS}>
                      <label
                        htmlFor="business-category"
                        className={ACCOUNT_WORKSPACE_FIELD_LABEL_CLASS}
                      >
                        {ACCOUNT_WORKSPACE_PAGE_COPY.businessCategory}
                      </label>
                      <Select
                        value={
                          snap.accountDetails.businessCategory == null
                          || snap.accountDetails.businessCategory === ""
                            ? undefined
                            : snap.accountDetails.businessCategory
                        }
                        disabled
                      >
                        <SelectTrigger
                          id="business-category"
                          className={ACCOUNT_WORKSPACE_SELECT_TRIGGER_CLASS}
                          aria-describedby="business-category-helper"
                        >
                          <SelectValue
                            placeholder={
                              ACCOUNT_WORKSPACE_PAGE_COPY.legalStructurePlaceholder
                            }
                          >
                            {snap.accountDetails.businessCategoryLabel
                              ?? snap.accountDetails.businessCategory
                              ?? undefined}
                          </SelectValue>
                        </SelectTrigger>
                        <SelectContent
                          position="popper"
                          align="start"
                          className={ACCOUNT_WORKSPACE_SELECT_MENU_CLASS}
                        >
                          {BUSINESS_CATEGORY_OPTIONS.map((option) => (
                            <SelectItem
                              className={ACCOUNT_WORKSPACE_SELECT_ITEM_CLASS}
                              key={option.value}
                              value={option.value}
                            >
                              {option.label}
                            </SelectItem>
                          ))}
                          {snap.accountDetails.businessCategory != null
                          && snap.accountDetails.businessCategory !== ""
                          && !BUSINESS_CATEGORY_OPTIONS.some(
                            (option) =>
                              option.value
                              === snap.accountDetails.businessCategory
                          ) ? (
                            <SelectItem
                              className={ACCOUNT_WORKSPACE_SELECT_ITEM_CLASS}
                              value={snap.accountDetails.businessCategory}
                            >
                              {snap.accountDetails.businessCategoryLabel
                                ?? snap.accountDetails.businessCategory}
                            </SelectItem>
                          ) : null}
                        </SelectContent>
                      </Select>
                    </div>
                    <p
                      id="business-category-helper"
                      className={ACCOUNT_WORKSPACE_FIELD_HELPER_CLASS}
                    >
                      {ACCOUNT_WORKSPACE_PAGE_COPY.businessCategoryHelper}
                    </p>
                  </div>
                </div>

                <hr className={ACCOUNT_WORKSPACE_IDENTITY_DIVIDER_CLASS} />

                <div className="grid grid-cols-1 gap-10 lg:grid-cols-2">
                  <div className={FIELD_STACK_CLASS}>
                    <label
                      htmlFor="main-operating-country"
                      className={ACCOUNT_WORKSPACE_FIELD_LABEL_CLASS}
                    >
                      {ACCOUNT_WORKSPACE_PAGE_COPY.mainOperatingCountry}
                    </label>
                    <Select
                      value={
                        snap.accountDetails.mainOperatingCountry === ""
                          ? undefined
                          : snap.accountDetails.mainOperatingCountry
                      }
                      disabled
                    >
                      <SelectTrigger
                        id="main-operating-country"
                        className={ACCOUNT_WORKSPACE_SELECT_TRIGGER_CLASS}
                      >
                        <SelectValue
                          placeholder={
                            ACCOUNT_WORKSPACE_PAGE_COPY.legalStructurePlaceholder
                          }
                        />
                      </SelectTrigger>
                      <SelectContent
                        position="popper"
                        align="start"
                        className={ACCOUNT_WORKSPACE_SELECT_MENU_CLASS}
                      >
                        {MAIN_OPERATING_COUNTRY_OPTIONS.map((option) => (
                          <SelectItem
                              className={ACCOUNT_WORKSPACE_SELECT_ITEM_CLASS} key={option.value} value={option.value}>
                            {option.label}
                          </SelectItem>
                        ))}
                        {snap.accountDetails.mainOperatingCountry !== ""
                        && !MAIN_OPERATING_COUNTRY_OPTIONS.some(
                          (option) =>
                            option.value
                            === snap.accountDetails.mainOperatingCountry
                        ) ? (
                          <SelectItem
                              className={ACCOUNT_WORKSPACE_SELECT_ITEM_CLASS}
                            value={snap.accountDetails.mainOperatingCountry}
                          >
                            {snap.accountDetails.mainOperatingCountry}
                          </SelectItem>
                        ) : null}
                      </SelectContent>
                    </Select>
                  </div>
                  <div className="flex flex-col gap-3">
                    <div className="flex flex-col gap-2">
                      <p className={ACCOUNT_WORKSPACE_FIELD_LABEL_CLASS}>
                        {ACCOUNT_WORKSPACE_PAGE_COPY.workspaceLogo}
                      </p>
                      <div className="flex flex-wrap items-center gap-3">
                        <span className="flex shrink-0 items-center rounded-op-md bg-input px-2.5 py-2">
                          <BrandLogoMark
                            brandLogoPublicUrl={
                              snap.accountDetails.brandLogoPreviewUrl
                            }
                            className="size-[34px]"
                          />
                        </span>
                        <Button
                          type="button"
                          variant="op-secondary"
                          onClick={() => fileInputRef.current?.click()}
                        >
                          {ACCOUNT_WORKSPACE_PAGE_COPY.uploadImage}
                        </Button>
                        <input
                          ref={fileInputRef}
                          type="file"
                          accept="image/jpeg,image/png,image/webp"
                          className="hidden"
                          onChange={(event) => {
                            const file = event.target.files?.[0] ?? null
                            pageModule.stageBrandLogo(file)
                            event.target.value = ""
                          }}
                        />
                      </div>
                    </div>
                    <p className={ACCOUNT_WORKSPACE_FIELD_HELPER_CLASS}>
                      {ACCOUNT_WORKSPACE_PAGE_COPY.workspaceLogoHelper}
                    </p>
                  </div>
                </div>
              </div>
            </section>
        </TabsContent>

        <TabsContent value="business-details" className="mt-0">
          <div className="flex flex-col gap-5">
            <section className={ACCOUNT_WORKSPACE_IDENTITY_CARD_CLASS}>
              <div className="flex flex-col gap-2">
                <h2 className={ACCOUNT_WORKSPACE_IDENTITY_TITLE_CLASS}>
                  {ACCOUNT_WORKSPACE_PAGE_COPY.businessIdentityTitle}
                </h2>
                <p className={ACCOUNT_WORKSPACE_IDENTITY_SUBTITLE_CLASS}>
                  {ACCOUNT_WORKSPACE_PAGE_COPY.businessIdentitySubtitle}
                </p>
              </div>

              <div className="flex flex-col gap-[30px]">
                <div className={FIELD_STACK_CLASS}>
                  <label
                    htmlFor="legal-structure"
                    className={ACCOUNT_WORKSPACE_FIELD_LABEL_CLASS}
                  >
                    {ACCOUNT_WORKSPACE_PAGE_COPY.legalStructure}
                  </label>
                  <Select
                    value={
                      business.legalStructure === ""
                        ? undefined
                        : business.legalStructure
                    }
                    onValueChange={(value) => {
                      pageModule.setLegalStructure(value)
                    }}
                  >
                    <SelectTrigger
                      id="legal-structure"
                      className={ACCOUNT_WORKSPACE_SELECT_TRIGGER_CLASS}
                    >
                      <SelectValue
                        placeholder={
                          ACCOUNT_WORKSPACE_PAGE_COPY.legalStructurePlaceholder
                        }
                      />
                    </SelectTrigger>
                    <SelectContent
                      position="popper"
                      align="start"
                      className={ACCOUNT_WORKSPACE_SELECT_MENU_CLASS}
                    >
                      {LEGAL_STRUCTURE_OPTIONS.map((option) => (
                        <SelectItem
                              className={ACCOUNT_WORKSPACE_SELECT_ITEM_CLASS} key={option.value} value={option.value}>
                          {option.label}
                        </SelectItem>
                      ))}
                      {business.legalStructure !== ""
                      && !LEGAL_STRUCTURE_OPTIONS.some(
                        (option) => option.value === business.legalStructure
                      ) ? (
                        <SelectItem
                              className={ACCOUNT_WORKSPACE_SELECT_ITEM_CLASS} value={business.legalStructure}>
                          {business.legalStructure}
                        </SelectItem>
                      ) : null}
                    </SelectContent>
                  </Select>
                </div>

                <hr className={ACCOUNT_WORKSPACE_IDENTITY_DIVIDER_CLASS} />

                <div className="grid grid-cols-1 gap-10 lg:grid-cols-2">
                  <div className="flex flex-col gap-3">
                    <div className={FIELD_STACK_CLASS}>
                      <label
                        htmlFor="legal-business-name"
                        className={ACCOUNT_WORKSPACE_FIELD_LABEL_CLASS}
                      >
                        {ACCOUNT_WORKSPACE_PAGE_COPY.legalBusinessName}
                      </label>
                      <Input
                        id="legal-business-name"
                        value={business.legalBusinessName}
                        maxLength={200}
                        aria-describedby="legal-business-name-helper"
                        onChange={(event) => {
                          pageModule.setLegalBusinessName(event.target.value)
                        }}
                        className={ACCOUNT_WORKSPACE_TEXT_INPUT_CLASS}
                      />
                    </div>
                    <p
                      id="legal-business-name-helper"
                      className={ACCOUNT_WORKSPACE_FIELD_HELPER_CLASS}
                    >
                      {ACCOUNT_WORKSPACE_PAGE_COPY.legalBusinessNameHelper}
                    </p>
                  </div>
                  <div className="flex flex-col gap-3">
                    <div className={FIELD_STACK_CLASS}>
                      <label
                        htmlFor="trading-name"
                        className={ACCOUNT_WORKSPACE_FIELD_LABEL_CLASS}
                      >
                        {ACCOUNT_WORKSPACE_PAGE_COPY.tradingName}
                      </label>
                      <Input
                        id="trading-name"
                        value={business.tradingName}
                        maxLength={200}
                        disabled={business.sameAsLegalBusinessName}
                        aria-describedby="trading-name-helper"
                        onChange={(event) => {
                          pageModule.setTradingName(event.target.value)
                        }}
                        className={ACCOUNT_WORKSPACE_TEXT_INPUT_CLASS}
                      />
                    </div>
                    <p
                      id="trading-name-helper"
                      className={ACCOUNT_WORKSPACE_FIELD_HELPER_CLASS}
                    >
                      {ACCOUNT_WORKSPACE_PAGE_COPY.tradingNameHelper}
                    </p>
                    <CheckboxLabel
                      id="same-as-legal-business-name"
                      checked={business.sameAsLegalBusinessName}
                      onCheckedChange={(checked) => {
                        pageModule.setSameAsLegalBusinessName(checked)
                      }}
                    >
                      {ACCOUNT_WORKSPACE_PAGE_COPY.sameAsLegalBusinessName}
                    </CheckboxLabel>
                  </div>
                </div>

                <hr className={ACCOUNT_WORKSPACE_IDENTITY_DIVIDER_CLASS} />

                <div className="grid grid-cols-1 gap-10 lg:grid-cols-2">
                  <div className="flex flex-col gap-3">
                    <div className={FIELD_STACK_CLASS}>
                      <label
                        htmlFor="company-number"
                        className={ACCOUNT_WORKSPACE_FIELD_LABEL_CLASS}
                      >
                        {ACCOUNT_WORKSPACE_PAGE_COPY.companyNumber}
                      </label>
                      <Input
                        id="company-number"
                        value={business.companyNumber}
                        maxLength={50}
                        aria-describedby="company-number-helper"
                        onChange={(event) => {
                          pageModule.setCompanyNumber(event.target.value)
                        }}
                        className={ACCOUNT_WORKSPACE_TEXT_INPUT_CLASS}
                      />
                    </div>
                    <p
                      id="company-number-helper"
                      className={ACCOUNT_WORKSPACE_FIELD_HELPER_CLASS}
                    >
                      {ACCOUNT_WORKSPACE_PAGE_COPY.companyNumberHelper}
                    </p>
                  </div>
                  <div className="flex flex-col gap-3">
                    <div className={FIELD_STACK_CLASS}>
                      <label
                        htmlFor="vat-number"
                        className={ACCOUNT_WORKSPACE_FIELD_LABEL_CLASS}
                      >
                        {ACCOUNT_WORKSPACE_PAGE_COPY.vatNumber}
                      </label>
                      <Input
                        id="vat-number"
                        value={business.vatNumber}
                        maxLength={50}
                        aria-describedby="vat-number-helper"
                        onChange={(event) => {
                          pageModule.setVatNumber(event.target.value)
                        }}
                        className={ACCOUNT_WORKSPACE_TEXT_INPUT_CLASS}
                      />
                    </div>
                    <p
                      id="vat-number-helper"
                      className={ACCOUNT_WORKSPACE_FIELD_HELPER_CLASS}
                    >
                      {ACCOUNT_WORKSPACE_PAGE_COPY.vatNumberHelper}
                    </p>
                  </div>
                </div>

                <hr className={ACCOUNT_WORKSPACE_IDENTITY_DIVIDER_CLASS} />

                <div className="flex flex-col gap-3">
                  <div className={FIELD_STACK_CLASS}>
                    <label
                      htmlFor="country-of-registration"
                      className={ACCOUNT_WORKSPACE_FIELD_LABEL_CLASS}
                    >
                      {ACCOUNT_WORKSPACE_PAGE_COPY.countryOfRegistration}
                    </label>
                    <Input
                      id="country-of-registration"
                      value={business.countryOfRegistration}
                      maxLength={100}
                      aria-describedby="country-of-registration-helper"
                      onChange={(event) => {
                        pageModule.setCountryOfRegistration(event.target.value)
                      }}
                      className={ACCOUNT_WORKSPACE_TEXT_INPUT_CLASS}
                    />
                  </div>
                  <p
                    id="country-of-registration-helper"
                    className={ACCOUNT_WORKSPACE_FIELD_HELPER_CLASS}
                  >
                    {ACCOUNT_WORKSPACE_PAGE_COPY.countryOfRegistrationHelper}
                  </p>
                </div>
              </div>
            </section>

            <section className={ACCOUNT_WORKSPACE_IDENTITY_CARD_CLASS}>
              <div className="flex flex-col gap-2">
                <h2 className={ACCOUNT_WORKSPACE_IDENTITY_TITLE_CLASS}>
                  {ACCOUNT_WORKSPACE_PAGE_COPY.businessAddressTitle}
                </h2>
                <p className={ACCOUNT_WORKSPACE_IDENTITY_SUBTITLE_CLASS}>
                  {ACCOUNT_WORKSPACE_PAGE_COPY.businessAddressSubtitle}
                </p>
              </div>

              <div className="flex flex-col gap-[30px]">
                <div className="grid grid-cols-1 gap-10 lg:grid-cols-2">
                  <div className={FIELD_STACK_CLASS}>
                    <label
                      htmlFor="address-line-1"
                      className={ACCOUNT_WORKSPACE_FIELD_LABEL_CLASS}
                    >
                      {ACCOUNT_WORKSPACE_PAGE_COPY.addressLine1}
                    </label>
                    <Input
                      id="address-line-1"
                      value={business.addressLine1}
                      maxLength={500}
                      onChange={(event) => {
                        pageModule.setAddressLine1(event.target.value)
                      }}
                      className={ACCOUNT_WORKSPACE_TEXT_INPUT_CLASS}
                    />
                  </div>
                  <div className={FIELD_STACK_CLASS}>
                    <label
                      htmlFor="address-line-2"
                      className={ACCOUNT_WORKSPACE_FIELD_LABEL_CLASS}
                    >
                      {ACCOUNT_WORKSPACE_PAGE_COPY.addressLine2}
                    </label>
                    <Input
                      id="address-line-2"
                      value={business.addressLine2}
                      maxLength={500}
                      placeholder={
                        ACCOUNT_WORKSPACE_PAGE_COPY.addressLine2Placeholder
                      }
                      onChange={(event) => {
                        pageModule.setAddressLine2(event.target.value)
                      }}
                      className={ACCOUNT_WORKSPACE_TEXT_INPUT_CLASS}
                    />
                  </div>
                </div>

                <div className="grid grid-cols-1 gap-10 lg:grid-cols-2">
                  <div className={FIELD_STACK_CLASS}>
                    <label
                      htmlFor="town-city"
                      className={ACCOUNT_WORKSPACE_FIELD_LABEL_CLASS}
                    >
                      {ACCOUNT_WORKSPACE_PAGE_COPY.townCity}
                    </label>
                    <Input
                      id="town-city"
                      value={business.townCity}
                      maxLength={150}
                      placeholder={
                        ACCOUNT_WORKSPACE_PAGE_COPY.townCityPlaceholder
                      }
                      onChange={(event) => {
                        pageModule.setTownCity(event.target.value)
                      }}
                      className={ACCOUNT_WORKSPACE_TEXT_INPUT_CLASS}
                    />
                  </div>
                  <div className={FIELD_STACK_CLASS}>
                    <label
                      htmlFor="county"
                      className={ACCOUNT_WORKSPACE_FIELD_LABEL_CLASS}
                    >
                      {ACCOUNT_WORKSPACE_PAGE_COPY.county}
                    </label>
                    <Input
                      id="county"
                      value={business.county}
                      maxLength={150}
                      onChange={(event) => {
                        pageModule.setCounty(event.target.value)
                      }}
                      className={ACCOUNT_WORKSPACE_TEXT_INPUT_CLASS}
                    />
                  </div>
                </div>

                <div className="grid grid-cols-1 gap-10 lg:grid-cols-2">
                  <div className={FIELD_STACK_CLASS}>
                    <label
                      htmlFor="postcode"
                      className={ACCOUNT_WORKSPACE_FIELD_LABEL_CLASS}
                    >
                      {ACCOUNT_WORKSPACE_PAGE_COPY.postcode}
                    </label>
                    <Input
                      id="postcode"
                      value={business.postcode}
                      maxLength={20}
                      placeholder={
                        ACCOUNT_WORKSPACE_PAGE_COPY.postcodePlaceholder
                      }
                      aria-invalid={business.postcodeError != null}
                      aria-describedby={
                        business.postcodeError != null
                          ? "postcode-error"
                          : undefined
                      }
                      onChange={(event) => {
                        pageModule.setPostcode(event.target.value)
                      }}
                      className={ACCOUNT_WORKSPACE_TEXT_INPUT_CLASS}
                    />
                    {business.postcodeError != null ? (
                      <p
                        id="postcode-error"
                        className="m-0 text-sm text-destructive"
                      >
                        {business.postcodeError}
                      </p>
                    ) : null}
                  </div>
                  <div className={FIELD_STACK_CLASS}>
                    <label
                      htmlFor="address-country"
                      className={ACCOUNT_WORKSPACE_FIELD_LABEL_CLASS}
                    >
                      {ACCOUNT_WORKSPACE_PAGE_COPY.country}
                    </label>
                    <Input
                      id="address-country"
                      value={business.country}
                      maxLength={100}
                      onChange={(event) => {
                        pageModule.setCountry(event.target.value)
                      }}
                      className={ACCOUNT_WORKSPACE_TEXT_INPUT_CLASS}
                    />
                  </div>
                </div>
              </div>
            </section>
          </div>
        </TabsContent>
        <TabsContent value="key-contacts" className="mt-0">
          <section className={ACCOUNT_WORKSPACE_IDENTITY_CARD_CLASS}>
            <div className="flex flex-col gap-2">
              <h2 className={ACCOUNT_WORKSPACE_IDENTITY_TITLE_CLASS}>
                {ACCOUNT_WORKSPACE_PAGE_COPY.primaryResponsibilitiesTitle}
              </h2>
              <p className={ACCOUNT_WORKSPACE_IDENTITY_SUBTITLE_CLASS}>
                {ACCOUNT_WORKSPACE_PAGE_COPY.primaryResponsibilitiesSubtitle}
              </p>
            </div>

            <div className="flex flex-col gap-[30px]">
              <div className={FIELD_GRID_CLASS}>
                <ContactMemberSelect
                  id="account-owner"
                  label={ACCOUNT_WORKSPACE_PAGE_COPY.accountOwner}
                  value={keyContacts.accountOwner?.userId ?? 0}
                  members={ownerMember}
                  disabled
                />
                <ContactMemberSelect
                  id="billing-contact"
                  label={ACCOUNT_WORKSPACE_PAGE_COPY.billingContact}
                  value={keyContacts.billingContactUserId}
                  members={eligibleMembers}
                  onValueChange={(userId) => {
                    pageModule.setBillingContactUserId(userId)
                  }}
                />
              </div>

              <hr className={ACCOUNT_WORKSPACE_IDENTITY_DIVIDER_CLASS} />

              <div className={FIELD_GRID_CLASS}>
                <ContactMemberSelect
                  id="privacy-contact"
                  label={ACCOUNT_WORKSPACE_PAGE_COPY.privacyContact}
                  value={keyContacts.privacyContactUserId}
                  members={eligibleMembers}
                  onValueChange={(userId) => {
                    pageModule.setPrivacyContactUserId(userId)
                  }}
                />
                <ContactMemberSelect
                  id="support-contact"
                  label={ACCOUNT_WORKSPACE_PAGE_COPY.supportContact}
                  value={keyContacts.supportContactUserId}
                  members={eligibleMembers}
                  onValueChange={(userId) => {
                    pageModule.setSupportContactUserId(userId)
                  }}
                />
              </div>
            </div>
          </section>
        </TabsContent>
        <TabsContent value="workspace-defaults" className="mt-0">
          <section className={ACCOUNT_WORKSPACE_IDENTITY_CARD_CLASS}>
            <div className="flex flex-col gap-2">
              <h2 className={ACCOUNT_WORKSPACE_IDENTITY_TITLE_CLASS}>
                {ACCOUNT_WORKSPACE_PAGE_COPY.workspaceDefaultsTitle}
              </h2>
              <p className={ACCOUNT_WORKSPACE_IDENTITY_SUBTITLE_CLASS}>
                {ACCOUNT_WORKSPACE_PAGE_COPY.workspaceDefaultsSubtitle}
              </p>
            </div>

            <div className="flex flex-col gap-[30px]">
              <div className={FIELD_GRID_CLASS}>
                <div className={FIELD_STACK_CLASS}>
                  <label
                    htmlFor="default-timezone"
                    className={ACCOUNT_WORKSPACE_FIELD_LABEL_CLASS}
                  >
                    {ACCOUNT_WORKSPACE_PAGE_COPY.defaultTimezone}
                  </label>
                  <Select
                    value={workspaceDefaults.defaultTimezone}
                    disabled
                  >
                    <SelectTrigger
                      id="default-timezone"
                      className={ACCOUNT_WORKSPACE_SELECT_TRIGGER_CLASS}
                    >
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent
                      position="popper"
                      align="start"
                      className={ACCOUNT_WORKSPACE_SELECT_MENU_CLASS}
                    >
                      <SelectItem
                        className={ACCOUNT_WORKSPACE_SELECT_ITEM_CLASS}
                        value={workspaceDefaults.defaultTimezone}
                      >
                        {workspaceDefaults.defaultTimezone}
                      </SelectItem>
                    </SelectContent>
                  </Select>
                </div>
                <div className={FIELD_STACK_CLASS}>
                  <label
                    htmlFor="default-currency"
                    className={ACCOUNT_WORKSPACE_FIELD_LABEL_CLASS}
                  >
                    {ACCOUNT_WORKSPACE_PAGE_COPY.defaultCurrency}
                  </label>
                  <Select
                    value={workspaceDefaults.defaultCurrency}
                    disabled
                  >
                    <SelectTrigger
                      id="default-currency"
                      className={ACCOUNT_WORKSPACE_SELECT_TRIGGER_CLASS}
                    >
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent
                      position="popper"
                      align="start"
                      className={ACCOUNT_WORKSPACE_SELECT_MENU_CLASS}
                    >
                      <SelectItem
                        className={ACCOUNT_WORKSPACE_SELECT_ITEM_CLASS}
                        value={workspaceDefaults.defaultCurrency}
                      >
                        {workspaceDefaults.defaultCurrency}
                      </SelectItem>
                    </SelectContent>
                  </Select>
                </div>
              </div>

              <hr className={ACCOUNT_WORKSPACE_IDENTITY_DIVIDER_CLASS} />

              <div className={FIELD_GRID_CLASS}>
                <div className={FIELD_STACK_CLASS}>
                  <label
                    htmlFor="default-language"
                    className={ACCOUNT_WORKSPACE_FIELD_LABEL_CLASS}
                  >
                    {ACCOUNT_WORKSPACE_PAGE_COPY.defaultLanguage}
                  </label>
                  <Select
                    value={workspaceDefaults.defaultLanguage}
                    disabled
                  >
                    <SelectTrigger
                      id="default-language"
                      className={ACCOUNT_WORKSPACE_SELECT_TRIGGER_CLASS}
                    >
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent
                      position="popper"
                      align="start"
                      className={ACCOUNT_WORKSPACE_SELECT_MENU_CLASS}
                    >
                      <SelectItem
                        className={ACCOUNT_WORKSPACE_SELECT_ITEM_CLASS}
                        value={workspaceDefaults.defaultLanguage}
                      >
                        {workspaceDefaults.defaultLanguage}
                      </SelectItem>
                    </SelectContent>
                  </Select>
                </div>
                <div className={FIELD_STACK_CLASS}>
                  <label
                    htmlFor="date-format"
                    className={ACCOUNT_WORKSPACE_FIELD_LABEL_CLASS}
                  >
                    {ACCOUNT_WORKSPACE_PAGE_COPY.dateFormat}
                  </label>
                  <Select value={workspaceDefaults.dateFormat} disabled>
                    <SelectTrigger
                      id="date-format"
                      className={ACCOUNT_WORKSPACE_SELECT_TRIGGER_CLASS}
                    >
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent
                      position="popper"
                      align="start"
                      className={ACCOUNT_WORKSPACE_SELECT_MENU_CLASS}
                    >
                      <SelectItem
                        className={ACCOUNT_WORKSPACE_SELECT_ITEM_CLASS}
                        value={workspaceDefaults.dateFormat}
                      >
                        {workspaceDefaults.dateFormat}
                      </SelectItem>
                    </SelectContent>
                  </Select>
                </div>
              </div>

              <hr className={ACCOUNT_WORKSPACE_IDENTITY_DIVIDER_CLASS} />

              <div className={FIELD_GRID_CLASS}>
                <div className={FIELD_STACK_CLASS}>
                  <label
                    htmlFor="week-starts-on"
                    className={ACCOUNT_WORKSPACE_FIELD_LABEL_CLASS}
                  >
                    {ACCOUNT_WORKSPACE_PAGE_COPY.weekStartsOn}
                  </label>
                  <Select
                    value={workspaceDefaults.weekStartsOn}
                    onValueChange={(value) => {
                      pageModule.setWeekStartsOn(
                        value as (typeof WEEK_STARTS_ON_OPTIONS)[number]["value"]
                      )
                    }}
                  >
                    <SelectTrigger
                      id="week-starts-on"
                      className={ACCOUNT_WORKSPACE_SELECT_TRIGGER_CLASS}
                    >
                      <SelectValue
                        placeholder={
                          ACCOUNT_WORKSPACE_PAGE_COPY.legalStructurePlaceholder
                        }
                      />
                    </SelectTrigger>
                    <SelectContent
                      position="popper"
                      align="start"
                      className={ACCOUNT_WORKSPACE_SELECT_MENU_CLASS}
                    >
                      {WEEK_STARTS_ON_OPTIONS.map((option) => (
                        <SelectItem
                          className={ACCOUNT_WORKSPACE_SELECT_ITEM_CLASS}
                          key={option.value}
                          value={option.value}
                        >
                          {option.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
                <div className={FIELD_STACK_CLASS}>
                  <label
                    htmlFor="default-reporting-period"
                    className={ACCOUNT_WORKSPACE_FIELD_LABEL_CLASS}
                  >
                    {ACCOUNT_WORKSPACE_PAGE_COPY.defaultReportingPeriod}
                  </label>
                  <Select
                    value={workspaceDefaults.defaultReportingPeriod}
                    onValueChange={(value) => {
                      pageModule.setDefaultReportingPeriod(
                        value as (typeof DEFAULT_REPORTING_PERIOD_OPTIONS)[number]["value"]
                      )
                    }}
                  >
                    <SelectTrigger
                      id="default-reporting-period"
                      className={ACCOUNT_WORKSPACE_SELECT_TRIGGER_CLASS}
                    >
                      <SelectValue
                        placeholder={
                          ACCOUNT_WORKSPACE_PAGE_COPY.legalStructurePlaceholder
                        }
                      />
                    </SelectTrigger>
                    <SelectContent
                      position="popper"
                      align="start"
                      className={ACCOUNT_WORKSPACE_SELECT_MENU_CLASS}
                    >
                      {DEFAULT_REPORTING_PERIOD_OPTIONS.map((option) => (
                        <SelectItem
                          className={ACCOUNT_WORKSPACE_SELECT_ITEM_CLASS}
                          key={option.value}
                          value={option.value}
                        >
                          {option.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              </div>

              <hr className={ACCOUNT_WORKSPACE_IDENTITY_DIVIDER_CLASS} />

              <div className={FIELD_GRID_CLASS}>
                <div className={FIELD_STACK_CLASS}>
                  <label
                    htmlFor="default-campaign-sender-name"
                    className={ACCOUNT_WORKSPACE_FIELD_LABEL_CLASS}
                  >
                    {ACCOUNT_WORKSPACE_PAGE_COPY.defaultCampaignSenderName}
                  </label>
                  <Input
                    id="default-campaign-sender-name"
                    value={campaignSenderDisplayName}
                    maxLength={200}
                    onChange={(event) => {
                      pageModule.setDefaultCampaignSenderName(
                        event.target.value
                      )
                    }}
                    className={ACCOUNT_WORKSPACE_TEXT_INPUT_CLASS}
                  />
                </div>
              </div>
            </div>
          </section>
        </TabsContent>
        <TabsContent value="account-controls" className="mt-0">
          <div className={ACCOUNT_CONTROLS_STACK_CLASS}>
            <section className={ACCOUNT_WORKSPACE_IDENTITY_CARD_CLASS}>
              <div className="flex flex-col gap-2">
                <h2 className={ACCOUNT_WORKSPACE_IDENTITY_TITLE_CLASS}>
                  {ACCOUNT_WORKSPACE_PAGE_COPY.accountControlsStatusTitle}
                </h2>
                <p className={ACCOUNT_WORKSPACE_IDENTITY_SUBTITLE_CLASS}>
                  {ACCOUNT_WORKSPACE_PAGE_COPY.accountControlsStatusSubtitle}
                </p>
              </div>

              {status != null ? (
                <div className={ACCOUNT_STATUS_COLUMNS_CLASS}>
                  <div className={ACCOUNT_STATUS_COLUMN_CLASS}>
                    <AccountStatusMetricRow label="Workspace status:">
                      <Badge variant="soft">{status.workspaceStatus}</Badge>
                    </AccountStatusMetricRow>
                    <hr className={ACCOUNT_WORKSPACE_IDENTITY_DIVIDER_CLASS} />
                    <AccountStatusMetricRow label="Plan status:">
                      <AccountStatusPlanValue
                        presentation={planStatusPresentation!}
                      />
                    </AccountStatusMetricRow>
                    <hr className={ACCOUNT_WORKSPACE_IDENTITY_DIVIDER_CLASS} />
                    <AccountStatusMetricRow label="Account created:">
                      <AccountStatusTextValue
                        value={formatDateOnly(status.accountCreatedAt)}
                      />
                    </AccountStatusMetricRow>
                    <hr className={ACCOUNT_WORKSPACE_IDENTITY_DIVIDER_CLASS} />
                    <AccountStatusMetricRow label="Active locations:">
                      <AccountStatusTextValue
                        value={String(status.activeLocations)}
                      />
                    </AccountStatusMetricRow>
                    <hr className={ACCOUNT_WORKSPACE_IDENTITY_DIVIDER_CLASS} />
                    <AccountStatusMetricRow label="Team members:">
                      <AccountStatusTextValue
                        value={String(status.teamMembers)}
                      />
                    </AccountStatusMetricRow>
                  </div>

                  <div className={ACCOUNT_STATUS_COLUMN_CLASS}>
                    <AccountStatusMetricRow label="Guest profiles:">
                      <AccountStatusTextValue
                        value={String(status.guestProfiles)}
                      />
                    </AccountStatusMetricRow>
                    <hr className={ACCOUNT_WORKSPACE_IDENTITY_DIVIDER_CLASS} />
                    <AccountStatusMetricRow label="Guest form status:">
                      <Badge variant="soft">{status.guestFormStatus}</Badge>
                    </AccountStatusMetricRow>
                    <hr className={ACCOUNT_WORKSPACE_IDENTITY_DIVIDER_CLASS} />
                    <AccountStatusMetricRow label="Billing status:">
                      <Badge variant="soft">{status.billingStatus}</Badge>
                    </AccountStatusMetricRow>
                    <hr className={ACCOUNT_WORKSPACE_IDENTITY_DIVIDER_CLASS} />
                    <AccountStatusMetricRow label="Last account update:">
                      <AccountStatusTextValue
                        value={formatDateOnly(status.lastAccountUpdateAt)}
                      />
                    </AccountStatusMetricRow>
                  </div>
                </div>
              ) : null}

              <div className={ACCOUNT_CONTROLS_ACTIONS_CLASS}>
                <Button
                  type="button"
                  variant="op-secondary"
                  className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
                  disabled={!canOpenBillingCredits}
                  onClick={() => {
                    if (!pageModule.requestNavigateAway(billingCreditsHref)) {
                      return
                    }
                    navigate(billingCreditsHref)
                  }}
                >
                  {ACCOUNT_WORKSPACE_PAGE_COPY.viewBilling}
                </Button>
                <Button
                  type="button"
                  variant="op-secondary"
                  className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
                  disabled={!canOpenBillingCredits}
                  onClick={() => {
                    if (!pageModule.requestNavigateAway(billingActivityHref)) {
                      return
                    }
                    navigate(billingActivityHref)
                  }}
                >
                  {ACCOUNT_WORKSPACE_PAGE_COPY.viewActivity}
                </Button>
              </div>
            </section>

            <section className={ACCOUNT_WORKSPACE_IDENTITY_CARD_CLASS}>
              <div className="flex flex-col gap-2">
                <h2 className={ACCOUNT_WORKSPACE_IDENTITY_TITLE_CLASS}>
                  {ACCOUNT_WORKSPACE_PAGE_COPY.dataOwnershipTitle}
                </h2>
                <p className={ACCOUNT_WORKSPACE_IDENTITY_SUBTITLE_CLASS}>
                  {ACCOUNT_WORKSPACE_PAGE_COPY.dataOwnershipSubtitle}
                </p>
              </div>
              <p className={ACCOUNT_CONTROLS_BODY_CLASS}>
                {ACCOUNT_WORKSPACE_PAGE_COPY.dataOwnershipBody}
              </p>
              <div className={ACCOUNT_CONTROLS_ACTIONS_CLASS}>
                <Button type="button" variant="op-secondary" disabled>
                  {ACCOUNT_WORKSPACE_PAGE_COPY.viewPrivacySettings}
                </Button>
                <Button
                  type="button"
                  variant="op-secondary"
                  disabled={snap.isSaving}
                  onClick={() => {
                    pageModule.requestExportGuestData()
                  }}
                >
                  {ACCOUNT_WORKSPACE_PAGE_COPY.exportGuestData}
                </Button>
              </div>
            </section>

            <section className={ACCOUNT_WORKSPACE_IDENTITY_CARD_CLASS}>
              <div className="flex flex-col gap-2">
                <h2 className={ACCOUNT_WORKSPACE_IDENTITY_TITLE_CLASS}>
                  {ACCOUNT_WORKSPACE_PAGE_COPY.dangerZoneTitle}
                </h2>
                <p className={ACCOUNT_WORKSPACE_IDENTITY_SUBTITLE_CLASS}>
                  {ACCOUNT_WORKSPACE_PAGE_COPY.dangerZoneSubtitle}
                </p>
              </div>
              <p className={ACCOUNT_CONTROLS_BODY_CLASS}>
                {ACCOUNT_WORKSPACE_PAGE_COPY.dangerZoneBody}
              </p>
              {!snap.isAccountOwner ? (
                <p className={ACCOUNT_CONTROLS_BODY_CLASS}>
                  {ACCOUNT_WORKSPACE_PAGE_COPY.dangerZoneOwnerOnlyHelper}
                </p>
              ) : null}
              <div className={ACCOUNT_CONTROLS_ACTIONS_CLASS}>
                {isPaused ? (
                  <Button
                    type="button"
                    variant="op-secondary"
                    disabled={dangerZoneDisabled}
                    onClick={() => {
                      pageModule.requestResumeWorkspace()
                    }}
                  >
                    {ACCOUNT_WORKSPACE_PAGE_COPY.resumeWorkspace}
                  </Button>
                ) : (
                  <Button
                    type="button"
                    variant="op-secondary"
                    disabled={dangerZoneDisabled}
                    onClick={() => {
                      pageModule.requestPauseWorkspace()
                    }}
                  >
                    {ACCOUNT_WORKSPACE_PAGE_COPY.pauseWorkspace}
                  </Button>
                )}
                <Button
                  type="button"
                  variant="op-secondary"
                  disabled={dangerZoneDisabled}
                  onClick={() => {
                    void pageModule.requestTransferOwnership()
                  }}
                >
                  {ACCOUNT_WORKSPACE_PAGE_COPY.transferOwnership}
                </Button>
                <Button
                  type="button"
                  variant="op-tertiary"
                  disabled={dangerZoneDisabled}
                  onClick={() => {
                    void pageModule.requestAccountExport()
                  }}
                >
                  {ACCOUNT_WORKSPACE_PAGE_COPY.requestAccountExport}
                </Button>
                <Button
                  type="button"
                  variant="op-tertiary"
                  disabled={dangerZoneDisabled}
                  onClick={() => {
                    void pageModule.requestAccountClosure()
                  }}
                >
                  {ACCOUNT_WORKSPACE_PAGE_COPY.requestAccountClosure}
                </Button>
              </div>
            </section>
          </div>
        </TabsContent>
        </div>
      </Tabs>

      <AccountWorkspaceConfirmDialog
        open={snap.renameConfirmOpen}
        title={ACCOUNT_WORKSPACE_PAGE_COPY.renameTitle}
        body={ACCOUNT_WORKSPACE_PAGE_COPY.renameBody}
        primaryLabel={ACCOUNT_WORKSPACE_PAGE_COPY.renameConfirm}
        busy={snap.isSaving}
        onOpenChange={(open) => {
          if (!open) {
            pageModule.closeRenameConfirm()
          }
        }}
        onPrimary={() => {
          void pageModule.confirmRename()
        }}
        onCancel={() => {
          pageModule.cancelRenameConfirm()
        }}
      />

      <AccountWorkspaceConfirmDialog
        open={snap.leaveDirtyOpen}
        title={ACCOUNT_WORKSPACE_PAGE_COPY.leaveDirtyTitle}
        body={ACCOUNT_WORKSPACE_PAGE_COPY.leaveDirtyBody}
        primaryLabel={ACCOUNT_WORKSPACE_PAGE_COPY.leaveDirtySave}
        busy={snap.isSaving}
        onOpenChange={(open) => {
          if (!open) {
            pageModule.closeLeaveDirty()
          }
        }}
        onPrimary={() => {
          void pageModule.confirmLeaveDirtySave()
        }}
        onCancel={() => {
          void pageModule.confirmLeaveDirtyCancel()
        }}
      />

      <AccountWorkspaceConfirmDialog
        open={snap.workspaceStatusConfirm != null}
        title={
          snap.workspaceStatusConfirm === "resume"
            ? ACCOUNT_WORKSPACE_PAGE_COPY.resumeTitle
            : ACCOUNT_WORKSPACE_PAGE_COPY.pauseTitle
        }
        body={
          snap.workspaceStatusConfirm === "resume"
            ? ACCOUNT_WORKSPACE_PAGE_COPY.resumeBody
            : ACCOUNT_WORKSPACE_PAGE_COPY.pauseBody
        }
        primaryLabel={
          snap.workspaceStatusConfirm === "resume"
            ? ACCOUNT_WORKSPACE_PAGE_COPY.resumeWorkspace
            : ACCOUNT_WORKSPACE_PAGE_COPY.pauseWorkspace
        }
        busy={snap.isSaving}
        onOpenChange={(open) => {
          if (!open) {
            pageModule.closeWorkspaceStatusConfirm()
          }
        }}
        onPrimary={() => {
          void pageModule.confirmWorkspaceStatusChange()
        }}
        onCancel={() => {
          pageModule.cancelWorkspaceStatusConfirm()
        }}
      />

      <AccountWorkspaceConfirmDialog
        open={accountRequestConfirmCopy != null}
        title={accountRequestConfirmCopy?.title ?? ""}
        body={accountRequestConfirmCopy?.body ?? ""}
        primaryLabel={accountRequestConfirmCopy?.primaryLabel ?? ""}
        busy={snap.isSaving}
        onOpenChange={(open) => {
          if (!open) {
            pageModule.cancelAccountRequestConfirm()
          }
        }}
        onPrimary={() => {
          void pageModule.confirmAccountRequest()
        }}
        onCancel={() => {
          pageModule.cancelAccountRequestConfirm()
        }}
      />

      {snap.guestDataExportDialog != null ? (
        <GuestDataExportDialog
          format={snap.guestDataExportDialog.format}
          isPreparing={snap.guestDataExportDialog.isPreparing}
          onOpenChange={(open) => {
            if (!open) {
              pageModule.closeGuestDataExportDialog()
            }
          }}
          onFormatChange={(format) => {
            pageModule.setGuestDataExportFormat(format)
          }}
          onDownload={() => {
            void pageModule.downloadGuestDataExport()
          }}
        />
      ) : null}
    </div>
  )
}
