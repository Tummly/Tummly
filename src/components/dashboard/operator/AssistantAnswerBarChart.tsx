import type { OperatorAiAssistantAnswerChart } from "@/lib/operatorAiAssistant/createOperatorAiAssistantModule"

type AssistantAnswerBarChartProps = {
  chart: OperatorAiAssistantAnswerChart
}

type ChartPoint = OperatorAiAssistantAnswerChart["series"][number]

/** Prefer the 12 PM–11 PM window for full-day Feedback series (Figma chart). */
function pickDisplaySeries(series: ChartPoint[]) {
  if (series.length !== 24) {
    return series
  }

  const afternoon = series.slice(12, 24)
  // Use the Figma afternoon window when it has a real spread of values.
  if (afternoon.filter((point) => point.value > 0).length >= 3) {
    return afternoon
  }

  const nonZeroIndexes = series
    .map((point, index) => (point.value > 0 ? index : -1))
    .filter((index) => index >= 0)
  if (nonZeroIndexes.length === 0) {
    return afternoon
  }

  const first = Math.max(0, nonZeroIndexes[0]! - 1)
  const last = Math.min(23, nonZeroIndexes[nonZeroIndexes.length - 1]! + 1)
  const span = last - first + 1
  if (span >= 12) {
    return series.slice(first, last + 1)
  }

  const center = Math.floor((first + last) / 2)
  const start = Math.max(0, Math.min(12, center - 5))
  return series.slice(start, start + 12)
}

/** Figma axis: "12 PM" then bare hour digits. */
function formatAxisLabel(label: string, index: number, series: ChartPoint[]) {
  const hourMatch = label.match(/^(\d{1,2})\s*(AM|PM)$/i)
  if (!hourMatch) {
    return label
  }

  const hour = hourMatch[1]
  const period = hourMatch[2]!.toUpperCase()
  if (index === 0 && hour === "12") {
    return `12 ${period}`
  }

  const prior = series[index - 1]?.label.match(/^(\d{1,2})\s*(AM|PM)$/i)
  if (prior && prior[2]!.toUpperCase() !== period && hour === "12") {
    return `12 ${period}`
  }

  return hour
}

/**
 * Figma-style Assistant bar chart: chunky columns, rounded tops only,
 * no axes/grid, short floor bars for low/zero values.
 */
export function AssistantAnswerBarChart({ chart }: AssistantAnswerBarChartProps) {
  const displaySeries = pickDisplaySeries(chart.series)
  const values = displaySeries.map((point) => point.value)
  const max = Math.max(1, ...values)
  const min = Math.min(...values)
  const hasVariance = max > min
  const highlightThreshold = max * 0.6
  const barAreaPx = 96
  // Figma keeps a visible short column even when the value is 0.
  const floorPx = Math.round(barAreaPx * 0.12)

  return (
    <div
      className="flex w-full flex-col gap-3"
      data-assistant-answer-chart=""
    >
      <p className="m-0 text-xs font-normal leading-4 tracking-normal text-op-assistant-list-subtitle">
        {chart.title}
      </p>
      <div
        className="flex items-end gap-[5px]"
        role="img"
        aria-label={chart.title}
      >
        {displaySeries.map((point, index) => {
          const scaled =
            point.value <= 0
              ? floorPx
              : Math.max(floorPx, Math.round((point.value / max) * barAreaPx))
          const isPeak =
            hasVariance && point.value >= highlightThreshold && point.value > 0
          return (
            <div
              key={`${point.label}-${index}`}
              className="flex min-w-0 flex-1 flex-col items-center gap-2"
              title={`${point.label}: ${point.value}`}
            >
              <div
                className="flex w-full items-end justify-center"
                style={{ height: barAreaPx }}
              >
                <div
                  className={
                    isPeak
                      ? "w-full rounded-t-[4px] bg-op-color-green-500"
                      : "w-full rounded-t-[4px] bg-op-assistant-list-border"
                  }
                  style={{ height: scaled }}
                />
              </div>
              <span className="w-full text-center text-[9px] leading-3 text-op-assistant-list-subtitle whitespace-nowrap">
                {formatAxisLabel(point.label, index, displaySeries)}
              </span>
            </div>
          )
        })}
      </div>
    </div>
  )
}
