import {
  ChevronLeftIcon,
  EllipsisVerticalIcon,
  XIcon,
} from "lucide-react"

import { Button } from "@/components/ui/button"
import { CheckboxLabel } from "@/components/ui/checkbox-label"
import {
  Drawer,
  DrawerClose,
  DrawerContent,
  DrawerDescription,
  DrawerTitle,
} from "@/components/ui/drawer"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { formatRelativeTime } from "@/lib/operatorHome/relativeTime"
import {
  OPERATOR_DRAWER_ACTION_ROW_CLASS,
  OPERATOR_DRAWER_PRIMARY_ACTION_CLASS,
  OPERATOR_NOTIFICATION_FILTER_TABLIST_CLASS,
  OPERATOR_NOTIFICATION_FILTER_TAB_CLASS,
  OPERATOR_RIGHT_DRAWER_BODY_CLASS,
  OPERATOR_RIGHT_DRAWER_CONTENT_CLASS,
} from "@/lib/operatorHome/shellResponsivePresentation"
import type {
  OperatorNotification,
  OperatorNotificationCategory,
  OperatorNotificationsSnapshot,
  OperatorNotificationsTab,
} from "@/lib/operatorNotifications/createOperatorNotificationsModule"
import { cn } from "@/lib/utils"

const TABS: Array<{ id: OperatorNotificationsTab; label: string }> = [
  { id: "all", label: "All" },
  { id: "product", label: "Product" },
  { id: "account", label: "Account" },
  { id: "unread", label: "Unread" },
]

const PREFERENCE_ROWS: Array<{
  id: OperatorNotificationCategory
  label: string
  description: string
}> = [
  {
    id: "product-updates",
    label: "Product updates",
    description: "New features, improvements and useful changes in Tummly.",
  },
  {
    id: "account-notices",
    label: "Account notices",
    description: "Billing, credits, subscription and setup reminders.",
  },
  {
    id: "weekly-brief-reminders",
    label: "Weekly brief reminders",
    description: "Let me know when my weekly summary is ready.",
  },
  {
    id: "tips-and-playbooks",
    label: "Tips and playbooks",
    description:
      "Practical suggestions for guest capture, offers and campaigns.",
  },
  {
    id: "campaign-and-report-updates",
    label: "Campaign and report updates",
    description: "Summaries when campaign results or reports are ready.",
  },
]

type OperatorNotificationsDrawerProps = {
  snapshot: OperatorNotificationsSnapshot
  onOpenChange: (open: boolean) => void
  onSetTab: (tab: OperatorNotificationsTab) => void
  onMarkOneRead: (id: number) => void
  onMarkVisibleRead: () => void
  onActivateCta: (id: number) => void
  onOpenSettings: () => void
  onCloseSettings: () => void
  onSetPreference: (
    category: OperatorNotificationCategory,
    enabled: boolean
  ) => void
  nowMs?: number
}

function NotificationRow({
  item,
  nowMs,
  onMarkOneRead,
  onActivateCta,
}: {
  item: OperatorNotification
  nowMs: number
  onMarkOneRead: (id: number) => void
  onActivateCta: (id: number) => void
}) {
  const unread = item.readAt == null
  const relative = formatRelativeTime(item.createdAt, nowMs)
  const hasCta =
    item.ctaLabel != null &&
    item.ctaLabel !== "" &&
    item.ctaHref != null &&
    item.ctaHref !== ""

  return (
    <article
      className={cn(
        "relative flex w-full flex-col gap-7 overflow-hidden rounded-lg bg-white p-[18px]",
        "dark:bg-white/5"
      )}
    >
      <div className="flex items-start justify-between gap-3 text-foreground">
        <div className="flex min-w-0 flex-1 flex-col gap-3.5">
          <h3 className="text-sm font-bold text-foreground">{item.title}</h3>
          <p className="text-xs font-medium leading-[17px] text-foreground">
            {item.body}
          </p>
        </div>
        {relative ? (
          <p className="shrink-0 text-xs font-medium text-foreground/80">
            {relative}
          </p>
        ) : null}
      </div>

      <div className={OPERATOR_DRAWER_ACTION_ROW_CLASS}>
        {hasCta ? (
          <Button
            type="button"
            variant="link"
            size="link-sm"
            className={cn(
              OPERATOR_DRAWER_PRIMARY_ACTION_CLASS,
              "text-xs font-medium text-[#141414] dark:text-foreground"
            )}
            onClick={() => onActivateCta(item.id)}
          >
            {item.ctaLabel}
          </Button>
        ) : (
          <span />
        )}
        {unread ? (
          <Button
            type="button"
            size="icon-xs"
            className="size-1.5 min-h-0 shrink-0 rounded-full border-0 bg-primary p-0 hover:bg-primary"
            aria-label={`Mark “${item.title}” as read`}
            onClick={() => onMarkOneRead(item.id)}
          />
        ) : (
          <span className="size-1.5 shrink-0" aria-hidden />
        )}
      </div>
    </article>
  )
}

function NotificationsSettingsPanel({
  snapshot,
  onCloseSettings,
  onSetPreference,
}: {
  snapshot: OperatorNotificationsSnapshot
  onCloseSettings: () => void
  onSetPreference: (
    category: OperatorNotificationCategory,
    enabled: boolean
  ) => void
}) {
  return (
    <div className="flex min-h-0 flex-1 flex-col bg-white dark:bg-popover">
      <div
        className={cn(
          OPERATOR_RIGHT_DRAWER_BODY_CLASS,
          "flex flex-col gap-[30px] px-[22px] py-5"
        )}
      >
        <Button
          type="button"
          variant="ghost"
          className="h-auto min-h-0 self-start gap-2.5 p-0 text-sm font-semibold text-foreground hover:bg-transparent"
          onClick={onCloseSettings}
        >
          <ChevronLeftIcon data-icon="inline-start" aria-hidden />
          Back to notification
        </Button>

        {snapshot.preferencesStatus === "loading" ? (
          <div
            className="flex min-h-48 items-center justify-center"
            role="status"
            aria-live="polite"
            aria-label="Loading Notification settings"
          >
            <div
              className="size-8 animate-spin rounded-full border-2 border-primary/25 border-t-primary"
              aria-hidden
            />
          </div>
        ) : snapshot.preferencesStatus === "error" ? (
          <p className="text-sm text-destructive" role="alert">
            {snapshot.preferencesError ??
              "Could not load Notification preferences. Please try again."}
          </p>
        ) : (
          <div className="flex flex-col gap-[30px]">
            {PREFERENCE_ROWS.map((row) => {
              const checked = snapshot.preferences[row.id]
              const checkboxId = `notification-pref-${row.id}`
              return (
                <CheckboxLabel
                  key={row.id}
                  id={checkboxId}
                  checked={checked}
                  disabled={snapshot.preferencesBusy}
                  onCheckedChange={(value) => {
                    onSetPreference(row.id, value)
                  }}
                  className={cn(snapshot.preferencesBusy && "opacity-80")}
                  labelClassName={cn(
                    "flex min-w-0 flex-1 cursor-pointer flex-col gap-1 font-normal",
                    snapshot.preferencesBusy && "cursor-wait"
                  )}
                >
                  <span className="text-sm font-semibold leading-normal text-[#141414] dark:text-foreground">
                    {row.label}
                  </span>
                  <span className="text-sm font-medium leading-normal text-[#7d7d7d] dark:text-muted-foreground">
                    {row.description}
                  </span>
                </CheckboxLabel>
              )
            })}
          </div>
        )}

        {snapshot.preferencesError != null &&
        snapshot.preferencesStatus === "loaded" ? (
          <p className="text-sm text-destructive" role="alert">
            {snapshot.preferencesError}
          </p>
        ) : null}
      </div>
    </div>
  )
}

/** Shell Notifications drawer — Figma 2912:17596 / settings 2929:5507. */
export function OperatorNotificationsDrawer({
  snapshot,
  onOpenChange,
  onSetTab,
  onMarkOneRead,
  onMarkVisibleRead,
  onActivateCta,
  onOpenSettings,
  onCloseSettings,
  onSetPreference,
  nowMs = Date.now(),
}: OperatorNotificationsDrawerProps) {
  const unreadLabel =
    snapshot.unreadCount === 1
      ? "1 unread"
      : `${snapshot.unreadCount} unread`

  const settingsOpen = snapshot.settingsOpen

  return (
    <Drawer
      open={snapshot.drawerOpen}
      onOpenChange={onOpenChange}
      direction="right"
    >
      <DrawerContent className={OPERATOR_RIGHT_DRAWER_CONTENT_CLASS}>
        <div className="flex min-h-0 flex-1 flex-col pt-[22px]">
          <div className="flex shrink-0 items-start gap-[22px] px-[22px]">
            <div className="flex min-w-0 flex-1 flex-col gap-3">
              <DrawerTitle className="text-lg font-bold text-foreground">
                {settingsOpen ? "Notification settings" : "Notifications"}
              </DrawerTitle>
              {settingsOpen ? (
                <DrawerDescription className="sr-only">
                  Choose which Notification categories you want to receive.
                </DrawerDescription>
              ) : (
                <DrawerDescription className="text-xs font-normal text-foreground">
                  {unreadLabel}
                </DrawerDescription>
              )}
            </div>
            <DrawerClose asChild>
              <Button
                type="button"
                variant="ghost"
                size="icon"
                className="size-[42px] shrink-0 rounded-xl bg-[#f1f1f1] hover:bg-[#e8e8e8] dark:bg-white/10"
                aria-label="Close Notifications"
              >
                <XIcon className="size-[18px]" aria-hidden />
              </Button>
            </DrawerClose>
          </div>

          {settingsOpen ? (
            <NotificationsSettingsPanel
              snapshot={snapshot}
              onCloseSettings={onCloseSettings}
              onSetPreference={onSetPreference}
            />
          ) : (
            <>
              <div className="mt-[22px] flex shrink-0 flex-col gap-4 border-t border-[#dedede] pb-4 dark:border-white/10">
                <div className="flex min-w-0 items-center gap-2 px-[22px] pt-4">
                  <div
                    role="tablist"
                    aria-label="Notification filters"
                    className={OPERATOR_NOTIFICATION_FILTER_TABLIST_CLASS}
                  >
                    {TABS.map((tab) => {
                      const selected = tab.id === snapshot.activeTab
                      return (
                        <Button
                          key={tab.id}
                          type="button"
                          variant="ghost"
                          role="tab"
                          aria-selected={selected}
                          className={cn(
                            OPERATOR_NOTIFICATION_FILTER_TAB_CLASS,
                            selected
                              ? "font-semibold text-foreground"
                              : "font-medium text-[#a6a6a6]"
                          )}
                          onClick={() => onSetTab(tab.id)}
                        >
                          {tab.label}
                        </Button>
                      )
                    })}
                  </div>

                  <DropdownMenu>
                    <DropdownMenuTrigger asChild>
                      <Button
                        type="button"
                        variant="ghost"
                        size="icon-sm"
                        className="size-11 shrink-0 text-foreground md:size-8"
                        aria-label="Notifications menu"
                      >
                        <EllipsisVerticalIcon className="size-4" aria-hidden />
                      </Button>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent
                      align="end"
                      className="z-120 w-[190px] rounded-[4px] p-0"
                    >
                      <DropdownMenuItem
                        className="rounded-none px-3.5 py-3.5 text-sm font-medium text-foreground data-disabled:opacity-60"
                        disabled={
                          snapshot.markReadBusy ||
                          snapshot.filteredItems.every(
                            (item) => item.readAt != null
                          )
                        }
                        onClick={onMarkVisibleRead}
                      >
                        Mark all as read
                      </DropdownMenuItem>
                      <DropdownMenuSeparator className="my-0" />
                      <DropdownMenuItem
                        className="rounded-none px-3.5 py-3.5 text-sm font-medium text-foreground"
                        onClick={() => {
                          void onOpenSettings()
                        }}
                      >
                        Settings
                      </DropdownMenuItem>
                    </DropdownMenuContent>
                  </DropdownMenu>
                </div>
              </div>

              <div
                role="tabpanel"
                className={cn(
                  OPERATOR_RIGHT_DRAWER_BODY_CLASS,
                  "bg-[#f1f1f1] px-[22px] py-5 dark:bg-black/20"
                )}
              >
                {snapshot.loadStatus === "loading" ||
                snapshot.loadStatus === "idle" ? (
                  <div
                    className="flex min-h-48 items-center justify-center"
                    role="status"
                    aria-live="polite"
                    aria-label="Loading Notifications"
                  >
                    <div
                      className="size-8 animate-spin rounded-full border-2 border-primary/25 border-t-primary"
                      aria-hidden
                    />
                  </div>
                ) : snapshot.loadStatus === "error" ? (
                  <div className="flex min-h-48 items-center justify-center">
                    <p className="text-sm text-destructive" role="alert">
                      {snapshot.loadError ??
                        "Could not load Notifications. Please try again."}
                    </p>
                  </div>
                ) : snapshot.filteredItems.length === 0 ? (
                  <div className="flex min-h-48 items-center justify-center">
                    <p className="text-sm font-medium text-[#919191]">
                      No Notifications here yet.
                    </p>
                  </div>
                ) : (
                  <ul className="flex flex-col gap-3">
                    {snapshot.filteredItems.map((item) => (
                      <li key={item.id}>
                        <NotificationRow
                          item={item}
                          nowMs={nowMs}
                          onMarkOneRead={onMarkOneRead}
                          onActivateCta={onActivateCta}
                        />
                      </li>
                    ))}
                  </ul>
                )}
              </div>
            </>
          )}
        </div>
      </DrawerContent>
    </Drawer>
  )
}
