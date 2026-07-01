using System.Net;

namespace TummlyBackend.Helpers.EmailTemplates
{
    public static class TrialRequestReceivedEmailTemplate
    {
        public const string Subject = "We've received your Tummly trial request";

        private const string Title = Subject;
        private const string Font = BaseEmailTemplate.FontFamily;

        public static string GenerateBody(
            string fullName,
            string businessName
        )
        {
            var firstName = ExtractFirstName(fullName);
            var safeFirstName = WebUtility.HtmlEncode(firstName);
            var safeRestaurantName = WebUtility.HtmlEncode(
                string.IsNullOrWhiteSpace(businessName)
                    ? "your restaurant"
                    : businessName.Trim()
            );

            return $@"
                <div style='display:block;{Font}'>
                    <h1 style='margin:0;
                               font-size:30px;
                               font-weight:600;
                               line-height:1.2;
                               color:#141414;
                               {Font}'>
                        {Title}
                    </h1>

                    {BaseEmailTemplate.RenderDivider()}

                    <div style='margin-top:32px;{Font}'>
                        <p style='margin:0 0 14px;
                                  font-size:14px;
                                  line-height:20px;
                                  color:#141414;
                                  {Font}'>
                            Hi {safeFirstName},
                        </p>

                        <p style='margin:0 0 14px;
                                  font-size:14px;
                                  line-height:20px;
                                  color:#141414;
                                  {Font}'>
                            Thanks for requesting a guided Tummly trial for {safeRestaurantName}.
                        </p>

                        <p style='margin:0 0 14px;
                                  font-size:14px;
                                  line-height:20px;
                                  color:#141414;
                                  {Font}'>
                            We&apos;ve received your details and will review your restaurant information before sending the next setup step.
                        </p>

                        <p style='margin:0 0 14px;
                                  font-size:14px;
                                  line-height:20px;
                                  color:#141414;
                                  {Font}'>
                            We usually review requests within 1&ndash;2 working days.
                        </p>

                        <p style='margin:0 0 14px;
                                  font-size:14px;
                                  line-height:20px;
                                  color:#141414;
                                  {Font}'>
                            If your request is approved, we&apos;ll send you a secure setup link so you can create your Tummly workspace and prepare your first guest feedback prompts.
                        </p>

                        <p style='margin:0 0 14px;
                                  font-size:14px;
                                  line-height:20px;
                                  color:#141414;
                                  {Font}'>
                            What happens next
                        </p>

                        <ol style='margin:0 0 14px;
                                   padding:0 0 0 21px;
                                   font-size:14px;
                                   line-height:20px;
                                   color:#141414;
                                   {Font}'>
                            <li style='margin:0 0 0;padding:0;{Font}'>We review your restaurant and contact details.</li>
                            <li style='margin:0 0 0;padding:0;{Font}'>If anything is missing, we may contact you for clarification.</li>
                            <li style='margin:0 0 0;padding:0;{Font}'>If approved, we send your secure setup link.</li>
                            <li style='margin:0 0 0;padding:0;{Font}'>You complete setup for your restaurant or locations.</li>
                            <li style='margin:0 0 0;padding:0;{Font}'>We help prepare your QR guest prompts and onboarding materials.</li>
                        </ol>

                        <p style='margin:0;
                                  font-size:14px;
                                  line-height:20px;
                                  color:#141414;
                                  {Font}'>
                            Thanks,<br />
                            The Tummly team
                        </p>
                    </div>
                </div>";
        }

        private static string ExtractFirstName(string? fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                return "there";
            }

            var firstToken = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];

            return string.IsNullOrWhiteSpace(firstToken) ? "there" : firstToken;
        }
    }
}
