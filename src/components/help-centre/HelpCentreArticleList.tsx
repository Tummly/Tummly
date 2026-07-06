import type { ReactNode } from "react"

import { Link } from "react-router-dom"



import type { HelpCentreArticle } from "@/types/helpCentre"
import { HELP_CENTRE_CONTACT_SUPPORT_SUMMARY } from "@/content/helpCentre/copy"
import {
  HELP_CENTRE_CONTACT_URL,
  helpCentreArticleUrl,
} from "@/config/support"



type HelpCentreArticleListProps = {

  articles: HelpCentreArticle[]

  emptyMessage?: ReactNode

}



function ArticleDivider() {

  return <hr className="m-0 w-full border-0 border-t border-[#e5e5e5]" />

}



function ArticleListItem({

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

      <span className="text-xl font-bold leading-normal text-[#141414] group-hover:underline">

        {title}

      </span>

      <span className="max-w-[795px] text-base leading-[22px] text-[#141414]">

        {summary}

      </span>

    </Link>

  )

}



export function HelpCentreArticleList({

  articles,

  emptyMessage,

}: HelpCentreArticleListProps) {

  if (articles.length === 0) {

    return (

      <p className="m-0 text-base leading-[22px] text-[#141414]">

        {emptyMessage}

      </p>

    )

  }



  return (

    <div className="flex flex-col gap-8">

      {articles.map((article, index) => (

        <div key={article.slug} className="flex flex-col gap-8">

          <ArticleListItem

            title={article.title}

            summary={article.summary}

            to={helpCentreArticleUrl(article.slug)}

          />

          {index < articles.length - 1 && <ArticleDivider />}

        </div>

      ))}

    </div>

  )

}



export function HelpCentreContactListItem() {

  return (

    <div className="flex flex-col gap-8">

      <ArticleDivider />

      <ArticleListItem

        title="Contact Tummly support"

        summary={HELP_CENTRE_CONTACT_SUPPORT_SUMMARY}

        to={HELP_CENTRE_CONTACT_URL}

      />

    </div>

  )

}


