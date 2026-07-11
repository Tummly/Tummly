import { ArrowLeftIcon } from "lucide-react";
import { Link } from "react-router-dom";

import Footer from "@/components/home/Footer";

export default function NotFoundPage() {
  return (
    <>
      <main className="flex w-full flex-1 flex-col bg-white text-[#141414]">
        <div className="mx-auto flex w-full max-w-7xl flex-1 flex-col gap-10 px-4 py-10 sm:px-6 sm:py-12 md:px-10 lg:px-16 lg:py-16 xl:px-20 2xl:max-w-[108rem] 2xl:px-45">
          <Link
            to="/"
            className="inline-flex w-fit items-center gap-2 text-base font-semibold text-primary transition-opacity hover:opacity-80 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary/30"
          >
            <ArrowLeftIcon className="size-5 text-primary" aria-hidden />
            Go Home
          </Link>

          <div className="flex max-w-xl flex-col gap-3">
            <p className="m-0 text-sm font-medium uppercase tracking-[0.08em] text-[#7d7d7d]">
              404
            </p>
            <h1 className="m-0 text-[clamp(2rem,5vw,2.875rem)] font-bold leading-normal text-[#141414]">
              Page not found
            </h1>
            <p className="m-0 text-base leading-6 text-[#525252] sm:text-lg sm:leading-7">
              That link does not match a page on Tummly. Head home to continue,
              or use the site navigation.
            </p>
          </div>
        </div>
      </main>
      <Footer />
    </>
  );
}
