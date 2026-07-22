import { ChevronDownIcon } from "lucide-react"
import { useMemo, useState } from "react"

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
import { Separator } from "@/components/ui/separator"
import {
  filterCatalogForSearch,
  isAddTagApplyDirty,
  type AddTagDialogSession,
} from "@/lib/operatorGuests/addTagDialogLogic"
import { cn } from "@/lib/utils"

import { GuestsRemovableChip } from "./GuestsRemovableChip"

type AddTagDialogProps = {
  open: boolean
  session: AddTagDialogSession | null
  busy?: boolean
  onOpenChange: (open: boolean) => void
  onStageTag: (tagId: string) => void
  onUnstageTag: (tagId: string) => void
  onSearchChange: (query: string) => void
  onCreateOpenChange: (open: boolean) => void
  onCreateNameChange: (name: string) => void
  onCreateTag: () => void
  onApply: () => void
}

export function AddTagDialog({
  open,
  session,
  busy = false,
  onOpenChange,
  onStageTag,
  onUnstageTag,
  onSearchChange,
  onCreateOpenChange,
  onCreateNameChange,
  onCreateTag,
  onApply,
}: AddTagDialogProps) {
  const [listOpen, setListOpen] = useState(false)

  const pendingTags = useMemo(() => {
    if (session == null) {
      return []
    }
    return session.pendingTagIds
      .map((id) => session.catalog.find((tag) => tag.id === id))
      .filter((tag): tag is NonNullable<typeof tag> => tag != null)
  }, [session])

  const filteredCatalog = useMemo(() => {
    if (session == null) {
      return []
    }
    return filterCatalogForSearch(
      session.catalog,
      session.searchQuery,
      session.pendingTagIds
    )
  }, [session])

  if (session == null) {
    return null
  }

  const guestCount = session.guestIds.length
  const dirty = isAddTagApplyDirty(session.openTagIds, session.pendingTagIds)
  const title =
    guestCount === 1
      ? "Add tags for 1 guest"
      : `Add tags for ${guestCount} guests`

  return (
    <Dialog
      open={open}
      onOpenChange={(next) => {
        if (!next) {
          setListOpen(false)
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
              {title}
            </DialogTitle>
          </DialogHeader>

          <div className="flex flex-col gap-4">
            <div className="flex flex-col gap-2">
              <label
                htmlFor="add-tag-search"
                className="text-sm font-semibold leading-5"
              >
                Find a tag
              </label>
              <Popover open={listOpen} onOpenChange={setListOpen}>
                <PopoverTrigger asChild>
                  <Button
                    type="button"
                    id="add-tag-search"
                    variant="outline"
                    className={cn(
                      "h-[50px] w-full justify-between rounded border border-[rgba(74,74,76,0.4)] bg-transparent px-[15px] text-left text-sm font-normal shadow-none hover:bg-transparent",
                      session.searchQuery.length === 0 && "text-[#7d7d7d]"
                    )}
                    aria-expanded={listOpen}
                    aria-haspopup="listbox"
                  >
                    <span className="truncate">
                      {session.searchQuery.length > 0
                        ? session.searchQuery
                        : "Search existing tags"}
                    </span>
                    <ChevronDownIcon className="size-4 shrink-0 opacity-60" />
                  </Button>
                </PopoverTrigger>
                <PopoverContent
                  align="start"
                  className="z-[130] w-[var(--radix-popover-trigger-width)] gap-0 rounded-lg p-1"
                  onOpenAutoFocus={(event) => event.preventDefault()}
                >
                  <div className="border-b border-border p-1.5">
                    <Input
                      autoFocus
                      value={session.searchQuery}
                      placeholder="Search existing tags"
                      className="h-9 rounded border-[rgba(74,74,76,0.4)]"
                      onChange={(event) => {
                        onSearchChange(event.target.value)
                      }}
                    />
                  </div>
                  <ul
                    role="listbox"
                    className="flex max-h-56 flex-col gap-0.5 overflow-y-auto py-1"
                    aria-label="Existing tags"
                  >
                    {filteredCatalog.map((tag) => (
                      <li key={tag.id}>
                        <Button
                          type="button"
                          variant="ghost"
                          role="option"
                          className="h-auto w-full justify-start rounded-md px-2.5 py-1.5 text-left text-sm font-normal text-foreground"
                          onClick={() => {
                            onStageTag(tag.id)
                            setListOpen(false)
                          }}
                        >
                          {tag.name} — {tag.guestCount} guests
                        </Button>
                      </li>
                    ))}
                    {filteredCatalog.length === 0 ? (
                      <li className="rounded-md px-2.5 py-1.5 text-sm text-muted-foreground">
                        No matching tags
                      </li>
                    ) : null}
                  </ul>
                  <Button
                    type="button"
                    variant="ghost"
                    className="mt-0.5 h-auto w-full justify-start rounded-md px-2.5 py-1.5 text-left text-sm font-medium text-primary"
                    onClick={() => {
                      onCreateOpenChange(true)
                      setListOpen(false)
                    }}
                  >
                    + Create new tag
                  </Button>
                </PopoverContent>
              </Popover>
            </div>

            {pendingTags.length > 0 ? (
              <div className="flex flex-wrap gap-3">
                {pendingTags.map((tag) => (
                  <GuestsRemovableChip
                    key={tag.id}
                    label={tag.name}
                    removeLabel={`Remove ${tag.name}`}
                    onRemove={() => {
                      onUnstageTag(tag.id)
                    }}
                  />
                ))}
              </div>
            ) : null}

            {session.createOpen ? (
              <>
                <Separator />
                <div className="flex items-end gap-3.5">
                  <div className="flex min-w-0 flex-1 flex-col gap-2">
                    <label
                      htmlFor="add-tag-create-name"
                      className="text-sm font-semibold leading-5"
                    >
                      Tag name
                    </label>
                    <Input
                      id="add-tag-create-name"
                      value={session.createName}
                      placeholder="Enter tag name"
                      className="h-[50px] rounded border-[rgba(74,74,76,0.4)] px-[15px] text-sm"
                      onChange={(event) => {
                        onCreateNameChange(event.target.value)
                      }}
                      onKeyDown={(event) => {
                        if (event.key === "Enter") {
                          event.preventDefault()
                          onCreateTag()
                        }
                      }}
                    />
                  </div>
                  <Button
                    type="button"
                    variant="operator-secondary"
                    className="h-[50px] shrink-0 rounded-[2px]"
                    disabled={busy || session.createName.trim().length === 0}
                    onClick={onCreateTag}
                  >
                    Create tag
                  </Button>
                </div>
              </>
            ) : null}
          </div>
        </div>

        <DialogFooter className="flex-row gap-3 sm:justify-start">
          <Button
            type="button"
            disabled={!dirty || busy}
            className="h-auto min-h-0 rounded-[2px] px-4 py-2.5 text-sm"
            onClick={onApply}
          >
            Apply tags
          </Button>
          <Button
            type="button"
            variant="operator-tertiary"
            className="h-auto min-h-0 rounded-[2px]"
            onClick={() => onOpenChange(false)}
          >
            Cancel
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
