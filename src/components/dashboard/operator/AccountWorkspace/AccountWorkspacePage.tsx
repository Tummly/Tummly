import { useEffect, useRef, useState, useSyncExternalStore } from "react"
import { useNavigate, useSearchParams } from "react-router-dom"

import brandLogoPlaceholder from "@/assets/images/brand-logo-placeholder.png"
import { AccountWorkspaceConfirmDialog } from "@/components/dashboard/operator/AccountWorkspace/AccountWorkspaceConfirmDialog"
import { GuestDataExportDialog } from "@/components/dashboard/operator/AccountWorkspace/GuestDataExportDialog"
import { useAccountWorkspacePageModuleApi } from "@/components/dashboard/operator/AccountWorkspace/utils/accountWorkspacePageModuleContext"
import { AddressPostcodeFields } from "@/components/form/AddressPostcodeFields"
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
  ACCOUNT_WORKSPACE_PAGE_COPY,
  ACCOUNT_WORKSPACE_SELECT_MENU_CLASS,
  DEFAULT_REPORTING_PERIOD_OPTIONS,
  LEGAL_STRUCTURE_OPTIONS,
  WEEK_STARTS_ON_OPTIONS,
  accountRequestConfirmLabels,
  formatAccountWorkspaceLastSaved,
  isUnitedKingdomCountry,
} from "@/lib/operatorAccountWorkspace/accountWorkspacePresentation"
import type { AccountWorkspaceTabId } from "@/lib/operatorAccountWorkspace/accountWorkspacePresentation"
import { resolveBrandLogoSrc } from "@/lib/brandLogo/resolveBrandLogoSrc"
import {
  BROWSER_BACK_HREF,
  registerLeaveDirtyGuard,
} from "@/lib/operatorNavigation/leaveDirtyGuard"
import {
  GUESTS_DETAIL_FIELD_CLASS,
  GUESTS_DETAIL_FIELD_LABEL_CLASS,
  GUESTS_DETAIL_FIELD_VALUE_CLASS,
  GUESTS_DETAIL_ROWS_STACK_CLASS,
  GUESTS_PAGE_HEADER_COPY_CLASS,
  GUESTS_PAGE_HEADER_ROW_CLASS,
  GUESTS_PAGE_PRIMARY_BUTTON_CLASS,
  GUESTS_PAGE_SECONDARY_BUTTON_CLASS,
  GUESTS_PAGE_STACK_CLASS,
  GUESTS_PAGE_SUBTITLE_CLASS,
  GUESTS_PAGE_TITLE_CLASS,
  GUESTS_SECTION_CLASS,
  GUESTS_SECTION_SUBTITLE_CLASS,
  GUESTS_SECTION_TITLE_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import { cn } from "@/lib/utils"

const FIELD_LABEL_CLASS = "text-sm font-medium text-foreground"
const FIELD_HELPER_CLASS = "m-0 text-xs font-medium text-muted-foreground"
const FIELD_STACK_CLASS = "flex flex-col gap-2"
const FIELD_GRID_CLASS = "grid grid-cols-1 gap-6 lg:grid-cols-2"

function StatusRow({ label, value }: { label: string; value: string }) {
  return (
    <div className={GUESTS_DETAIL_FIELD_CLASS}>
      <p className={GUESTS_DETAIL_FIELD_LABEL_CLASS}>{label}</p>
      <p className={GUESTS_DETAIL_FIELD_VALUE_CLASS}>{value}</p>
    </div>
  )
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
      <label htmlFor={id} className={FIELD_LABEL_CLASS}>
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
        <SelectTrigger id={id} className="h-8 w-full">
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
            <SelectItem key={member.userId} value={String(member.userId)}>
              <span className="flex flex-col gap-0.5 text-left">
                <span>{member.fullName}</span>
                <span className="text-xs font-normal text-muted-foreground">
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
  const [addressOverridden, setAddressOverridden] = useState(false)

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
  const workspaceNameError = snap.accountDetails.workspaceNameError
  const business = snap.businessDetails
  const keyContacts = snap.keyContacts
  const workspaceDefaults = snap.workspaceDefaults
  const isUkAddress = isUnitedKingdomCountry(business.country)
  const eligibleMembers = keyContacts.eligibleMembers
  const ownerMember =
    keyContacts.accountOwner != null
      ? [keyContacts.accountOwner]
      : eligibleMembers
  const isPaused = status?.workspaceStatus.toLowerCase() === "paused"
  const dangerZoneDisabled = !snap.isAccountOwner || snap.isSaving

  return (
    <div className={GUESTS_PAGE_STACK_CLASS}>
      <div className={GUESTS_PAGE_HEADER_ROW_CLASS}>
        <div className={GUESTS_PAGE_HEADER_COPY_CLASS}>
          <h1 className={GUESTS_PAGE_TITLE_CLASS}>
            {ACCOUNT_WORKSPACE_PAGE_COPY.title}
          </h1>
          <p className={GUESTS_PAGE_SUBTITLE_CLASS}>
            {ACCOUNT_WORKSPACE_PAGE_COPY.subtitle}
          </p>
          <p className="m-0 text-sm font-medium text-muted-foreground">
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
        className="gap-6"
      >
        <TabsList
          variant="line"
          className="h-auto w-full justify-start overflow-x-auto"
        >
          {snap.tabs.map((tab) => (
            <TabsTrigger
              key={tab.id}
              value={tab.id}
              className={cn(
                "rounded-none px-3 py-2 text-sm font-medium text-op-button-date-text",
                "data-active:font-semibold data-active:text-foreground",
                "group-data-[variant=line]/tabs-list:data-active:after:bg-op-button-primary-background"
              )}
            >
              {tab.label}
            </TabsTrigger>
          ))}
        </TabsList>

        <TabsContent value="account-details" className="mt-0">
          <div className="flex flex-col gap-6">
            <section className={GUESTS_SECTION_CLASS}>
              <h2 className={GUESTS_SECTION_TITLE_CLASS}>Identity</h2>
              <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
                <div className="flex flex-col gap-2">
                  <label
                    htmlFor="workspace-name"
                    className="text-sm font-medium text-foreground"
                  >
                    Workspace name
                  </label>
                  <Input
                    id="workspace-name"
                    value={snap.accountDetails.workspaceName}
                    maxLength={200}
                    aria-invalid={workspaceNameError != null}
                    aria-describedby={
                      workspaceNameError != null
                        ? "workspace-name-error"
                        : undefined
                    }
                    onChange={(event) => {
                      pageModule.setWorkspaceName(event.target.value)
                    }}
                  />
                  {workspaceNameError != null ? (
                    <p
                      id="workspace-name-error"
                      className="m-0 text-sm text-destructive"
                    >
                      {workspaceNameError}
                    </p>
                  ) : null}
                </div>
                <div className="flex flex-col gap-2">
                  <p className="text-sm font-medium text-foreground">
                    Account structure
                  </p>
                  <Input
                    value={snap.accountDetails.accountStructure}
                    readOnly
                    disabled
                  />
                </div>
                <div className="flex flex-col gap-2">
                  <p className="text-sm font-medium text-foreground">
                    Business category
                  </p>
                  <Input
                    value={
                      snap.accountDetails.businessCategoryLabel
                      ?? snap.accountDetails.businessCategory
                      ?? ""
                    }
                    readOnly
                    disabled
                  />
                </div>
                <div className="flex flex-col gap-2">
                  <p className="text-sm font-medium text-foreground">
                    Main operating country
                  </p>
                  <Input
                    value={snap.accountDetails.mainOperatingCountry}
                    readOnly
                    disabled
                  />
                </div>
              </div>

              <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
                <span className="relative size-16 shrink-0 overflow-hidden rounded-[2px]">
                  <img
                    src={
                      snap.accountDetails.brandLogoPreviewUrl?.startsWith(
                        "blob:"
                      )
                        ? snap.accountDetails.brandLogoPreviewUrl
                        : resolveBrandLogoSrc(
                            snap.accountDetails.brandLogoPreviewUrl
                          ) ?? brandLogoPlaceholder
                    }
                    alt=""
                    className="size-full object-cover"
                  />
                </span>
                <div className="flex flex-wrap gap-3">
                  <Button
                    type="button"
                    variant="op-secondary"
                    onClick={() => fileInputRef.current?.click()}
                  >
                    {ACCOUNT_WORKSPACE_PAGE_COPY.uploadImage}
                  </Button>
                  <Button type="button" variant="op-tertiary" disabled>
                    {ACCOUNT_WORKSPACE_PAGE_COPY.manageGuestFacingBrand}
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
            </section>

            <section className={GUESTS_SECTION_CLASS}>
              <div className="flex items-center justify-between gap-4">
                <h2 className={GUESTS_SECTION_TITLE_CLASS}>Account status</h2>
                {status != null ? (
                  <Badge variant="soft">{status.workspaceStatus}</Badge>
                ) : null}
              </div>
              {status != null ? (
                <div className={GUESTS_DETAIL_ROWS_STACK_CLASS}>
                  <StatusRow
                    label="Workspace status"
                    value={status.workspaceStatus}
                  />
                  <StatusRow label="Plan status" value={status.planStatus} />
                  <StatusRow
                    label="Billing status"
                    value={status.billingStatus}
                  />
                  <StatusRow
                    label="Account created"
                    value={formatDateOnly(status.accountCreatedAt)}
                  />
                  <StatusRow
                    label="Active locations"
                    value={String(status.activeLocations)}
                  />
                  <StatusRow
                    label="Team members"
                    value={String(status.teamMembers)}
                  />
                  <StatusRow
                    label="Guest profiles"
                    value={String(status.guestProfiles)}
                  />
                  <StatusRow
                    label="Guest form status"
                    value={status.guestFormStatus}
                  />
                  <StatusRow
                    label="Last account update"
                    value={formatDateOnly(status.lastAccountUpdateAt)}
                  />
                </div>
              ) : null}
            </section>
          </div>
        </TabsContent>

        <TabsContent value="business-details" className="mt-0">
          <div className="flex flex-col gap-6">
            <section className={GUESTS_SECTION_CLASS}>
              <div className="flex flex-col gap-2">
                <h2 className={GUESTS_SECTION_TITLE_CLASS}>
                  {ACCOUNT_WORKSPACE_PAGE_COPY.businessIdentityTitle}
                </h2>
                <p className={GUESTS_SECTION_SUBTITLE_CLASS}>
                  {ACCOUNT_WORKSPACE_PAGE_COPY.businessIdentitySubtitle}
                </p>
              </div>

              <div className="flex flex-col gap-6">
                <div className={FIELD_STACK_CLASS}>
                  <label
                    htmlFor="legal-structure"
                    className={FIELD_LABEL_CLASS}
                  >
                    {ACCOUNT_WORKSPACE_PAGE_COPY.legalStructure}
                  </label>
                  <Select
                    value={
                      business.legalStructure === ""
                        ? "__clear__"
                        : business.legalStructure
                    }
                    onValueChange={(value) => {
                      pageModule.setLegalStructure(
                        value === "__clear__" ? "" : value
                      )
                    }}
                  >
                    <SelectTrigger id="legal-structure" className="h-8 w-full">
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
                      <SelectItem value="__clear__">
                        {ACCOUNT_WORKSPACE_PAGE_COPY.legalStructurePlaceholder}
                      </SelectItem>
                      {LEGAL_STRUCTURE_OPTIONS.map((option) => (
                        <SelectItem key={option.value} value={option.value}>
                          {option.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>

                <div className={FIELD_GRID_CLASS}>
                  <div className={FIELD_STACK_CLASS}>
                    <label
                      htmlFor="legal-business-name"
                      className={FIELD_LABEL_CLASS}
                    >
                      {ACCOUNT_WORKSPACE_PAGE_COPY.legalBusinessName}
                    </label>
                    <Input
                      id="legal-business-name"
                      value={business.legalBusinessName}
                      maxLength={200}
                      onChange={(event) => {
                        pageModule.setLegalBusinessName(event.target.value)
                      }}
                    />
                    <p className={FIELD_HELPER_CLASS}>
                      {ACCOUNT_WORKSPACE_PAGE_COPY.legalBusinessNameHelper}
                    </p>
                  </div>
                  <div className="flex flex-col gap-3">
                    <div className={FIELD_STACK_CLASS}>
                      <label
                        htmlFor="trading-name"
                        className={FIELD_LABEL_CLASS}
                      >
                        {ACCOUNT_WORKSPACE_PAGE_COPY.tradingName}
                      </label>
                      <Input
                        id="trading-name"
                        value={business.tradingName}
                        maxLength={200}
                        disabled={business.sameAsLegalBusinessName}
                        onChange={(event) => {
                          pageModule.setTradingName(event.target.value)
                        }}
                      />
                      <p className={FIELD_HELPER_CLASS}>
                        {ACCOUNT_WORKSPACE_PAGE_COPY.tradingNameHelper}
                      </p>
                    </div>
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

                <div className={FIELD_GRID_CLASS}>
                  <div className={FIELD_STACK_CLASS}>
                    <label
                      htmlFor="company-number"
                      className={FIELD_LABEL_CLASS}
                    >
                      {ACCOUNT_WORKSPACE_PAGE_COPY.companyNumber}
                    </label>
                    <Input
                      id="company-number"
                      value={business.companyNumber}
                      maxLength={50}
                      onChange={(event) => {
                        pageModule.setCompanyNumber(event.target.value)
                      }}
                    />
                    <p className={FIELD_HELPER_CLASS}>
                      {ACCOUNT_WORKSPACE_PAGE_COPY.companyNumberHelper}
                    </p>
                  </div>
                  <div className={FIELD_STACK_CLASS}>
                    <label htmlFor="vat-number" className={FIELD_LABEL_CLASS}>
                      {ACCOUNT_WORKSPACE_PAGE_COPY.vatNumber}
                    </label>
                    <Input
                      id="vat-number"
                      value={business.vatNumber}
                      maxLength={50}
                      onChange={(event) => {
                        pageModule.setVatNumber(event.target.value)
                      }}
                    />
                    <p className={FIELD_HELPER_CLASS}>
                      {ACCOUNT_WORKSPACE_PAGE_COPY.vatNumberHelper}
                    </p>
                  </div>
                </div>

                <div className={FIELD_STACK_CLASS}>
                  <label
                    htmlFor="country-of-registration"
                    className={FIELD_LABEL_CLASS}
                  >
                    {ACCOUNT_WORKSPACE_PAGE_COPY.countryOfRegistration}
                  </label>
                  <Input
                    id="country-of-registration"
                    value={business.countryOfRegistration}
                    maxLength={100}
                    onChange={(event) => {
                      pageModule.setCountryOfRegistration(event.target.value)
                    }}
                  />
                  <p className={FIELD_HELPER_CLASS}>
                    {ACCOUNT_WORKSPACE_PAGE_COPY.countryOfRegistrationHelper}
                  </p>
                </div>
              </div>
            </section>

            <section className={GUESTS_SECTION_CLASS}>
              <div className="flex flex-col gap-2">
                <h2 className={GUESTS_SECTION_TITLE_CLASS}>
                  {ACCOUNT_WORKSPACE_PAGE_COPY.businessAddressTitle}
                </h2>
                <p className={GUESTS_SECTION_SUBTITLE_CLASS}>
                  {ACCOUNT_WORKSPACE_PAGE_COPY.businessAddressSubtitle}
                </p>
              </div>

              <div className="flex flex-col gap-6">
                {isUkAddress ? (
                  <AddressPostcodeFields
                    address={business.addressLine1}
                    postcode={business.postcode}
                    addressOverridden={addressOverridden}
                    onAddressChange={(value) => {
                      pageModule.setAddressLine1(value)
                    }}
                    onPostcodeChange={(value) => {
                      pageModule.setPostcode(value)
                    }}
                    onAddressOverriddenChange={setAddressOverridden}
                    postcodeError={business.postcodeError ?? undefined}
                    required={false}
                  />
                ) : (
                  <div className={FIELD_GRID_CLASS}>
                    <div className={FIELD_STACK_CLASS}>
                      <label
                        htmlFor="address-line-1"
                        className={FIELD_LABEL_CLASS}
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
                      />
                    </div>
                    <div className={FIELD_STACK_CLASS}>
                      <label
                        htmlFor="address-line-2-non-uk"
                        className={FIELD_LABEL_CLASS}
                      >
                        {ACCOUNT_WORKSPACE_PAGE_COPY.addressLine2}
                      </label>
                      <Input
                        id="address-line-2-non-uk"
                        value={business.addressLine2}
                        maxLength={500}
                        onChange={(event) => {
                          pageModule.setAddressLine2(event.target.value)
                        }}
                      />
                    </div>
                  </div>
                )}

                {isUkAddress ? (
                  <div className={FIELD_STACK_CLASS}>
                    <label
                      htmlFor="address-line-2"
                      className={FIELD_LABEL_CLASS}
                    >
                      {ACCOUNT_WORKSPACE_PAGE_COPY.addressLine2}
                    </label>
                    <Input
                      id="address-line-2"
                      value={business.addressLine2}
                      maxLength={500}
                      onChange={(event) => {
                        pageModule.setAddressLine2(event.target.value)
                      }}
                    />
                  </div>
                ) : null}

                <div className={FIELD_GRID_CLASS}>
                  <div className={FIELD_STACK_CLASS}>
                    <label htmlFor="town-city" className={FIELD_LABEL_CLASS}>
                      {ACCOUNT_WORKSPACE_PAGE_COPY.townCity}
                    </label>
                    <Input
                      id="town-city"
                      value={business.townCity}
                      maxLength={150}
                      onChange={(event) => {
                        pageModule.setTownCity(event.target.value)
                      }}
                    />
                  </div>
                  <div className={FIELD_STACK_CLASS}>
                    <label htmlFor="county" className={FIELD_LABEL_CLASS}>
                      {ACCOUNT_WORKSPACE_PAGE_COPY.county}
                    </label>
                    <Input
                      id="county"
                      value={business.county}
                      maxLength={150}
                      onChange={(event) => {
                        pageModule.setCounty(event.target.value)
                      }}
                    />
                  </div>
                </div>

                {isUkAddress ? (
                  <div className={FIELD_STACK_CLASS}>
                    <label
                      htmlFor="address-country"
                      className={FIELD_LABEL_CLASS}
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
                    />
                  </div>
                ) : (
                  <div className={FIELD_GRID_CLASS}>
                    <div className={FIELD_STACK_CLASS}>
                      <label
                        htmlFor="postcode-non-uk"
                        className={FIELD_LABEL_CLASS}
                      >
                        {ACCOUNT_WORKSPACE_PAGE_COPY.postcode}
                      </label>
                      <Input
                        id="postcode-non-uk"
                        value={business.postcode}
                        maxLength={20}
                        aria-invalid={business.postcodeError != null}
                        aria-describedby={
                          business.postcodeError != null
                            ? "postcode-error"
                            : undefined
                        }
                        onChange={(event) => {
                          pageModule.setPostcode(event.target.value)
                        }}
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
                        htmlFor="address-country-non-uk"
                        className={FIELD_LABEL_CLASS}
                      >
                        {ACCOUNT_WORKSPACE_PAGE_COPY.country}
                      </label>
                      <Input
                        id="address-country-non-uk"
                        value={business.country}
                        maxLength={100}
                        onChange={(event) => {
                          pageModule.setCountry(event.target.value)
                        }}
                      />
                    </div>
                  </div>
                )}
              </div>
            </section>
          </div>
        </TabsContent>
        <TabsContent value="key-contacts" className="mt-0">
          <section className={GUESTS_SECTION_CLASS}>
            <div className="flex flex-col gap-2">
              <h2 className={GUESTS_SECTION_TITLE_CLASS}>
                {ACCOUNT_WORKSPACE_PAGE_COPY.primaryResponsibilitiesTitle}
              </h2>
              <p className={GUESTS_SECTION_SUBTITLE_CLASS}>
                {ACCOUNT_WORKSPACE_PAGE_COPY.primaryResponsibilitiesSubtitle}
              </p>
            </div>

            <div className="flex flex-col gap-6">
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
              <p className={FIELD_HELPER_CLASS}>
                {ACCOUNT_WORKSPACE_PAGE_COPY.keyContactsTeamHelper}
              </p>
            </div>
          </section>
        </TabsContent>
        <TabsContent value="workspace-defaults" className="mt-0">
          <section className={GUESTS_SECTION_CLASS}>
            <div className="flex flex-col gap-2">
              <h2 className={GUESTS_SECTION_TITLE_CLASS}>
                {ACCOUNT_WORKSPACE_PAGE_COPY.workspaceDefaultsTitle}
              </h2>
              <p className={GUESTS_SECTION_SUBTITLE_CLASS}>
                {ACCOUNT_WORKSPACE_PAGE_COPY.workspaceDefaultsSubtitle}
              </p>
            </div>

            <div className="flex flex-col gap-6">
              <div className={FIELD_GRID_CLASS}>
                <div className={FIELD_STACK_CLASS}>
                  <label
                    htmlFor="week-starts-on"
                    className={FIELD_LABEL_CLASS}
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
                    <SelectTrigger id="week-starts-on" className="h-8 w-full">
                      <SelectValue placeholder="Select" />
                    </SelectTrigger>
                    <SelectContent
                      className={ACCOUNT_WORKSPACE_SELECT_MENU_CLASS}
                    >
                      {WEEK_STARTS_ON_OPTIONS.map((option) => (
                        <SelectItem key={option.value} value={option.value}>
                          {option.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
                <div className={FIELD_STACK_CLASS}>
                  <label
                    htmlFor="default-reporting-period"
                    className={FIELD_LABEL_CLASS}
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
                      className="h-8 w-full"
                    >
                      <SelectValue placeholder="Select" />
                    </SelectTrigger>
                    <SelectContent
                      className={ACCOUNT_WORKSPACE_SELECT_MENU_CLASS}
                    >
                      {DEFAULT_REPORTING_PERIOD_OPTIONS.map((option) => (
                        <SelectItem key={option.value} value={option.value}>
                          {option.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              </div>

              <div className={FIELD_STACK_CLASS}>
                <label
                  htmlFor="default-campaign-sender-name"
                  className={FIELD_LABEL_CLASS}
                >
                  {ACCOUNT_WORKSPACE_PAGE_COPY.defaultCampaignSenderName}
                </label>
                <Input
                  id="default-campaign-sender-name"
                  value={workspaceDefaults.defaultCampaignSenderName}
                  maxLength={200}
                  onChange={(event) => {
                    pageModule.setDefaultCampaignSenderName(event.target.value)
                  }}
                />
                <p className={FIELD_HELPER_CLASS}>
                  {ACCOUNT_WORKSPACE_PAGE_COPY.defaultCampaignSenderNameHelper}
                </p>
              </div>

              <div className={FIELD_GRID_CLASS}>
                <StatusRow
                  label={ACCOUNT_WORKSPACE_PAGE_COPY.defaultTimezone}
                  value={workspaceDefaults.defaultTimezone}
                />
                <StatusRow
                  label={ACCOUNT_WORKSPACE_PAGE_COPY.defaultCurrency}
                  value={workspaceDefaults.defaultCurrency}
                />
                <StatusRow
                  label={ACCOUNT_WORKSPACE_PAGE_COPY.defaultLanguage}
                  value={workspaceDefaults.defaultLanguage}
                />
                <StatusRow
                  label={ACCOUNT_WORKSPACE_PAGE_COPY.dateFormat}
                  value={workspaceDefaults.dateFormat}
                />
              </div>
            </div>
          </section>
        </TabsContent>
        <TabsContent value="account-controls" className="mt-0">
          <div className="flex flex-col gap-6">
            <section className={GUESTS_SECTION_CLASS}>
              <div className="flex flex-col gap-2">
                <div className="flex items-center justify-between gap-4">
                  <h2 className={GUESTS_SECTION_TITLE_CLASS}>
                    {ACCOUNT_WORKSPACE_PAGE_COPY.accountControlsStatusTitle}
                  </h2>
                  {status != null ? (
                    <Badge variant="soft">{status.workspaceStatus}</Badge>
                  ) : null}
                </div>
                <p className={GUESTS_SECTION_SUBTITLE_CLASS}>
                  {ACCOUNT_WORKSPACE_PAGE_COPY.accountControlsStatusSubtitle}
                </p>
              </div>
              {status != null ? (
                <div className={GUESTS_DETAIL_ROWS_STACK_CLASS}>
                  <StatusRow
                    label="Workspace status"
                    value={status.workspaceStatus}
                  />
                  <StatusRow label="Plan status" value={status.planStatus} />
                  <StatusRow
                    label="Billing status"
                    value={status.billingStatus}
                  />
                  <StatusRow
                    label="Account created"
                    value={formatDateOnly(status.accountCreatedAt)}
                  />
                  <StatusRow
                    label="Active locations"
                    value={String(status.activeLocations)}
                  />
                  <StatusRow
                    label="Team members"
                    value={String(status.teamMembers)}
                  />
                  <StatusRow
                    label="Guest profiles"
                    value={String(status.guestProfiles)}
                  />
                  <StatusRow
                    label="Guest form status"
                    value={status.guestFormStatus}
                  />
                  <StatusRow
                    label="Last account update"
                    value={formatDateOnly(status.lastAccountUpdateAt)}
                  />
                </div>
              ) : null}
              <div className="flex flex-wrap gap-3">
                <Button
                  type="button"
                  variant="op-secondary"
                  className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
                  disabled
                >
                  {ACCOUNT_WORKSPACE_PAGE_COPY.viewBilling}
                </Button>
                <Button
                  type="button"
                  variant="op-secondary"
                  className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
                  disabled
                >
                  {ACCOUNT_WORKSPACE_PAGE_COPY.viewActivity}
                </Button>
              </div>
            </section>

            <section className={GUESTS_SECTION_CLASS}>
              <div className="flex flex-col gap-2">
                <h2 className={GUESTS_SECTION_TITLE_CLASS}>
                  {ACCOUNT_WORKSPACE_PAGE_COPY.dataOwnershipTitle}
                </h2>
                <p className={GUESTS_SECTION_SUBTITLE_CLASS}>
                  {ACCOUNT_WORKSPACE_PAGE_COPY.dataOwnershipSubtitle}
                </p>
              </div>
              <p className="m-0 text-sm font-medium text-muted-foreground">
                {ACCOUNT_WORKSPACE_PAGE_COPY.dataOwnershipBody}
              </p>
              <div className="flex flex-wrap gap-3">
                <Button type="button" variant="op-secondary" disabled>
                  {ACCOUNT_WORKSPACE_PAGE_COPY.viewPrivacySettings}
                </Button>
                <Button
                  type="button"
                  variant="op-tertiary"
                  disabled={snap.isSaving}
                  onClick={() => {
                    pageModule.requestExportGuestData()
                  }}
                >
                  {ACCOUNT_WORKSPACE_PAGE_COPY.exportGuestData}
                </Button>
              </div>
            </section>

            <section className={GUESTS_SECTION_CLASS}>
              <div className="flex flex-col gap-2">
                <h2 className={GUESTS_SECTION_TITLE_CLASS}>
                  {ACCOUNT_WORKSPACE_PAGE_COPY.dangerZoneTitle}
                </h2>
                <p className={GUESTS_SECTION_SUBTITLE_CLASS}>
                  {ACCOUNT_WORKSPACE_PAGE_COPY.dangerZoneSubtitle}
                </p>
              </div>
              <p className="m-0 text-sm font-medium text-muted-foreground">
                {ACCOUNT_WORKSPACE_PAGE_COPY.dangerZoneBody}
              </p>
              {!snap.isAccountOwner ? (
                <p className="m-0 text-sm font-medium text-[var(--op-color-gray-550)]">
                  {ACCOUNT_WORKSPACE_PAGE_COPY.dangerZoneOwnerOnlyHelper}
                </p>
              ) : null}
              <div className="flex flex-wrap gap-3">
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
