import { useCallback, useEffect, useMemo, useState } from "react"
import { useSearchParams } from "react-router-dom"

import type { SupportQuerySubmitterType } from "@/types/support"

const PAGE_SIZE_OPTIONS = [20, 50, 100] as const
const DEFAULT_PAGE_SIZE = 20

export type SupportInboxState = {
  q: string
  status: string
  topic: string
  type: "ALL" | SupportQuerySubmitterType
  page: number
  pageSize: number
}

function parsePageSize(value: string | null) {
  const parsed = Number(value)
  return PAGE_SIZE_OPTIONS.includes(parsed as (typeof PAGE_SIZE_OPTIONS)[number])
    ? parsed
    : DEFAULT_PAGE_SIZE
}

function parsePage(value: string | null) {
  const parsed = Number(value)
  return Number.isInteger(parsed) && parsed >= 1 ? parsed : 1
}

function parseType(value: string | null): SupportInboxState["type"] {
  if (value === "operator" || value === "contact") {
    return value
  }
  return "ALL"
}

export function useSupportInboxParams() {
  const [searchParams, setSearchParams] = useSearchParams()

  const state = useMemo<SupportInboxState>(
    () => ({
      q: searchParams.get("q") ?? "",
      status: searchParams.get("status") ?? "ALL",
      topic: searchParams.get("topic") ?? "ALL",
      type: parseType(searchParams.get("type")),
      page: parsePage(searchParams.get("page")),
      pageSize: parsePageSize(searchParams.get("pageSize")),
    }),
    [searchParams]
  )

  const [searchDraft, setSearchDraft] = useState(state.q)

  useEffect(() => {
    setSearchDraft(state.q)
  }, [state.q])

  useEffect(() => {
    const handle = window.setTimeout(() => {
      const next = searchDraft.trim()
      if (next === state.q.trim()) {
        return
      }

      const params = new URLSearchParams(searchParams)
      if (next) {
        params.set("q", next)
      } else {
        params.delete("q")
      }
      params.delete("page")
      setSearchParams(params, { replace: true })
    }, 300)

    return () => window.clearTimeout(handle)
  }, [searchDraft, searchParams, setSearchParams, state.q])

  const patchParams = useCallback(
    (patch: Partial<SupportInboxState>, resetPage = true) => {
      const params = new URLSearchParams(searchParams)

      if (patch.q !== undefined) {
        const next = patch.q.trim()
        if (next) {
          params.set("q", next)
        } else {
          params.delete("q")
        }
      }

      if (patch.status !== undefined) {
        if (patch.status === "ALL") {
          params.delete("status")
        } else {
          params.set("status", patch.status)
        }
      }

      if (patch.topic !== undefined) {
        if (patch.topic === "ALL") {
          params.delete("topic")
        } else {
          params.set("topic", patch.topic)
        }
      }

      if (patch.type !== undefined) {
        if (patch.type === "ALL") {
          params.delete("type")
        } else {
          params.set("type", patch.type)
        }
      }

      if (patch.pageSize !== undefined) {
        if (patch.pageSize === DEFAULT_PAGE_SIZE) {
          params.delete("pageSize")
        } else {
          params.set("pageSize", String(patch.pageSize))
        }
      }

      if (patch.page !== undefined) {
        if (patch.page <= 1) {
          params.delete("page")
        } else {
          params.set("page", String(patch.page))
        }
      } else if (resetPage) {
        params.delete("page")
      }

      setSearchParams(params, { replace: true })
    },
    [searchParams, setSearchParams]
  )

  return {
    state,
    searchDraft,
    setSearchDraft,
    setStatus: (status: string) => patchParams({ status }),
    setTopic: (topic: string) => patchParams({ topic }),
    setType: (type: SupportInboxState["type"]) => patchParams({ type }),
    setPage: (page: number) => patchParams({ page }, false),
    setPageSize: (pageSize: number) => patchParams({ pageSize: pageSize, page: 1 }),
    pageSizeOptions: PAGE_SIZE_OPTIONS,
  }
}
