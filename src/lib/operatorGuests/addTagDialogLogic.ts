/** Add Tag dialog pure helpers — intersection pre-fill, dirty Apply, additive delta. */

import type { GuestTag } from "@/lib/operatorGuests/guestTag"

export type { GuestTag }

export type AddTagDialogSession = {
  guestIds: readonly string[]
  openTagIds: readonly string[]
  pendingTagIds: readonly string[]
  catalog: readonly GuestTag[]
  searchQuery: string
  createOpen: boolean
  createName: string
}

export function normalizeTagName(name: string): string {
  return name.trim().replace(/\s+/g, " ").toLowerCase()
}

export function intersectTagIds(
  memberships: readonly (readonly string[])[]
): string[] {
  if (memberships.length === 0) {
    return []
  }

  const [first, ...rest] = memberships
  return first.filter((id) => rest.every((set) => set.includes(id)))
}

export function tagSetsEqual(
  a: readonly string[],
  b: readonly string[]
): boolean {
  if (a.length !== b.length) {
    return false
  }

  const sortedA = [...a].sort()
  const sortedB = [...b].sort()
  return sortedA.every((id, index) => id === sortedB[index])
}

export function isAddTagApplyDirty(
  openTagIds: readonly string[],
  pendingTagIds: readonly string[]
): boolean {
  return !tagSetsEqual(openTagIds, pendingTagIds)
}

export function addDeltaFromPending(
  openTagIds: readonly string[],
  pendingTagIds: readonly string[]
): { addedTagIds: string[]; ignoredRemovalTagIds: string[] } {
  const open = new Set(openTagIds)
  const pending = new Set(pendingTagIds)
  const addedTagIds = [...pending].filter((id) => !open.has(id))
  const ignoredRemovalTagIds = [...open].filter((id) => !pending.has(id))
  return { addedTagIds, ignoredRemovalTagIds }
}

export function openAddTagSession(input: {
  guestIds: readonly string[]
  membershipsByGuestId: ReadonlyMap<string, readonly string[]>
  catalog: readonly GuestTag[]
  createOpen?: boolean
}): AddTagDialogSession {
  const memberships = input.guestIds.map(
    (guestId) => input.membershipsByGuestId.get(guestId) ?? []
  )
  const openTagIds = intersectTagIds(memberships)

  return {
    guestIds: [...input.guestIds],
    openTagIds,
    pendingTagIds: [...openTagIds],
    catalog: [...input.catalog],
    searchQuery: "",
    createOpen: input.createOpen ?? false,
    createName: "",
  }
}

export function setAddTagSearchQuery(
  session: AddTagDialogSession,
  searchQuery: string
): AddTagDialogSession {
  return { ...session, searchQuery }
}

export function setAddTagCreateOpen(
  session: AddTagDialogSession,
  createOpen: boolean
): AddTagDialogSession {
  return {
    ...session,
    createOpen,
    createName: createOpen ? session.createName : "",
  }
}

export function setAddTagCreateName(
  session: AddTagDialogSession,
  createName: string
): AddTagDialogSession {
  return { ...session, createName }
}

export function stageTag(
  session: AddTagDialogSession,
  tagId: string
): AddTagDialogSession {
  if (session.pendingTagIds.includes(tagId)) {
    return session
  }

  return {
    ...session,
    pendingTagIds: [...session.pendingTagIds, tagId],
    searchQuery: "",
  }
}

export function unstageTag(
  session: AddTagDialogSession,
  tagId: string
): AddTagDialogSession {
  return {
    ...session,
    pendingTagIds: session.pendingTagIds.filter((id) => id !== tagId),
  }
}

/** Stage an existing or newly created catalog tag after API create. */
export function stageCreatedTag(
  session: AddTagDialogSession,
  tag: GuestTag
): AddTagDialogSession {
  const normalized = normalizeTagName(tag.name)
  const existing = session.catalog.find(
    (item) => normalizeTagName(item.name) === normalized
  )
  const catalog =
    existing != null
      ? session.catalog
      : [...session.catalog, tag]
  const tagId = existing?.id ?? tag.id

  return {
    ...stageTag({ ...session, catalog }, tagId),
    createOpen: false,
    createName: "",
    searchQuery: "",
  }
}

export function filterCatalogForSearch(
  catalog: readonly GuestTag[],
  searchQuery: string,
  pendingTagIds: readonly string[]
): GuestTag[] {
  const q = normalizeTagName(searchQuery)
  return catalog.filter((tag) => {
    if (pendingTagIds.includes(tag.id)) {
      return false
    }
    if (q.length === 0) {
      return true
    }
    return normalizeTagName(tag.name).includes(q)
  })
}
