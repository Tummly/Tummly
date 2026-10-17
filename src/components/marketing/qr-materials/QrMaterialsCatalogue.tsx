import { MarketingButton } from "@/components/marketing/MarketingButton"
import { ProductCaptureMaterialsPanel } from "@/components/marketing/product/ProductCaptureMaterialsPanel"
import { RequestTrialLink } from "@/components/navigation/RequestTrialLink"
import {
  QR_MATERIALS_CATALOGUE,
  QR_MATERIALS_SECTION_BODY,
  QR_MATERIALS_SECTION_HEADING,
  QR_MATERIALS_SECTION_INSET,
} from "@/content/marketing/qrMaterialsPage"
import { cn } from "@/lib/utils"

/**
 * Figma QR materials catalogue `5661:19364`.
 * Centered intro, reused Capture materials panel (`4902:19649` / `6106:17567`),
 * then physical / digital deployment band.
 */
export function QrMaterialsCatalogue() {
  const { deployment } = QR_MATERIALS_CATALOGUE

  return (
    <section
      className="w-full bg-white"
      aria-labelledby="qr-materials-catalogue-heading"
    >
      <div
        className={cn(
          "flex w-full flex-col items-center gap-[46px] pt-12.5 pb-12.5 lg:gap-[70px] lg:pt-[70px] lg:pb-[70px]",
          QR_MATERIALS_SECTION_INSET,
        )}
      >
        <header className="flex w-full max-w-[571px] flex-col items-center gap-[18px] text-center">
          <h2
            id="qr-materials-catalogue-heading"
            className={cn("m-0 w-full", QR_MATERIALS_SECTION_HEADING)}
          >
            {QR_MATERIALS_CATALOGUE.titleLine1}
            <br aria-hidden />
            {QR_MATERIALS_CATALOGUE.titleLine2}
          </h2>
          <p className={cn("m-0 w-full max-w-[570px]", QR_MATERIALS_SECTION_BODY)}>
            {QR_MATERIALS_CATALOGUE.body}
          </p>
        </header>

        <div className="w-full">
          <ProductCaptureMaterialsPanel
            intro={{
              eyebrow: QR_MATERIALS_CATALOGUE.panelEyebrow,
              title: QR_MATERIALS_CATALOGUE.panelTitle,
              body: QR_MATERIALS_CATALOGUE.panelBody,
              titleId: "qr-materials-catalogue-panel-heading",
            }}
          />
        </div>

        <div className="flex w-full flex-col gap-[38px] bg-[#eef8f1] px-5 py-9 sm:px-10">
          <div className="flex w-full max-w-[1200px] flex-col gap-3 text-[#111]">
            <h3 className="m-0 font-jakarta text-[24px] font-medium leading-[32px] lg:text-[30px] lg:leading-[38px]">
              {deployment.title}
            </h3>
            <p className="m-0 font-sans text-base font-normal leading-[23px]">
              {deployment.body}
            </p>
          </div>

          <div className="flex w-full flex-col gap-5 bg-white lg:flex-row lg:gap-5">
            <article className="flex min-h-[180px] min-w-0 flex-1 flex-col gap-2.5 bg-white px-6 py-[22px]">
              <h4 className="m-0 font-jakarta text-lg font-bold leading-6 text-[#111]">
                {deployment.physical.title}
              </h4>
              <p className="m-0 font-sans text-sm font-normal leading-5 text-[#111]">
                {deployment.physical.body}
              </p>
              <div className="mt-auto pt-2">
                <MarketingButton asChild variant="primary">
                  <RequestTrialLink planIntent={{ plan: "Pilot" }}>
                    {deployment.physical.ctaLabel}
                  </RequestTrialLink>
                </MarketingButton>
              </div>
            </article>

            <article className="flex min-h-[180px] min-w-0 flex-1 flex-col gap-2.5 bg-white px-6 py-[22px] text-[#111]">
              <h4 className="m-0 font-jakarta text-lg font-bold leading-6">
                {deployment.digital.title}
              </h4>
              <p className="m-0 font-sans text-sm font-normal leading-5">
                {deployment.digital.body}
              </p>
              <p className="m-0 font-sans text-[13px] font-medium leading-[19px]">
                {deployment.digital.note}
              </p>
            </article>
          </div>
        </div>
      </div>
    </section>
  )
}
