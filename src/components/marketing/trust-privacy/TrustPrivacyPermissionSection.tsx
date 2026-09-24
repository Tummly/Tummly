import { TRUST_PRIVACY_PERMISSION } from "@/content/marketing/trustPrivacyPage"
import { marketingChromeContentInset } from "@/lib/marketing-layout"
import { cn } from "@/lib/utils"

const sectionHeading =
  "font-jakarta text-[34px] font-medium leading-normal text-[#141414] lg:text-[46px]"

const sectionBody =
  "text-base font-normal leading-[22px] text-[#141414] lg:text-[18px] lg:leading-6"

/** Figma permission section (`4974:26885`). */
export function TrustPrivacyPermissionSection() {
  return (
    <section className="w-full bg-[#f0f0f0]">
      <div
        className={cn(
          "mx-auto flex w-full flex-col gap-15",
          marketingChromeContentInset,
          "py-17.5",
        )}
      >
        <div className="flex max-w-137 flex-col gap-4.5">
          <h2 className={cn("m-0", sectionHeading)}>
            {TRUST_PRIVACY_PERMISSION.title}
          </h2>
          <p className={cn("m-0", sectionBody)}>
            {TRUST_PRIVACY_PERMISSION.intro}
          </p>
        </div>

        <div className="flex w-full flex-col gap-10">
          <p className={cn("m-0 max-w-168.5", sectionBody)}>
            {TRUST_PRIVACY_PERMISSION.recordsLead}
          </p>

          <div className="grid w-full grid-cols-1 gap-10 md:grid-cols-3">
            {TRUST_PRIVACY_PERMISSION.records.map((record) => (
              <div key={record.id} className="flex flex-col gap-2">
                <h3 className="m-0 text-lg font-medium leading-6 text-[#141414]">
                  {record.title}
                </h3>
                <p className="m-0 text-base font-normal leading-normal text-[#141414] lg:text-lg lg:leading-6">
                  {record.body}
                </p>
              </div>
            ))}
          </div>

          <div className="flex max-w-168.5 flex-col gap-3">
            <p className={cn("m-0", sectionBody)}>
              {TRUST_PRIVACY_PERMISSION.closing}
            </p>
            <p className="m-0 text-base font-normal leading-6 text-[#141414]">
              {TRUST_PRIVACY_PERMISSION.note}
            </p>
          </div>
        </div>
      </div>
    </section>
  )
}
