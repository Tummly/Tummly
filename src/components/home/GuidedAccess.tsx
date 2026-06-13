import step1Image from "@/assets/images/guided-access-step1.png"
import step2Image from "@/assets/images/guided-access-step2.png"
import step3Image from "@/assets/images/guided-access-step3.png"

type AccessStepCardProps = {
  step: string
  title: string
  description: string
  image: string
  imageAlt: string
  isLast?: boolean
}

function StepIndicator({ isLast = false }: { isLast?: boolean }) {
  return (
    <div className="flex h-2.5 w-full items-center gap-1.25" aria-hidden>
      <span className="size-2.5 shrink-0 bg-black" />
      {!isLast && <span className="h-px flex-1 bg-black" />}
    </div>
  )
}

function AccessStepCard({
  step,
  title,
  description,
  image,
  imageAlt,
  isLast = false,
}: AccessStepCardProps) {
  return (
    <article className="flex flex-col gap-6.5">
      <div className="relative aspect-436/230 w-full overflow-hidden rounded-[6px]">
        <img
          src={image}
          alt={imageAlt}
          className="absolute inset-0 size-full rounded-[6px] object-cover"
          loading="lazy"
          decoding="async"
        />
      </div>

      <StepIndicator isLast={isLast} />

      <div className="flex flex-col gap-3">
        <p className="m-0 text-sm font-bold uppercase leading-[normal] text-black">
          {step}
        </p>
        <h3 className="m-0 font-serif text-2xl leading-[normal] text-black">
          {title}
        </h3>
        <p className="m-0 text-sm font-normal leading-4.75 text-black">
          {description}
        </p>
      </div>
    </article>
  )
}

const accessSteps = [
  {
    step: "Step 1",
    title: "Request guided access",
    description:
      "Tell us about your restaurant, location count, role and main goal so we can route your setup correctly.",
    image: step1Image,
    imageAlt: "Request guided access",
  },
  {
    step: "Step 2",
    title: "Verify your email",
    description:
      "We send a short code to confirm your email before reviewing the request.",
    image: step2Image,
    imageAlt: "Verify your email",
  },
  {
    step: "Step 3",
    title: "Create your workspace",
    description:
      "Once approved, you receive a secure setup link to create your account, add your restaurant details and start the right setup path.",
    image: step3Image,
    imageAlt: "Create your workspace",
  },
] as const

function GuidedAccess() {
  return (
    <section className="w-full bg-[#f8f8f8]">
      <div className="mx-auto flex w-full max-w-360 flex-col gap-12 px-4 py-12 sm:gap-14 sm:px-6 sm:py-16 md:px-10 lg:gap-15 lg:px-16 lg:py-22.5 xl:px-45">
        <header className="flex max-w-2xl flex-col gap-3">
          <h2 className="m-0 text-[clamp(1.75rem,4vw,2.625rem)] font-bold leading-[normal] text-[#232323]">
            How guided access works
          </h2>
          <p className="m-0 text-base font-medium leading-6.5 text-[#232323] sm:text-[17px] lg:text-lg">
            Request access, verify your email and receive the right setup link
            once your restaurant details are reviewed.
          </p>
        </header>

        <div className="grid grid-cols-1 gap-7.5 md:grid-cols-2 lg:grid-cols-3">
          {accessSteps.map((item, index) => (
            <AccessStepCard
              key={item.step}
              step={item.step}
              title={item.title}
              description={item.description}
              image={item.image}
              imageAlt={item.imageAlt}
              isLast={index === accessSteps.length - 1}
            />
          ))}
        </div>

        <p className="m-0 max-w-2xl text-sm leading-5 text-[#232323]">
          After your workspace is opened, we help you prepare your first Guest
          Loop with starter QR materials, guest form setup, offer guidance and
          launch support.
        </p>
      </div>
    </section>
  )
}

export default GuidedAccess
