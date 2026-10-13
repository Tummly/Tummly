import { describe, expect, it } from "vitest"

import { parseGroundedLiveAnswerMarkdown } from "./groundedLiveAnswerMarkdown"

describe("parseGroundedLiveAnswerMarkdown", () => {
  it("parses short headings, bold text, paragraphs, and line breaks", () => {
    expect(
      parseGroundedLiveAnswerMarkdown(
        "## Summary\n\nGuests **liked the service**.\nWait times improved."
      )
    ).toEqual([
      {
        type: "heading",
        level: 2,
        children: [{ type: "text", value: "Summary" }],
      },
      {
        type: "paragraph",
        children: [
          { type: "text", value: "Guests " },
          { type: "bold", value: "liked the service" },
          { type: "text", value: "." },
          { type: "lineBreak" },
          { type: "text", value: "Wait times improved." },
        ],
      },
    ])
  })

  it("parses top-level unordered and ordered lists without nesting lists", () => {
    expect(
      parseGroundedLiveAnswerMarkdown(
        "- Friendly service\n- **Fast** checkout\n  - Nested detail\n\n1. First\n2. Second"
      )
    ).toEqual([
      {
        type: "unorderedList",
        items: [
          [{ type: "text", value: "Friendly service" }],
          [{ type: "bold", value: "Fast" }, { type: "text", value: " checkout" }],
        ],
      },
      {
        type: "paragraph",
        children: [{ type: "text", value: "- Nested detail" }],
      },
      {
        type: "orderedList",
        items: [
          [{ type: "text", value: "First" }],
          [{ type: "text", value: "Second" }],
        ],
      },
    ])
  })

  it("parses blockquotes for feedback excerpts", () => {
    expect(
      parseGroundedLiveAnswerMarkdown('> "Service was slow at 8 PM."')
    ).toEqual([
      {
        type: "blockquote",
        children: [{ type: "quote", value: "Service was slow at 8 PM." }],
      },
    ])
  })

  it("parses curly double quotes as feedback quote spans", () => {
    expect(
      parseGroundedLiveAnswerMarkdown(
        '1. Priya Nguyen (Email) — “Bill had an unexpected charge.”'
      )
    ).toEqual([
      {
        type: "orderedList",
        items: [
          [
            { type: "text", value: "Priya Nguyen (Email) — " },
            { type: "quote", value: "Bill had an unexpected charge." },
          ],
        ],
      },
    ])
  })

  it("parses markdown tables", () => {
    expect(
      parseGroundedLiveAnswerMarkdown(
        "| Metric | Value |\n| --- | --- |\n| Covers | 24 |\n| New guests | 24 |"
      )
    ).toEqual([
      {
        type: "table",
        headers: ["Metric", "Value"],
        rows: [
          ["Covers", "24"],
          ["New guests", "24"],
        ],
      },
    ])
  })

  it("flattens unsupported markdown into plain text tokens", () => {
    expect(
      parseGroundedLiveAnswerMarkdown(
        [
          "[Report](https://example.com) and ![Chart](chart.png)",
          "`code` and <strong>raw HTML</strong>",
          "#### Not a short heading",
          "",
          "```ts",
          "const score = 4",
          "```",
        ].join("\n")
      )
    ).toEqual([
      {
        type: "paragraph",
        children: [
          {
            type: "text",
            value: "[Report](https://example.com) and ![Chart](chart.png)",
          },
          { type: "lineBreak" },
          { type: "text", value: "`code` and <strong>raw HTML</strong>" },
          { type: "lineBreak" },
          { type: "text", value: "#### Not a short heading" },
        ],
      },
      {
        type: "paragraph",
        children: [{ type: "text", value: "const score = 4" }],
      },
    ])
  })
})
