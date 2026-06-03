import React from "react";
import "../../assets/css/homepage.css";
import card1 from "../../assets/images/card1.png";
import card2 from "../../assets/images/card2.png";
import card3 from "../../assets/images/card3.jpg";

function About() {
  return (
    <section className="tummly-section">

      <div className="container">

        <div className="section-header">

          <h2>Why Tummly?</h2>

          <p>
            A simple way to capture guests, learn what they think and bring
            them back — without adding more admin.
          </p>

        </div>

        <div className="features-grid">

          <div className="feature-card">

            <div className="image-wrapper">
              <img
                src={card1}
                alt="Capture guests from everyday touchpoints"
              />
            </div>

            <h3>
              Capture guests from everyday touchpoints
            </h3>

            <p>
              Turn counter cards, receipts, packaging, delivery inserts and
              digital links into consented guest sign-ups.
            </p>

          </div>

          <div className="feature-card">

            <div className="image-wrapper">
              <img
                src={card2}
                alt="Hear what guests really think"
              />
            </div>

            <h3>
              Hear what guests really think
            </h3>

            <p>
              Collect quick private feedback, issue tags and comments before
              small problems become public complaints.
            </p>

          </div>

          <div className="feature-card">

            <div className="image-wrapper">
              <img
                src={card3}
                alt="Bring guests back with simple offers"
              />
            </div>

            <h3>
              Bring guests back with simple offers
            </h3>

            <p>
              Send thank-you, quiet-day and win-back offers to opted-in guests,
              with expiry and redemption controls built in.
            </p>

          </div>

        </div>

      </div>

    </section>
  );
}

export default About;