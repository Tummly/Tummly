import { expect, test } from "@playwright/test";

import { ABOUT_HEADINGS, openAboutPage, section } from "./about-page";

for (const width of [375, 1024, 1440]) {
  test(`About page has no horizontal overflow and no clipped principle cards at ${width}px`, async ({
    page,
  }) => {
    await page.setViewportSize({ width, height: 900 });
    await openAboutPage(page);
    await page.locator("footer").last().scrollIntoViewIfNeeded();
    expect(
      await page.evaluate(() => document.documentElement.scrollWidth),
    ).toBeLessThanOrEqual(width);

    const cards = section(page, ABOUT_HEADINGS.principles).locator("article");
    await expect(cards).toHaveCount(5);
    const boxes = await cards.evaluateAll((articles) =>
      articles.map((article) => {
        const rect = article.getBoundingClientRect();
        const clipped = [...article.querySelectorAll("h3, p")].some(
          (text) =>
            text.scrollWidth > text.clientWidth + 1 ||
            text.getBoundingClientRect().right > rect.right + 1,
        );
        return { width: rect.width, clipped };
      }),
    );
    for (const box of boxes) {
      expect(box.clipped).toBe(false);
      // Mobile cards are a 260px scroll track; wider screens wrap the row.
      expect(box.width).toBeLessThanOrEqual(width === 375 ? 260 : width);
    }
  });
}

test("Principle cards wrap 3 + 2 at 1024px and sit in one row at 1440px", async ({
  page,
}) => {
  await page.setViewportSize({ width: 1024, height: 900 });
  await openAboutPage(page);
  const cards = section(page, ABOUT_HEADINGS.principles).locator("article");
  const rowsAt1024 = new Set(
    await cards.evaluateAll((articles) =>
      articles.map((a) => Math.round(a.getBoundingClientRect().top)),
    ),
  );
  expect(rowsAt1024.size).toBe(2);

  await page.setViewportSize({ width: 1440, height: 900 });
  const tops = await cards.evaluateAll((articles) =>
    articles.map((a) => Math.round(a.getBoundingClientRect().top)),
  );
  expect(new Set(tops).size).toBe(1);
});

test("About page follows the Figma measurements at 1440px", async ({
  page,
}) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  await openAboutPage(page);

  // Hero `5673:24144`: Jakarta 54 / 60 semibold.
  const h1 = page.getByRole("heading", { level: 1 });
  await expect(h1).toHaveCSS("font-size", "54px");
  await expect(h1).toHaveCSS("line-height", "60px");
  await expect(h1).toHaveCSS("font-weight", "600");

  // Section titles: Jakarta 46 medium.
  for (const heading of [
    ABOUT_HEADINGS.industries,
    ABOUT_HEADINGS.mission,
    ABOUT_HEADINGS.company,
  ]) {
    const title = page.getByRole("heading", { name: heading });
    await expect(title).toHaveCSS("font-size", "46px");
    await expect(title).toHaveCSS("line-height", "52px");
  }

  // Industry card `7183:10664`: 476px tall, radius 12.
  const industryCard = section(page, ABOUT_HEADINGS.industries)
    .locator("article")
    .first();
  await expect(industryCard).toHaveCSS("height", "476px");
  await expect(industryCard).toHaveCSS("border-radius", "12px");

  // Principle card `5673:24216`: beige, radius 8, padding 46 / 26 / 26.
  const principleCard = section(page, ABOUT_HEADINGS.principles)
    .locator("article")
    .first();
  await expect(principleCard).toHaveCSS("border-radius", "8px");
  await expect(principleCard).toHaveCSS(
    "background-color",
    "rgb(236, 234, 230)",
  );
  await expect(principleCard).toHaveCSS("padding", "46px 26px 26px");

  // Reality card `5673:24199`: 493px tall, 1px #e5e5e5 border.
  const realityCard = section(page, ABOUT_HEADINGS.reality)
    .locator("article")
    .first();
  await expect(realityCard).toHaveCSS("height", "493px");
  await expect(realityCard).toHaveCSS("border-top-color", "rgb(229, 229, 229)");

  // Company card `7024:13916`: white, radius 4, padding 26, 24px glyph in a 46px badge.
  const companyCard = section(page, ABOUT_HEADINGS.company)
    .locator("article")
    .first();
  await expect(companyCard).toHaveCSS("border-radius", "4px");
  await expect(companyCard).toHaveCSS("padding", "26px");
  await expect(companyCard.locator("svg")).toHaveCSS("width", "24px");
  await expect(section(page, ABOUT_HEADINGS.mission).locator("svg")).toHaveCSS(
    "width",
    "20px",
  );

  // Hero buttons `6610:33200` / `6610:33208`: 8px radius (shared default is 4px).
  const hero = section(page, ABOUT_HEADINGS.hero);
  await expect(hero.getByRole("link", { name: "Get started" })).toHaveCSS(
    "border-radius",
    "8px",
  );
  await expect(hero.getByRole("link", { name: "See how it works" })).toHaveCSS(
    "border-radius",
    "8px",
  );

  // Sign-up `5661:24423`: #14a946 button, #fafafa social buttons, 348px legal note.
  const cta = section(page, ABOUT_HEADINGS.signUp);
  await expect(
    cta.getByRole("button", { name: "Continue with Google" }),
  ).toHaveCSS("background-color", "rgb(250, 250, 250)");
  await expect(cta.getByText(/^Read our/)).toHaveCSS("max-width", "348px");
  await expect(cta.getByRole("button", { name: "Get started" })).toHaveCSS(
    "height",
    "46px",
  );
  await expect(cta.getByRole("button", { name: "Get started" })).toHaveCSS(
    "background-color",
    "rgb(20, 169, 70)",
  );
});

for (const width of [1280, 1366]) {
  test(`Header nav, logo and actions do not overlap at ${width}px`, async ({
    page,
  }) => {
    await page.setViewportSize({ width, height: 900 });
    await openAboutPage(page);
    const header = page.locator("header");
    const resources = await header
      .getByRole("button", { name: "Resources" })
      .boundingBox();
    const logo = await header
      .getByRole("img", { name: "Tummly" })
      .first()
      .boundingBox();
    const logIn = await header
      .getByRole("link", { name: "Log in" })
      .boundingBox();
    expect(resources && logo && logIn).toBeTruthy();
    // Nav ends before the centred logo; the logo ends before the actions.
    expect(resources!.x + resources!.width).toBeLessThan(logo!.x);
    expect(logo!.x + logo!.width).toBeLessThan(logIn!.x);
    expect(
      await page.evaluate(() => document.documentElement.scrollWidth),
    ).toBeLessThanOrEqual(width);
  });
}

test("Header follows the Figma frame `5700:25213` at 1440px", async ({
  page,
}) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  await openAboutPage(page);
  const header = page.locator("header");

  // Nav labels, in Figma order.
  const labels = await header
    .getByRole("navigation", { name: "Main" })
    .locator("> div")
    .first()
    .locator("a, button")
    .allInnerTexts();
  expect(labels.map((label) => label.trim())).toEqual([
    "Product",
    "How it works",
    "Pricing",
    "30-day Pilot",
    "For groups",
    "Resources",
  ]);

  // Buttons are 42px high with a 4px radius; "Log in" is 40px with a 4px radius.
  const tryDemo = header.getByRole("link", { name: "Try demo" });
  const getStarted = header.getByRole("link", { name: "Get started" });
  for (const button of [tryDemo, getStarted]) {
    await expect(button).toHaveCSS("height", "42px");
    await expect(button).toHaveCSS("border-radius", "4px");
  }
  const logIn = header.getByRole("link", { name: "Log in" });
  await expect(logIn).toHaveCSS("height", "40px");
  await expect(logIn).toHaveCSS("border-radius", "4px");
});
