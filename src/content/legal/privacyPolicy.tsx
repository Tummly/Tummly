import { LegalDocLink } from "./LegalDocLink"
import { LEGAL_ROUTES } from "@/constants/legalRoutes"
import type { LegalPageContent } from "./types"

export const privacyPolicyContent: LegalPageContent = {
  title: "Privacy Policy",
  description: "Also referred to as our Privacy Policy. This notice explains how Tummly handles personal data across the website, trial request flow, operator accounts, Smart Guest Links, guest pages, dashboard shop, support and related services. Effective date: 9 July 2026",
  documentKey: "privacy",
  sections: [
  {
    id: "about-this-privacy-notice",
    title: "1. About this Privacy Notice",
    content: (
      <>
      <p><LegalDocLink to={LEGAL_ROUTES.privacy}>This Privacy Notice</LegalDocLink> explains how Tummly.com Limited collects, uses, stores, shares and otherwise processes personal data in connection with Tummly.</p>
      <p>It should be read together with <LegalDocLink to={LEGAL_ROUTES.terms}>our Terms and Conditions</LegalDocLink>, <LegalDocLink to={LEGAL_ROUTES.cookiePolicy}>Cookie Policy</LegalDocLink>, and any privacy information shown on a Tummly guest page, trial request form, account screen, checkout page, support page or dashboard shop checkout.</p>
      <p>This notice is intended for website visitors, trial applicants, restaurant operators, authorised users, guests who use Tummly guest-facing pages, shop purchasers, support contacts and other people whose personal data is handled through Tummly.</p>
      <p>Where a restaurant or hospitality operator uses Tummly, that operator may also have its own privacy notice. If you are a guest, you should also read the privacy information provided by the relevant restaurant, operator or brand.</p>
      <p>Tummly may act as controller for some personal data and processor for other personal data. Section 4 explains this distinction.</p>
      </>
    ),
  },
  {
    id: "who-we-are",
    title: "2. Who we are",
    content: (
      <>
      <p>Tummly is operated by Tummly.com Limited, a company registered in England and Wales under company number 16236040. Our registered office is 71-75 Shelton Street, Covent Garden, London, WC2H 9JQ, United Kingdom.</p>
      <p>In this notice, “Tummly”, “we”, “us” and “our” mean Tummly.com Limited.</p>
      <p>You can contact us about privacy matters at compliance@tummly.com. If your question is about a specific restaurant’s use of your guest data, we may ask you to contact that restaurant directly or we may help route the request where appropriate.</p>
      </>
    ),
  },
  {
    id: "who-this-notice-applies-to",
    title: "3. Who this notice applies to",
    content: (
      <>
      <p>This notice applies to:</p>
      <ul>
        <li>operators and applicants who request access, complete operator setup, use the dashboard, buy QR materials or credits, or use Tummly for their business</li>
        <li>authorised users who sign in to an operator account</li>
        <li>guests who scan a Smart Guest Link, open a guest-facing page, submit private feedback, provide contact details, claim or redeem an offer, or interact with a restaurant through Tummly</li>
        <li>people whose contact details are imported, connected or otherwise provided by an operator</li>
        <li>website visitors, help centre users, support contacts and people who communicate with Tummly.</li>
      </ul>
      <p>Tummly is designed for restaurant and hospitality operators. The dashboard shop and operator account features are intended for business use.</p>
      </>
    ),
  },
  {
    id: "our-data-protection-roles",
    title: "4. Our data protection roles",
    content: (
      <>
      <p>Tummly acts as controller for personal data where we decide how and why it is processed. This includes personal data relating to website visitors, trial applicants, operator accounts, account security, billing contacts, dashboard shop purchasers, support contacts, Tummly marketing, Tummly analytics, service administration, fraud and abuse prevention, product improvement and legal compliance.</p>
      <p>For restaurant guest records processed through Tummly on behalf of an operator, the operator normally acts as controller and Tummly acts as processor, unless a separate written agreement states otherwise. This means the operator decides the purpose of collecting and using the guest information, and Tummly provides the technology and related processing services.</p>
      <p>For Imported Contact Data uploaded, connected or provided by an operator, the operator is normally controller and Tummly is normally processor. The operator is responsible for ensuring the data was lawfully collected and can lawfully be used in Tummly.</p>
      <p>Tummly may also act as controller for certain Service Data and Aggregated Insights used for service administration, security, product analytics, benchmarking, support, compliance and service development. Where that data is personal data, we handle it in accordance with this notice and applicable law.</p>
      <p>Unless a separate written agreement says otherwise, Tummly and an operator are not joint controllers. Each party is responsible for the personal data it controls.</p>
      </>
    ),
  },
  {
    id: "quick-summary",
    title: "5. Quick summary",
    content: (
      <>
      <p>Guests use Tummly guest pages to share private feedback and contact details with the named restaurant or operator.</p>
      <p>The named restaurant or operator normally controls guest feedback records, imported contact data, guest lists, offers, campaigns and restaurant follow-up activity.</p>
      <p>Tummly provides the technology, hosting, support, security, analytics, fulfilment and related service processing.</p>
      <p>Submitting guest feedback allows the feedback, contact details and related submission information to be shared with the named restaurant so they can review, manage and, where appropriate, respond to the submission.</p>
      <p>Guest feedback is not automatically published as a public review.</p>
      <p>Operators are responsible for ensuring they have a lawful basis, valid consent, valid soft opt-in or another permitted basis before sending marketing to guests or imported contacts.</p>
      <p>Tummly does not sell identifiable guest contact data and does not share identifiable guest lists across operators.</p>
      <p>Tummly may use Service Data and properly aggregated or anonymised information to operate, secure, improve and develop the service, including benchmark-style insights and recommendations.</p>
      </>
    ),
  },
  {
    id: "personal-data-we-collect",
    title: "6. Personal data we collect",
    content: (
      <>
      <p>We may collect and process the following types of personal data, depending on how you use Tummly:</p>
      <p>Depending on how you use Tummly, we may collect identity and contact data; business and operator data; account and security data; guest feedback and contact data; Imported Contact Data; offer, campaign and redemption data; dashboard shop, billing and fulfilment data; support and operational data; technical, analytics and cookie data; AI, Service Data and Aggregated Insights; marketing, research and survey data; and unexpected sensitive information that you choose to include in a comment or support message.</p>
      <p>The full category table with examples is in the downloadable <LegalDocLink to={LEGAL_ROUTES.privacy}>Privacy Policy</LegalDocLink>.</p>
      </>
    ),
  },
  {
    id: "purposes-lawful-bases-and-controller-roles",
    title: "7. Purposes, lawful bases and controller roles",
    content: (
      <>
      <p>The table below summarises the main purposes for which personal data is processed, the usual controller role and the lawful basis Tummly normally relies on where Tummly acts as controller. Where Tummly acts as processor, the operator is responsible for identifying its own lawful basis and providing any required privacy information.</p>
      <p>Where Tummly acts as controller, the main purposes include trial request and access review; operator setup and account administration; security, authentication and abuse prevention; guest feedback and restaurant follow-up; guest lists and Imported Contact Data; operator marketing to guests; offers, campaigns and redemption; dashboard shop, fulfilment and billing; Tummly marketing, research and surveys; cookies and analytics; AI-assisted features and insights; and legal, compliance and rights handling.</p>
      <p>Each purpose has a usual controller role and lawful basis. Where Tummly acts as processor, the operator is responsible for its own lawful basis. The full purposes table is in the downloadable <LegalDocLink to={LEGAL_ROUTES.privacy}>Privacy Policy</LegalDocLink>.</p>
      </>
    ),
  },
  {
    id: "trial-requests-approval-and-operator-setup",
    title: "8. Trial requests, approval and operator setup",
    content: (
      <>
      <p>When an applicant requests trial access, we process information needed to receive the application, verify the email address, review suitability, prevent misuse, communicate the outcome and create an operator setup path where approved.</p>
      <p>This may include business name, business category, location count, main location, town or city, postcode, business link, applicant name, work email, mobile number, role, goal, OTP verification status, application status, approval notes and communication history.</p>
      <p>If approved, we may use the information to generate setup invitations, activation records and related operator account details. If declined or paused, we may keep records needed for audit, support, fraud prevention, relationship management and legal or compliance purposes.</p>
      </>
    ),
  },
  {
    id: "operator-accounts-sign-in-and-security",
    title: "9. Operator accounts, sign-in and security",
    content: (
      <>
      <p>When an operator or authorised user creates or uses an account, we process account, authentication, location, role, contact and security information.</p>
      <p>This may include full name, email address, phone number, password hash, sign-in activity, one-time passcode events, trusted device information, Activation Code metadata, activation period status, failed sign-in attempts, IP address, device/browser information, security logs and support actions.</p>
      <p>We process this information to provide the account, authenticate users, prevent unauthorised access, apply activation gates, route users to the correct workspace, investigate suspected misuse and protect operators, guests, Tummly and the service.</p>
      </>
    ),
  },
  {
    id: "smart-guest-links-guest-feedback-and-restaurant-follow-up",
    title: "10. Smart Guest Links, guest feedback and restaurant follow-up",
    content: (
      <>
      <p>When a guest scans a Smart Guest Link or opens a guest page, we process information needed to load the page, identify the relevant restaurant or location, collect the guest submission, prevent abuse and deliver the submission to the named operator.</p>
      <p>Guest feedback collected through Tummly is private feedback for the restaurant team. It is not automatically published as a public review.</p>
      <p>If you submit a guest form, your feedback, contact details and related submission information will be shared with the named restaurant or operator so they can review, manage and, where appropriate, respond to your submission.</p>
      <p>The named operator is normally controller for the guest feedback and contact details it receives through Tummly. Tummly normally acts as processor for that operator, while also acting as controller for limited technical, security, service administration and service-improvement processing described in this notice.</p>
      <p>Guests should contact the relevant restaurant directly about food, service, refunds, allergens, safety, delivery or in-store complaints. Tummly provides the technology used by the operator and is not the restaurant, seller, delivery provider or food service provider.</p>
      </>
    ),
  },
  {
    id: "guest-lists-imported-contact-data-soft-opt-in-and-operator-marketing",
    title: "11. Guest lists, imported contact data, soft opt-in and operator marketing",
    content: (
      <>
      <p>Where guest list, offer, campaign, email, SMS or other supported electronic messaging features are enabled, operators are responsible for deciding whether they have a lawful basis, valid consent, valid soft opt-in or another permission to contact guests or imported contacts.</p>
      <p>Where an operator relies on soft opt-in, the operator must ensure the contact details were obtained in the context of a sale or negotiation for a sale of the operator’s own goods or services; the marketing is limited to the operator’s own similar food, drink, hospitality, offer or restaurant service communications; the guest was given a clear chance to opt out when the details were collected; and every marketing message includes a clear opt-out or unsubscribe route.</p>
      <p>Where a guest page uses a soft opt-in notice, the notice should clearly identify the relevant restaurant, operator or brand before the guest submits the form. If the guest opts out, Tummly may record and process that opt-out or suppression status on behalf of the operator.</p>
      <p>Submitting feedback does not permit unrelated marketing, third-party marketing, sale of contact data, public review incentives or messages outside the permission, soft opt-in or other lawful basis that applies.</p>
      <p>Operators may import or connect contact data only where they lawfully collected it, have authority to use it in Tummly, and can identify the source, permission status, channel eligibility, opt-out status and restrictions that apply.</p>
      <p>Operators are responsible for giving imported contacts any privacy information required by law, unless Tummly has expressly agreed to do so in writing.</p>
      <p>Operators must not import bought, rented, scraped, brokered, unlawfully shared, platform-prohibited or unclear-origin lists. Operators must not import payment card data, passwords, identity documents, health or allergy notes, children’s data, special category data or other unnecessary sensitive information unless Tummly has expressly enabled the field and the operator has completed appropriate legal review.</p>
      <p>Contacts with unknown permission, disputed origin, service-only status or missing consent/soft-opt-in evidence should not receive marketing through Tummly unless the operator can lawfully justify the send.</p>
      <p>Tummly may process Imported Contact Data and permission metadata to provide the service, deduplicate records, manage suppression, support campaigns, maintain audit and support records, prevent abuse, protect deliverability, operate and secure Tummly, and create Aggregated Insights in accordance with this notice and <LegalDocLink to={LEGAL_ROUTES.terms}>the Terms and Conditions</LegalDocLink>.</p>
      <p>Tummly may reject, quarantine, delete, suppress, limit or require proof for Imported Contact Data where we reasonably believe the data is unlawful, excessive, unreliable, unsupported, unsafe or likely to create complaints, deliverability issues or compliance risk.</p>
      </>
    ),
  },
  {
    id: "offers-campaigns-and-redemption",
    title: "12. Offers, campaigns and redemption",
    content: (
      <>
      <p>Where offer, campaign and redemption tools are available, we process personal data needed to create, send, manage, measure and support those features.</p>
      <p>This may include guest eligibility, contact channel, campaign membership, offer claim, redemption status, expiry, location, staff verification, timestamps, suppression status and delivery or message events.</p>
      <p>The operator is responsible for the content, timing, targeting, legality and fulfilment of its offers, campaigns and messages. Tummly provides the workspace and related processing tools.</p>
      <p>We may process campaign and redemption data to provide reporting, prevent abuse, protect deliverability, manage suppression, support recovery actions and improve the service.</p>
      </>
    ),
  },
  {
    id: "dashboard-shop-qr-materials-fulfilment-and-billing",
    title: "13. Dashboard shop, QR materials, fulfilment and billing",
    content: (
      <>
      <p>If you buy QR materials, credits, print packs, assisted setup or other add-ons through Tummly, we process the information needed to manage the order, verify authority, take payment, issue receipts or invoices, produce materials, deliver items, provide support and keep business records.</p>
      <p>This may include operator account details, purchaser name and contact details, billing details, payment status, order details, delivery address, artwork, logos, proof approvals, QR placement copy, fulfilment status, delivery tracking information and correspondence.</p>
      <p>Payments may be processed by a third-party payment provider. We may receive payment status, transaction identifiers, limited payment method information and invoice information. We do not intend to store full payment card details.</p>
      <p>We may share order, artwork, delivery and contact information with print, fulfilment, courier, payment, billing, support and fraud-prevention providers where needed to fulfil the order and protect the service.</p>
      </>
    ),
  },
  {
    id: "support-complaints-and-operational-communications",
    title: "14. Support, complaints and operational communications",
    content: (
      <>
      <p>If you contact Tummly support, we process the information you provide and information connected with your account or issue so we can respond, investigate, resolve the issue, improve support and keep records.</p>
      <p>Support data may include contact details, message content, screenshots, attachments, account information, technical logs, shop order details, activation status, QR details and internal notes.</p>
      <p>We may send service, security, account, trial, billing, fulfilment, legal and operational messages where needed. These messages are not general marketing and you may not be able to opt out of them while you use the service.</p>
      </>
    ),
  },
  {
    id: "tummly-marketing-research-and-surveys",
    title: "15. Tummly marketing, research and surveys",
    content: (
      <>
      <p>We may contact operator applicants, operators or business contacts about Tummly services, product updates, trials, research, surveys, events, offers and related business communications.</p>
      <p>Our lawful bases may include consent where required, legitimate interests in promoting and improving Tummly to relevant business contacts, and performing or preparing a contract. You can opt out of Tummly marketing communications using the unsubscribe route in the message or by contacting us.</p>
      <p>We may invite operators or users to take part in surveys, research or interviews. Participation is voluntary unless it forms part of a specific contracted service.</p>
      <p>We do not use guest contact data submitted to a restaurant through Tummly to send Tummly’s own general marketing unless we have a separate lawful basis and the individual has been given appropriate privacy information.</p>
      </>
    ),
  },
  {
    id: "cookies-analytics-and-similar-technologies",
    title: "16. Cookies, analytics and similar technologies",
    content: (
      <>
      <p>We use cookies and similar technologies for necessary site functions, security, preferences and analytics where enabled.</p>
      <p>Non-essential analytics or marketing technologies will be used only where consent or another lawful basis applies. You can manage your choices through the cookie banner where available, or by opening Cookie settings from the site footer.</p>
      <p><LegalDocLink to={LEGAL_ROUTES.cookiePolicy}>Our Cookie Policy</LegalDocLink> explains the categories of cookies and similar technologies we use, how long they last and how to manage your preferences.</p>
      </>
    ),
  },
  {
    id: "ai-assisted-features-service-data-and-aggregated-insights",
    title: "17. AI-assisted features, Service Data and Aggregated Insights",
    content: (
      <>
      <p>Tummly may provide AI-assisted summaries, draft messages, suggested tags, weekly briefs, recommended actions, support assistance or similar features where enabled.</p>
      <p>AI-assisted outputs are support tools and should be reviewed by an operator before use. They should not be treated as legal, tax, accounting, medical, food safety, employment or professional advice.</p>
      <p>We may process Service Data to operate, secure, monitor, support, analyse, improve and develop Tummly. Service Data may include QR performance, form activity, campaign performance, offer and redemption activity, shop order metadata, support activity, operational analytics, product usage, diagnostics and security events.</p>
      <p>We may create Aggregated Insights from Service Data, Restaurant Data, Guest Data, Imported Contact Data, QR performance, form activity, campaign performance, offer and redemption activity, shop order metadata, support activity and operational analytics, provided the output does not identify individual guests or disclose an operator’s identifiable guest list.</p>
      <p>Aggregated Insights may be used to provide reporting, benchmark-style insights, AI-assisted summaries, recommendations, product improvements, service planning and other non-identifying service intelligence. We do not sell identifiable guest contact data or share identifiable guest lists across operators.</p>
      <p>Pseudonymised data may still be personal data under data protection law. We treat pseudonymised personal data as personal data until it has been anonymised in a way that is no longer reasonably likely to identify an individual.</p>
      <p>Before producing benchmark-style outputs or sharing non-identifying insights externally, we may apply aggregation, anonymisation, access-control or threshold measures designed to reduce the risk of identifying an individual guest or exposing another operator’s identifiable guest list or commercially sensitive information.</p>
      <p>We do not permit identifiable Guest Data or Imported Contact Data to be used to train public or general-purpose AI models unless this is lawful, appropriately disclosed and controlled. Where AI providers process personal data for Tummly, we use appropriate contractual and technical safeguards.</p>
      </>
    ),
  },
  {
    id: "where-we-collect-personal-data-from",
    title: "18. Where we collect personal data from",
    content: (
      <>
      <p>We may collect personal data:</p>
      <ul>
        <li>directly from you when you use the website, submit a trial request, complete operator setup, use the dashboard, submit guest feedback, make a shop purchase, contact support or otherwise communicate with us</li>
        <li>from operators who upload, connect, import or provide contact data, location data, staff details, artwork, QR details, shop orders or support information</li>
        <li>automatically from your browser, device, cookies, security logs, usage activity, QR scans and interactions with Tummly pages</li>
        <li>from third-party providers such as hosting, analytics, email, SMS, payment, billing, address lookup, security, support, print, fulfilment, courier and AI providers</li>
        <li>from public sources or business information sources where needed to verify an operator, prevent misuse, contact a business or comply with legal requirements.</li>
      </ul>
      </>
    ),
  },
  {
    id: "who-we-share-personal-data-with",
    title: "19. Who we share personal data with",
    content: (
      <>
      <p>We share personal data only where needed for the purposes described in this notice, where instructed by an operator, where you have asked us to, or where required or permitted by law.</p>
      <p>Recipients may include named restaurants and operators; hosting and infrastructure providers; email, SMS and messaging providers; analytics and cookie providers; payment, billing and fraud-prevention providers; print, fulfilment and courier providers; AI and automation providers; support, monitoring and security providers; professional advisers, insurers and auditors; regulators, courts and law enforcement; and prospective buyers, investors or corporate transaction parties.</p>
      <p>The full sharing table is in the downloadable <LegalDocLink to={LEGAL_ROUTES.privacy}>Privacy Policy</LegalDocLink>.</p>
      </>
    ),
  },
  {
    id: "international-transfers",
    title: "20. International transfers",
    content: (
      <>
      <p>We and our providers may process personal data in the United Kingdom, the European Economic Area and other countries.</p>
      <p>Where personal data is transferred outside the UK and the destination is not covered by an adequacy regulation, we will use an appropriate transfer mechanism where required, such as the UK International Data Transfer Agreement, the UK Addendum to the EU Standard Contractual Clauses, or another lawful mechanism.</p>
      <p>You can contact us for more information about international transfers relevant to your personal data.</p>
      </>
    ),
  },
  {
    id: "how-long-we-keep-personal-data",
    title: "21. How long we keep personal data",
    content: (
      <>
      <p>We keep personal data only for as long as reasonably needed for the purposes described in this notice, unless a longer period is required or permitted for legal, accounting, security, dispute, suppression, backup, fraud prevention, regulatory or compliance purposes.</p>
      <p>Retention approaches vary by record type — for example trial request records are normally kept up to 24 months after last interaction if no account is created; operator account and contract records for the relationship and normally up to 6 years after closure where needed; guest and Imported Contact Data as instructed by the operator or service configuration; shop, fulfilment and billing records normally up to 6 years where needed; and aggregated or anonymised data which may be kept longer.</p>
      <p>The full retention table is in the downloadable <LegalDocLink to={LEGAL_ROUTES.privacy}>Privacy Policy</LegalDocLink>.</p>
      </>
    ),
  },
  {
    id: "your-rights",
    title: "22. Your rights",
    content: (
      <>
      <p>Depending on the circumstances and applicable law, you may have the right to:</p>
      <ul>
        <li>be informed about how your personal data is used</li>
        <li>access a copy of your personal data</li>
        <li>correct inaccurate or incomplete personal data</li>
        <li>request deletion of personal data</li>
        <li>restrict processing of personal data</li>
        <li>object to certain processing, including direct marketing and certain legitimate-interest processing</li>
        <li>receive certain personal data in a portable format</li>
        <li>withdraw consent where consent is the lawful basis</li>
        <li>complain to the Information Commissioner’s Office.</li>
      </ul>
      <p>To exercise rights in relation to personal data for which Tummly is controller, contact compliance@tummly.com. We may need to verify your identity before responding.</p>
      <p>We will normally respond to rights requests within one month, unless the law allows us to extend the response period or refuse the request in specific circumstances.</p>
      <p>To exercise rights in relation to guest data controlled by a restaurant or operator, contact the relevant restaurant or operator. We may assist the operator where Tummly acts as processor.</p>
      <p>You can opt out of Tummly marketing by using the unsubscribe route in the message or contacting us. You can opt out of a restaurant’s marketing by using the unsubscribe or opt-out route in that restaurant’s message or by contacting the restaurant.</p>
      </>
    ),
  },
  {
    id: "automated-decision-making-and-profiling",
    title: "23. Automated decision-making and profiling",
    content: (
      <>
      <p>Tummly may use automation, rules, AI assistance or analytics to support security, abuse prevention, account routing, rate limits, campaign controls, deliverability, product analytics, summaries, drafts, tags and recommendations.</p>
      <p>We do not intend to make decisions about individuals that have legal or similarly significant effects based solely on automated processing. If this changes, we will update this notice and provide any additional information required by law.</p>
      <p>AI-assisted and automated outputs may be incomplete or inaccurate and should be reviewed before use.</p>
      </>
    ),
  },
  {
    id: "security",
    title: "24. Security",
    content: (
      <>
      <p>We use technical and organisational measures designed to protect personal data. These may include access controls, authentication, password hashing, one-time passcodes, trusted device checks, rate limiting, tenant isolation, encryption in transit, logging and monitoring, supplier due diligence, confidentiality duties and incident response processes.</p>
      <p>No online service is completely secure. Operators and authorised users must protect their credentials, devices, invitation links, Activation Codes and account access.</p>
      <p>If you believe your Tummly account or personal data has been compromised, contact us promptly.</p>
      </>
    ),
  },
  {
    id: "children-and-sensitive-information",
    title: "25. Children and sensitive information",
    content: (
      <>
      <p>Tummly is not directed at children and is not intended for people under 16. Operators must not use Tummly to knowingly collect children’s data through guest forms, imports or campaigns, or to target children with marketing.</p>
      <p>Tummly is not designed to collect special category data or sensitive information such as health information, allergy notes, identity documents, payment card data, passwords or children’s data through guest forms or imports, unless Tummly has expressly enabled a field and the operator has completed appropriate legal review.</p>
      <p>If you include sensitive information in a feedback comment or support message, we may process it only as needed to provide the service, route the request, protect safety, comply with law, establish or defend legal claims, or delete/restrict it where appropriate.</p>
      </>
    ),
  },
  {
    id: "third-party-links-and-services",
    title: "26. Third-party links and services",
    content: (
      <>
      <p>Tummly may link to third-party websites, payment providers, delivery services, review platforms, social platforms or operator websites. Those third parties are responsible for their own privacy practices.</p>
      <p>Operators may connect third-party tools or import data from external systems where enabled. Operators are responsible for ensuring they have authority to connect, export, import and use that data in Tummly.</p>
      </>
    ),
  },
  {
    id: "complaints-and-contact-details",
    title: "27. Complaints and contact details",
    content: (
      <>
      <p>Contact us at compliance@tummly.com if you have questions about this notice or Tummly’s handling of personal data.</p>
      <p>You also have the right to complain to the Information Commissioner’s Office, the UK regulator for data protection. You can visit ico.org.uk or call the ICO helpline using the contact details published by the ICO.</p>
      <p>If your complaint relates to how a specific restaurant uses your guest data, we may direct you to that restaurant or assist the restaurant where Tummly acts as processor.</p>
      </>
    ),
  },
  {
    id: "changes-to-this-notice",
    title: "28. Changes to this notice",
    content: (
      <>
      <p>We may update <LegalDocLink to={LEGAL_ROUTES.privacy}>this Privacy Notice</LegalDocLink> from time to time. The updated version will be posted on Tummly.com with a new effective date.</p>
      <p>Where changes materially affect how we process personal data, we will take appropriate steps to notify affected users, operators or guests where required by law.</p>
      <p>Please review this notice periodically for the latest information on how Tummly handles personal data.</p>
      </>
    ),
  }
  ],
}
