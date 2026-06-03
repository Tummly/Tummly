import React from "react";
import "../../assets/css/homepage.css";
import card4 from "../../assets/images/card4.jpg";
import card5 from "../../assets/images/card5.jpg";
import card6 from "../../assets/images/card6.jpg";

function Hospitality() {
  return (
    <section className="hospitality-section">

      <div className="container">

        <div className="section-header">

          <h2>
            Built for restaurants, cafés
            <br />
            and hospitality groups
          </h2>

          <p>
            A simple way to capture guests, learn what they think and bring
            them back — without adding more admin.
          </p>

        </div>

        <div className="slider-container">

          <div className="slider-track">

            <div className="slider-card">

              <div className="image-holder">
                <img
                  src={card4}
                  alt="Takeaways and quick-service restaurants"
                />
              </div>

              <h3>
                Takeaways and quick-service restaurants
              </h3>

              <p>
                Invite guests to join from counters, receipts, packaging and
                delivery inserts, so one-off orders can become direct guest
                relationships.
              </p>

            </div>

            <div className="slider-card">

              <div className="image-holder">
                <img
                  src={card5}
                  alt="Cafés, coffee shops and bakeries"
                />
              </div>

              <h3>
                Cafés, coffee shops and bakeries
              </h3>

              <p>
                Grow your regulars list, collect quick feedback and send simple
                offers for quieter periods, new items or return visits.
              </p>

            </div>

            <div className="slider-card">

              <div className="image-holder">
                <img
                  src={card6}
                  alt="Casual dining and hospitality"
                />
              </div>

              <h3>
                Casual dining and hospitality
              </h3>

              <p>
                Collect private feedback after visits and follow up with guests
                who choose to hear from you again.
              </p>

            </div>

          </div>

        </div>

        <div className="slider-controls">

          <div className="arrow-buttons">

            <button
              className="btn-arrow btn-prev"
              aria-label="Previous"
            >
              <svg
                width="16"
                height="16"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
                strokeLinejoin="round"
              >
                <polyline points="15 18 9 12 15 6"></polyline>
              </svg>
            </button>

            <button
              className="btn-arrow btn-next"
              aria-label="Next"
            >
              <svg
                width="16"
                height="16"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
                strokeLinejoin="round"
              >
                <polyline points="9 18 15 12 9 6"></polyline>
              </svg>
            </button>

          </div>

          <div className="progress-bar-container">
            <div className="progress-bar-fill"></div>
          </div>

        </div>

      </div>

    </section>
  );
}

export default Hospitality;