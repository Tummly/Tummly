import { useMemo, useState } from "react"
import { ChevronDownIcon } from "lucide-react"

import { GuestsRemovableChip } from "@/components/dashboard/operator/Guests/GuestsRemovableChip"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover"
import {
  Select,
  SelectContent,
  SelectGroup,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import type {
  FeedbackDetectedTagsEditor,
  FeedbackDetailsLoaded,
} from "@/lib/operatorFeedback/createFeedbackDetailsModule"
import {
  FEEDBACK_DIALOG_SELECT_GROUP_CLASS,
  FEEDBACK_DIALOG_SELECT_ITEM_CLASS,
  FEEDBACK_DIALOG_SELECT_MENU_CLASS,
  FEEDBACK_FIELD_LABEL_CLASS,
} from "@/lib/operatorFeedback/feedbackPresentation"
import {
  DETECTED_TAG_KEYS,
  DETECTED_TAG_LABELS,
  type DetectedTagKey,
} from "@/lib/operatorHome/detectedTags"
import {
  OPERATOR_SHELL_MENU_ITEM_CLASS,
  OPERATOR_SHELL_MENU_PANEL_CLASS,
} from "@/lib/operatorHome/shellResponsivePresentation"
import type { FeedbackSentiment } from "@/types/dashboard"
import { cn } from "@/lib/utils"

type FeedbackEditIssueTagsDialogProps = {
  editTags: FeedbackDetectedTagsEditor
  details: FeedbackDetailsLoaded | null
  onOpenChange: (open: boolean) => void
  onStageTag: (key: string) => void
  onUnstageTag: (key: string) => void
  onSentimentChange: (sentiment: FeedbackSentiment) => void
  onApply: () => void
}

const SELECT_TRIGGER_CLASS =
  "!h-[50px] !min-h-[50px] w-full justify-between rounded border border-input bg-transparent px-[15px] text-left text-sm font-normal shadow-none hover:bg-transparent aria-expanded:bg-transparent dark:bg-transparent dark:hover:bg-transparent dark:aria-expanded:bg-transparent"

const SELECT_MENU_CLASS = cn(
  "z-[130] w-[var(--radix-popover-trigger-width)] gap-0 p-0",
  OPERATOR_SHELL_MENU_PANEL_CLASS,
  "bg-op-background-primary"
)

const SELECT_ITEM_CLASS = cn(
  "h-auto w-full justify-start text-left text-sm font-normal text-foreground",
  OPERATOR_SHELL_MENU_ITEM_CLASS
)

const SELECT_LIST_CLASS =
  "flex max-h-56 flex-col divide-y divide-op-border-default overflow-y-auto"

const SENTIMENT_OPTIONS: Array<{
  value: FeedbackSentiment
  label: string
}> = [
  { value: "positive", label: "Positive" },
  { value: "neutral", label: "Neutral" },
  { value: "negative", label: "Negative" },
]

function filterDetectedTagCatalog(
  draftTagKeys: readonly string[],
  searchQuery: string
): DetectedTagKey[] {
  const q = searchQuery.trim().toLowerCase()
  return DETECTED_TAG_KEYS.filter((key) => {
    if (draftTagKeys.includes(key)) {
      return false
    }
    if (q.length === 0) {
      return true
    }
    return DETECTED_TAG_LABELS[key].toLowerCase().includes(q)
  })
}

/** Edit Issue tags — Guests Add tags chrome, closed Detected Tag vocabulary. */
export function FeedbackEditIssueTagsDialog({
  editTags,
  details,
  onOpenChange,
  onStageTag,
  onUnstageTag,
  onSentimentChange,
  onApply,
}: FeedbackEditIssueTagsDialogProps) {
  const [listOpen, setListOpen] = useState(false)
  const [searchQuery, setSearchQuery] = useState("")

  const requiresSentiment = details?.classificationStatus === "Failed"
  const busy = editTags.saveStatus === "saving"

  const pendingLabels = useMemo(
    () =>
      editTags.draftTagKeys.map((key) => ({
        key,
        label:
          key in DETECTED_TAG_LABELS
            ? DETECTED_TAG_LABELS[key as DetectedTagKey]
            : key,
      })),
    [editTags.draftTagKeys]
  )

  const filteredCatalog = useMemo(
    () => filterDetectedTagCatalog(editTags.draftTagKeys, searchQuery),
    [editTags.draftTagKeys, searchQuery]
  )

  if (!editTags.isOpen) {
    return null
  }

  return (
    <Dialog
      open
      onOpenChange={(next) => {
        if (!next) {
          setListOpen(false)
          setSearchQuery("")
        }
        onOpenChange(next)
      }}
    >
      <DialogContent
        showCloseButton
        className="gap-[60px] p-8 sm:max-w-[560px]"
      >
        <div className="flex flex-col gap-5">
          <DialogHeader className="gap-0 pr-10">
            <DialogTitle className="text-2xl font-bold tracking-normal">
              Edit issue tags
            </DialogTitle>
          </DialogHeader>

          <div className="flex flex-col gap-4">
            {requiresSentiment ? (
              <div className="flex flex-col gap-2">
                <label
                  htmlFor="edit-issue-tags-sentiment"
                  className={FEEDBACK_FIELD_LABEL_CLASS}
                >
                  Classification
                </label>
                <Select
                  value={editTags.draftSentiment ?? undefined}
                  onValueChange={(value) => {
                    onSentimentChange(value as FeedbackSentiment)
                  }}
                  disabled={busy}
                >
                  <SelectTrigger
                    id="edit-issue-tags-sentiment"
                    className={SELECT_TRIGGER_CLASS}
                  >
                    <SelectValue placeholder="Select sentiment" />
                  </SelectTrigger>
                  <SelectContent
                    className={cn(FEEDBACK_DIALOG_SELECT_MENU_CLASS, "z-[130]")}
                  >
                    <SelectGroup className={FEEDBACK_DIALOG_SELECT_GROUP_CLASS}>
                      {SENTIMENT_OPTIONS.map((option) => (
                        <SelectItem
                          key={option.value}
                          value={option.value}
                          className={FEEDBACK_DIALOG_SELECT_ITEM_CLASS}
                        >
                          {option.label}
                        </SelectItem>
                      ))}
                    </SelectGroup>
                  </SelectContent>
                </Select>
              </div>
            ) : null}

            <div className="flex flex-col gap-2">
                <label
                  htmlFor="edit-issue-tags-search"
                  className={FEEDBACK_FIELD_LABEL_CLASS}
                >
                  Find a tag
                </label>
                <Popover open={listOpen} onOpenChange={setListOpen}>
                  <PopoverTrigger asChild>
                    <Button
                      type="button"
                      id="edit-issue-tags-search"
                      variant="op-ghost"
                      disabled={busy}
                      className={cn(
                        SELECT_TRIGGER_CLASS,
                        searchQuery.length === 0
                          && "text-op-input-placeholder"
                      )}
                      aria-expanded={listOpen}
                      aria-haspopup="listbox"
                    >
                    <span className="truncate">
                      {searchQuery.length > 0
                        ? searchQuery
                        : "Search issue tags"}
                    </span>
                    <ChevronDownIcon className="size-4 shrink-0 opacity-60" />
                  </Button>
                </PopoverTrigger>
                <PopoverContent
                  align="start"
                  className={SELECT_MENU_CLASS}
                  onOpenAutoFocus={(event) => event.preventDefault()}
                >
                  <div className="border-b border-op-border-default p-1.5">
                    <Input
                      autoFocus
                      value={searchQuery}
                      placeholder="Search issue tags"
                      className="h-9 rounded border-input bg-transparent shadow-none dark:bg-transparent"
                      onChange={(event) => {
                        setSearchQuery(event.target.value)
                      }}
                    />
                  </div>
                  <ul
                    role="listbox"
                    className={SELECT_LIST_CLASS}
                    aria-label="Issue tags"
                  >
                    {filteredCatalog.map((key) => (
                      <li key={key}>
                        <Button
                          type="button"
                          variant="op-ghost"
                          role="option"
                          className={SELECT_ITEM_CLASS}
                          onClick={() => {
                            onStageTag(key)
                            setListOpen(false)
                            setSearchQuery("")
                          }}
                        >
                          {DETECTED_TAG_LABELS[key]}
                        </Button>
                      </li>
                    ))}
                    {filteredCatalog.length === 0 ? (
                      <li className="px-3 py-3 text-sm text-muted-foreground">
                        No matching tags
                      </li>
                    ) : null}
                  </ul>
                </PopoverContent>
              </Popover>
            </div>

            {pendingLabels.length > 0 ? (
              <div className="flex flex-wrap gap-3">
                {pendingLabels.map((tag) => (
                  <GuestsRemovableChip
                    key={tag.key}
                    label={tag.label}
                    removeLabel={`Remove ${tag.label}`}
                    onRemove={() => {
                      onUnstageTag(tag.key)
                    }}
                  />
                ))}
              </div>
            ) : null}

            {editTags.saveError != null ? (
              <p className="text-sm text-destructive" role="alert">
                {editTags.saveError}
              </p>
            ) : null}
          </div>
        </div>

        <DialogFooter className="flex-row gap-3 sm:justify-start">
          <Button
            type="button"
            disabled={!editTags.canApply || busy}
            className="h-auto min-h-0 rounded-[2px] px-4 py-2.5 text-sm"
            onClick={onApply}
          >
            Apply tags
          </Button>
          <Button
            type="button"
            variant="op-tertiary"
            className="h-auto min-h-0 rounded-[2px]"
            disabled={busy}
            onClick={() => onOpenChange(false)}
          >
            Cancel
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
