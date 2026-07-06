import type { HelpCentreQueryDetail } from "@/types/helpCentre"

function formatTimestamp(value: string) {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) {
    return value
  }

  return date.toLocaleString("en-GB", {
    day: "numeric",
    month: "short",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  })
}

function authorLabel(
  kind: HelpCentreQueryDetail["messages"][number]["authorKind"]
) {
  switch (kind) {
    case "SUPPORT":
      return "Tummly Support"
    case "OPERATOR":
    case "SUBMITTER":
      return "You"
    default:
      return "You"
  }
}

type HelpCentreThreadMessageProps = {
  message: HelpCentreQueryDetail["messages"][number]
}

export function HelpCentreThreadMessage({
  message,
}: HelpCentreThreadMessageProps) {
  const isSupport = message.authorKind === "SUPPORT"

  return (
    <article
      className={
        isSupport
          ? "rounded-xl border border-[#e5e5e5] bg-[#f6f6f6] px-5 py-4"
          : "rounded-xl border border-[#e5e5e5] bg-white px-5 py-4"
      }
    >
      <div className="mb-2 flex items-center justify-between gap-3">
        <span className="text-sm font-bold text-[#141414]">
          {authorLabel(message.authorKind)}
        </span>
        <span className="text-sm leading-[22px] text-[#6b6b6b]">
          {formatTimestamp(message.createdAt)}
        </span>
      </div>
      <p className="m-0 text-base leading-[22px] whitespace-pre-wrap text-[#141414]">
        {message.body}
      </p>
    </article>
  )
}
