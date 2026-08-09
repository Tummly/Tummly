import type { ReactNode } from "react"
import { XIcon } from "lucide-react"

import { Button } from "@/components/ui/button"
import {
  Drawer,
  DrawerClose,
  DrawerContent,
  DrawerDescription,
  DrawerTitle,
} from "@/components/ui/drawer"
import { Spinner } from "@/components/ui/spinner"
import {
  CAMPAIGN_TEMPLATE_PREVIEW_CONTENT_CLASS,
  CAMPAIGN_TEMPLATE_PREVIEW_OVERLAY_CLASS,
} from "@/lib/operatorCampaigns/campaignTemplatePreviewPresentation"
import { OPERATOR_RIGHT_DRAWER_BODY_CLASS } from "@/lib/operatorHome/shellResponsivePresentation"
import { cn } from "@/lib/utils"

export type CampaignDetailPreviewShellProps = {
  open: boolean
  title: string
  subtitle: string
  closeAriaLabel: string
  loadStatus: "idle" | "loading" | "loaded" | "error"
  loadError: string | null
  retryLabel: string
  footerDisclaimer?: string | null
  primaryActionLabel?: string | null
  secondaryActionLabel?: string | null
  onOpenChange: (open: boolean) => void
  onRetry: () => void
  onPrimaryAction?: () => void
  children?: ReactNode
}

/**
 * Shared Campaign Detail / Campaign Preview chrome (Figma 5116:19403).
 * Catalogue S6 and later Campaign Detail adapters supply body + actions.
 */
export function CampaignDetailPreviewShell({
  open,
  title,
  subtitle,
  closeAriaLabel,
  loadStatus,
  loadError,
  retryLabel,
  footerDisclaimer = null,
  primaryActionLabel = null,
  secondaryActionLabel = null,
  onOpenChange,
  onRetry,
  onPrimaryAction,
  children,
}: CampaignDetailPreviewShellProps) {
  const showBody = loadStatus === "loaded"

  return (
    <Drawer open={open} onOpenChange={onOpenChange} direction="right">
      <DrawerContent
        overlayClassName={CAMPAIGN_TEMPLATE_PREVIEW_OVERLAY_CLASS}
        className={CAMPAIGN_TEMPLATE_PREVIEW_CONTENT_CLASS}
      >
        <div className="flex min-h-0 flex-1 flex-col">
          <header className="flex shrink-0 items-start gap-[22px] px-[22px] pb-[22px] pt-8">
            <div className="flex min-w-0 flex-1 flex-col gap-3">
              <DrawerTitle className="m-0 text-2xl font-bold leading-normal text-op-text-primary">
                {title}
              </DrawerTitle>
              <DrawerDescription className="m-0 max-w-[385px] text-sm font-medium leading-normal text-[var(--op-color-gray-550)]">
                {subtitle}
              </DrawerDescription>
            </div>
            <DrawerClose asChild>
              <Button
                type="button"
                variant="op-collapse"
                size="icon"
                className="shrink-0"
                aria-label={closeAriaLabel}
              >
                <XIcon className="size-[18px]" aria-hidden />
              </Button>
            </DrawerClose>
          </header>

          {loadStatus === "loading" || loadStatus === "idle" ? (
            <div
              className={cn(
                OPERATOR_RIGHT_DRAWER_BODY_CLASS,
                "flex items-center justify-center px-[22px] pb-[22px]"
              )}
              role="status"
              aria-live="polite"
              aria-label="Loading campaign preview"
            >
              <Spinner />
            </div>
          ) : null}

          {loadStatus === "error" ? (
            <div
              className={cn(
                OPERATOR_RIGHT_DRAWER_BODY_CLASS,
                "flex flex-col items-center justify-center gap-3 px-[22px] pb-[22px]"
              )}
            >
              <p className="m-0 text-sm text-muted-foreground">{loadError}</p>
              <Button
                type="button"
                variant="op-secondary"
                onClick={() => {
                  onRetry()
                }}
              >
                {retryLabel}
              </Button>
            </div>
          ) : null}

          {showBody ? (
            <>
              <div className={OPERATOR_RIGHT_DRAWER_BODY_CLASS}>{children}</div>
              {footerDisclaimer != null
              || primaryActionLabel != null
              || secondaryActionLabel != null ? (
                <footer className="flex shrink-0 flex-col gap-4 border-t border-op-card-border p-[22px]">
                  {footerDisclaimer != null ? (
                    <p className="m-0 text-sm font-medium leading-normal text-[var(--op-color-gray-550)]">
                      {footerDisclaimer}
                    </p>
                  ) : null}
                  <div className="flex flex-wrap items-center gap-3">
                    {primaryActionLabel != null ? (
                      <Button
                        type="button"
                        variant="op-primary"
                        onClick={onPrimaryAction}
                      >
                        {primaryActionLabel}
                      </Button>
                    ) : null}
                    {secondaryActionLabel != null ? (
                      <DrawerClose asChild>
                        <Button type="button" variant="op-tertiary">
                          {secondaryActionLabel}
                        </Button>
                      </DrawerClose>
                    ) : null}
                  </div>
                </footer>
              ) : null}
            </>
          ) : null}
        </div>
      </DrawerContent>
    </Drawer>
  )
}
