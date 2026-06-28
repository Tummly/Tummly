using System.Net;

namespace TummlyBackend.Helpers.EmailTemplates
{
    public static class TrialDeclineEmailTemplate
    {
        public const string Subject = "Update on your Tummly trial request";

        private const string Title = "Trial request update";
        private const string Font = BaseEmailTemplate.FontFamily;
        private const string SupportEmail = "support@tummly.com";
        private const string FeedbackHeading = "Reason";

        public static string GenerateBody(
            string fullName,
            string? declineReason = null
        )
        {
            var firstName = ExtractFirstName(fullName);
            var safeFirstName = WebUtility.HtmlEncode(firstName);
            var feedbackBlock = string.IsNullOrWhiteSpace(declineReason)
                ? string.Empty
                : BaseEmailTemplate.RenderAdminFeedbackBlock(
                    FeedbackHeading,
                    declineReason
                );

            return $@"
                <div style='display:block;{Font}'>
                    <h1 style='margin:0 0 32px;
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
                            Thank you for your interest in Tummly. Unfortunately, your trial request cannot be approved at this time.
                        </p>

                        {feedbackBlock}

                        <p style='margin:0;
                                  font-size:14px;
                                  line-height:20px;
                                  color:#141414;
                                  {Font}'>
                            If you have questions,
                            <a href='mailto:{SupportEmail}'
                               style='color:#141414;text-decoration:underline;{Font}'>
                                contact support
                            </a>.
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
