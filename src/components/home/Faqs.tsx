import { useState } from "react";
import "../../assets/css/homepage.css";

interface FaqItem {
  question: string;
  answer: string;
}

function Faqs() {
  const [activeIndex, setActiveIndex] = useState<number | null>(null);

  const toggleFaq = (index: number) => {
    setActiveIndex(activeIndex === index ? null : index);
  };

  const faqData: FaqItem[] = [
    {
      question: "What does Tummly help my restaurant do?",
      answer:
        "Tummly helps you turn everyday guest moments into a repeat-visit loop: private feedback, consented guest sign-ups, simple offers, campaign messages and weekly insight in one restaurant workspace.",
    },
    {
      question: "How do guests join or leave feedback?",
      answer:
        "Guests can respond from approved touchpoints such as in-store prompts, printed materials, receipts, packaging, delivery inserts or digital Smart Guest Links. They complete a short mobile form and choose whether to share contact details or opt in for future messages.",
    },
    {
      question: "Do guests need to download an app?",
      answer:
        "No. Guests open a short mobile experience from a scan or link. They can leave feedback quickly and choose whether to join your restaurant's guest list.",
    },
    {
      question: "Do I need to change my POS, website or delivery setup?",
      answer:
        "No. Tummly is designed to work alongside your existing channels. You can start with guest touchpoints and campaigns without replacing your POS, website, ordering system or delivery platforms.",
    },
    {
      question: "What happens after I request access?",
      answer:
        "You submit your restaurant details and verify your email. We review the request, and if approved, we send a secure setup link so you can create your restaurant workspace.",
    },
    {
      question: "What is included in the guided trial?",
      answer:
        "Approved trials include access to the core workspace, guest forms, private feedback, guest list, offers, campaigns, weekly brief, a standard message allowance and starter QR materials matched to your setup.",
    },
    {
      question: "Who controls the guest list?",
      answer:
        "Your restaurant controls its identifiable guest list. Tummly provides the secure workspace to manage feedback, consent, offers and messages. Guests can opt out where required.",
    },
    {
      question: "Can Tummly help with public reviews?",
      answer:
        "Tummly is built for private feedback first. You should not use Tummly to reward, gate or manipulate public reviews. If public review features are added later, they must remain separate from incentives and follow platform rules.",
    },
    {
      question: "Will I be charged when I request access?",
      answer:
        "No payment is taken when you request access. Any paid plan, extra message usage, premium print packs or add-ons are confirmed before use.",
    },
  ];

  return (
    <section className="faq-section">
      <div className="faq-container">
        <div className="faq-title-col">
          <h2>FAQs</h2>
        </div>

        <div className="faq-accordion-col">
          {faqData.map((item, index) => {
            const isOpen = activeIndex === index;
            return (
              <div
                className={`faq-item ${isOpen ? "active" : ""}`}
                key={index}
              >
                <button
                  className="faq-toggle"
                  aria-expanded={isOpen}
                  onClick={() => toggleFaq(index)}
                  type="button"
                >
                  <span>{item.question}</span>
                  <span className="faq-icon-wrapper">
                    <span className="icon-line horizontal"></span>
                    <span className="icon-line vertical"></span>
                  </span>
                </button>

                <div className="faq-content">
                  <div className="faq-content-inner">
                    <p>{item.answer}</p>
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      </div>
    </section>
  );
}

export default Faqs;
