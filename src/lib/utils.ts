import { clsx, type ClassValue } from "clsx"
import { extendTailwindMerge } from "tailwind-merge"

/**
 * Operator `@theme` font sizes (`--text-op-*`) must be registered as font-size,
 * otherwise twMerge treats them like `text-op-*-color` and drops the size when
 * both appear on one element (e.g. KPI trend `text-op-xs` + `text-op-kpi-info-color`).
 */
const twMerge = extendTailwindMerge({
  extend: {
    classGroups: {
      "font-size": [
        {
          text: ["op-xs", "op-sm", "op-lg", "op-xl", "op-kpi-info-size"],
        },
      ],
    },
  },
})

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}
