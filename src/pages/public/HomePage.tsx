import { useEffect } from "react";
import { useLocation } from "react-router-dom";

import {
  REQUEST_TRIAL_HASH,
  scrollToRequestTrial,
} from "@/lib/scrollToRequestTrial";
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
    if (pathname !== "/" || hash !== REQUEST_TRIAL_HASH) {
      return;
    }

    const frame = requestAnimationFrame(() => {
      requestAnimationFrame(() => {
        scrollToRequestTrial();
      });
    });

    return () => cancelAnimationFrame(frame);
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
