import { useState, type ReactNode } from "react"
import { ChevronDownIcon, MessageSquare, Tag } from "lucide-react"

import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from "@/components/ui/accordion"
import { AiIcon } from "@/components/ui/ai-icon"
import { Button } from "@/components/ui/button"
import { GUESTS_PAGE_SECONDARY_BUTTON_CLASS } from "@/lib/operatorGuests/guestsPresentation"
import {
  NEEDS_ATTENTION_ROW_ACTIONS_CLASS,
  NEEDS_ATTENTION_ROW_BODY_CLASS,
  NEEDS_ATTENTION_ROW_COPY_CLASS,
  NEEDS_ATTENTION_ROW_ICON_CLASS,
  NEEDS_ATTENTION_ROW_LIST_CLASS,
  NEEDS_ATTENTION_ROW_META_CLASS,
  NEEDS_ATTENTION_ROW_TITLE_CLASS,
  OPERATOR_HOME_CARD_PADDED_CLASS,
  OPERATOR_HOME_CHROME_BUTTON_CLASS,
  OPERATOR_HOME_CHROME_ICON_CLASS,
  OPERATOR_HOME_EMPTY_SHELL_CENTERED_CLASS,
  OPERATOR_HOME_EMPTY_TITLE_CLASS,
  OPERATOR_HOME_GRAY_SHELL_TITLE_CLASS,
  OPERATOR_HOME_HEADER_COPY_CLASS,
  OPERATOR_HOME_HEADER_ROW_CLASS,
  OPERATOR_HOME_SUBTITLE_CLASS,
  WARNING_ROW_CLASS,
} from "@/lib/operatorHome/operatorHomeSectionPresentation"
import { cn } from "@/lib/utils"

const NEEDS_ATTENTION_ACCORDION_VALUE = "needs-attention"

export type NeedsAttentionRowTone = "warning" | "ai"

export type NeedsAttentionRowIcon = "message" | "tag" | "ai"

export type NeedsAttentionRowAction = {
  key: string
  label: string
  onClick: () => void
}

type NeedsAttentionSectionProps = {
  title: string
  subtitle: string
  children: ReactNode
}

type NeedsAttentionRowProps = {
  title: string
  body: string
  metaLine: string
  tone?: NeedsAttentionRowTone
  icon?: NeedsAttentionRowIcon
  actions: readonly NeedsAttentionRowAction[]
}

type NeedsAttentionEmptyProps = {
  copy: string
}

type NeedsAttentionRowListProps = {
  children: ReactNode
}

/**
 * Shared Needs attention accordion. Home and Offers use this block.
 */
export function NeedsAttentionSection({
  title,
  subtitle,
  children,
}: NeedsAttentionSectionProps) {
  const [openValues, setOpenValues] = useState<string[]>([
    NEEDS_ATTENTION_ACCORDION_VALUE,
  ])
  const isOpen = openValues.includes(NEEDS_ATTENTION_ACCORDION_VALUE)

  return (
    <section className={OPERATOR_HOME_CARD_PADDED_CLASS}>
      <Accordion
        type="multiple"
        value={openValues}
        onValueChange={setOpenValues}
      >
        <AccordionItem
          value={NEEDS_ATTENTION_ACCORDION_VALUE}
          className="border-none"
        >
          <AccordionTrigger
            className={cn(
              OPERATOR_HOME_HEADER_ROW_CLASS,
              "cursor-pointer py-0 hover:no-underline **:data-[slot=accordion-trigger-icon]:hidden"
            )}
          >
            <div className={OPERATOR_HOME_HEADER_COPY_CLASS}>
              <h2 className={OPERATOR_HOME_GRAY_SHELL_TITLE_CLASS}>{title}</h2>
              <p className={OPERATOR_HOME_SUBTITLE_CLASS}>{subtitle}</p>
            </div>
            <span className={OPERATOR_HOME_CHROME_BUTTON_CLASS} aria-hidden>
              <ChevronDownIcon
                className={cn(
                  OPERATOR_HOME_CHROME_ICON_CLASS,
                  "transition-transform duration-200 motion-reduce:transition-none",
                  isOpen && "rotate-180"
                )}
              />
            </span>
          </AccordionTrigger>

          <AccordionContent className="pt-6 pb-0 sm:pt-8 md:pt-10">
            {children}
          </AccordionContent>
        </AccordionItem>
      </Accordion>
    </section>
  )
}

export function NeedsAttentionEmpty({ copy }: NeedsAttentionEmptyProps) {
  return (
    <div className={OPERATOR_HOME_EMPTY_SHELL_CENTERED_CLASS}>
      <p className={OPERATOR_HOME_EMPTY_TITLE_CLASS}>{copy}</p>
    </div>
  )
}

export function NeedsAttentionRowList({ children }: NeedsAttentionRowListProps) {
  return <div className={NEEDS_ATTENTION_ROW_LIST_CLASS}>{children}</div>
}

function NeedsAttentionRowGlyph({ icon }: { icon: NeedsAttentionRowIcon }) {
  if (icon === "ai") {
    return <AiIcon size={16} className="shrink-0" />
  }

  if (icon === "tag") {
    return <Tag className={NEEDS_ATTENTION_ROW_ICON_CLASS} aria-hidden />
  }

  return (
    <MessageSquare className={NEEDS_ATTENTION_ROW_ICON_CLASS} aria-hidden />
  )
}

/** Figma Needs attention card (3344:39087). */
export function NeedsAttentionRow({
  title,
  body,
  metaLine,
  tone = "warning",
  icon,
  actions,
}: NeedsAttentionRowProps) {
  const resolvedIcon: NeedsAttentionRowIcon =
    icon ?? (tone === "ai" ? "ai" : "message")

  return (
    <div className={WARNING_ROW_CLASS}>
      <NeedsAttentionRowGlyph icon={resolvedIcon} />
      <div className={NEEDS_ATTENTION_ROW_COPY_CLASS}>
        <div className={NEEDS_ATTENTION_ROW_TITLE_CLASS}>{title}</div>
        <div className={NEEDS_ATTENTION_ROW_BODY_CLASS}>{body}</div>
        <div className={NEEDS_ATTENTION_ROW_META_CLASS}>{metaLine}</div>
      </div>
      <div className={NEEDS_ATTENTION_ROW_ACTIONS_CLASS}>
        {actions.map((action) => (
          <Button
            key={action.key}
            type="button"
            variant="op-tertiary"
            className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
            onClick={action.onClick}
          >
            {action.label}
          </Button>
        ))}
      </div>
    </div>
  )
}
