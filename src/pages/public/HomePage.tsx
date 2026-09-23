import { useEffect } from "react";
import { useLocation } from "react-router-dom";

import { MARKETING_FAQS_HASH } from "@/constants/marketingNav";
import { warmCtaLaunchBg } from "@/lib/prefetchCtaLaunchBg";
import Hero from "../../components/home/Hero";
import About from "../../components/home/About";
import Hospitality from "../../components/home/Hospitality";
import Services from "../../components/home/Services";
import Setup from "../../components/home/Setup";
import GuidedTrial from "../../components/home/GuidedTrial";
import GuidedAccess from "../../components/home/GuidedAccess";
import FAQs from "../../components/home/Faqs";
import CTALaunch from "../../components/home/CTALaunch";
import Footer from "../../components/home/Footer";

function HomePage() {
  const { hash, pathname } = useLocation();

  useEffect(() => {
    if (pathname !== "/") {
      return;
    }

    // Idle prefetch so the bottom CTA is warm without competing with hero LCP.
    const idleId =
      "requestIdleCallback" in window
        ? window.requestIdleCallback(() => warmCtaLaunchBg())
        : window.setTimeout(() => warmCtaLaunchBg(), 1);

    return () => {
      if ("cancelIdleCallback" in window && typeof idleId === "number") {
        window.cancelIdleCallback(idleId);
      } else {
        window.clearTimeout(idleId);
      }
    };
  }, [pathname]);

  useEffect(() => {
    if (pathname !== "/") {
      return;
    }

    if (hash === MARKETING_FAQS_HASH) {
      const frame = requestAnimationFrame(() => {
        requestAnimationFrame(() => {
          document
            .getElementById("faqs")
            ?.scrollIntoView({ behavior: "smooth" });
        });
      });

      return () => cancelAnimationFrame(frame);
    }
  }, [hash, pathname]);

  return (
    <>
      <Hero />
      <About />
      <Hospitality />
      <Services />
      <Setup />
      <GuidedTrial />
      <GuidedAccess />
      <FAQs />
      <CTALaunch />
      <Footer />
    </>
  );
}

export default HomePage;
