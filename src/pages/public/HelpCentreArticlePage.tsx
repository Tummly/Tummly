import { Link, Navigate, useParams } from "react-router-dom"
import { ArrowLeft } from "lucide-react"

import { HELP_CENTRE_URL } from "@/config/support"
import Footer from "@/components/home/Footer"
import {
  HelpCentreArticleBody,
  HelpCentreArticleIntro,
  splitHelpCentreArticleBody,
} from "@/components/help-centre/HelpCentreArticleBody"
import {
  helpCentreArticleSectionInner,
  helpCentreArticleSectionShell,
  helpCentreSectionPadding,
} from "@/components/help-centre/helpCentreLayout"
import { HelpCentreRelatedArticles } from "@/components/help-centre/HelpCentreRelatedArticles"
import {
  getHelpCentreArticle,
  HELP_CENTRE_ARTICLES,
} from "@/content/helpCentre/articles"

export default function HelpCentreArticlePage() {
  const { slug } = useParams<{ slug: string }>()
  const article = slug ? getHelpCentreArticle(slug) : null

  if (!article) {
    return <Navigate to={HELP_CENTRE_URL} replace />
  }

  const relatedArticles = HELP_CENTRE_ARTICLES.filter((item) =>
    article.relatedSlugs.includes(item.slug)
  )
  const { introBlocks } = splitHelpCentreArticleBody(article.body)

  return (
    <>
      <section className="w-full bg-white">
        <div className={`${helpCentreArticleSectionShell} ${helpCentreSectionPadding}`}>
          <div className={`${helpCentreArticleSectionInner} flex flex-col gap-[42px]`}>
            <Link
              to={HELP_CENTRE_URL}
              className="inline-flex items-center gap-3 text-lg font-medium tracking-[-0.36px] text-[#232323] no-underline hover:underline"
            >
              <span className="flex items-center rounded-[40px] bg-[#f4f4f4] p-2.5">
                <ArrowLeft className="size-4" aria-hidden />
              </span>
              Back
            </Link>

            <div className="flex flex-col gap-[22px] text-base leading-[22px] text-[#141414]">
              <h1 className="m-0 text-[28px] font-bold leading-normal text-[#141414]">
                {article.title}
              </h1>
              <HelpCentreArticleIntro blocks={introBlocks} />
            </div>

            <HelpCentreArticleBody body={article.body} />
          </div>
        </div>
      </section>

      <HelpCentreRelatedArticles articles={relatedArticles} />
      <Footer />
    </>
  )
}
