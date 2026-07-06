type HelpCentreArticleBodyProps = {
  body: string
}

type ArticleSection = {
  heading: string
  blocks: string[]
}

export function splitHelpCentreArticleBody(body: string) {
  const trimmed = body.trim()
  const rawSections = trimmed.split(/\n\n(?=## )/)

  let intro = ""
  let sectionParts = rawSections

  if (rawSections.length > 0 && !rawSections[0].startsWith("## ")) {
    intro = rawSections[0]
    sectionParts = rawSections.slice(1)
  }

  const sections: ArticleSection[] = sectionParts.map((section) => {
    const lines = section.trim().split("\n")
    const heading = lines[0].replace(/^##\s+/, "")
    const rest = lines.slice(1).join("\n").trim()
    const blocks = rest ? rest.split(/\n\n+/).map((block) => block.trim()) : []

    return { heading, blocks }
  })

  const introBlocks = intro
    ? intro.split(/\n\n+/).map((block) => block.trim()).filter(Boolean)
    : []

  return { introBlocks, sections }
}

function renderBlock(block: string, key: string) {
  const trimmed = block.trim()

  if (trimmed.startsWith("### ")) {
    return (
      <h3
        key={key}
        className="text-base font-bold text-[#141414]"
      >
        {trimmed.replace(/^###\s+/, "")}
      </h3>
    )
  }

  if (/^\d+\.\s/.test(trimmed)) {
    const items = trimmed.split("\n").filter(Boolean)
    return (
      <ol key={key} className="list-decimal space-y-0 pl-0">
        {items.map((item) => (
          <li key={item} className="ms-6">
            {item.replace(/^\d+\.\s+/, "")}
          </li>
        ))}
      </ol>
    )
  }

  if (trimmed.startsWith("- ")) {
    const items = trimmed.split("\n").filter(Boolean)
    return (
      <ul key={key} className="list-disc space-y-0 pl-0">
        {items.map((item) => (
          <li key={item} className="ms-6">
            {item.replace(/^-\s+/, "")}
          </li>
        ))}
      </ul>
    )
  }

  return <p key={key}>{trimmed}</p>
}

function ArticleSectionBlock({ section }: { section: ArticleSection }) {
  return (
    <section className="flex flex-col gap-[22px] text-base leading-[22px] text-[#141414]">
      <h2 className="text-lg font-bold text-[#141414]">{section.heading}</h2>
      {section.blocks.map((block, index) =>
        renderBlock(block, `${section.heading}-${index}`)
      )}
    </section>
  )
}

export function HelpCentreArticleIntro({ blocks }: { blocks: string[] }) {
  if (blocks.length === 0) {
    return null
  }

  return (
    <>
      {blocks.map((block, index) =>
        renderBlock(block, `intro-${index}`)
      )}
    </>
  )
}

export function HelpCentreArticleBody({ body }: HelpCentreArticleBodyProps) {
  const { sections } = splitHelpCentreArticleBody(body)

  if (sections.length === 0) {
    return null
  }

  return (
    <div className="flex flex-col gap-[42px]">
      {sections.map((section) => (
        <ArticleSectionBlock key={section.heading} section={section} />
      ))}
    </div>
  )
}
