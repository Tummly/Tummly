import { clsx, type ClassValue } from "clsx"
import { extendTailwindMerge } from "tailwind-merge"

/**
 * Operator `@theme` tokens that twMerge must know about:
 * - font sizes (`--text-op-*`) — otherwise treated like `text-op-*-color` and
 *   dropped when both appear (e.g. KPI trend `text-op-xs` + `text-op-kpi-info-color`).
 * - radii (`rounded-op-*`) — otherwise they do not conflict with `rounded-full`
 *   / `rounded-xs`, so Operator buttons keep the base pill radius.
 */
const twMerge = extendTailwindMerge({
  extend: {
    classGroups: {
      "font-size": [
        {
          text: ["op-xs", "op-sm", "op-lg", "op-xl", "op-kpi-info-size"],
        },
      ],
      rounded: [
        {
          rounded: ["op-sm", "op-md", "op-lg", "op-xl", "op-toast"],
        },
      ],
    },
  },
})

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}
