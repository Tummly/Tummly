import { LegalDocLink } from "./LegalDocLink"
import { LEGAL_ROUTES } from "@/constants/legalRoutes"
import type { LegalPageContent } from "./types"

export const termsOfServiceContent: LegalPageContent = {
  title: "Terms of Service",
  description: "These Terms and Conditions explain how you may access and use Tummly. They apply to the Tummly website, trial request flow, operator account setup, operator dashboard, Smart Guest Links, QR codes, guest feedback pages, help centre, dashboard shop and related services made available by Tummly.com Limited. Effective date: 9 July 2026",
  documentKey: "terms",
  sections: [
  {
    id: "about-these-terms",
    title: "1. About these terms",
    content: (
      <>
      <p>These terms form a legal agreement between you and Tummly.com Limited. They should be read together with <LegalDocLink to={LEGAL_ROUTES.privacy}>our Privacy Notice</LegalDocLink>, <LegalDocLink to={LEGAL_ROUTES.cookiePolicy}>Cookie Policy</LegalDocLink>, any applicable order form, checkout page, plan description, support terms, dashboard shop terms, written pilot agreement and any supplementary terms that Tummly accepts with you.</p>
      <p>Some parts of these terms apply only to restaurant operators or business users. Other parts apply to guests and website visitors. Where a clause is stated to apply to operators, it applies to the restaurant, company or other organisation using Tummly and to the person accepting these terms on its behalf.</p>
      <p>The dashboard shop, paid plan and checkout parts of these terms are intended for business purchases by restaurant operators acting for trade or business purposes. They are not intended for consumer purchases. Nothing in these terms removes any statutory rights that a guest or consumer cannot legally waive.</p>
      <p>Document hierarchy: if there is a conflict between these terms and a separate written order form, written pilot agreement or checkout confirmation accepted by Tummly, the specific commercial terms in that document will apply to that conflict. For a dashboard shop purchase, the checkout details for that transaction will apply to the product, price, quantity, delivery address and payment terms shown at checkout. The rest of these terms will continue to apply.</p>
      </>
    ),
  },
  {
    id: "who-we-are",
    title: "2. Who we are",
    content: (
      <>
      <p>Tummly is operated by Tummly.com Limited, a company registered in England and Wales under company number 16236040. Our registered office is 71-75 Shelton Street, Covent Garden, London, WC2H 9JQ, United Kingdom.</p>
      <p>You can contact us about these terms or the Tummly service at compliance@tummly.com. For support requests, please use the support route shown on the Tummly website or in your account where available.</p>
      </>
    ),
  },
  {
    id: "who-these-terms-apply-to",
    title: "3. Who these terms apply to",
    content: (
      <>
      <p>These terms apply to:</p>
      <ul>
        <li>operators and applicants who request access, create an account, use the dashboard, place Smart Guest Links or QR materials, buy materials or credits, or run guest-facing activity through Tummly</li>
        <li>authorised users who sign in to Tummly on behalf of a restaurant, group, operator or location</li>
        <li>guests who scan a Smart Guest Link, open a Tummly guest page, submit private feedback, claim an offer or interact with a restaurant through a Tummly page; and</li>
        <li>visitors who use Tummly.com, the help centre or legal pages.</li>
      </ul>
      <p>If you accept these terms on behalf of a restaurant, company, group or other organisation, you confirm that you have authority to bind that organisation. In these terms, "you" means both you personally and the organisation you represent where the context requires it.</p>
      <p>Nothing in these terms removes any statutory rights that a guest or consumer cannot legally waive.</p>
      </>
    ),
  },
  {
    id: "what-tummly-provides",
    title: "4. What Tummly provides",
    content: (
      <>
      <p>Tummly Guest Loop helps restaurants collect private guest feedback, use QR-led Smart Guest Links, manage guest relationship data where enabled, run controlled offers and campaigns where enabled, and understand what needs action from one workspace.</p>
      <p>Tummly is not a point-of-sale system, delivery marketplace, order processing platform, payment processor for restaurant food orders, public review platform, staff scheduling tool, payroll system, broad business intelligence platform or full loyalty points engine.</p>
      <p>The features available to you depend on your plan, trial status, account approval, location configuration, usage credits, country availability, and the features Tummly has made available at the relevant time. Some features may be pilot, beta, planned, limited, usage-based or available only with a paid plan, checkout purchase or written agreement.</p>
      </>
    ),
  },
  {
    id: "definitions",
    title: "5. Definitions",
    content: (
      <>
      <p>These terms use defined meanings for Account, Activation Code, Authorised User, Checkout, Credits, Guest, Guest Data, Materials, Operator, Restaurant Data, Services, Smart Guest Link, Trial Period, Aggregated Insights, Imported Contact Data, Marketing Suppression Record, Service Data and Soft Opt-In Notice.</p>
      <p>The full definitions table is in the downloadable Terms document.</p>
      </>
    ),
  },
  {
    id: "requesting-access-approval-and-trials",
    title: "6. Requesting access, approval and trials",
    content: (
      <>
      <p>Tummly may be offered through guided access, pilots, trials, paid plans or invite-only onboarding. Submitting a trial request does not guarantee approval, account access, a particular feature, a particular launch date or a paid plan.</p>
      <p>You must provide accurate applicant, business, category, location and contact information.</p>
      <p>We may verify your email address and may ask for more information before approving, declining or pausing a request.</p>
      <p>If approved, we may send an operator setup invitation link. That link may expire, be revoked or be replaced by a new link.</p>
      <p>If your account requires an Activation Code, you must enter a valid code before accessing the dashboard.</p>
      <p>No payment is taken when you request access unless a separate paid order form, checkout page or written agreement clearly states otherwise. Your trial will not automatically convert into a paid subscription unless you accept paid terms, complete a paid checkout or otherwise agree to a paid plan.</p>
      <p>We may decline, delay, limit or withdraw trial access where we reasonably believe the service is not suitable, information supplied is incomplete or inaccurate, capacity is limited, there is a compliance concern, the request falls outside the intended launch scope, or access would create security, operational, legal or support risk.</p>
      <p>Trial access is provided for evaluation and launch support. We may change trial length, access limits, available features, onboarding steps, print fulfilment and support arrangements where needed.</p>
      </>
    ),
  },
  {
    id: "account-security-and-authorised-users",
    title: "7. Account security and authorised users",
    content: (
      <>
      <p>You are responsible for keeping account credentials, invitation links, Activation Codes, one-time passcodes and trusted-device tokens secure. You must not share credentials or allow unauthorised people to access your account.</p>
      <p>Use strong passwords and keep your sign-in device secure.</p>
      <p>Tell us promptly if you suspect account misuse, unauthorised access, a compromised QR asset, or a security incident involving Tummly.</p>
      <p>Ensure authorised users act within their role and authority. Orders, purchases, messages, campaigns, exports and other actions taken by an authorised user may bind the operator account.</p>
      <p>Remove or update access for people who leave your business or no longer need access.</p>
      <p>Do not attempt to access another operator's data, location, account or Smart Guest Link controls.</p>
      <p>We may use one-time passcodes, trusted device checks, account locks, rate limits, activation gates and other security controls. We may suspend or restrict access while we investigate suspected misuse, account compromise, security risk or breach of these terms.</p>
      </>
    ),
  },
  {
    id: "restaurant-responsibilities",
    title: "8. Restaurant responsibilities",
    content: (
      <>
      <p>Operators remain responsible for their restaurant, food, service, guest relationships, offers, promotions, staff training, consumer notices, privacy compliance, marketing compliance and any commitments made to guests.</p>
      <p>Keep your business name, locations, opening information, address, contact details, billing details, delivery addresses and staff instructions accurate.</p>
      <p>Use Smart Guest Links only for your own locations and lawful hospitality use cases.</p>
      <p>Place QR materials in a way that is clear, safe, not misleading and appropriate for guests.</p>
      <p>Ensure staff understand how Tummly feedback, offers, codes, redemption screens and support steps work before launch.</p>
      <p>Do not use Tummly to collect unlawful, excessive, misleading, sensitive or irrelevant personal data from guests.</p>
      <p>Do not upload, send or publish content that is unlawful, discriminatory, abusive, defamatory, misleading, infringing, malicious or harmful.</p>
      <p>Do not use Tummly for regulated, age-restricted or high-risk promotions unless you have obtained any required approvals, licences, checks and legal review.</p>
      <p>If a location changes ownership, closes, changes brand, changes address or stops using Tummly, you must update the account and remove or replace affected QR materials promptly.</p>
      </>
    ),
  },
  {
    id: "smart-guest-links-qr-codes-and-materials",
    title: "9. Smart Guest Links, QR codes and materials",
    content: (
      <>
      <p>A Smart Guest Link is tied to a restaurant or location. The QR code is only the access point; operators are responsible for where and how it is displayed.</p>
      <p>Do not alter, obscure or reuse a QR code in a way that misrepresents the restaurant, location or guest journey.</p>
      <p>Test each QR code before placing it on counters, receipts, packaging, delivery inserts, stickers, table cards, windows or digital channels.</p>
      <p>Remove or replace QR materials if a location closes, ownership changes, a campaign ends, a code is rotated or Tummly asks you to do so for security, privacy, technical or compliance reasons.</p>
      <p>Use only current QR assets generated or approved for the relevant location. Old or copied QR assets may become inactive, unsuitable, misleading or unsafe if your location, campaign, offer, QR token, account status or guest journey changes.</p>
      <p>Do not place Tummly QR materials in a way that suggests endorsement by a marketplace, POS provider, review platform or other third party unless that third party has approved it.</p>
      <p>Digital QR downloads may be available in your dashboard. Physical starter packs, replacements, reorders, premium branded packs and delivery tracking are available only where confirmed by Tummly in your plan, pilot, checkout or written arrangement.</p>
      <p>If printed materials are damaged, missing, incorrect or unsafe to use, contact support with your business name, affected location, order reference where available, a description of the issue and photos where available. Replacement is subject to verification, stock, delivery coverage, operational capacity and the terms of your plan, pilot or shop purchase.</p>
      </>
    ),
  },
  {
    id: "guest-facing-pages-and-private-feedback",
    title: "10. Guest-facing pages and private feedback",
    content: (
      <>
      <p>Guest feedback collected through Tummly is private feedback for the restaurant team. It is not a public review, is not automatically published and should not be treated as a verified public rating.</p>
      <p>Guests must not be pressured to submit feedback, claim an offer or join a guest list.</p>
      <p>Guests must be shown clear information about how their feedback and contact details will be used.</p>
      <p>When a guest submits a Tummly guest-facing form, the guest understands that their feedback, contact details and related submission information will be shared with the named operator and relevant location so the operator can review, manage and, where appropriate, respond to the submission.</p>
      <p>Where the guest-facing form includes a lawful Soft Opt-In Notice, the operator may use the guest's contact details for that limited similar-products-and-services marketing purpose only where the legal conditions for soft opt-in are met and the guest has not opted out. Guests must be able to opt out at collection and in later messages.</p>
      <p>Operators must handle guest feedback fairly, professionally and lawfully.</p>
      <p>Operators must not use guest submissions to harass, discriminate against, retaliate against or unfairly profile guests.</p>
      <p>Tummly may apply rate limits, token checks, security filters and abuse controls to protect guest pages and service integrity.</p>
      <p>Guests should contact the relevant restaurant directly about food, service, refunds, safety issues, allergens, delivery problems or in-store complaints. Tummly provides the technology used by the operator and is not the restaurant, seller, delivery provider or food service provider.</p>
      </>
    ),
  },
  {
    id: "guest-lists-imported-data-soft-opt-in-and-marketing-communications",
    title: "11. Guest lists, imported data, soft opt-in and marketing communications",
    content: (
      <>
      <p>This section applies to contact details collected through Tummly and to Imported Contact Data uploaded, connected or otherwise provided by an operator.</p>
      <p>Where guest list, offer, campaign, email, SMS or other supported electronic messaging features are enabled, operators must only contact guests where they have a lawful basis and the required consent, soft opt-in or other permission. Operators must respect opt-outs, unsubscribe requests, Marketing Suppression Records and guest preferences.</p>
      <p>Where an operator relies on soft opt-in, the operator must ensure the contact details were obtained in the context of a sale or negotiation for a sale of the operator's own goods or services; the marketing is limited to the operator's own similar food, drink, hospitality, offer or restaurant service communications; the guest was given a clear chance to opt out when the details were collected; and every marketing message includes a clear opt-out or unsubscribe route.</p>
      <p>A Soft Opt-In Notice may use wording such as: "The restaurant may send you occasional offers and updates for similar food and services. Tick here if you do not want to receive these." The live notice should clearly identify the relevant operator, restaurant or brand before the guest submits the form. The wording, channel, placement and presentation must be clear, prominent, accurate, consistent with the relevant privacy information and lawful for the operator's use case. Tummly may change, restrict or remove soft opt-in wording, channels or campaign access where needed for compliance, trust, deliverability or service integrity.</p>
      <p>Submitting feedback allows the feedback, contact details and related submission information to be shared with the named operator for feedback management, customer service and appropriate follow-up. It does not permit unrelated marketing, third-party marketing, sale of contact data, or messages outside the permission, soft opt-in or lawful basis that applies.</p>
      <p>Operators may import or connect contact data only where they lawfully collected it, have authority to use it in Tummly, and can identify the source, permission status, channel eligibility, opt-out status and any restrictions that apply.</p>
      <p>Operators must not import bought, rented, scraped, brokered, unlawfully shared, platform-prohibited or unclear-origin lists. Operators must not import payment card data, passwords, identity documents, health or allergy notes, children's data, special category data or other unnecessary sensitive information unless Tummly has expressly enabled the field and the operator has completed appropriate legal review.</p>
      <p>Before sending marketing to Imported Contact Data, the operator must record or confirm the contact's eligible permission status. Contacts with unknown permission, disputed origin, service-only status or missing consent/soft-opt-in evidence must not receive marketing through Tummly unless the operator can lawfully justify the send.</p>
      <p>Operators must preserve suppression and opt-out records when importing or updating data. Suppression lists must not be bypassed, overwritten or deleted to send marketing.</p>
      <p>Do not send spam, deceptive messages, excessive messages or messages to people who have opted out.</p>
      <p>Do not combine Tummly guest data with other datasets in a way that breaches privacy law, marketing law, platform terms, contractual restrictions or guest expectations.</p>
      <p>Do not use guest contact details for unrelated purposes unless the guest has been clearly informed and the use is lawful.</p>
      <p>Keep records needed to show the source, timing, wording, channel, permission status and opt-out status of guest contact details where required.</p>
      <p>Tummly may reject, quarantine, delete, suppress, limit, sample or require proof for Imported Contact Data where we reasonably believe the data is unlawful, excessive, unreliable, unsupported, unsafe, prohibited by platform terms, or likely to create complaints, deliverability issues or compliance risk.</p>
      <p>Tummly may process Imported Contact Data and permission metadata to provide the Services, deduplicate records, manage suppression, support campaigns, maintain audit and support records, prevent abuse, protect deliverability, operate and secure Tummly, and create Aggregated Insights in accordance with these terms and <LegalDocLink to={LEGAL_ROUTES.privacy}>the Privacy Notice</LegalDocLink>.</p>
      <p>Operators are responsible for the content, timing, targeting, frequency and legality of their restaurant messages, campaigns and offers. Tummly provides the workspace and related processing tools and may block sends, suppress contacts, throttle usage, disable campaigns, require proof of permission or restrict messaging where needed.</p>
      </>
    ),
  },
  {
    id: "offers-campaigns-and-redemption",
    title: "12. Offers, campaigns and redemption",
    content: (
      <>
      <p>Where offer and campaign tools are available, operators are responsible for making sure each offer is clear, lawful, genuine, honourable and manageable in-store.</p>
      <p>State the offer value, eligibility, expiry date, participating locations, redemption method, exclusions, limits and any significant conditions before the guest claims or uses the offer.</p>
      <p>Do not create misleading promotions, unfair restrictions, hidden conditions or offers your restaurant cannot reasonably fulfil.</p>
      <p>Use unique codes, expiry controls, one-use rules, staff verification or other controls where available.</p>
      <p>Honour valid offers that guests receive unless there is suspected fraud, abuse, technical error or a clear published restriction.</p>
      <p>Keep redemption records accurate and train staff on already-used, expired, invalid, not-found and manager-override states.</p>
      <p>Do not create prize draws, competitions, alcohol promotions, age-restricted promotions or regulated promotions through Tummly unless you have completed appropriate legal and operational checks.</p>
      <p>Tummly may pause, remove or refuse offers or campaigns that appear unlawful, misleading, abusive, likely to harm guests, likely to cause operational issues, likely to generate complaints, or likely to damage Tummly service integrity.</p>
      </>
    ),
  },
  {
    id: "public-reviews-and-reputation-platforms",
    title: "13. Public reviews and reputation platforms",
    content: (
      <>
      <p>Tummly is built for private feedback first. Operators must not use Tummly to reward, gate, filter, manipulate or misrepresent public reviews on Google, TripAdvisor, marketplace platforms or any other third-party review platform.</p>
      <p>Do not offer guests a reward for leaving a public review.</p>
      <p>Do not ask only satisfied guests to leave a public review while diverting dissatisfied guests elsewhere.</p>
      <p>Do not create fake, staff-written, paid, hidden-incentive, misleading or undisclosed reviews.</p>
      <p>Do not use private feedback scores, sentiment, tags or complaint outcomes as a gate before asking for public reviews.</p>
      <p>Follow the rules of each review platform and applicable consumer protection law.</p>
      <p>Any future public-review-related feature must remain separate from private feedback incentives and must be used only in a way that is lawful, transparent and consistent with platform rules.</p>
      </>
    ),
  },
  {
    id: "ai-assisted-features-and-insights",
    title: "14. AI-assisted features and insights",
    content: (
      <>
      <p>Tummly may provide AI-assisted summaries, draft messages, suggested tags, campaign drafts, weekly briefs or recommended actions where enabled. AI outputs are support tools, not final decisions.</p>
      <p>Review AI-assisted drafts before sending, publishing or acting on them.</p>
      <p>Do not rely on AI output as legal, tax, accounting, medical, employment, food safety or professional advice.</p>
      <p>Check the source period, sample size, guest context and operational context before acting on an AI output.</p>
      <p>Do not use AI outputs to fabricate evidence, hide negative feedback, manipulate public reviews, mislead guests or send high-risk messages without human approval.</p>
      <p>AI output may be incomplete, inaccurate, based on limited data or unsuitable for your situation. Tummly may limit, suspend, change or disable AI-assisted features where needed for safety, quality, cost control, compliance or service integrity.</p>
      </>
    ),
  },
  {
    id: "fees-subscriptions-usage-credits-and-billing",
    title: "15. Fees, subscriptions, usage credits and billing",
    content: (
      <>
      <p>No payment is taken when you request access. Paid plans, subscriptions, usage credits, SMS or email usage, AI usage, print packs, reorders, assisted setup and add-ons apply only where they are shown in an order form, checkout page, pricing page, dashboard shop or written agreement accepted by you.</p>
      <p>Your trial will not automatically convert into a paid subscription unless you accept paid terms, complete a paid checkout or otherwise agree to a paid plan.</p>
      <p>Fees may be charged per location, per account, by usage, by credit bundle, by add-on, by subscription period or by a custom plan.</p>
      <p>Prices, taxes, billing periods, renewal terms, payment dates, included credits, usage limits, overage rules and cancellation routes will be shown in the relevant order form, checkout, pricing page or written agreement where applicable.</p>
      <p>Unless stated otherwise, prices may be shown exclusive of VAT and applicable taxes. You are responsible for applicable VAT, taxes, third-party charges and payment method charges unless stated otherwise.</p>
      <p>Subscriptions, where enabled, may renew automatically for the period shown at checkout or in your order form unless cancelled in accordance with the relevant cancellation process or notice period. We will not use this clause to create an automatic paid renewal where no paid subscription has been accepted.</p>
      <p>Usage credits may expire, be plan-specific, be limited to a service type, be subject to fair-use limits, have no cash value, not roll over, and be non-refundable unless the applicable checkout, order form or written agreement says otherwise.</p>
      <p>If you downgrade, cancel or stop using Tummly, unused credits may expire or become unavailable unless your order form says otherwise. Credits must not be resold, transferred between unrelated operators, or used for unlawful or abusive messaging.</p>
      <p>We may suspend paid features, messaging, AI usage, shop ordering, fulfilment or account access for non-payment, excessive usage, failed payment, suspected fraud, chargeback, misuse or breach of these terms.</p>
      <p>If billing is not yet enabled in the product, your pilot or trial remains subject to the written commercial terms we confirm separately.</p>
      </>
    ),
  },
  {
    id: "dashboard-shop-qr-materials-and-fulfilment",
    title: "16. Dashboard shop, QR materials and fulfilment",
    content: (
      <>
      <p>Where Tummly provides a dashboard shop or checkout, operators may be able to buy or request Materials, reorders, premium branded packs, usage credits, message credits, AI credits, assisted setup or other add-ons. Availability may vary by plan, location, stock, fulfilment capacity, delivery coverage and account status.</p>
      <p>A shop order or checkout submission is an offer to buy the selected item. Tummly accepts the order only when we confirm acceptance, take payment, dispatch the item, make the digital item available, or otherwise confirm fulfilment. We may reject or cancel an order before acceptance where payment fails, stock is unavailable, information is inaccurate, the order appears fraudulent or abusive, delivery is not available, or fulfilment would create legal, operational, security or compliance risk.</p>
      <p>Operators are responsible for making sure that shop order details are accurate, including business name, location, delivery address, artwork, logo files, QR placement wording, contact details, product quantity and any proof approval.</p>
      <p>Digital downloads and self-print PDFs may be available immediately and may be tied to the current location, QR token, campaign, offer or account settings. If you later change those settings, earlier downloads or printed versions may become unsuitable and may need to be replaced.</p>
      <p>Physical materials may be produced or delivered by third-party suppliers. Delivery dates are estimates unless expressly confirmed as binding. Tummly is not responsible for delays caused by incorrect delivery details, courier issues, supplier issues, customs, stock limits, force majeure events or operator delay in approving proofs.</p>
      <p>Custom or branded materials may require proof approval. Once a proof is approved, you are responsible for errors in the approved proof unless the delivered materials materially differ from the approved proof because of Tummly's or its supplier's error.</p>
      <p>Unless required by law or agreed by Tummly, custom printed materials, used materials, opened materials, digital downloads, self-print assets, credits and services already performed are non-refundable. Damaged, missing or incorrect materials must be reported promptly with evidence. The remedy may be replacement, reprint, credit or refund, depending on the issue and applicable law.</p>
      <p>Additional detail for shop purchases is set out in Supplementary Terms C - Shop, QR Materials and Fulfilment Terms.</p>
      </>
    ),
  },
  {
    id: "support-and-help-centre",
    title: "17. Support and help centre",
    content: (
      <>
      <p>Tummly may provide help centre articles, email support, guided setup, operational support or pilot support. Support availability depends on your plan, pilot arrangement, service maturity and the nature of the issue.</p>
      <p>Support content is general guidance. Account-specific issues, privacy requests, billing questions, suspected security incidents, activation issues and fulfilment problems may require a support request, identity check, operator authorisation or compliance review.</p>
      </>
    ),
  },
  {
    id: "service-availability-beta-features-and-changes",
    title: "18. Service availability, beta features and changes",
    content: (
      <>
      <p>We aim to provide a reliable service, but we do not guarantee uninterrupted, error-free or always-available access. Tummly may be affected by maintenance, updates, third-party provider outages, security controls, usage limits, internet issues, email or SMS provider issues, device or browser behaviour, force majeure events, or operational constraints.</p>
      <p>We may update, improve, replace, restrict, rename or remove features. Where a change materially affects an active paid service, we will take reasonable steps to notify affected operators.</p>
      <p>We may run pilots, beta features, experiments or limited-access functionality. Such features may be incomplete, contain errors, change without notice, be withdrawn, have limited support, use test data or not be suitable for production reliance. Do not rely on beta or pilot features for legal, compliance, financial, safety or mission-critical decisions unless Tummly confirms otherwise in writing.</p>
      </>
    ),
  },
  {
    id: "third-party-services-and-integrations",
    title: "19. Third-party services and integrations",
    content: (
      <>
      <p>Tummly may use third-party providers for hosting, infrastructure, analytics, email, SMS, billing, payment processing, AI assistance, address lookup, support tooling, security, monitoring, print production and fulfilment. These providers may have their own terms and privacy notices.</p>
      <p>Tummly is not responsible for third-party restaurant services, POS systems, delivery platforms, review platforms, websites, payment systems, social platforms, fulfilment carriers or integrations that are not controlled by Tummly.</p>
      <p>Unless Tummly clearly confirms otherwise in writing, Tummly does not guarantee any live integration with a marketplace, POS provider, review platform, payment provider or delivery platform.</p>
      </>
    ),
  },
  {
    id: "intellectual-property-and-restaurant-content",
    title: "20. Intellectual property and restaurant content",
    content: (
      <>
      <p>Tummly and its software, design, workflows, dashboards, Smart Guest Link system, documentation, content, brand, trade marks and know-how are owned by Tummly or its licensors. We grant you a limited, revocable, non-exclusive, non-transferable right to use the Services for your internal restaurant operations during your authorised access period.</p>
      <p>You retain ownership of your restaurant name, logo, menu information, content, guest data and other materials you provide. You grant Tummly a licence to host, process, display and use those materials as needed to provide, secure, support, improve and fulfil the Services, including showing your restaurant name on guest-facing pages, QR materials, shop previews, printed materials and support records.</p>
      <p>You confirm that you have the right to provide restaurant names, logos, artwork, images, trade marks, offer copy, location details and other content you submit to Tummly or to a Tummly supplier.</p>
      <p>Tummly will not use your restaurant name or logo in a public case study, public customer list or external promotional claim without your consent, except where your restaurant name must appear as part of the service you have requested, such as on guest-facing pages, QR materials, account screens, support communications, fulfilment records or legal records.</p>
      <p>You must not copy, reverse engineer, resell, modify, scrape, frame, benchmark for a competitor, or build a competing service using Tummly software, content or workflows except as permitted by law.</p>
      <p>Tummly owns Aggregated Insights, platform analytics, diagnostic outputs, non-identifying benchmarks, product learnings and derived service improvements that Tummly creates from operation of the Services. This does not transfer ownership of operator content, identifiable Restaurant Data or identifiable Guest Data to Tummly.</p>
      </>
    ),
  },
  {
    id: "data-protection-and-privacy",
    title: "21. Data protection and privacy",
    content: (
      <>
      <p>Each party must comply with applicable data protection and electronic communications laws, including the UK GDPR, Data Protection Act 2018 and PECR where they apply.</p>
      <p>For restaurant guest records processed through Tummly on behalf of an operator, the operator normally acts as controller and Tummly acts as processor unless a separate written agreement states otherwise. For Tummly website visitors, trial applicants, operator accounts, support contacts, security logs, billing contacts, shop purchasers and Tummly service administration, Tummly acts as controller.</p>
      <p><LegalDocLink to={LEGAL_ROUTES.privacy}>Our Privacy Notice</LegalDocLink> explains how we process personal data when we act as controller. The Supplementary Data Processing Terms below apply where Tummly processes personal data as processor for an operator.</p>
      <p>Tummly may also determine the purposes and means of processing certain Service Data and Aggregated Insights for service administration, security, fraud and abuse prevention, product analytics, service improvement, benchmarking, support, compliance and development of Tummly services. Where such processing involves personal data, Tummly will handle it in accordance with <LegalDocLink to={LEGAL_ROUTES.privacy}>the Privacy Notice</LegalDocLink> and applicable law.</p>
      <p>Operators are responsible for giving guests any privacy information required for their own use of guest data, for choosing lawful bases for their communications, and for responding to guest rights requests where they are controller.</p>
      </>
    ),
  },
  {
    id: "confidentiality",
    title: "22. Confidentiality",
    content: (
      <>
      <p>Each party must protect the other party's confidential information and use it only for the purposes of these terms. Confidential information includes non-public product information, pricing, account data, guest data, security information, business information, dashboards, campaign data, technical materials, shop order details, supplier information and information marked or reasonably understood to be confidential.</p>
      <p>Confidentiality obligations do not apply to information that is already public, lawfully received from another source, independently developed without using confidential information, or required to be disclosed by law, regulator, court or competent authority.</p>
      </>
    ),
  },
  {
    id: "acceptable-use",
    title: "23. Acceptable use",
    content: (
      <>
      <p>You must not:</p>
      <ul>
        <li>use Tummly for unlawful, fraudulent, harmful, discriminatory, exploitative or misleading purposes</li>
        <li>attempt to bypass security controls, rate limits, activation gates, tenant isolation or access restrictions</li>
        <li>submit malware, harmful code, automated scraping, credential attacks or abusive traffic</li>
        <li>send spam or unlawful direct marketing</li>
        <li>import bought, scraped, brokered, platform-prohibited, unclear-origin or unlawfully obtained contact data</li>
        <li>collect excessive, sensitive or irrelevant personal data from guests</li>
        <li>use Tummly to manipulate public reviews or mislead consumers</li>
        <li>misrepresent your relationship with Tummly or with any marketplace, POS provider, review platform or third party</li>
        <li>copy, scrape or extract data from Tummly except through approved export tools or as allowed by law</li>
        <li>use Tummly to buy, print, display or distribute materials that are unlawful, misleading, infringing, unsafe or unrelated to your authorised restaurant locations</li>
        <li>use Tummly in a way that harms guests, operators, Tummly, third-party platforms or service integrity.</li>
      </ul>
      </>
    ),
  },
  {
    id: "suspension-restriction-and-termination",
    title: "24. Suspension, restriction and termination",
    content: (
      <>
      <p>We may suspend, restrict or terminate access if:</p>
      <ul>
        <li>you breach these terms, an order form, checkout term or applicable law</li>
        <li>your trial, approval, invite, Activation Code or Trial Period ends</li>
        <li>you do not pay fees when due</li>
        <li>we reasonably suspect security misuse, fraud, unlawful marketing, data misuse, public review manipulation, offer abuse, shop abuse, payment abuse, chargeback risk or unauthorised access</li>
        <li>we are required or reasonably expected to do so by law, regulator request, platform rule, court order or third-party provider requirement</li>
        <li>the service is discontinued, materially changed or no longer suitable for your use case.</li>
      </ul>
      <p>We will take reasonable steps to give notice where practical, but we may act immediately where needed to protect guests, operators, Tummly, data, service integrity or legal compliance.</p>
      <p>You may stop using Tummly at any time. Paid subscriptions, if enabled, may require cancellation through the relevant billing process or written notice period shown in your order form. Termination does not affect rights or obligations that have already accrued.</p>
      <p>Suspension or termination may affect active QR codes, guest pages, campaigns, offers, shop orders, fulfilment requests, credits and support access.</p>
      </>
    ),
  },
  {
    id: "data-export-deletion-retention-and-service-insights",
    title: "25. Data export, deletion, retention and service insights",
    content: (
      <>
      <p>Where export tools are available, operators may export Restaurant Data before termination. After termination, we may retain data for a limited period to allow reactivation, comply with legal obligations, resolve disputes, maintain security, honour opt-outs and suppression lists, and keep business records.</p>
      <p>We may delete or anonymise Restaurant Data after the applicable retention period, unless we are required or permitted to retain it for legal, security, accounting, dispute, backup, suppression, fraud prevention or compliance purposes.</p>
      <p>Following termination, QR links and guest-facing pages may be disabled. Shop order, billing, tax, fraud prevention, fulfilment, support, suppression and compliance records may be retained for as long as reasonably required for those purposes.</p>
      <p>Tummly may collect and use Service Data to operate, secure, monitor, support, analyse, improve and develop the Services, including abuse prevention, deliverability protection, product analytics, service planning, technical diagnostics, quality control and reliability monitoring.</p>
      <p>Tummly may create Aggregated Insights from Service Data, Restaurant Data, Guest Data, Imported Contact Data, QR performance, form activity, campaign performance, offer and redemption activity, shop order metadata, support activity and operational analytics, provided the output does not identify an individual guest or disclose an operator's identifiable guest list.</p>
      <p>Tummly may use Aggregated Insights to provide reporting, benchmark-style insights, AI-assisted summaries, recommendations, product improvements, service planning and other non-identifying service intelligence. Aggregated Insights may continue to be used after an operator stops using Tummly.</p>
      <p>Tummly will not sell identifiable guest contact data, share identifiable guest lists across operators, or publish operator-identifiable benchmark data without the relevant operator's consent unless required or permitted by law.</p>
      <p>Pseudonymised data may still be personal data under data protection law. Tummly will treat pseudonymised personal data as personal data until it has been anonymised in a way that is no longer reasonably likely to identify an individual.</p>
      </>
    ),
  },
  {
    id: "change-of-ownership-and-account-transfer",
    title: "26. Change of ownership and account transfer",
    content: (
      <>
      <p>You must notify Tummly promptly if your restaurant, group, location, brand or account ownership changes, or if you no longer have authority to use the account.</p>
      <p>You must not transfer an account, guest list, QR materials, Smart Guest Links, billing relationship, shop order or subscription to a new owner without Tummly's written approval.</p>
      <p>Before approving a transfer, Tummly may require verification of authority, updated account details, new commercial terms, privacy review, guest-data instructions, deletion or export steps, replacement QR materials, or confirmation that the new owner will comply with these terms.</p>
      <p>If ownership changes without approval, Tummly may suspend account access, guest-facing pages, QR links, campaigns, offers, shop fulfilment and data exports until the correct operator and lawful data handling position are confirmed.</p>
      </>
    ),
  },
  {
    id: "disclaimers",
    title: "27. Disclaimers",
    content: (
      <>
      <p>Tummly provides tools and information for restaurant guest relationship management. We do not guarantee revenue growth, scan rates, repeat visits, review scores, campaign performance, deliverability, redemption rates, guest responses, operational outcomes, QR material performance, return on investment or fitness for a particular commercial result.</p>
      <p>Operators remain responsible for food, service, hygiene, allergens, pricing, consumer offers, staff actions, customer service, fulfilment and legal compliance. Guests should contact the restaurant directly about food orders, refunds, safety issues or service complaints.</p>
      <p>To the maximum extent permitted by law, the Services are provided on an "as is" and "as available" basis. This does not affect rights or warranties that cannot lawfully be excluded.</p>
      </>
    ),
  },
  {
    id: "liability",
    title: "28. Liability",
    content: (
      <>
      <p>Nothing in these terms limits or excludes liability that cannot lawfully be limited or excluded, including liability for death or personal injury caused by negligence, fraud, fraudulent misrepresentation, or liability that cannot be excluded under data protection law or consumer protection law.</p>
      <p>Subject to the paragraph above, Tummly is not liable for indirect, consequential, special or punitive losses; loss of profit, revenue, goodwill, business, opportunity or anticipated savings; loss or corruption of data where appropriate backups or controls were not maintained; guest behaviour; restaurant operational failures; third-party service outages; print supplier delays; courier delays; operator-approved proof errors; or misuse of the Services by you or your users.</p>
      <p>For business operators, Tummly's total aggregate liability arising out of or in connection with the Services is limited to the greater of: (a) GBP 100; or (b) the fees paid by you to Tummly for the affected Services in the three months before the event giving rise to the claim. This cap does not apply to liability that cannot lawfully be capped.</p>
      <p>For guests and consumers, nothing in these terms affects any legal rights that cannot be excluded or limited under applicable consumer law.</p>
      </>
    ),
  },
  {
    id: "indemnity-from-business-operators",
    title: "29. Indemnity from business operators",
    content: (
      <>
      <p>If you are an operator or business user, you must reimburse and protect Tummly, its officers, employees, contractors and suppliers from losses, claims, fines, costs, damages and expenses arising from your breach of these terms, unlawful use of the Services, restaurant content, artwork, shop order instructions, offers, campaigns, marketing messages, guest data misuse, public review manipulation, infringement of third-party rights, or failure to comply with applicable law.</p>
      </>
    ),
  },
  {
    id: "changes-to-these-terms",
    title: "30. Changes to these terms",
    content: (
      <>
      <p>We may update these terms from time to time. The updated version will be posted on Tummly.com with a new effective date. Where changes materially affect active paid operators, we will take reasonable steps to notify them. Continued use of the Services after the effective date means you accept the updated terms.</p>
      <p>If you do not agree to updated terms, you must stop using Tummly and, where applicable, cancel your paid service in accordance with your order form or billing process.</p>
      </>
    ),
  },
  {
    id: "notices-and-communications",
    title: "31. Notices and communications",
    content: (
      <>
      <p>We may send notices by email, dashboard notification, website notice, support message or other contact details linked to your account. You must keep your account contact details accurate. Legal notices to Tummly should be sent to compliance@tummly.com and may also be sent to our registered office.</p>
      <p>Notices about shop orders, fulfilment, proofs, delivery, billing or support may be sent to the contact details linked to the order or account.</p>
      </>
    ),
  },
  {
    id: "complaints",
    title: "32. Complaints",
    content: (
      <>
      <p>If you have a complaint about Tummly, contact us through the support route provided on the website or at compliance@tummly.com. We will review the issue and respond with the next appropriate step.</p>
      <p>Guest complaints about food, service, refunds, allergens, safety, delivery or restaurant operations should be directed to the relevant restaurant. We may direct guests to the restaurant where the complaint concerns the operator rather than Tummly.</p>
      </>
    ),
  },
  {
    id: "general-legal-terms",
    title: "33. General legal terms",
    content: (
      <>
      <p>You may not assign your rights or obligations without our prior written consent.</p>
      <p>We may assign, transfer or subcontract our rights and obligations where this does not materially reduce your rights under these terms.</p>
      <p>If part of these terms is found invalid or unenforceable, the rest remains in force.</p>
      <p>A delay in enforcing these terms does not waive our right to enforce them later.</p>
      <p>No person other than you and Tummly has rights to enforce these terms unless the terms expressly say otherwise.</p>
      <p>These terms, together with any order form, checkout, plan terms, Supplementary Data Processing Terms and policies referenced in them, form the agreement between you and Tummly for the Services.</p>
      </>
    ),
  },
  {
    id: "governing-law-and-courts",
    title: "34. Governing law and courts",
    content: (
      <>
      <p>These terms are governed by the laws of England and Wales. The courts of England and Wales have exclusive jurisdiction, except where applicable consumer law gives a guest or consumer the right to bring proceedings elsewhere.</p>
      </>
    ),
  },
  {
    id: "data-processing-terms",
    title: "Supplementary Terms A - Data Processing Terms",
    content: (
      <>
      <p>These Supplementary Data Processing Terms apply where Tummly processes personal data on behalf of an operator as processor. They are intended to form part of the written contract between the operator as controller and Tummly as processor.</p>
      <h4>Scope and duration</h4>
      <p>The subject matter is the processing of personal data through Tummly Guest Loop and related services. Processing continues for the duration of the operator's authorised use and any post-termination retention period needed for export, deletion, legal compliance, dispute resolution, security, backups and suppression records.</p>
      <h4>Nature and purpose of processing</h4>
      <p>Tummly processes personal data to provide, secure, maintain and support Smart Guest Links, guest feedback forms, soft opt-in notices where configured, consent and opt-out capture where enabled, imported contact management, guest records, offers, campaigns, redemptions, insights, support, security, analytics, administration, shop fulfilment and related service functions.</p>
      <h4>Data subjects and personal data</h4>
      <p>Where Tummly acts as processor, data subjects may include restaurant guests, operator users, support contacts and imported contacts.</p>
      <p>The personal data involved depends on the data subject and may include contact details, feedback, consent and opt-out status, account and security metadata, support messages, import metadata and related operational records.</p>
      <p>The full data subjects table is in the downloadable Terms document.</p>
      <h4>Operator obligations</h4>
      <p>The operator is responsible for the lawfulness, fairness and transparency of its processing as controller.</p>
      <p>The operator must provide clear privacy information to guests where required.</p>
      <p>The operator must ensure it has a lawful basis and, where required, valid consent, valid soft opt-in or other lawful permission for guest communications and marketing.</p>
      <p>The operator must ensure that Imported Contact Data was lawfully collected, may be uploaded to Tummly, and may be used for the permissions, channels and purposes selected in the Services.</p>
      <p>The operator must maintain accurate Marketing Suppression Records and must not instruct Tummly to send marketing to guests or imported contacts who have opted out or whose permission status is unknown or insufficient for the proposed message.</p>
      <p>The operator must not instruct Tummly to process personal data unlawfully or in a way that is outside the scope of the Services.</p>
      <p>The operator must respond to guest rights requests where it is controller and must give Tummly reasonable information needed to assist.</p>
      <h4>Tummly processor obligations</h4>
      <p>Where Tummly acts as processor, Tummly will:</p>
      <ul>
        <li>process personal data only on documented instructions from the operator, unless required by law</li>
        <li>ensure personnel authorised to process personal data are subject to confidentiality duties</li>
        <li>implement appropriate technical and organisational measures designed to protect personal data</li>
        <li>assist the operator with data subject requests, security obligations, data protection impact assessments and regulator queries where reasonably required and proportionate</li>
        <li>notify the operator without undue delay after becoming aware of a personal data breach affecting operator-controlled personal data</li>
        <li>delete or return personal data at the end of the Services unless retention is required or permitted by law</li>
        <li>make available reasonable information needed to demonstrate compliance with processor obligations.</li>
      </ul>
      <h4>Sub-processors</h4>
      <p>The operator gives Tummly general authorisation to use sub-processors for hosting, infrastructure, email, SMS, analytics, AI assistance, billing, payment processing, security, support, monitoring, print production and fulfilment. Tummly will require sub-processors to protect personal data under written terms that are no less protective than these Data Processing Terms in all material respects.</p>
      <p>Tummly will take reasonable steps to provide information about sub-processors or sub-processor categories on request and to inform affected operators of material changes where required by law. If an operator reasonably objects to a sub-processor change on data protection grounds, the operator must notify Tummly promptly and the parties will discuss a reasonable resolution.</p>
      <h4>International transfers</h4>
      <p>Where Tummly or its sub-processors transfer personal data outside the UK, Tummly will use an appropriate transfer mechanism where required, such as an adequacy regulation, UK International Data Transfer Agreement, UK Addendum to EU Standard Contractual Clauses, or another lawful mechanism.</p>
      <h4>Security measures</h4>
      <p>Security measures may include tenant isolation, access controls, authentication, password hashing, one-time passcodes, rate limiting, encrypted transport, role-based access, logging and monitoring where available, backup and recovery controls where used, incident response, staff confidentiality and supplier due diligence. The measures may evolve over time to reflect risk, service maturity and technology.</p>
      <h4>Audits and information</h4>
      <p>On reasonable written request, Tummly will provide information needed to demonstrate compliance with these Data Processing Terms. Audits must be reasonable, proportionate, confidential, not disrupt the Services, not create security risk and may be satisfied by policies, summaries, security information, certifications or third-party reports where appropriate.</p>
      <h4>Data subject requests and regulator correspondence</h4>
      <p>If Tummly receives a request from a guest or other data subject relating to personal data processed for an operator, Tummly may direct the person to the operator or assist the operator as required by applicable law. The operator remains responsible for deciding how to respond where it is controller.</p>
      <h4>Aggregated and anonymised data</h4>
      <p>Tummly may create and use Aggregated Insights and anonymised data for service operation, security, analytics, benchmarking, product improvement, AI-assisted features, support, abuse prevention and development of Tummly services.</p>
      <p>Tummly will not sell identifiable guest contact data, share identifiable guest lists across operators, or disclose an operator's identifiable guest list through Aggregated Insights.</p>
      <p>Where an output is not truly anonymised and remains personal data, Tummly will process it in accordance with applicable data protection law and <LegalDocLink to={LEGAL_ROUTES.privacy}>the Privacy Notice</LegalDocLink>.</p>
      </>
    ),
  },
  {
    id: "offer-campaign-and-public-review-standards",
    title: "Supplementary Terms B - Offer, Campaign and Public Review Standards",
    content: (
      <>
      <p>These standards apply to operators using Tummly offer, campaign, messaging, feedback, redemption or review-related features where available.</p>
      <h4>Offer standards</h4>
      <p>Offers must be clear, genuine, time-limited where appropriate and operationally deliverable.</p>
      <p>All significant conditions must be shown clearly before the guest acts on the offer.</p>
      <p>Expiry, participating locations, exclusions, redemption method, limits and eligibility must be clear.</p>
      <p>Operators must train staff before a live offer is distributed.</p>
      <p>Operators must not use offers in a way that is misleading, unfair, discriminatory, unsafe or likely to cause avoidable complaints.</p>
      <p>Age-restricted, alcohol, prize, competition or regulated promotions require appropriate legal and operational checks before use.</p>
      <h4>Campaign and message standards</h4>
      <p>Campaigns must only be sent to guests who are eligible to receive them.</p>
      <p>Where a campaign relies on soft opt-in, the operator must be able to show that the soft opt-in conditions are met for the relevant contacts and channels.</p>
      <p>Imported contacts must not be included in campaigns unless their source, channel eligibility and permission status support the proposed message.</p>
      <p>Messages must identify the relevant restaurant and must not mislead guests about the sender, offer, expiry or reason for contact.</p>
      <p>Unsubscribe and opt-out requests must be honoured promptly.</p>
      <p>Suppression lists must not be bypassed.</p>
      <p>Guest data must not be used to target vulnerable individuals unfairly.</p>
      <p>Operators must review AI-drafted messages before use.</p>
      <h4>Redemption standards</h4>
      <p>Where available, operators should use unique codes, one-use controls, expiry controls, redemption logs or staff verification.</p>
      <p>Operators must not knowingly redeem the same one-use offer more than once unless a manager override is appropriate.</p>
      <p>Operators must handle already-used, expired, invalid and not-found states consistently and fairly.</p>
      <p>Suspected fraud or abuse should be recorded and reported through support where appropriate.</p>
      <h4>Public review standards</h4>
      <p>Private feedback incentives must not be tied to leaving a public review.</p>
      <p>Public review prompts must not be gated by private feedback score, sentiment, tags or complaint outcome.</p>
      <p>Operators must not create, commission, buy, conceal incentives for, or otherwise manipulate public reviews.</p>
      <p>Operators must follow the rules of each review platform and applicable consumer protection law.</p>
      <p>Tummly may suspend or restrict features where it reasonably suspects public review manipulation or misleading review activity.</p>
      </>
    ),
  },
  {
    id: "shop-qr-materials-and-fulfilment-terms",
    title: "Supplementary Terms C - Shop, QR Materials and Fulfilment Terms",
    content: (
      <>
      <p>These terms apply where Tummly makes a dashboard shop, checkout, reorder flow, credit top-up, print pack order, digital download or assisted setup purchase available to operators.</p>
      <h4>Business use only</h4>
      <p>The Tummly dashboard shop is intended for operators buying for business use. By placing a shop order, you confirm that you are acting for business purposes and have authority to place the order for the operator account.</p>
      <h4>Products and availability</h4>
      <p>Shop products may include starter QR materials, replacement QR materials, counter cards, table tents, stickers, delivery inserts, packaging stickers, receipt assets, premium branded packs, self-print PDFs, digital QR downloads, usage credits, message credits, AI credits, assisted setup and other add-ons.</p>
      <p>All products are subject to availability, plan eligibility, account status, location setup, delivery coverage, stock, production capacity and any restrictions shown at checkout.</p>
      <h4>Order details and acceptance</h4>
      <p>You are responsible for checking the product, quantity, location, delivery address, billing details, artwork, proof, QR placement copy and price before submitting an order.</p>
      <p>Tummly may accept, reject or cancel an order where payment fails, stock is unavailable, a delivery address is invalid, the product is unavailable for your plan or location, a proof is not approved, there is suspected fraud or abuse, or fulfilment would create legal, security, privacy, operational or compliance risk.</p>
      <p>If Tummly cancels an accepted paid order before fulfilment for a reason not caused by you, Tummly will normally refund the amount paid for the cancelled item.</p>
      <h4>Prices, VAT, payment and invoices</h4>
      <p>The price, VAT treatment, delivery charge, payment method, renewal rule, credit allocation and any included allowance will be shown at checkout or in the relevant order form where applicable.</p>
      <p>Payment may be processed by a third-party payment provider. Your use of that payment provider may be subject to its own terms and checks.</p>
      <p>Tummly may refuse or delay fulfilment until payment is authorised and any fraud, billing or verification checks are complete.</p>
      <p>Where available, VAT invoices or receipts may be issued through the dashboard, payment provider, email or support process.</p>
      <h4>Credits and usage allowances</h4>
      <p>Credits may be limited to the service type shown at purchase, such as SMS, email, AI, fulfilment, print or other usage.</p>
      <p>Credits are not cash, do not earn interest and may not be exchanged for cash.</p>
      <p>Credits may expire, be non-transferable, be non-refundable, be subject to fair-use controls, or be lost on cancellation, downgrade or termination unless the checkout or order form says otherwise.</p>
      <p>Tummly may block or reverse credits obtained through error, fraud, abuse, chargeback or breach of these terms.</p>
      <h4>Digital downloads and self-print assets</h4>
      <p>Digital QR downloads, self-print PDFs and other digital assets may be made available immediately after account setup or purchase.</p>
      <p>You are responsible for checking that the asset matches the correct restaurant, location, campaign, offer, QR token and guest journey before printing or distributing it.</p>
      <p>If a location, offer, campaign, QR token, account status or guest journey changes, previously downloaded or printed materials may become outdated and may need to be replaced.</p>
      <p>Tummly does not guarantee that self-printed materials will scan correctly if they are printed at poor quality, resized incorrectly, damaged, obscured, placed behind reflective surfaces, or used in unsuitable lighting or environments.</p>
      <h4>Custom and branded materials</h4>
      <p>For branded or custom materials, you are responsible for providing accurate artwork, logos, colours, copy, location details, brand permissions and proof approvals.</p>
      <p>Tummly may reject artwork, copy or instructions that appear unlawful, misleading, infringing, offensive, low quality, unsafe, technically unsuitable or inconsistent with Tummly service standards.</p>
      <p>Colours, finishes, paper stock, cut lines, placement, size and QR presentation may vary within reasonable production tolerances. Screen previews may not match printed output exactly.</p>
      <p>Once you approve a proof, Tummly is not responsible for spelling, location, offer, logo, colour, copy or layout issues that were visible in the approved proof, unless the delivered materials materially differ from the approved proof because of Tummly's or its supplier's error.</p>
      <h4>Delivery, risk and failed delivery</h4>
      <p>Delivery dates are estimates unless Tummly expressly confirms otherwise in writing.</p>
      <p>You are responsible for providing a complete and accurate delivery address and ensuring someone can receive the delivery where required.</p>
      <p>Tummly may use third-party printers, fulfilment partners and couriers. Delays may occur because of stock, production, courier, weather, customs, industrial action, address, access or operational issues.</p>
      <p>If delivery fails because the address is wrong, access is unavailable, the recipient refuses delivery, or you do not collect from a courier or collection point, Tummly may charge reasonable re-delivery, reprint or handling costs.</p>
      <h4>Damaged, missing or incorrect materials</h4>
      <p>You should inspect physical materials promptly after delivery.</p>
      <p>If materials are damaged, missing, materially incorrect or unsafe to use, contact Tummly support promptly with your business name, affected location, order reference, issue description and photos where available.</p>
      <p>If Tummly verifies that the issue was caused by Tummly or its supplier, the remedy may be replacement, reprint, credit or refund. The appropriate remedy will depend on the issue, product type, delivery status and applicable law.</p>
      <p>If you report an issue late, continue using the materials, alter the materials, approve an incorrect proof, or provide incorrect order details, Tummly may be unable to replace or refund the order.</p>
      <h4>Returns, cancellations and refunds</h4>
      <p>Unless required by law or agreed by Tummly, custom printed materials, used materials, opened materials, digital downloads, self-print assets, credits and services already performed are non-refundable.</p>
      <p>Standard non-custom materials may be returnable only where Tummly agrees in writing and the materials are unused, complete and in a resalable condition.</p>
      <p>If you want to cancel a physical materials order, contact support as soon as possible. Cancellation may not be possible after production, proof approval, dispatch or digital delivery has started.</p>
      <p>Refunds, where approved, may be made to the original payment method or as account credit unless applicable law requires otherwise.</p>
      <h4>QR performance and operator responsibility</h4>
      <p>Shop purchases and QR materials do not guarantee scan volume, feedback submissions, guest opt-ins, offer claims, redemptions, repeat visits, review improvements or revenue.</p>
      <p>You remain responsible for placement, staff training, guest prompts, offer clarity, redemption process, in-store execution, campaign setup and replacing outdated materials.</p>
      <h4>Suspension and misuse</h4>
      <p>Tummly may suspend shop access, cancel fulfilment, disable QR materials or refuse reorders where it reasonably suspects fraud, payment abuse, unauthorised ordering, public review manipulation, unlawful marketing, misleading materials, unsafe use, account compromise or breach of these terms.</p>
      </>
    ),
  }
  ],
}
