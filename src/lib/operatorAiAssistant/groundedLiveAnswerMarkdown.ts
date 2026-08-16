export type GroundedLiveAnswerInline =
  | { type: "text"; value: string }
  | { type: "bold"; value: string }
  | { type: "lineBreak" }

type GroundedLiveAnswerListBlock =
  | {
      type: "unorderedList"
      items: GroundedLiveAnswerInline[][]
    }
  | {
      type: "orderedList"
      items: GroundedLiveAnswerInline[][]
    }

export type GroundedLiveAnswerBlock =
  | {
      type: "heading"
      level: 1 | 2 | 3
      children: GroundedLiveAnswerInline[]
    }
  | {
      type: "paragraph"
      children: GroundedLiveAnswerInline[]
    }
  | GroundedLiveAnswerListBlock

function parseInline(value: string): GroundedLiveAnswerInline[] {
  const tokens: GroundedLiveAnswerInline[] = []
  const pattern = /(\*\*|__)(.+?)\1/g
  let cursor = 0

  for (const match of value.matchAll(pattern)) {
    const index = match.index
    if (index > cursor) {
      tokens.push({ type: "text", value: value.slice(cursor, index) })
    }
    tokens.push({ type: "bold", value: match[2] })
    cursor = index + match[0].length
  }

  if (cursor < value.length) {
    tokens.push({ type: "text", value: value.slice(cursor) })
  }

  return tokens
}

export function parseGroundedLiveAnswerMarkdown(
  markdown: string
): GroundedLiveAnswerBlock[] {
  const blocks: GroundedLiveAnswerBlock[] = []
  let paragraph: GroundedLiveAnswerInline[] = []
  let listType: GroundedLiveAnswerListBlock["type"] | null = null
  let listItems: GroundedLiveAnswerInline[][] = []
  let fenceMarker: "`" | "~" | null = null

  const flushParagraph = () => {
    if (paragraph.length > 0) {
      blocks.push({ type: "paragraph", children: paragraph })
      paragraph = []
    }
  }

  const flushList = () => {
    if (listType) {
      blocks.push({ type: listType, items: listItems })
      listType = null
      listItems = []
    }
  }

  for (const line of markdown.replace(/\r\n?/g, "\n").split("\n")) {
    const fence = line.match(/^ {0,3}(`{3,}|~{3,})(.*)$/)
    if (fenceMarker) {
      if (fence?.[1][0] === fenceMarker && fence[2].trim().length === 0) {
        flushParagraph()
        fenceMarker = null
        continue
      }
      if (paragraph.length > 0) {
        paragraph.push({ type: "lineBreak" })
      }
      paragraph.push({ type: "text", value: line })
      continue
    }
    if (fence) {
      flushParagraph()
      flushList()
      fenceMarker = fence[1][0] as "`" | "~"
      continue
    }

    if (line.trim().length === 0) {
      flushParagraph()
      flushList()
      continue
    }

    const heading = line.match(/^ {0,3}(#{1,3})\s+(.+?)\s*#*\s*$/)
    if (heading) {
      flushParagraph()
      flushList()
      blocks.push({
        type: "heading",
        level: heading[1].length as 1 | 2 | 3,
        children: parseInline(heading[2]),
      })
      continue
    }

    const unorderedItem = line.match(/^[-+*]\s+(.+)$/)
    const orderedItem = line.match(/^\d+[.)]\s+(.+)$/)
    if (unorderedItem || orderedItem) {
      flushParagraph()
      const type = unorderedItem ? "unorderedList" : "orderedList"
      if (listType !== type) {
        flushList()
        listType = type
      }
      listItems.push(parseInline((unorderedItem ?? orderedItem)![1]))
      continue
    }

    flushList()
    const nestedListItem = line.match(/^ {2,}((?:[-+*]|\d+[.)])\s+.+)$/)
    const paragraphLine = nestedListItem?.[1] ?? line
    if (paragraph.length > 0) {
      paragraph.push({ type: "lineBreak" })
    }
    paragraph.push(...parseInline(paragraphLine))
  }

  flushParagraph()
  flushList()
  return blocks
}
