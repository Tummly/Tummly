import {
  parseGroundedLiveAnswerMarkdown,
  type GroundedLiveAnswerInline,
} from "@/lib/operatorAiAssistant/groundedLiveAnswerMarkdown"

const BODY_TEXT_CLASS =
  "text-sm leading-5 text-[var(--op-color-gray-550)]"

function InlineContent({
  children,
}: {
  children: GroundedLiveAnswerInline[]
}) {
  return children.map((token, index) => {
    if (token.type === "lineBreak") {
      return <br key={index} />
    }
    if (token.type === "bold") {
      return (
        <span key={index} className="font-medium text-op-text-primary">
          {token.value}
        </span>
      )
    }
    return <span key={index}>{token.value}</span>
  })
}

export function GroundedLiveAnswerBody({ body }: { body: string }) {
  const blocks = parseGroundedLiveAnswerMarkdown(body)

  return (
    <div className={`flex flex-col gap-2 ${BODY_TEXT_CLASS}`}>
      {blocks.map((block, blockIndex) => {
        if (block.type === "heading") {
          return (
            <p
              key={blockIndex}
              className="text-sm leading-5 font-medium text-op-text-primary"
            >
              <InlineContent>{block.children}</InlineContent>
            </p>
          )
        }
        if (block.type === "unorderedList") {
          return (
            <ul key={blockIndex} className="list-disc space-y-1 pl-5">
              {block.items.map((item, itemIndex) => (
                <li key={itemIndex}>
                  <InlineContent>{item}</InlineContent>
                </li>
              ))}
            </ul>
          )
        }
        if (block.type === "orderedList") {
          return (
            <ol key={blockIndex} className="list-decimal space-y-1 pl-5">
              {block.items.map((item, itemIndex) => (
                <li key={itemIndex}>
                  <InlineContent>{item}</InlineContent>
                </li>
              ))}
            </ol>
          )
        }
        return (
          <p key={blockIndex}>
            <InlineContent>{block.children}</InlineContent>
          </p>
        )
      })}
    </div>
  )
}
