import { useSyncExternalStore } from "react"

import { useWriteActiveTabToSearchParams } from "@/hooks/useWriteActiveTabToSearchParams"

import { GuestPermissionsSection } from "@/components/dashboard/operator/PrivacyConsent/GuestPermissionsSection"
import { ConsentWordingSection } from "@/components/dashboard/operator/PrivacyConsent/ConsentWordingSection"
import { PermissionRecordsSection } from "@/components/dashboard/operator/PrivacyConsent/PermissionRecordsSection"
import { PrivacyActivitySection } from "@/components/dashboard/operator/PrivacyConsent/PrivacyActivitySection"
import { PrivacySetupStatusSection } from "@/components/dashboard/operator/PrivacyConsent/PrivacySetupStatusSection"
import { usePrivacyConsentPageModuleApi } from "@/components/dashboard/operator/PrivacyConsent/utils/privacyConsentPageModuleContext"
import { OperatorFilterSheetDialog } from "@/components/dashboard/operator/FilterSheet/OperatorFilterSheetDialog"
import { Button } from "@/components/ui/button"
import { Spinner } from "@/components/ui/spinner"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import {
  ACCOUNT_WORKSPACE_FULL_BLEED_BOTTOM,
  ACCOUNT_WORKSPACE_FULL_BLEED_X,
  ACCOUNT_WORKSPACE_PAGE_STACK_CLASS,
  ACCOUNT_WORKSPACE_PAGE_SUBTITLE_CLASS,
  ACCOUNT_WORKSPACE_SHELL_PAD_BOTTOM,
  ACCOUNT_WORKSPACE_SHELL_PAD_X,
  ACCOUNT_WORKSPACE_TAB_BODY_CLASS,
  ACCOUNT_WORKSPACE_TAB_LIST_CLASS,
  ACCOUNT_WORKSPACE_TAB_TRIGGER_CLASS,
  ACCOUNT_WORKSPACE_TABS_RULE_CLASS,
} from "@/lib/operatorAccountWorkspace/accountWorkspacePresentation"
import {
  GUESTS_PAGE_HEADER_COPY_CLASS,
  GUESTS_PAGE_HEADER_ROW_CLASS,
  GUESTS_PAGE_TITLE_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import { permissionRecordsFilterSheetSchema } from "@/lib/operatorPrivacyConsent/permissionRecordsFilterSheetSchema"
import {
  PRIVACY_CONSENT_PAGE_COPY,
  type PrivacyConsentTabId,
} from "@/lib/operatorPrivacyConsent/privacyConsentPresentation"
import { cn } from "@/lib/utils"

const LOAD_ERROR_MESSAGE = "Could not load Privacy & consent."

export function PrivacyConsentPage() {
  const pageModule = usePrivacyConsentPageModuleApi()
  const snap = useSyncExternalStore(
    pageModule.subscribe,
    pageModule.getSnapshot,
    pageModule.getSnapshot
  )
  const copy = PRIVACY_CONSENT_PAGE_COPY

  useWriteActiveTabToSearchParams(snap.activeTabId)

  const permissionRecordsSchema = permissionRecordsFilterSheetSchema({
    locations: snap.permissionRecordsLocationOptions,
  })

  if (snap.loadStatus === "idle" || snap.loadStatus === "loading") {
    return (
      <div
        className="flex flex-1 items-center justify-center"
        role="status"
        aria-live="polite"
        aria-label="Loading Privacy and consent"
      >
        <Spinner />
      </div>
    )
  }

  if (snap.loadStatus === "error") {
    return (
      <div className="flex flex-1 flex-col items-center justify-center gap-3">
        <p className="m-0 text-sm text-muted-foreground">{LOAD_ERROR_MESSAGE}</p>
        <Button
          type="button"
          variant="op-secondary"
          onClick={() => {
            void pageModule.retryLoad()
          }}
        >
          Retry
        </Button>
      </div>
    )
  }

  return (
    <div className={ACCOUNT_WORKSPACE_PAGE_STACK_CLASS}>
      <div className={GUESTS_PAGE_HEADER_ROW_CLASS}>
        <div className={GUESTS_PAGE_HEADER_COPY_CLASS}>
          <h1 className={GUESTS_PAGE_TITLE_CLASS}>{copy.title}</h1>
          <p className={ACCOUNT_WORKSPACE_PAGE_SUBTITLE_CLASS}>
            {copy.subtitle}
          </p>
        </div>
      </div>

      <Tabs
        value={snap.activeTabId}
        onValueChange={(value) => {
          pageModule.requestTabChange(value as PrivacyConsentTabId)
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
          <TabsContent value="privacy-setup" className="mt-0 flex flex-col gap-5">
            <PrivacySetupStatusSection rows={snap.privacySetupRows} />
            <ConsentWordingSection
              emailWording={snap.emailConsentWording}
              smsWording={snap.smsConsentWording}
              emailEnabled={snap.emailMarketingEnabled}
              smsEnabled={snap.smsMarketingEnabled}
              readOnly={!snap.actorCanManage}
              onSave={pageModule.saveConsentWording}
            />
          </TabsContent>

          <TabsContent value="guest-permissions" className="mt-0">
            <GuestPermissionsSection
              cards={snap.guestPermissions}
              readOnly={!snap.actorCanManage}
              onEnabledChange={(id, enabled) => {
                void pageModule.setGuestPermissionEnabled(id, enabled)
              }}
            />
          </TabsContent>

          <TabsContent value="permission-records" className="mt-0">
            <PermissionRecordsSection
              searchQuery={snap.permissionRecordsSearchQuery}
              filterChips={snap.permissionRecordsFilterChips}
              filterChipCount={snap.permissionRecordsFilterChipCount}
              rows={snap.permissionRecordsRows}
              empty={snap.permissionRecordsEmpty}
              canViewGuests={snap.canViewGuests}
              onSearchQueryChange={pageModule.setPermissionRecordsSearchQuery}
              onOpenFilters={pageModule.openPermissionRecordsFilters}
              onRemoveFilterChip={pageModule.removePermissionRecordsFilterChip}
              onClearSearchAndFilters={
                pageModule.clearPermissionRecordsSearchAndFilters
              }
              onViewRecord={pageModule.viewPermissionRecord}
              pageRangeLabel={snap.permissionRecordsPageRangeLabel}
              canGoPrevious={snap.permissionRecordsCanGoPrevious}
              canGoNext={snap.permissionRecordsCanGoNext}
              onPreviousPage={pageModule.goToPreviousPermissionRecordsPage}
              onNextPage={pageModule.goToNextPermissionRecordsPage}
            />
          </TabsContent>

          <TabsContent value="activity" className="mt-0">
            <PrivacyActivitySection items={snap.activityItems} />
          </TabsContent>
        </div>
      </Tabs>

      <OperatorFilterSheetDialog
        open={snap.permissionRecordsFiltersOpen}
        title={copy.permissionRecordsFiltersTitle}
        schema={permissionRecordsSchema}
        session={snap.permissionRecordsFiltersSession}
        onSessionChange={pageModule.setPermissionRecordsFiltersSession}
        onOpenChange={(open) => pageModule.setPermissionRecordsFiltersOpen(open)}
        onApply={() => pageModule.applyPermissionRecordsFilters()}
      />
    </div>
  )
}
