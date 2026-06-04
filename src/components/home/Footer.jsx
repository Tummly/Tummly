import React from "react";
import "../../assets/css/footer.css";

function Footer() {
  return (
    <footer className="tummly-footer-section">
      <div className="footer-container">

        <div className="footer-top-row">

          <div className="footer-Logo">
            <span className="Logo-text">
              tummly<span>®</span>
            </span>
          </div>

          <div className="footer-actions">
            <a href="#" className="btn-footer-login">
              Login
            </a>

            <a href="#" className="btn-footer-request">
              Request trial
            </a>
          </div>

        </div>

        <div className="footer-middle-row">

          <div className="footer-links">
            <span className="copyright">
              © 2026 Tummly.com Limited. All rights reserved.
            </span>

            <a href="#">Help Centre</a>
            <a href="#">Contact</a>
            <a href="#">Terms</a>
            <a href="#">Privacy</a>
            <a href="#">Cookie settings</a>
          </div>

          <div className="footer-security">

            <svg
              width="12"
              height="12"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="2.5"
              strokeLinecap="round"
              strokeLinejoin="round"
            >
              <rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect>
              <path d="M7 11V7a5 5 0 0 1 10 0v4"></path>
            </svg>

            <span>Secure restaurant access</span>

          </div>

        </div>

        <div className="footer-bottom-row">
          <p>
            Tummly is operated by <a href="#">TUMMLY.COM LIMITED</a>,
            company number 16236040. Registered office: 71–75 Shelton
            Street, Covent Garden, London, WC2H 9JQ. Registered in
            England and Wales.
          </p>
        </div>

      </div>
    </footer>
  );
}

export default Footer;