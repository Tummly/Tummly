import React, { useState, useEffect, useRef } from "react";
import "../../assets/css/homepage.css";
import trial1 from "../../assets/images/trial1.jpg";
import trial2 from "../../assets/images/trial2.jpg";
import trial3 from "../../assets/images/trial3.jpg";
import trial4 from "../../assets/images/trial4.jpg";
import trial5 from "../../assets/images/trial5.jpg";
import trial6 from "../../assets/images/trial6.jpg";
import trial7 from "../../assets/images/trial7.jpg";

function GuidedTrial() {
  const originalCards = [
    {
      img: trial1,
      title: "Standard workspace access",
      desc: "Use the Tummly workspace during your trial, including guest forms, private feedback, guest list, offers, campaigns and your weekly brief.",
      wrapperClass: "trial-image-wrapper"
    },
    {
      img: trial2,
      title: "Starter QR Pack",
      desc: "Approved trials include starter printed QR materials matched to your setup, so guests can scan and respond from key in-store or takeaway touchpoints.",
      wrapperClass: "trial-image-wrapper bg-light"
    },
    {
      img: trial3,
      title: "Smart Guest Links",
      desc: "Use trackable links for digital channels, receipts, messages or places where a printed QR prompt is not the best fit.",
      wrapperClass: "trial-image-wrapper"
    },
    {
      img: trial4,
      title: "Feedback and opt-in form",
      desc: "Let guests share quick private feedback and choose whether to join your restaurant’s guest list.",
      wrapperClass: "trial-image-wrapper"
    },
    {
      img: trial5,
      title: "Automated Guest Insights",
      desc: "Understand spending trends and customer sentiment with automatically compiled visual data updates directly inside your dashboard.",
      wrapperClass: "trial-image-wrapper"
    },
    {
      img: trial6,
      title: "Dedicated Launch Support",
      desc: "Get premium setup assistance from our team to customize alignment options before your digital platform goes completely live.",
      wrapperClass: "trial-image-wrapper"
    },
    {
      img: trial7,
      title: "Custom Marketing Assets",
      desc: "Access tailormade design templates perfectly calibrated for print or social media distribution to boost audience conversion.",
      wrapperClass: "trial-image-wrapper"
    }
  ];

  const cardsData = [...originalCards, ...originalCards, ...originalCards];
  const [currentIndex, setCurrentIndex] = useState(originalCards.length);
  const [isTransitioning, setIsTransitioning] = useState(true);
  const trackRef = useRef(null);

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
      setTimeout(() => {
        setIsTransitioning(true);
      }, 20);
    }
  }, [isTransitioning]);

  const logicalIndex = currentIndex % totalOriginal;
  const progressPercentage = ((logicalIndex + 1) / totalOriginal) * 100;

  return (
    <section className="trial-included-section">
      <div className="trial-container">
        
        <div className="trial-header">
          <h2>What’s included in your guided trial</h2>
          <p>
            Your guided trial includes access to the core workspace, starter
            materials and a standard message allowance to help you launch your
            first Guest Loop. We confirm your setup before your workspace is
            opened.
          </p>
        </div>

        <div className="slider-container">
          <div 
            className="trial-track" 
            ref={trackRef}
            style={{
              transition: isTransitioning ? "transform 0.6s cubic-bezier(0.25, 1, 0.5, 1)" : "none",
              "--current-index": currentIndex // Passing index to CSS smoothly
            }}
          >
            {cardsData.map((card, index) => (
              <div className="trial-card" key={index}>
                <div className={card.wrapperClass}>
                  <img src={card.img} alt={card.title} />
                </div>
                <div className="trial-card-content">
                  <h3>{card.title}</h3>
                  <p>{card.desc}</p>
                </div>
              </div>
            ))}
          </div>
        </div>

        <div className="trial-controls">
          <div className="trial-arrows">
            <button
              className="trial-btn trial-btn-prev"
              aria-label="Previous page"
              onClick={prevSlide}
            >
              <svg
                width="16"
                height="16"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2.5"
                strokeLinecap="round"
                strokeLinejoin="round"
              >
                <polyline points="15 18 9 12 15 6"></polyline>
              </svg>
            </button>

            <button
              className="trial-btn trial-btn-next"
              aria-label="Next page"
              onClick={nextSlide}
            >
              <svg
                width="16"
                height="16"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2.5"
                strokeLinecap="round"
                strokeLinejoin="round"
              >
                <polyline points="9 18 15 12 9 6"></polyline>
              </svg>
            </button>
          </div>

          <div className="trial-progress-container">
            <div 
              className="trial-progress-line"
              style={{ 
                width: `${progressPercentage}%`, 
                transition: isTransitioning ? "width 0.3s ease" : "none"
              }}
            ></div>
          </div>
        </div>

      </div>
    </section>
  );
}

export default GuidedTrial;