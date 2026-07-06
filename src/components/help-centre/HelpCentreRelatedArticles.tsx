import { Link } from "react-router-dom"

import {
  helpCentreArticleSectionShell,
  helpCentreRelatedSectionInner,
  helpCentreSectionPadding,
} from "@/components/help-centre/helpCentreLayout"
import { HELP_CENTRE_CONTACT_SUPPORT_SUMMARY } from "@/content/helpCentre/copy"
import {
  HELP_CENTRE_CONTACT_URL,
  helpCentreArticleUrl,
} from "@/config/support"
import type { HelpCentreArticle } from "@/types/helpCentre"

type HelpCentreRelatedArticlesProps = {
  articles: HelpCentreArticle[]
  showContactSupport?: boolean
}

function RelatedArticleItem({
  title,
  summary,
  to,
}: {
  title: string
  summary: string
  to: string
}) {
  return (
    <Link to={to} className="group flex flex-col gap-2.5 no-underline">
      <span className="text-xl font-bold leading-normal text-[#141414] underline underline-offset-2 group-hover:text-[#14a74a]">
        {title}
      </span>
      <span className="max-w-[795px] text-base leading-[22px] text-[#141414]">
        {summary}
      </span>
    </Link>
  )
}

export function HelpCentreRelatedArticles({
  articles,
  showContactSupport = true,
}: HelpCentreRelatedArticlesProps) {
  if (articles.length === 0 && !showContactSupport) {
    return null
  }

  return (
    <section className="w-full bg-[#f4f4f4]">
      <div className={`${helpCentreArticleSectionShell} ${helpCentreSectionPadding}`}>
        <div className={`${helpCentreRelatedSectionInner} flex flex-col gap-[42px]`}>
          <h2 className="m-0 text-[28px] font-bold leading-normal text-[#141414]">
            Related articles
          </h2>

          <div className="flex flex-col gap-[42px]">
            {articles.map((article) => (
              <RelatedArticleItem
                key={article.slug}
                title={article.title}
                summary={article.summary}
                to={helpCentreArticleUrl(article.slug)}
              />
            ))}

            {showContactSupport && (
              <RelatedArticleItem
                title="Contact Tummly support"
                summary={HELP_CENTRE_CONTACT_SUPPORT_SUMMARY}
                to={HELP_CENTRE_CONTACT_URL}
              />
            )}
          </div>
        </div>
      </div>
    </section>
  )
}
