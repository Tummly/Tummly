import { useEffect, useState } from "react"
import { Link } from "react-router-dom"

import { getMyHelpCentreQueries } from "@/api/helpCentreApi"
import { HelpCentreQueryList } from "@/components/help-centre/HelpCentreQueryList"
import {
  helpCentreArticleSectionInner,
  helpCentreArticleSectionShell,
  helpCentreSectionPadding,
} from "@/components/help-centre/helpCentreLayout"
import Footer from "@/components/home/Footer"
import { Button } from "@/components/ui/button"
import {
  HELP_CENTRE_CONTACT_URL,
  HELP_CENTRE_URL,
} from "@/config/support"
import {
  HELP_CENTRE_MY_QUERIES_EMPTY,
  HELP_CENTRE_MY_QUERIES_SUMMARY,
} from "@/content/helpCentre/copy"
import type { HelpCentreQueryListItem } from "@/types/helpCentre"

export default function MyQueriesPage() {
  const [queries, setQueries] = useState<HelpCentreQueryListItem[]>([])
  const [state, setState] = useState<"loading" | "loaded" | "error">("loading")

  useEffect(() => {
    let active = true

    void (async () => {
      try {
        const result = await getMyHelpCentreQueries()
        if (active) {
          setQueries(result)
          setState("loaded")
        }
      } catch {
        if (active) {
          setState("error")
        }
      }
    })()

    return () => {
      active = false
    }
  }, [])

  return (
    <>
      <section className="w-full bg-white">
        <div className={`${helpCentreArticleSectionShell} ${helpCentreSectionPadding}`}>
          <div className={`${helpCentreArticleSectionInner} flex flex-col gap-8`}>
            <Link
              to={HELP_CENTRE_URL}
              className="w-fit text-base font-medium text-[#14a74a] underline-offset-4 hover:underline"
            >
              ← Back to Help Centre
            </Link>

            <div className="flex flex-col gap-2.5">
              <h1 className="m-0 text-[28px] font-bold leading-normal text-[#141414] lg:text-[32px]">
                My queries
              </h1>
              <p className="m-0 max-w-[640px] text-base leading-[22px] text-[#141414]">
                {HELP_CENTRE_MY_QUERIES_SUMMARY}
              </p>
            </div>

            {state === "loading" && (
              <p className="m-0 text-base leading-[22px] text-[#6b6b6b]">
                Loading queries...
              </p>
            )}

            {state === "error" && (
              <p className="m-0 text-base leading-[22px] text-destructive">
                Unable to load your queries right now.
              </p>
            )}

            {state === "loaded" && queries.length === 0 && (
              <div className="flex flex-col items-start gap-4 rounded-xl border border-dashed border-[#e5e5e5] px-6 py-10">
                <p className="m-0 text-base leading-[22px] text-[#6b6b6b]">
                  {HELP_CENTRE_MY_QUERIES_EMPTY}
                </p>
                <Button
                  asChild
                  className="h-auto rounded-[84px] bg-[#14a74a] px-[17px] py-[9px] text-base font-medium leading-5 text-white hover:bg-[#129641]"
                >
                  <Link to={HELP_CENTRE_CONTACT_URL}>Contact us</Link>
                </Button>
              </div>
            )}

            {queries.length > 0 && <HelpCentreQueryList queries={queries} />}
          </div>
        </div>
      </section>

      <Footer />
    </>
  )
}
