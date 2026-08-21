/** Home Needs attention section body + row chrome (ticket 03). */

import type { HomeNeedsAttentionProjection } from "@/lib/operatorHome/buildHomeNeedsAttention"
import { NEEDS_ATTENTION_LOAD_ERROR } from "@/lib/operatorHome/operatorHomeSectionPresentation"

export {
  NEEDS_ATTENTION_DUPLICATE_DRAFT_ERROR,
  NEEDS_ATTENTION_DUPLICATE_DRAFT_TOAST,
  NEEDS_ATTENTION_EMPTY_COPY,
  NEEDS_ATTENTION_LOAD_ERROR,
  NEEDS_ATTENTION_VIEW_ALL_LABEL,
  WARNING_ROW_CLASS,
} from "@/lib/operatorHome/operatorHomeSectionPresentation"

export type HomeNeedsAttentionLoadStatus = "idle" | "loading" | "loaded" | "error"

export type HomeNeedsAttentionSectionBody =
  | { mode: "loading" }
  | { mode: "error"; message: string }
  | { mode: "empty" }
  | {
      mode: "rows"
      rows: HomeNeedsAttentionProjection["visibleRows"]
      showViewAll: boolean
    }

export function resolveHomeNeedsAttentionSectionBody(input: {
  loadStatus: HomeNeedsAttentionLoadStatus
  projection: HomeNeedsAttentionProjection | null
  errorMessage: string | null
  expanded: boolean
}): HomeNeedsAttentionSectionBody {
  if (input.loadStatus === "idle" || input.loadStatus === "loading") {
    return { mode: "loading" }
  }

  if (input.loadStatus === "error") {
    return {
      mode: "error",
      message: input.errorMessage?.trim() || NEEDS_ATTENTION_LOAD_ERROR,
    }
  }

  const projection = input.projection
  if (projection == null || projection.isEmpty) {
    return { mode: "empty" }
  }

  if (input.expanded) {
    return {
      mode: "rows",
      rows: projection.allRows,
      showViewAll: false,
    }
  }

  return {
    mode: "rows",
    rows: projection.visibleRows,
    showViewAll: projection.showViewAll,
  }
}
