import { LegalDocLink } from "./LegalDocLink"
import { LEGAL_ROUTES } from "@/constants/legalRoutes"
import type { LegalPageContent } from "./types"

export const cookiePolicyContent: LegalPageContent = {
  title: "Cookie Policy",
  description: "This Cookie Policy explains how Tummly.com Limited uses cookies and similar storage or access technologies when you visit Tummly.com, use the operator dashboard, open a guest page through a Smart Guest Link, use the help centre, interact with dashboard shop or checkout flows, or receive certain Tummly emails and messages. Effective date: 9 July 2026",
  documentKey: "cookie-policy",
  sections: [
  {
    id: "who-we-are-and-how-to-contact-us",
    title: "1. Who we are and how to contact us",
    content: (
      <>
      <p>Tummly is operated by Tummly.com Limited, a company registered in England and Wales under company number 16236040. Our registered office is 71-75 Shelton Street, Covent Garden, London, WC2H 9JQ, United Kingdom.</p>
      <p>You can contact us about <LegalDocLink to={LEGAL_ROUTES.cookiePolicy}>this Cookie Policy</LegalDocLink> at compliance@tummly.com. For account or support questions, please use the support route shown on the Tummly website or in your account where available.</p>
      </>
    ),
  },
  {
    id: "what-cookies-and-similar-technologies-are",
    title: "2. What cookies and similar technologies are",
    content: (
      <>
      <p>Cookies are small text files placed on your browser or device. They can help a website remember information about your visit, keep you signed in, protect accounts, measure usage, remember preferences, or support checkout and security flows.</p>
      <p>This policy also covers similar storage and access technologies. These may include local storage, session storage, scripts, tags, pixels, web beacons, device or browser identifiers, link decoration, campaign parameters, embedded third-party tools and similar technologies that store information on, or access information from, your device.</p>
      <p>Some of these technologies are set directly by Tummly. Others may be set by service providers acting for us, such as analytics, hosting, security, payment, support or fulfilment providers.</p>
      </>
    ),
  },
  {
    id: "where-this-policy-applies",
    title: "3. Where this policy applies",
    content: (
      <>
      <p>This policy applies when you use or interact with:</p>
      <ul>
        <li>the Tummly public website and legal pages</li>
        <li>trial request and account setup flows</li>
        <li>the operator dashboard and related account areas</li>
        <li>Smart Guest Links, QR-led guest pages and guest feedback forms</li>
        <li>the help centre, contact routes and support tools</li>
        <li>dashboard shop, checkout, billing, fulfilment and reorder flows where available</li>
        <li>Tummly emails, SMS or other supported messages that contain tracking links or similar technologies.</li>
      </ul>
      <p>Your cookie choices may apply only to the browser, device, account session, domain or subdomain where they are made. If you use another browser, device or domain, you may need to set your choices again.</p>
      </>
    ),
  },
  {
    id: "why-tummly-uses-these-technologies",
    title: "4. Why Tummly uses these technologies",
    content: (
      <>
      <p>Tummly may use cookies and similar technologies to:</p>
      <ul>
        <li>operate the website, dashboard, guest pages and help centre</li>
        <li>keep accounts secure and support sign-in, one-time passcodes, trusted device controls and fraud prevention</li>
        <li>remember your cookie preferences and other basic choices</li>
        <li>maintain basket, checkout, billing, fulfilment and shop flows where available</li>
        <li>protect guest forms and QR pages from abuse, spam and excessive submissions</li>
        <li>measure how visitors, operators and guests use Tummly, where permitted</li>
        <li>understand trial request, setup, QR scan, guest feedback, offer, campaign and shop performance</li>
        <li>improve product quality, support, onboarding and user experience</li>
        <li>support help centre search, contact routes, support widgets and issue diagnostics</li>
        <li>support future marketing or advertising technologies only where introduced and where required consent or another lawful route is in place.</li>
      </ul>
      <p>Where a cookie or similar technology involves personal data, <LegalDocLink to={LEGAL_ROUTES.privacy}>our Privacy Notice</LegalDocLink> explains how we use that personal data, our lawful bases, retention, sharing and your privacy rights.</p>
      </>
    ),
  },
  {
    id: "your-choices-and-consent",
    title: "5. Your choices and consent",
    content: (
      <>
      <p>Strictly necessary technologies are always on because they are needed to provide the service, protect accounts, remember your cookie choice, secure guest pages, process checkout activity or provide a feature you have requested.</p>
      <p>For non-essential technologies, Tummly will ask for consent where required. This includes analytics, performance, marketing and certain support or personalisation technologies unless an applicable legal exception allows their use and any required objection or control mechanism is provided.</p>
      <p>When the cookie banner or settings control is shown, you should be able to:</p>
      <ul>
        <li>accept all optional technologies</li>
        <li>reject non-essential technologies</li>
        <li>manage choices by category where available</li>
        <li>save your choices</li>
        <li>change or withdraw your choices later.</li>
      </ul>
      <p>Rejecting non-essential technologies will not prevent you from using the core Tummly service. Some analytics, support, personalisation, measurement, marketing or future features may be unavailable or less tailored.</p>
      <p>You can also manage cookies through your browser settings. Browser controls may block or delete some technologies, but blocking strictly necessary technologies may affect sign-in, account security, checkout, dashboard use or guest form operation.</p>
      </>
    ),
  },
  {
    id: "cookie-and-technology-categories",
    title: "6. Cookie and technology categories",
    content: (
      <>
      <p>Tummly groups cookies and similar technologies into categories: strictly necessary; functional / preferences; analytics / performance; support / communication; payment / shop security; and marketing / advertising.</p>
      <p>Strictly necessary technologies stay on because they are required for core operation, security, consent records, sign-in, guest form protection or checkout. Non-essential categories generally need consent where the law requires it, unless an applicable exception applies.</p>
      <p>The full category table, including purpose and consent position for each row, is in the downloadable <LegalDocLink to={LEGAL_ROUTES.cookiePolicy}>Cookie Policy</LegalDocLink>.</p>
      </>
    ),
  },
  {
    id: "current-and-planned-cookie-list",
    title: "7. Current and planned cookie list",
    content: (
      <>
      <p>The table below describes the main cookies and similar technologies Tummly uses or may use as part of the Tummly service. Exact names, providers and durations may vary as the service develops, as providers update their technologies, or where a feature is not enabled for all users. We review this list periodically and update it when we become aware of material changes.</p>
      <p>Tummly uses or may use technologies such as cookie preference records; session, authentication and account security; one-time passcode and trusted-device controls; CSRF, rate-limit and guest form protection; workspace and dashboard preference storage; help centre and support preferences; analytics page-view and event measurement; QR scan and guest journey measurement; email and message delivery technologies; dashboard shop, basket and checkout technologies; payment provider technologies; error monitoring and diagnostics; and marketing or advertising technologies if introduced later.</p>
      <p>Exact names, providers, types and durations may vary as the service develops. The full technology list is in the downloadable <LegalDocLink to={LEGAL_ROUTES.cookiePolicy}>Cookie Policy</LegalDocLink>.</p>
      </>
    ),
  },
  {
    id: "analytics-qr-journeys-and-product-measurement",
    title: "8. Analytics, QR journeys and product measurement",
    content: (
      <>
      <p>Tummly uses, or may use, analytics and measurement technologies to understand how the service performs. This may include page views, trial request steps, OTP verification, operator setup, account activation, QR scan journeys, guest form completion, offer and campaign performance, dashboard usage, help centre searches and shop conversion.</p>
      <p>Analytics information helps us improve Tummly, detect product issues, understand what operators need, measure feature adoption, reduce support friction and create aggregated or anonymised service insights. We do not use analytics to sell identifiable guest contact data or share identifiable guest lists across restaurants.</p>
      <p>Where analytics uses cookies or similar technologies that require consent, we will not use those optional analytics technologies unless you have accepted them or unless an applicable legal exception allows use and the required controls are provided.</p>
      </>
    ),
  },
  {
    id: "dashboard-shop-checkout-and-fulfilment-technologies",
    title: "9. Dashboard shop, checkout and fulfilment technologies",
    content: (
      <>
      <p>If dashboard shop, checkout, billing or QR material fulfilment features are available, Tummly and its providers may use technologies needed to maintain basket state, process payment, prevent fraud, confirm orders, generate invoices, manage delivery, support refunds, handle damaged or missing materials and provide fulfilment updates.</p>
      <p>Some checkout and payment technologies are strictly necessary for the transaction or for payment security. Payment providers may also set their own cookies or similar technologies, particularly where checkout is embedded or hosted by that provider. Their own privacy and cookie notices may also apply.</p>
      </>
    ),
  },
  {
    id: "support-help-centre-and-communication-technologies",
    title: "10. Support, help centre and communication technologies",
    content: (
      <>
      <p>Tummly may use support, help centre and communication technologies to help users find answers, contact support, diagnose account issues, route tickets, monitor service problems and improve support quality.</p>
      <p>Emails, SMS or other supported messages may contain tracking links or similar technologies that help us understand delivery, bounces, clicks, unsubscribes, errors, abuse and campaign performance. Where this involves personal data, it is handled as described in <LegalDocLink to={LEGAL_ROUTES.privacy}>our Privacy Notice</LegalDocLink>.</p>
      </>
    ),
  },
  {
    id: "third-party-technologies",
    title: "11. Third-party technologies",
    content: (
      <>
      <p>Tummly may work with third-party providers for hosting, security, analytics, support, email, SMS, payment, billing, fulfilment, monitoring, AI assistance and other service functions. These providers may set or access cookies and similar technologies where needed for the relevant feature.</p>
      <p>Where we ask for consent for third-party technologies, we aim to provide clear information about the third party and the purpose before the technology is used. Where a third-party technology is strictly necessary for a feature you request, such as checkout security, it may be used without optional-cookie consent.</p>
      <p>Tummly is not responsible for cookie or tracking practices on third-party websites that we link to, including payment-provider, courier, support or partner websites. Those websites should provide their own notices and controls.</p>
      </>
    ),
  },
  {
    id: "browser-controls-withdrawal-and-deleting-cookies",
    title: "12. Browser controls, withdrawal and deleting cookies",
    content: (
      <>
      <p>You can change or withdraw cookie choices through Tummly’s cookie controls where available. You can also block or delete cookies through your browser settings. Browser controls are usually specific to the browser and device you are using.</p>
      <p>Withdrawing consent stops future optional use from that point, but it does not automatically delete information already collected before withdrawal where we have a lawful reason to keep it. Deleting cookies or local storage may remove your saved preferences and may require you to set choices again.</p>
      <p>Blocking all cookies or storage may affect sign-in, account security, dashboard routing, guest forms, checkout, support tools, or other core service functions.</p>
      </>
    ),
  },
  {
    id: "changes-to-this-policy",
    title: "13. Changes to this policy",
    content: (
      <>
      <p>We may update <LegalDocLink to={LEGAL_ROUTES.cookiePolicy}>this Cookie Policy</LegalDocLink> from time to time to reflect changes in law, guidance, technology, providers, product features or our use of cookies and similar technologies. The latest version will be posted on Tummly.com with an updated effective date or last reviewed date.</p>
      <p>If we introduce material new non-essential technologies, such as targeted advertising or retargeting technologies, we will update this policy and request consent where required before using them.</p>
      </>
    ),
  }
  ],
}
