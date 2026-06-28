using System.Net;

namespace TummlyBackend.Helpers.EmailTemplates
{
    public static class TrialMoreInfoEmailTemplate
    {
        public const string Subject = "Action required: Tummly trial request";

        private const string Title = "More information needed";
        private const string Font = BaseEmailTemplate.FontFamily;
        private const string FeedbackHeading = "What we need from you";

        public static string GenerateBody(
            string fullName,
            string? moreInfoMessage = null
        )
        {
            var firstName = ExtractFirstName(fullName);
            var safeFirstName = WebUtility.HtmlEncode(firstName);
            var feedbackBlock = string.IsNullOrWhiteSpace(moreInfoMessage)
                ? RenderLegacyDetailsRequest()
                : BaseEmailTemplate.RenderAdminFeedbackBlock(
                    FeedbackHeading,
                    moreInfoMessage
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
                            Our team needs a few more details before activating your trial.
                        </p>

                        {feedbackBlock}

                        <p style='margin:0;
                                  font-size:14px;
                                  line-height:20px;
                                  color:#141414;
                                  {Font}'>
                            We will process your application as soon as you reply.
                        </p>
                    </div>
                </div>";
        }

        private static string RenderLegacyDetailsRequest()
        {
            return $@"
                        <p style='margin:0 0 14px;
                                  font-size:14px;
                                  line-height:20px;
                                  color:#141414;
                                  {Font}'>
                            Reply to this email with your restaurant&apos;s physical address or business registration details.
                        </p>";
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
