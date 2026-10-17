import { useState } from "react"
import {
  parseGroundedLiveAnswerMarkdown,
  type GroundedLiveAnswerBlock,
  type GroundedLiveAnswerInline,
} from "@/lib/operatorAiAssistant/groundedLiveAnswerMarkdown"

const BODY_TEXT_CLASS =
  "text-sm leading-5 text-[var(--op-color-gray-550)]"

/** Show "Show N more" only when the list has more than 8 items. */
const COLLAPSE_AFTER_ITEM_COUNT = 8
/** When collapsed, show this many items before "Show N more". */
const COLLAPSED_VISIBLE_ITEMS = 8

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
    if (token.type === "quote") {
      return (
        <span
          key={index}
          className="text-op-text-primary"
          data-assistant-feedback-quote=""
        >
          &quot;{token.value}&quot;
        </span>
      )
    }
    return <span key={index}>{token.value}</span>
  })
}

function shouldCollapseList(
  block: Extract<GroundedLiveAnswerBlock, { type: "unorderedList" | "orderedList" }>
) {
  return block.items.length > COLLAPSE_AFTER_ITEM_COUNT
}

function ListBlock({
  block,
  expanded,
  onExpand,
}: {
  block: Extract<GroundedLiveAnswerBlock, { type: "unorderedList" | "orderedList" }>
  expanded: boolean
  onExpand: () => void
}) {
  const collapse = shouldCollapseList(block)
  const visible =
    !collapse || expanded
      ? block.items
      : block.items.slice(0, COLLAPSED_VISIBLE_ITEMS)
  const hiddenCount = block.items.length - visible.length
  const ListTag = block.type === "orderedList" ? "ol" : "ul"
  const listClass =
    block.type === "orderedList"
      ? "list-decimal space-y-1 pl-5"
      : "list-disc space-y-1 pl-5"

  return (
    <div className="flex flex-col gap-1">
      <ListTag className={listClass}>
        {visible.map((item, itemIndex) => (
          <li key={itemIndex}>
            <InlineContent>{item}</InlineContent>
          </li>
        ))}
      </ListTag>
      {hiddenCount > 0 ? (
        <button
          type="button"
          className="self-start text-xs font-medium text-op-assistant-list-title underline-offset-2 hover:underline"
          onClick={onExpand}
        >
          Show {hiddenCount} more
        </button>
      ) : null}
    </div>
  )
}

export function GroundedLiveAnswerBody({ body }: { body: string }) {
  const blocks = parseGroundedLiveAnswerMarkdown(body)
  const [expandedLists, setExpandedLists] = useState<Record<number, boolean>>({})

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
        if (block.type === "blockquote") {
          return (
            <p
              key={blockIndex}
              className="m-0 border-l-2 border-op-assistant-list-border pl-3 text-op-text-primary"
            >
              <InlineContent>{block.children}</InlineContent>
            </p>
          )
        }
        if (block.type === "table") {
          return (
            <div
              key={blockIndex}
              className="overflow-x-auto rounded-[8px] border border-op-assistant-list-border"
            >
              <table className="w-full border-collapse text-left text-sm">
                <thead>
                  <tr className="border-b border-op-assistant-list-border">
                    {block.headers.map((header) => (
                      <th
                        key={header}
                        className="px-2 py-1.5 font-medium text-op-text-primary"
                      >
                        {header}
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {block.rows.map((row, rowIndex) => (
                    <tr
                      key={rowIndex}
                      className="border-b border-op-assistant-list-border last:border-b-0"
                    >
                      {row.map((cell, cellIndex) => (
                        <td key={cellIndex} className="px-2 py-1.5">
                          {cell}
                        </td>
                      ))}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )
        }
        if (block.type === "unorderedList" || block.type === "orderedList") {
          return (
            <ListBlock
              key={blockIndex}
              block={block}
              expanded={expandedLists[blockIndex] === true}
              onExpand={() => {
                setExpandedLists((prev) => ({ ...prev, [blockIndex]: true }))
              }}
            />
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
