import { useMemo, useState } from "react"
import { Link } from "react-router-dom"

import Footer from "@/components/home/Footer"
import {
  HelpCentreArticleList,
  HelpCentreContactListItem,
} from "@/components/help-centre/HelpCentreArticleList"
import { HelpCentreContactCta } from "@/components/help-centre/HelpCentreContactCta"
import { HelpCentreHero } from "@/components/help-centre/HelpCentreHero"
import {
  helpCentreHubSectionInner,
  helpCentreHubSectionShell,
  helpCentreSectionPadding,
} from "@/components/help-centre/helpCentreLayout"
import {
  HELP_CENTRE_CONTACT_URL,
  HELP_CENTRE_MY_QUERIES_URL,
} from "@/config/support"
import { filterHelpCentreArticles } from "@/content/helpCentre/articles"
import { useAuthStore } from "@/stores/authStore"

export default function HelpCentreHubPage() {
  const [search, setSearch] = useState("")
  const token = useAuthStore((state) => state.token)
  const role = useAuthStore((state) => state.role)
  const isOperator = Boolean(token && role === "USER")
  const articles = useMemo(
    () => filterHelpCentreArticles(search),
    [search]
  )

  const showContactRow = !search.trim()

  return (
    <>
      <HelpCentreHero search={search} onSearchChange={setSearch} />

      <section className="w-full bg-white">
        <div className={`${helpCentreHubSectionShell} ${helpCentreSectionPadding}`}>
          <div className={`${helpCentreHubSectionInner} flex flex-col gap-8`}>
            {isOperator && (
              <div className="flex justify-end">
                <Link
                  to={HELP_CENTRE_MY_QUERIES_URL}
                  className="text-sm font-medium text-[#14a74a] underline-offset-4 hover:underline"
                >
                  View my queries
                </Link>
              </div>
            )}
            <HelpCentreArticleList              articles={articles}
              emptyMessage={
                <>
                  No articles match your search. Try different keywords or{" "}
                  <Link
                    to={HELP_CENTRE_CONTACT_URL}
                    className="font-medium text-[#14a74a] underline"
                  >
                    contact us
                  </Link>
                  .
                </>
              }
            />
            {showContactRow && articles.length > 0 && (
              <HelpCentreContactListItem />
            )}
          </div>
        </div>
      </section>

      <HelpCentreContactCta />
      <Footer />
    </>
  )
}
