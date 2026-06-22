import { useEffect } from "react";
import { useLocation } from "react-router-dom";

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
  const { hash } = useLocation();

  useEffect(() => {
    if (hash === "#request-trial") {
      document
        .getElementById("request-trial")
        ?.scrollIntoView({ behavior: "smooth" });
    }
  }, [hash]);

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
