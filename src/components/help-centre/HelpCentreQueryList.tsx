import { Link } from "react-router-dom"

import { HelpCentreStatusBadge } from "@/components/help-centre/HelpCentreStatusBadge"
import { helpCentreMyQueryUrl } from "@/config/support"
import type { HelpCentreQueryListItem } from "@/types/helpCentre"

function QueryDivider() {
  return <hr className="m-0 w-full border-0 border-t border-[#e5e5e5]" />
}

function formatUpdatedAt(value: string) {
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

type HelpCentreQueryListProps = {
  queries: HelpCentreQueryListItem[]
}

export function HelpCentreQueryList({ queries }: HelpCentreQueryListProps) {
  return (
    <div className="flex flex-col gap-8">
      {queries.map((query, index) => (
        <div key={query.id} className="flex flex-col gap-8">
          <Link
            to={helpCentreMyQueryUrl(query.id)}
            className="group flex flex-col gap-3 no-underline sm:flex-row sm:items-start sm:justify-between"
          >
            <div className="flex min-w-0 flex-1 flex-col gap-2.5">
              <span className="text-xl font-bold leading-normal text-[#141414] group-hover:underline">
                {query.topicLabel}
              </span>
              {query.preview && (
                <span className="line-clamp-2 max-w-[795px] text-base leading-[22px] text-[#141414]">
                  {query.preview}
                </span>
              )}
            </div>
            <div className="flex shrink-0 flex-col items-start gap-2 sm:items-end">
              <HelpCentreStatusBadge
                status={query.status}
                statusLabel={query.statusLabel}
              />
              <span className="text-sm leading-[22px] text-[#6b6b6b]">
                Updated {formatUpdatedAt(query.updatedAt)}
              </span>
            </div>
          </Link>
          {index < queries.length - 1 && <QueryDivider />}
        </div>
      ))}
    </div>
  )
}
