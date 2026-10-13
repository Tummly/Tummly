import { HOME_VALUE_PROPS } from "@/content/marketing/homePage"
import { marketingChromeContentInset } from "@/lib/marketing-layout"
import { cn } from "@/lib/utils"

/**
 * Home value props — Figma `4902:19631`.
 * Four centered columns with vertical dividers on desktop.
 */
export function HomeValueProps() {
  return (
    <section
      className="w-full bg-[#fafafa]"
      aria-labelledby="home-value-props-heading"
    >
      <div
        className={cn(
          "mx-auto flex w-full flex-col items-center gap-14 pt-[30px] pb-[70px]",
          marketingChromeContentInset,
        )}
      >
        <h2
          id="home-value-props-heading"
          className="m-0 max-w-3xl text-center font-sans text-[20px] font-medium leading-6 text-[#141414]"
        >
          {HOME_VALUE_PROPS.heading}
        </h2>

        <ul
          className={cn(
            "m-0 flex w-full list-none flex-col gap-10 p-0",
            "lg:flex-row lg:items-stretch lg:gap-0 lg:divide-x lg:divide-[#d8d8d8]",
          )}
        >
          {HOME_VALUE_PROPS.items.map((item) => (
            <li
              key={item.id}
              className="flex min-w-0 flex-1 flex-col items-center gap-3 px-0 text-center text-[#141414] lg:px-9"
            >
              <h3 className="m-0 w-full font-sans text-[18px] font-medium leading-6">
                {item.title}
              </h3>
              <p className="m-0 w-full font-sans text-sm font-normal leading-5">
                {item.body}
              </p>
            </li>
          ))}
        </ul>
      </div>
    </section>
  )
}
