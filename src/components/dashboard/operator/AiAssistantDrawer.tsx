import { HistoryIcon, PlusCircleIcon, XIcon } from "lucide-react"

import { AiIcon } from "@/components/ui/ai-icon"
import { Button } from "@/components/ui/button"
import {
  Drawer,
  DrawerClose,
  DrawerContent,
  DrawerDescription,
  DrawerTitle,
} from "@/components/ui/drawer"
import type { OperatorAiAssistantSnapshot } from "@/lib/operatorAiAssistant/createOperatorAiAssistantModule"
import {
  OPERATOR_RIGHT_DRAWER_BODY_CLASS,
  OPERATOR_RIGHT_DRAWER_CONTENT_CLASS,
} from "@/lib/operatorHome/shellResponsivePresentation"
import { cn } from "@/lib/utils"

type AiAssistantDrawerProps = {
  snapshot: OperatorAiAssistantSnapshot
  onOpenChange: (open: boolean) => void
  onStartNewChat: () => void
  onOpenRecent: () => void
}

const HEADER_TEXT_ACTION_CLASS =
  "h-auto min-h-11 gap-1.5 rounded-op-sm px-0 py-0 text-sm font-normal text-op-text-primary hover:bg-transparent md:min-h-0"

/** Operator AI Assistant right Drawer — empty greeting. Figma 3454:56016. */
export function AiAssistantDrawer({
  snapshot,
  onOpenChange,
  onStartNewChat,
  onOpenRecent,
}: AiAssistantDrawerProps) {
  return (
    <Drawer
      open={snapshot.drawerOpen}
      onOpenChange={onOpenChange}
      direction="right"
    >
      <DrawerContent className={OPERATOR_RIGHT_DRAWER_CONTENT_CLASS}>
        <div className="flex min-h-0 flex-1 flex-col pt-[22px]">
          <div className="flex shrink-0 items-center justify-between gap-[22px] px-[22px]">
            <div className="flex min-w-0 flex-wrap items-center gap-[22px]">
              <Button
                type="button"
                variant="op-ghost"
                className={HEADER_TEXT_ACTION_CLASS}
                onClick={onStartNewChat}
              >
                <PlusCircleIcon className="size-[18px]" aria-hidden />
                New chat
              </Button>
              <Button
                type="button"
                variant="op-ghost"
                className={HEADER_TEXT_ACTION_CLASS}
                onClick={onOpenRecent}
              >
                <HistoryIcon className="size-[18px]" aria-hidden />
                Recent
              </Button>
            </div>
            <DrawerClose asChild>
              <Button
                type="button"
                variant="ghost"
                size="icon"
                className="size-[42px] shrink-0 rounded-[2px] bg-op-color-gray-70 hover:bg-op-color-gray-85 dark:bg-op-color-gray-950 dark:hover:bg-op-color-gray-950"
                aria-label="Close AI Assistant"
              >
                <XIcon className="size-[18px]" aria-hidden />
              </Button>
            </DrawerClose>
          </div>

          <DrawerTitle className="sr-only">AI Assistant</DrawerTitle>
          <DrawerDescription className="sr-only">
            Ask about feedback, guests, offers, campaigns or performance.
          </DrawerDescription>

          <div
            className={cn(
              OPERATOR_RIGHT_DRAWER_BODY_CLASS,
              "flex flex-col px-[30px]"
            )}
          >
            <div className="flex flex-1 flex-col items-center justify-center gap-4 py-10">
              <AiIcon size={48} />
              <div className="flex flex-col items-center gap-3 text-center">
                <p className="text-lg font-medium text-[var(--op-color-gray-625)]">
                  {snapshot.greeting.hello}
                </p>
                <p className="bg-gradient-to-r from-[var(--op-color-green-600)] to-[var(--op-color-blue-600)] bg-clip-text text-[26px] leading-8 font-medium text-transparent dark:text-transparent">
                  {snapshot.greeting.headline}
                </p>
              </div>
              <p className="max-w-[365px] text-center text-base leading-[22px] font-normal text-[var(--op-color-gray-550)]">
                {snapshot.greeting.body}
              </p>
            </div>
          </div>
        </div>
      </DrawerContent>
    </Drawer>
  )
}
