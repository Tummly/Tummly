import { useEffect, useRef, useSyncExternalStore } from "react"
import { useNavigate, useSearchParams } from "react-router-dom"

import brandLogoPlaceholder from "@/assets/images/brand-logo-placeholder.png"
import { AccountWorkspaceConfirmDialog } from "@/components/dashboard/operator/AccountWorkspace/AccountWorkspaceConfirmDialog"
import { useAccountWorkspacePageModuleApi } from "@/components/dashboard/operator/AccountWorkspace/utils/accountWorkspacePageModuleContext"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import {
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
} from "@/components/ui/tabs"
import {
  ACCOUNT_WORKSPACE_PAGE_COPY,
  formatAccountWorkspaceLastSaved,
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
  GUESTS_SECTION_TITLE_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import { cn } from "@/lib/utils"

function StatusRow({ label, value }: { label: string; value: string }) {
  return (
    <div className={GUESTS_DETAIL_FIELD_CLASS}>
      <p className={GUESTS_DETAIL_FIELD_LABEL_CLASS}>{label}</p>
      <p className={GUESTS_DETAIL_FIELD_VALUE_CLASS}>{value}</p>
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
  const [searchParams, setSearchParams] = useSearchParams()
  const navigate = useNavigate()
  const fileInputRef = useRef<HTMLInputElement>(null)

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

        <TabsContent value="business-details" className="mt-0" />
        <TabsContent value="key-contacts" className="mt-0" />
        <TabsContent value="workspace-defaults" className="mt-0" />
        <TabsContent value="account-controls" className="mt-0" />
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
    </div>
  )
}
