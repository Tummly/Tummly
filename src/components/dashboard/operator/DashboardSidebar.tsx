import type { CSSProperties, ReactNode } from "react"
import { NavLink } from "react-router-dom"

import { Button } from "@/components/ui/button"
import { cn } from "@/lib/utils"
import type {
  OperatorSidebarFooterNavId,
  OperatorSidebarNavItem,
  OperatorSidebarNavModel,
  OperatorSidebarPrimaryNavId,
} from "@/lib/operatorHome/sidebarNav"
import {
  resolveSettingsChromeActive,
  resolveSettingsDisclosureOpen,
} from "@/lib/operatorHome/sidebarNav"
import { tryLeaveDirtyNavigate } from "@/lib/operatorNavigation/leaveDirtyGuard"

import chevronIcon from "@/assets/operator-home/sidenav/chevron.svg"
import homeDefaultIcon from "@/assets/operator-home/sidenav/home-default.svg"
import homeFocusedIcon from "@/assets/operator-home/sidenav/home-focused.svg"
import guestsDefaultIcon from "@/assets/operator-home/sidenav/guests-default.svg"
import guestsFocusedIcon from "@/assets/operator-home/sidenav/guests-focused.svg"
import captureDefaultIcon from "@/assets/operator-home/sidenav/capture-default.svg"
import captureFocusedIcon from "@/assets/operator-home/sidenav/capture-focused.svg"
import feedbackDefaultIcon from "@/assets/operator-home/sidenav/feedback-default.svg"
import feedbackFocusedIcon from "@/assets/operator-home/sidenav/feedback-focused.svg"
import campaignsDefaultIcon from "@/assets/operator-home/sidenav/campaigns-default.svg"
import campaignsFocusedIcon from "@/assets/operator-home/sidenav/campaigns-focused.svg"
import offersDefaultIcon from "@/assets/operator-home/sidenav/offers-default.svg"
import offersFocusedIcon from "@/assets/operator-home/sidenav/offers-focused.svg"
import reportsDefaultIcon from "@/assets/operator-home/sidenav/reports-default.svg"
import reportsFocusedIcon from "@/assets/operator-home/sidenav/reports-focused.svg"
import settingsDefaultIcon from "@/assets/operator-home/sidenav/settings-default.svg"
import settingsFocusedIcon from "@/assets/operator-home/sidenav/settings-focused.svg"
import menuDefaultIcon from "@/assets/operator-home/sidenav/menu-default.svg"
import menuFocusedIcon from "@/assets/operator-home/sidenav/menu-focused.svg"
import tummlyShopDefaultIcon from "@/assets/operator-home/sidenav/tummly-shop-default.svg"
import tummlyShopFocusedIcon from "@/assets/operator-home/sidenav/tummly-shop-focused.svg"

type SideNavIconPair = {
  default: string
  focused: string
}

const MENU_ICON: SideNavIconPair = {
  default: menuDefaultIcon,
  focused: menuFocusedIcon,
}

const NAV_ICONS: Record<
  OperatorSidebarPrimaryNavId | OperatorSidebarFooterNavId,
  SideNavIconPair
> = {
  home: { default: homeDefaultIcon, focused: homeFocusedIcon },
  guests: { default: guestsDefaultIcon, focused: guestsFocusedIcon },
  capture: { default: captureDefaultIcon, focused: captureFocusedIcon },
  feedback: { default: feedbackDefaultIcon, focused: feedbackFocusedIcon },
  campaigns: { default: campaignsDefaultIcon, focused: campaignsFocusedIcon },
  offers: { default: offersDefaultIcon, focused: offersFocusedIcon },
  reports: { default: reportsDefaultIcon, focused: reportsFocusedIcon },
  "tummly-shop": {
    default: tummlyShopDefaultIcon,
    focused: tummlyShopFocusedIcon,
  },
}

const SETTINGS_ICON: SideNavIconPair = {
  default: settingsDefaultIcon,
  focused: settingsFocusedIcon,
}

const CHEVRON_ICON: SideNavIconPair = {
  default: chevronIcon,
  focused: chevronIcon,
}

type DashboardSidebarProps = {
  sidebarNav: OperatorSidebarNavModel
  /** Desktop collapse only; mobile drawer always shows labels. */
  collapsed?: boolean
  /**
   * Hamburger pin from click (not hover peek). Drives the focused menu
   * glyph and active colour.
   */
  menuPinned?: boolean
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
 * Figma Side-nav Icons — Default (outline) vs Focused (filled).
 * Mask + `bg-current` so light/dark item tokens colour the glyph.
 */
function SideNavIcon({
  icons,
  active = false,
  className,
}: {
  icons: SideNavIconPair
  active?: boolean
  className?: string
}) {
  const src = active ? icons.focused : icons.default
  return (
    <span
      aria-hidden
      className={cn(
        "block size-4.5 shrink-0 bg-current mask-alpha",
        "[mask:var(--side-nav-icon)_center/contain_no-repeat]",
        "[-webkit-mask:var(--side-nav-icon)_center/contain_no-repeat]",
        className
      )}
      style={
        {
          "--side-nav-icon": `url("${src}")`,
        } as CSSProperties
      }
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
  icons,
  active = false,
  trailing,
}: {
  label: string
  collapsed: boolean
  icons: SideNavIconPair
  active?: boolean
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
        <SideNavIcon icons={icons} active={active} />
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

function iconsForItem(item: OperatorSidebarNavItem): SideNavIconPair {
  if (item.id in NAV_ICONS) {
    return NAV_ICONS[item.id as keyof typeof NAV_ICONS]
  }
  return SETTINGS_ICON
}

export function DashboardSidebar({
  sidebarNav,
  collapsed = false,
  menuPinned = false,
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
  const settingsChromeActive = resolveSettingsChromeActive(
    sidebarNav.settings.forceExpanded
  )

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
                aria-pressed={menuPinned}
                className={cn(
                  "size-auto h-full w-full min-h-0 justify-start rounded-none px-4 py-2.5",
                  "hover:bg-op-sidebar-item-hover-background",
                  "aria-expanded:bg-transparent",
                  menuPinned
                    ? "text-op-sidebar-item-active hover:text-op-sidebar-item-active aria-expanded:text-op-sidebar-item-active"
                    : "text-op-sidebar-item-default hover:text-op-sidebar-item-default aria-expanded:text-op-sidebar-item-default"
                )}
                aria-label={menuPinned ? "Unpin sidebar" : "Pin sidebar open"}
                aria-expanded={!collapsed}
                onClick={onToggleCollapsed}
              >
                <SideNavIcon
                  icons={MENU_ICON}
                  active={menuPinned}
                  className="size-5"
                />
              </Button>
            </div>
          ) : null}

          <nav aria-label="Dashboard sections" className="flex flex-col">
            {sidebarNav.primary.map((item) => {
              const icons = iconsForItem(item)
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
                      icons={icons}
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
                    icons={icons}
                    active={item.active}
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
                      active: settingsChromeActive,
                      collapsed: true,
                      interactive: true,
                    }),
                    "h-auto min-h-0 justify-center gap-0 rounded-none border-0 px-1.5 py-1",
                    settingsChromeActive
                      ? "aria-expanded:bg-op-sidebar-item-active-background aria-expanded:text-op-sidebar-item-active hover:bg-op-sidebar-item-active-background hover:text-op-sidebar-item-active"
                      : "text-op-sidebar-item-default hover:bg-op-sidebar-item-hover-background hover:text-op-sidebar-item-default"
                  )}
                  onClick={onExpandSidebarAndOpenSettings}
                >
                  <span className="flex size-[42px] shrink-0 items-center justify-center rounded-[4px] p-3">
                    <SideNavIcon
                      icons={SETTINGS_ICON}
                      active={settingsChromeActive}
                    />
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
                      active: settingsChromeActive,
                      collapsed: false,
                      interactive: true,
                    }),
                    "h-auto min-h-0 justify-between gap-0 rounded-none border-0 px-1.5 py-1 pr-[18px]",
                    settingsChromeActive
                      ? "aria-expanded:bg-op-sidebar-item-active-background aria-expanded:text-op-sidebar-item-active hover:bg-op-sidebar-item-active-background hover:text-op-sidebar-item-active"
                      : "text-op-sidebar-item-default hover:bg-op-sidebar-item-hover-background hover:text-op-sidebar-item-default aria-expanded:bg-transparent aria-expanded:text-op-sidebar-item-default"
                  )}
                  onClick={onToggleSettingsExpanded}
                >
                  <NavRowContent
                    label={sidebarNav.settings.label}
                    collapsed={false}
                    icons={SETTINGS_ICON}
                    active={settingsChromeActive}
                    trailing={
                      <SideNavIcon
                        icons={CHEVRON_ICON}
                        active={settingsChromeActive}
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
            const icons = iconsForItem(item)
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
                    icons={icons}
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
                  icons={icons}
                  active={item.active}
                />
              </NavLink>
            )
          })}
        </div>
      </div>
    </aside>
  )
}
