import { useRef, useState } from "react"
import { ChevronDownIcon } from "lucide-react"

import { MarketingNavLink } from "@/components/marketing/MarketingNavLink"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { MARKETING_RESOURCES_NAV } from "@/constants/marketingNav"
import { cn } from "@/lib/utils"

type MarketingResourcesMenuProps = {
  className?: string
  triggerClassName?: string
  onNavigate?: () => void
}

export function MarketingResourcesMenu({
  className,
  triggerClassName,
  onNavigate,
}: MarketingResourcesMenuProps) {
  const [open, setOpen] = useState(false)
  const closeTimerRef = useRef<number | null>(null)

  const clearCloseTimer = () => {
    if (closeTimerRef.current != null) {
      window.clearTimeout(closeTimerRef.current)
      closeTimerRef.current = null
    }
  }

  const scheduleClose = () => {
    clearCloseTimer()
    closeTimerRef.current = window.setTimeout(() => {
      setOpen(false)
    }, 150)
  }

  return (
    <DropdownMenu open={open} onOpenChange={setOpen}>
      <div
        className={cn("relative", className)}
        onMouseEnter={() => {
          clearCloseTimer()
          setOpen(true)
        }}
        onMouseLeave={scheduleClose}
      >
        <DropdownMenuTrigger
          className={cn(
            "inline-flex items-center gap-1.5 rounded-sm text-sm font-normal text-[#141414] outline-none hover:underline focus-visible:ring-2 focus-visible:ring-[#141414]/30",
            triggerClassName,
          )}
        >
          Resources
          <ChevronDownIcon className="size-3.5 text-[#141414]" aria-hidden />
        </DropdownMenuTrigger>
        <DropdownMenuContent
          align="start"
          sideOffset={12}
          className="z-[60] w-[220px] rounded-[6px] border-0 bg-[#f0f0f0] p-7 shadow-none ring-0"
          onMouseEnter={clearCloseTimer}
          onMouseLeave={scheduleClose}
        >
          <DropdownMenuGroup className="flex flex-col gap-6">
            {MARKETING_RESOURCES_NAV.map((item) => (
              <DropdownMenuItem
                key={item.id}
                className="cursor-pointer rounded-none p-0 focus:bg-transparent"
                onSelect={(event) => {
                  if (item.href.kind === "placeholder") {
                    event.preventDefault()
                  }
                }}
              >
                <MarketingNavLink
                  label={item.label}
                  href={item.href}
                  className="w-full text-sm"
                  onNavigate={() => {
                    setOpen(false)
                    onNavigate?.()
                  }}
                />
              </DropdownMenuItem>
            ))}
          </DropdownMenuGroup>
        </DropdownMenuContent>
      </div>
    </DropdownMenu>
  )
}
