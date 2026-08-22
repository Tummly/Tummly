import { describe, expect, it } from "vitest"

import {
  filterConversationsByTitle,
  formatConversationListMeta,
  groupRecentConversations,
  recentGroupIdForActivity,
} from "./assistantConversationList"

function item(
  id: string,
  title: string,
  lastActivityAt: string,
  ownedLocationName = "Camden"
) {
  return {
    id,
    title,
    ownedLocationName,
    lastActivityAt,
    isArchived: false,
  }
}

describe("groupRecentConversations", () => {
  const now = new Date(2026, 7, 13, 15, 0, 0).getTime()

  it("groups with now() into Today / Yesterday / Previous 7 days / Older", () => {
    const today = new Date(2026, 7, 13, 10, 0, 0).toISOString()
    const todayLater = new Date(2026, 7, 13, 14, 0, 0).toISOString()
    const yesterday = new Date(2026, 7, 12, 18, 0, 0).toISOString()
    const previous = new Date(2026, 7, 8, 9, 0, 0).toISOString()
    const older = new Date(2026, 7, 1, 9, 0, 0).toISOString()

    const groups = groupRecentConversations(
      [
        item("older", "Older thread", older),
        item("today-early", "Morning ask", today),
        item("prev", "Week ask", previous),
        item("today-late", "Afternoon ask", todayLater),
        item("yest", "Yesterday ask", yesterday),
      ],
      now
    )

    expect(groups.map((group) => group.id)).toEqual([
      "today",
      "yesterday",
      "previous7",
      "older",
    ])
    expect(groups[0]?.rows.map((row) => row.id)).toEqual([
      "today-late",
      "today-early",
    ])
    expect(groups[1]?.rows.map((row) => row.id)).toEqual(["yest"])
    expect(groups[2]?.rows.map((row) => row.id)).toEqual(["prev"])
    expect(groups[3]?.rows.map((row) => row.id)).toEqual(["older"])
    expect(groups.map((group) => group.label)).toEqual([
      "Today",
      "Yesterday",
      "Previous 7 days",
      "Older",
    ])
  })

  it("omits empty groups", () => {
    const today = new Date(2026, 7, 13, 10, 0, 0).toISOString()
    const groups = groupRecentConversations(
      [item("1", "Only today", today)],
      now
    )
    expect(groups).toHaveLength(1)
    expect(groups[0]?.id).toBe("today")
  })

  it("puts the start of yesterday in Yesterday and eight days ago in Older", () => {
    expect(
      recentGroupIdForActivity(new Date(2026, 7, 12, 0, 0, 0).toISOString(), now)
    ).toBe("yesterday")
    expect(
      recentGroupIdForActivity(new Date(2026, 7, 5, 12, 0, 0).toISOString(), now)
    ).toBe("older")
  })
})

describe("filterConversationsByTitle", () => {
  const rows = [
    item("1", "Weekly feedback themes", "2026-08-13T10:00:00.000Z"),
    item("2", "Camden service issues", "2026-08-12T10:00:00.000Z"),
    item("3", "August offer idea", "2026-08-10T10:00:00.000Z"),
  ]

  it("filters the loaded list by title on the client", () => {
    expect(filterConversationsByTitle(rows, "offer").map((row) => row.id)).toEqual(
      ["3"]
    )
    expect(
      filterConversationsByTitle(rows, "FEEDBACK").map((row) => row.id)
    ).toEqual(["1"])
  })

  it("returns the full list when the query is blank", () => {
    expect(filterConversationsByTitle(rows, "   ")).toHaveLength(3)
  })
})

describe("formatConversationListMeta", () => {
  it("uses Owned location and relative last activity, including 12 minutes ago", () => {
    const now = Date.parse("2026-08-13T12:00:00.000Z")
    expect(
      formatConversationListMeta("Camden", "2026-08-13T11:48:00.000Z", now)
    ).toBe("Camden · 12 minutes ago")
    expect(
      formatConversationListMeta("Shoreditch", "2026-08-13T11:48:00.000Z", now)
    ).toBe("Shoreditch · 12 minutes ago")
    expect(
      formatConversationListMeta("All Locations", "2026-08-13T11:48:00.000Z", now)
    ).toBe("All Locations · 12 minutes ago")
  })
})
