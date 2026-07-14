import type { ComponentType } from "react"
import {
  BarChart3Icon,
  HomeIcon,
  MegaphoneIcon,
  MessageSquareIcon,
  QrCodeIcon,
  SettingsIcon,
  TagIcon,
  UserPlusIcon,
} from "lucide-react"

import { Button } from "@/components/ui/button"
import { cn } from "@/lib/utils"
import type { OperatorSidebarNavId } from "@/lib/operatorHome/sidebarNav"
import type { OperatorShellPresentation } from "@/types/operatorHome"

const NAV_ICONS: Record<
  OperatorSidebarNavId,
  ComponentType<{ className?: string; "aria-hidden"?: boolean }>
> = {
  home: HomeIcon,
  guests: UserPlusIcon,
  capture: QrCodeIcon,
  feedback: MessageSquareIcon,
  campaigns: MegaphoneIcon,
  offers: TagIcon,
  reports: BarChart3Icon,
  settings: SettingsIcon,
}

type OperatorDashboardSidebarProps = {
  sidebarNav: OperatorShellPresentation["sidebarNav"]
  /** Desktop collapse only; mobile drawer always shows labels. */
  collapsed?: boolean
  onToggleCollapsed?: () => void
  className?: string
}

export function OperatorDashboardSidebar({
  sidebarNav,
  collapsed = false,
  onToggleCollapsed,
  className,
}: OperatorDashboardSidebarProps) {
  return (
    <aside
      aria-label="Operator navigation"
      data-collapsed={collapsed ? "true" : undefined}
      className={cn(
        "flex h-full min-h-0 w-full flex-col bg-transparent",
        className
      )}
    >
      <nav
        aria-label="Dashboard sections"
        className={cn(
          "mt-2 flex min-h-0 flex-1 flex-col gap-1.5 overflow-y-auto overflow-x-hidden py-4",
          // Collapsed rows own px-[26px] (Figma 2929:3875); expanded pads the nav.
          !collapsed && "px-[26px]"
        )}
      >
        {sidebarNav.map((item) => {
          const Icon = NAV_ICONS[item.id]
          const itemClass = cn(
            "flex items-center text-left text-sm font-medium",
            "transition-[background-color,color,padding,gap] duration-200 ease-out",
            "motion-reduce:transition-none",
            collapsed
              ? "h-11 shrink-0 gap-0 px-[26px] py-1.5"
              : "w-full gap-3 rounded-[4px] px-3 py-2.5",
            !collapsed &&
              (item.active
                ? "bg-white text-foreground dark:bg-white/16 dark:text-white"
                : "text-[#535353] dark:text-[#797979]"),
            collapsed && "text-[#535353] dark:text-[#797979]",
            !item.navigable && "cursor-not-allowed opacity-70"
          )

          const iconWrapClass = cn(
            collapsed &&
              "flex items-center rounded-[4px] px-3 py-1.5",
            collapsed &&
              item.active &&
              "bg-white text-foreground dark:bg-white/16 dark:text-white"
          )

          const label = (
            <span
              className={cn(
                "truncate transition-[opacity,max-width] duration-200 ease-out",
                "motion-reduce:transition-none",
                collapsed
                  ? "max-w-0 overflow-hidden opacity-0"
                  : "max-w-[12rem] opacity-100"
              )}
              aria-hidden={collapsed || undefined}
            >
              {item.label}
            </span>
          )

          const content = (
            <>
              {collapsed ? (
                <span className={iconWrapClass}>
                  <Icon className="size-4 shrink-0" aria-hidden />
                </span>
              ) : (
                <Icon className="size-4 shrink-0" aria-hidden />
              )}
              {label}
            </>
          )

          if (!item.navigable) {
            return (
              <button
                key={item.id}
                type="button"
                disabled
                aria-disabled="true"
                aria-label={item.label}
                title={collapsed ? item.label : undefined}
                className={itemClass}
              >
                {content}
              </button>
            )
          }

          return (
            <span
              key={item.id}
              aria-current={item.active ? "page" : undefined}
              aria-label={item.label}
              title={collapsed ? item.label : undefined}
              className={itemClass}
            >
              {content}
            </span>
          )
        })}
      </nav>

      {onToggleCollapsed ? (
        <div className="shrink-0 px-[38px] py-3">
          <Button
            type="button"
            variant="ghost"
            size="icon-sm"
            className="size-auto h-auto min-h-0 rounded-sm p-0 text-foreground hover:bg-transparent dark:hover:bg-transparent"
            aria-label={collapsed ? "Expand sidebar" : "Collapse sidebar"}
            aria-expanded={!collapsed}
            onClick={onToggleCollapsed}
          >
            {/* Figma Sidebar control 2929:3823 — 18×16 solid left rail */}
            <svg
              width={18}
              height={16}
              viewBox="0 0 18 16"
              fill="none"
              xmlns="http://www.w3.org/2000/svg"
              aria-hidden
              className="block"
            >
              <path
                fillRule="evenodd"
                clipRule="evenodd"
                d="M0 0H18V16H0V0ZM6 2H16V14H6V2Z"
                fill="currentColor"
              />
            </svg>
          </Button>
        </div>
      ) : null}
    </aside>
  )
}
