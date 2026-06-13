import { useState, useEffect, useRef } from "react";
import "../../assets/css/homepage.css";
import card4 from "../../assets/images/card4.jpg";
import card5 from "../../assets/images/card5.jpg";
import card6 from "../../assets/images/card6.jpg";
import card7 from "../../assets/images/card7.jpg";
import { Button } from "@/components/ui/button";

interface HospitalityCard {
  img: string;
  title: string;
  desc: string;
}

function Hospitality() {
  const originalCards: HospitalityCard[] = [
    {
      img: card4,
      title: "Takeaways and quick-service restaurants",
      desc: "Invite guests to join from counters, receipts, packaging and delivery inserts, so one-off orders can become direct guest relationships.",
    },
    {
      img: card5,
      title: "Cafés, coffee shops and bakeries",
      desc: "Grow your regulars list, collect quick feedback and send simple offers for quieter periods, new items or return visits.",
    },
    {
      img: card6,
      title: "Casual dining and hospitality",
      desc: "Collect private feedback after visits and follow up with guests who choose to hear from you again.",
    },
    {
      img: card7,
      title: "Fine dining experiences",
      desc: "Build stronger guest relationships with personalised follow-ups, premium experiences and loyalty-focused communication.",
    },
  ];

  const cardsData = [...originalCards, ...originalCards, ...originalCards];

  const [currentIndex, setCurrentIndex] = useState(originalCards.length);
  const [isTransitioning, setIsTransitioning] = useState(true);
  const trackRef = useRef<HTMLDivElement>(null);

  const totalOriginal = originalCards.length;

  const nextSlide = () => {
    if (!isTransitioning) return;
    setCurrentIndex((prev) => prev + 1);
  };

  const prevSlide = () => {
    if (!isTransitioning) return;
    setCurrentIndex((prev) => prev - 1);
  };

  useEffect(() => {
    const handleTransitionEnd = () => {
      if (currentIndex >= totalOriginal * 2) {
        setIsTransitioning(false);
        setCurrentIndex(currentIndex - totalOriginal);
      } else if (currentIndex < totalOriginal) {
        setIsTransitioning(false);
        setCurrentIndex(currentIndex + totalOriginal);
      }
    };

    const track = trackRef.current;
    if (track) {
      track.addEventListener("transitionend", handleTransitionEnd);
    }

    return () => {
      if (track) {
        track.removeEventListener("transitionend", handleTransitionEnd);
      }
    };
  }, [currentIndex, totalOriginal]);

  useEffect(() => {
    if (!isTransitioning) {
      const timeout = setTimeout(() => {
        setIsTransitioning(true);
      }, 35);
      return () => clearTimeout(timeout);
    }
  }, [isTransitioning]);

  const logicalIndex = currentIndex % totalOriginal;
  const progressPercentage = ((logicalIndex + 1) / totalOriginal) * 100;

  return (
    <section className="hospitality-section">
      <div className="container">
        <div className="section-header">
          <h2 style={{ color: "#fff" }}>
            Built for restaurants, cafés
            <br />
            and hospitality groups
          </h2>

          <p style={{ color: "rgba(255,255,255,0.75)" }}>
            A simple way to capture guests, learn what they think and bring
            them back — without adding more admin.
          </p>
        </div>

        <div className="slider-outer-wrapper">
          <div className="slider-container">
            <div
              className="slider-track"
              ref={trackRef}
              style={{
                transition: isTransitioning
                  ? "transform 0.6s cubic-bezier(0.25, 1, 0.5, 1)"
                  : "none",
                transform: `translateX(calc(-1 * ${currentIndex} * (100% / var(--items-per-view) + var(--gap-space) / var(--items-per-view) - var(--gap-space))))`,
              }}
            >
              {cardsData.map((card, index) => (
                <div className="slider-card" key={index}>
                  <div className="image-holder">
                    <img src={card.img} alt={card.title} />
                  </div>
                  <h3>{card.title}</h3>
                  <p>{card.desc}</p>
                </div>
              ))}
            </div>
          </div>
        </div>

        <div className="slider-controls">
          <div className="arrow-buttons">
            <Button
              variant="outline-inverse"
              size="icon"
              aria-label="Previous slide"
              onClick={prevSlide}
            >
              <svg
                width="18"
                height="18"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2.5"
              >
                <polyline points="15 18 9 12 15 6"></polyline>
              </svg>
            </Button>

            <Button
              variant="default"
              size="icon"
              aria-label="Next slide"
              onClick={nextSlide}
            >
              <svg
                width="18"
                height="18"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2.5"
              >
                <polyline points="9 18 15 12 9 6"></polyline>
              </svg>
            </Button>
          </div>

          <div className="progress-bar-container">
            <div
              className="progress-bar-fill"
              style={{
                width: `${progressPercentage}%`,
                transition: isTransitioning ? "width 0.3s ease" : "none",
              }}
            ></div>
          </div>
        </div>
      </div>
    </section>
  );
}

export default Hospitality;
