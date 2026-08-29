import type { ReactNode } from "react"
import { NavLink } from "react-router-dom"

import { Button } from "@/components/ui/button"
import { cn } from "@/lib/utils"
import type {
  OperatorSidebarFooterNavId,
  OperatorSidebarNavItem,
  OperatorSidebarNavModel,
  OperatorSidebarPrimaryNavId,
} from "@/lib/operatorHome/sidebarNav"
import { resolveSettingsDisclosureOpen } from "@/lib/operatorHome/sidebarNav"
import { tryLeaveDirtyNavigate } from "@/lib/operatorNavigation/leaveDirtyGuard"

import chevronIcon from "@/assets/operator-home/sidenav/chevron.svg"
import homeIcon from "@/assets/operator-home/sidenav/home.svg"
import guestsIcon from "@/assets/operator-home/sidenav/guests.svg"
import captureIcon from "@/assets/operator-home/sidenav/capture.svg"
import feedbackIcon from "@/assets/operator-home/sidenav/feedback.svg"
import campaignsIcon from "@/assets/operator-home/sidenav/campaigns.svg"
import offersIcon from "@/assets/operator-home/sidenav/offers.svg"
import reportsIcon from "@/assets/operator-home/sidenav/reports.svg"
import settingsIcon from "@/assets/operator-home/sidenav/settings.svg"
import tummlyShopIcon from "@/assets/operator-home/sidenav/tummly-shop.svg"

/** Shell collapse hamburger — inline so `currentColor` follows light/dark tokens. */
function SideNavMenuIcon({ className }: { className?: string }) {
  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      width={20}
      height={20}
      viewBox="0 0 20 20"
      fill="none"
      aria-hidden
      className={cn("size-5 shrink-0", className)}
    >
      <path
        d="M2 14.8H18V16H2V14.8ZM2 11.2H18V12.4H2V11.2ZM2 7.6H18V8.8H2V7.6ZM2 4H18V5.2H2V4Z"
        fill="currentColor"
      />
    </svg>
  )
}

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

type DashboardSidebarProps = {
  sidebarNav: OperatorSidebarNavModel
  /** Desktop collapse only; mobile drawer always shows labels. */
  collapsed?: boolean
  onToggleCollapsed?: () => void
  /** Persisted Settings disclosure preference (default open). */
  settingsExpanded?: boolean
  onToggleSettingsExpanded?: () => void
  /** Collapsed gear: expand sidebar and open Settings group. */
  onExpandSidebarAndOpenSettings?: () => void
  onNavigate?: () => void
  className?: string
}

/**
 * SideNav assets are SVG `<img>`s baked at #AEAEAE — tint with filters for
 * enabled (#676767) and active (primary) in light mode.
 */
function SideNavIcon({
  src,
  active = false,
  enabled = false,
  className,
}: {
  src: string
  active?: boolean
  /** Navigable / interactive idle — darken baked #AEAEAE toward #676767. */
  enabled?: boolean
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
        // #AEAEAE → #676767 (174/255 * 0.592 ≈ 103)
        enabled && !active && "brightness-[0.592] dark:brightness-100",
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
    // Figma Side-nav_item collapsed: 52×50 with py-4 / pl-6 (px-1.5 py-1).
    "px-1.5 py-1",
    interactive
      ? "text-op-sidebar-item-default"
      : "text-op-sidebar-item-disabled",
    "transition-[background-color,color,opacity] duration-200 ease-out",
    "motion-reduce:transition-none",
    collapsed && "justify-center leading-none",
    // Green rail is absolutely positioned so active/inactive content stays aligned.
    active &&
      "bg-op-sidebar-item-active-background text-op-sidebar-item-active after:absolute after:inset-y-0 after:right-0 after:w-0.5 after:bg-op-action-primary",
    !active && interactive && "hover:bg-op-sidebar-item-hover-background",
    !interactive && "cursor-not-allowed"
  )
}

function NavRowContent({
  label,
  collapsed,
  iconSrc,
  active = false,
  enabled = false,
  trailing,
}: {
  label: string
  collapsed: boolean
  iconSrc: string
  active?: boolean
  enabled?: boolean
  trailing?: ReactNode
}) {
  return (
    <>
      <span
        className={cn(
          "flex min-w-0 items-center rounded-[4px] p-3",
          // Figma icon frame is a fixed 42×42 square; hide label so line-height
          // cannot stretch the row taller than the icon when collapsed.
          collapsed && "size-[42px] shrink-0 justify-center"
        )}
      >
        <SideNavIcon
          src={iconSrc}
          active={active}
          enabled={enabled}
        />
        {!collapsed ? (
          <span className="max-w-[12rem] truncate pl-3 text-inherit">
            {label}
          </span>
        ) : null}
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

export function DashboardSidebar({
  sidebarNav,
  collapsed = false,
  onToggleCollapsed,
  settingsExpanded = true,
  onToggleSettingsExpanded,
  onExpandSidebarAndOpenSettings,
  onNavigate,
  className,
}: DashboardSidebarProps) {
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
        "bg-op-sidebar-background",
        className
      )}
    >
      <div className="flex min-h-0 flex-1 flex-col justify-between overflow-y-auto overflow-x-hidden">
        <div className="flex flex-col gap-3">
          {onToggleCollapsed ? (
            <div className="flex h-[49px] w-full shrink-0 items-center border-b border-op-border-default">
              <Button
                type="button"
                variant="ghost"
                size="icon-sm"
                className="size-auto h-full w-full min-h-0 justify-start rounded-none px-4 py-2.5 text-op-text-primary hover:bg-op-sidebar-item-hover-background hover:text-op-text-primary aria-expanded:bg-transparent aria-expanded:text-op-text-primary"
                aria-label={collapsed ? "Expand sidebar" : "Collapse sidebar"}
                aria-expanded={!collapsed}
                onClick={onToggleCollapsed}
              >
                <SideNavMenuIcon />
              </Button>
            </div>
          ) : null}

          <nav aria-label="Dashboard sections" className="flex flex-col">
            {sidebarNav.primary.map((item) => {
              const icon = iconForItem(item)
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
                      "h-auto min-h-0 justify-start gap-0 rounded-none border-0 px-1.5 py-1 hover:bg-transparent hover:text-op-sidebar-item-disabled disabled:opacity-100"
                    )}
                  >
                    <NavRowContent
                      label={item.label}
                      collapsed={collapsed}
                      iconSrc={icon}
                    />
                  </Button>
                )
              }

              return (
                <NavLink
                  key={item.id}
                  to={item.to ?? ""}
                  end={item.id === "home"}
                  aria-current={item.active ? "page" : undefined}
                  aria-label={item.label}
                  title={collapsed ? item.label : undefined}
                  className={rowClass}
                  onClick={(event) => {
                    const href = item.to ?? ""
                    if (!tryLeaveDirtyNavigate(href)) {
                      event.preventDefault()
                      return
                    }
                    onNavigate?.()
                  }}
                >
                  <NavRowContent
                    label={item.label}
                    collapsed={collapsed}
                    iconSrc={icon}
                    active={item.active}
                    enabled
                  />
                </NavLink>
              )
            })}

            <div
              className="mx-4 my-2 h-px shrink-0 bg-op-border-default"
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
                    "h-auto min-h-0 justify-center gap-0 rounded-none border-0 px-1.5 py-1 text-op-sidebar-item-default hover:bg-op-sidebar-item-hover-background hover:text-op-sidebar-item-default"
                  )}
                  onClick={onExpandSidebarAndOpenSettings}
                >
                  <span className="flex size-[42px] shrink-0 items-center justify-center rounded-[4px] p-3">
                    <SideNavIcon src={settingsIcon} enabled />
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
                    "h-auto min-h-0 justify-between gap-0 rounded-none border-0 px-1.5 py-1 pr-[18px] text-op-sidebar-item-default hover:bg-op-sidebar-item-hover-background hover:text-op-sidebar-item-default aria-expanded:bg-transparent aria-expanded:text-op-sidebar-item-default"
                  )}
                  onClick={onToggleSettingsExpanded}
                >
                  <NavRowContent
                    label={sidebarNav.settings.label}
                    collapsed={false}
                    iconSrc={settingsIcon}
                    enabled
                    trailing={
                      <SideNavIcon
                        src={chevronIcon}
                        enabled
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
                  {sidebarNav.settings.children.map((child) => {
                    const childClassName = cn(
                      "h-auto min-h-0 w-full justify-start rounded-none border-0 px-1.5 py-0",
                      "text-sm font-medium leading-5",
                      child.active
                        ? "text-op-sidebar-item-active"
                        : child.navigable
                          ? "text-op-sidebar-item-default hover:bg-op-sidebar-item-hover-background hover:text-op-sidebar-item-default"
                          : "text-op-sidebar-item-disabled hover:bg-transparent hover:text-op-sidebar-item-disabled disabled:opacity-100"
                    )
                    const label = (
                      <span className="flex w-full items-center rounded-[4px] px-3 py-1.5 pl-10 text-inherit">
                        {child.label}
                      </span>
                    )

                    if (!child.navigable) {
                      return (
                        <li key={child.id}>
                          <Button
                            type="button"
                            variant="ghost"
                            disabled
                            aria-disabled="true"
                            aria-label={child.label}
                            className={childClassName}
                          >
                            {label}
                          </Button>
                        </li>
                      )
                    }

                    return (
                      <li key={child.id}>
                        <NavLink
                          to={child.to ?? ""}
                          aria-current={child.active ? "page" : undefined}
                          aria-label={child.label}
                          className={cn(
                            childClassName,
                            "flex items-center"
                          )}
                          onClick={(event) => {
                            const href = child.to ?? ""
                            if (!tryLeaveDirtyNavigate(href)) {
                              event.preventDefault()
                              return
                            }
                            onNavigate?.()
                          }}
                        >
                          {label}
                        </NavLink>
                      </li>
                    )
                  })}
                </ul>
              ) : null}
            </div>
          </nav>
        </div>

        <div className="flex flex-col items-stretch pb-2">
          {sidebarNav.footer.map((item) => {
            const icon = iconForItem(item)
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
                    "h-auto min-h-0 justify-start gap-0 rounded-none border-0 px-1.5 py-1 hover:bg-transparent hover:text-op-sidebar-item-disabled disabled:opacity-100"
                  )}
                >
                  <NavRowContent
                    label={item.label}
                    collapsed={collapsed}
                    iconSrc={icon}
                  />
                </Button>
              )
            }

            return (
              <NavLink
                key={item.id}
                to={item.to ?? ""}
                aria-current={item.active ? "page" : undefined}
                aria-label={item.label}
                title={collapsed ? item.label : undefined}
                className={rowClass}
                onClick={(event) => {
                  const href = item.to ?? ""
                  if (!tryLeaveDirtyNavigate(href)) {
                    event.preventDefault()
                    return
                  }
                  onNavigate?.()
                }}
              >
                <NavRowContent
                  label={item.label}
                  collapsed={collapsed}
                  iconSrc={icon}
                  active={item.active}
                  enabled
                />
              </NavLink>
            )
          })}
        </div>
      </div>
    </aside>
  )
}
