import { Link } from "react-router-dom"

import { Button } from "@/components/ui/button"
import { HELP_CENTRE_CONTACT_URL } from "@/config/support"
import {
  helpCentreHubSectionInner,
  helpCentreHubSectionShell,
  helpCentreSectionPadding,
} from "@/components/help-centre/helpCentreLayout"

export function HelpCentreContactCta() {
  return (
    <section className="w-full bg-[#f6f6f6]">
      <div className={`${helpCentreHubSectionShell} ${helpCentreSectionPadding}`}>
        <div className={`${helpCentreHubSectionInner} flex flex-col gap-[52px]`}>
          <div className="flex flex-col gap-2.5">
            <h2 className="m-0 text-[28px] font-bold leading-normal text-[#141414] lg:text-[32px]">
              Still can&apos;t find what you want?
            </h2>
            <p className="m-0 max-w-[440px] text-base leading-[22px] text-[#141414]">
              If you can&apos;t find the answer you need, send us a request and
              we&apos;ll help you find the right next step.
            </p>
          </div>
          <Button
            asChild
            className="h-auto w-fit rounded-[84px] bg-[#14a74a] px-[17px] py-[9px] text-base font-medium leading-5 text-white hover:bg-[#129641]"
          >
            <Link to={HELP_CENTRE_CONTACT_URL}>Contact us</Link>
          </Button>
        </div>
      </div>
    </section>
  )
}
