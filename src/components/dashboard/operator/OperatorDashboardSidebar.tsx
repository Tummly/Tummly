import type { ReactNode } from "react"

import { Button } from "@/components/ui/button"
import { cn } from "@/lib/utils"
import type {
  OperatorSidebarFooterNavId,
  OperatorSidebarNavItem,
  OperatorSidebarNavModel,
  OperatorSidebarPrimaryNavId,
} from "@/lib/operatorHome/sidebarNav"
import { resolveSettingsDisclosureOpen } from "@/lib/operatorHome/sidebarNav"

import menuIcon from "@/assets/operator-home/sidenav/menu.svg"
import chevronIcon from "@/assets/operator-home/sidenav/chevron.svg"
import homeIcon from "@/assets/operator-home/sidenav/home-default.svg"
import guestsIcon from "@/assets/operator-home/sidenav/guests.svg"
import captureIcon from "@/assets/operator-home/sidenav/capture.svg"
import feedbackIcon from "@/assets/operator-home/sidenav/feedback.svg"
import campaignsIcon from "@/assets/operator-home/sidenav/campaigns.svg"
import offersIcon from "@/assets/operator-home/sidenav/offers.svg"
import reportsIcon from "@/assets/operator-home/sidenav/reports.svg"
import settingsIcon from "@/assets/operator-home/sidenav/settings.svg"
import tummlyShopIcon from "@/assets/operator-home/sidenav/tummly-shop.svg"

const NAV_ICONS: Record<
  OperatorSidebarPrimaryNavId | OperatorSidebarFooterNavId,
  string
> = {
  home: homeIcon,
  guests: guestsIcon,
  capture: captureIcon,
  feedback: feedbackIcon,
  campaigns: campaignsIcon,
  offers: offersIcon,
  reports: reportsIcon,
  "tummly-shop": tummlyShopIcon,
}

type OperatorDashboardSidebarProps = {
  sidebarNav: OperatorSidebarNavModel
  /** Desktop collapse only; mobile drawer always shows labels. */
  collapsed?: boolean
  onToggleCollapsed?: () => void
  /** Persisted Settings disclosure preference (default open). */
  settingsExpanded?: boolean
  onToggleSettingsExpanded?: () => void
  /** Collapsed gear: expand sidebar and open Settings group. */
  onExpandSidebarAndOpenSettings?: () => void
  className?: string
}

function SideNavIcon({
  src,
  active = false,
  className,
}: {
  src: string
  active?: boolean
  className?: string
}) {
  return (
    <img
      src={src}
      alt=""
      width={18}
      height={18}
      aria-hidden
      className={cn(
        "block size-[18px] shrink-0 object-contain",
        // SVGs ship at #5D5D5D; tint to focused item colour when active.
        active && "brightness-0 dark:invert",
        className
      )}
    />
  )
}

function navItemClass({
  active,
  collapsed,
  interactive,
}: {
  active: boolean
  collapsed: boolean
  interactive?: boolean
}) {
  return cn(
    "relative flex w-full items-center text-left text-sm font-medium leading-5",
    "px-1.5 py-1.5",
    "text-[var(--operator-sidenav-item)]",
    "transition-[background-color,color,opacity] duration-200 ease-out",
    "motion-reduce:transition-none",
    collapsed && "justify-center",
    // Green rail is absolutely positioned so active/inactive content stays aligned.
    active &&
      "bg-[var(--operator-sidenav-item-bg-focused)] text-[var(--operator-sidenav-item-focused)] after:absolute after:inset-y-0 after:right-0 after:w-0.5 after:bg-[var(--operator-sidenav-active-rail)]",
    !active && interactive && "hover:bg-[var(--operator-sidenav-item-bg-hover)]",
    !interactive && "cursor-not-allowed"
  )
}

function NavRowContent({
  label,
  collapsed,
  iconSrc,
  active = false,
  trailing,
}: {
  label: string
  collapsed: boolean
  iconSrc: string
  active?: boolean
  trailing?: ReactNode
}) {
  return (
    <>
      <span className="flex min-w-0 items-center rounded-[4px] p-3">
        <SideNavIcon src={iconSrc} active={active} />
        <span
          className={cn(
            "truncate pl-3 text-inherit transition-[opacity,max-width] duration-200 ease-out",
            "motion-reduce:transition-none",
            collapsed
              ? "max-w-0 overflow-hidden p-0 opacity-0"
              : "max-w-[12rem] opacity-100"
          )}
          aria-hidden={collapsed || undefined}
        >
          {label}
        </span>
      </span>
      {!collapsed && trailing ? trailing : null}
    </>
  )
}

function iconForItem(item: OperatorSidebarNavItem): string {
  if (item.id in NAV_ICONS) {
    return NAV_ICONS[item.id as keyof typeof NAV_ICONS]
  }
  return settingsIcon
}

export function OperatorDashboardSidebar({
  sidebarNav,
  collapsed = false,
  onToggleCollapsed,
  settingsExpanded = true,
  onToggleSettingsExpanded,
  onExpandSidebarAndOpenSettings,
  className,
}: OperatorDashboardSidebarProps) {
  const settingsOpen = resolveSettingsDisclosureOpen(
    settingsExpanded,
    sidebarNav.settings.forceExpanded
  )
  const showSettingsChildren = !collapsed && settingsOpen

  return (
    <aside
      aria-label="Operator navigation"
      data-collapsed={collapsed ? "true" : undefined}
      className={cn(
        "flex h-full min-h-0 w-full flex-col overflow-hidden",
        "bg-[var(--operator-sidenav-bg)]",
        className
      )}
    >
      <div className="flex min-h-0 flex-1 flex-col justify-between overflow-y-auto overflow-x-hidden">
        <div className="flex flex-col gap-3">
          {onToggleCollapsed ? (
            <div className="flex h-[49px] w-full shrink-0 items-center border-b border-[var(--operator-sidenav-border)]">
              <Button
                type="button"
                variant="ghost"
                size="icon-sm"
                className="size-auto h-full w-full min-h-0 justify-start rounded-none px-4 py-2.5 text-[var(--operator-sidenav-item)] hover:bg-[var(--operator-sidenav-item-bg-hover)] hover:text-[var(--operator-sidenav-item)] aria-expanded:bg-transparent aria-expanded:text-[var(--operator-sidenav-item)]"
                aria-label={collapsed ? "Expand sidebar" : "Collapse sidebar"}
                aria-expanded={!collapsed}
                onClick={onToggleCollapsed}
              >
                <SideNavIcon src={menuIcon} className="size-5" />
              </Button>
            </div>
          ) : null}

          <nav aria-label="Dashboard sections" className="flex flex-col">
            {sidebarNav.primary.map((item) => {
              const iconSrc = iconForItem(item)
              const rowClass = navItemClass({
                active: item.active,
                collapsed,
                interactive: item.navigable,
              })

              if (!item.navigable) {
                return (
                  <Button
                    key={item.id}
                    type="button"
                    variant="ghost"
                    disabled
                    aria-disabled="true"
                    aria-label={item.label}
                    title={collapsed ? item.label : undefined}
                    className={cn(
                      rowClass,
                      "h-auto min-h-0 justify-start gap-0 rounded-none border-0 px-1.5 py-1.5 hover:bg-transparent hover:text-[var(--operator-sidenav-item)] disabled:opacity-100"
                    )}
                  >
                    <NavRowContent
                      label={item.label}
                      collapsed={collapsed}
                      iconSrc={iconSrc}
                      active={item.active}
                    />
                  </Button>
                )
              }

              return (
                <span
                  key={item.id}
                  aria-current={item.active ? "page" : undefined}
                  aria-label={item.label}
                  title={collapsed ? item.label : undefined}
                  className={rowClass}
                >
                  <NavRowContent
                    label={item.label}
                    collapsed={collapsed}
                    iconSrc={iconSrc}
                    active={item.active}
                  />
                </span>
              )
            })}

            <div
              className="mx-4 my-0 h-px shrink-0 bg-[var(--operator-sidenav-border)]"
              aria-hidden
            />

            <div className="flex flex-col">
              {collapsed ? (
                <Button
                  type="button"
                  variant="ghost"
                  aria-label="Settings"
                  title="Settings"
                  className={cn(
                    navItemClass({
                      active: false,
                      collapsed: true,
                      interactive: true,
                    }),
                    "h-auto min-h-0 justify-center gap-0 rounded-none border-0 px-1.5 py-1.5 text-[var(--operator-sidenav-item)] hover:bg-[var(--operator-sidenav-item-bg-hover)] hover:text-[var(--operator-sidenav-item)]"
                  )}
                  onClick={onExpandSidebarAndOpenSettings}
                >
                  <span className="flex items-center rounded-[4px] p-3">
                    <SideNavIcon src={settingsIcon} />
                  </span>
                </Button>
              ) : (
                <Button
                  type="button"
                  variant="ghost"
                  aria-label="Settings"
                  aria-expanded={settingsOpen}
                  className={cn(
                    navItemClass({
                      active: false,
                      collapsed: false,
                      interactive: true,
                    }),
                    "h-auto min-h-0 justify-between gap-0 rounded-none border-0 px-1.5 py-1.5 pr-[18px] text-[var(--operator-sidenav-item)] hover:bg-[var(--operator-sidenav-item-bg-hover)] hover:text-[var(--operator-sidenav-item)] aria-expanded:bg-transparent aria-expanded:text-[var(--operator-sidenav-item)]"
                  )}
                  onClick={onToggleSettingsExpanded}
                >
                  <NavRowContent
                    label={sidebarNav.settings.label}
                    collapsed={false}
                    iconSrc={settingsIcon}
                    trailing={
                      <SideNavIcon
                        src={chevronIcon}
                        className={cn(
                          "transition-transform duration-200 ease-out motion-reduce:transition-none",
                          !settingsOpen && "rotate-180"
                        )}
                      />
                    }
                  />
                </Button>
              )}

              {showSettingsChildren ? (
                <ul className="flex flex-col pb-3">
                  {sidebarNav.settings.children.map((child) => (
                    <li key={child.id}>
                      <Button
                        type="button"
                        variant="ghost"
                        disabled
                        aria-disabled="true"
                        aria-label={child.label}
                        className={cn(
                          "h-auto min-h-0 w-full justify-start rounded-none border-0 px-1.5 py-0",
                          "text-sm font-medium leading-5 text-[var(--operator-sidenav-item)]",
                          "hover:bg-transparent hover:text-[var(--operator-sidenav-item)] disabled:opacity-100"
                        )}
                      >
                        <span className="flex w-full items-center rounded-[4px] px-3 py-1.5 pl-10 text-inherit">
                          {child.label}
                        </span>
                      </Button>
                    </li>
                  ))}
                </ul>
              ) : null}
            </div>
          </nav>
        </div>

        <div className="flex flex-col items-stretch pb-2">
          {sidebarNav.footer.map((item) => (
            <Button
              key={item.id}
              type="button"
              variant="ghost"
              disabled
              aria-disabled="true"
              aria-label={item.label}
              title={collapsed ? item.label : undefined}
              className={cn(
                navItemClass({
                  active: false,
                  collapsed,
                  interactive: false,
                }),
                "h-auto min-h-0 justify-start gap-0 rounded-none border-0 px-1.5 py-1.5 hover:bg-transparent hover:text-[var(--operator-sidenav-item)] disabled:opacity-100"
              )}
            >
              <NavRowContent
                label={item.label}
                collapsed={collapsed}
                iconSrc={iconForItem(item)}
              />
            </Button>
          ))}
        </div>
      </div>
    </aside>
  )
}
