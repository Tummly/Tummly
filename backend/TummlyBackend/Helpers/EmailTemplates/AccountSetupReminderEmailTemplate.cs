using System.Globalization;
using System.Net;

namespace TummlyBackend.Helpers.EmailTemplates
{
    public static class AccountSetupReminderEmailTemplate
    {
        public const string Subject =
            "Your Tummly setup link is still waiting";

        private const string Title = Subject;
        private const string Font = BaseEmailTemplate.FontFamily;

        public static string GenerateBody(
            string setupLink,
            DateTime expiresAtUtc
        )
        {
            var safeLink = WebUtility.HtmlEncode(setupLink);
            var expiryLabel = expiresAtUtc.ToString(
                "d MMMM yyyy",
                CultureInfo.InvariantCulture
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
                            Your restaurant has been approved for a guided Tummly trial, but your setup has not been completed yet.
                        </p>

                        <p style='margin:0;
                                  font-size:14px;
                                  line-height:20px;
                                  color:#141414;
                                  {Font}'>
                            Use the secure setup link below to create your account, confirm your restaurant details and prepare your first Guest Loop.
                        </p>

                        <div style='margin-top:22px;{Font}'>
                            <a href='{safeLink}'
                               target='_blank'
                               rel='noopener noreferrer'
                               style='background-color:#14a74a;
                                      color:#ffffff;
                                      display:inline-block;
                                      padding:15px 17px;
                                      border-radius:4px;
                                      font-size:16px;
                                      font-weight:500;
                                      line-height:20px;
                                      text-decoration:none;
                                      text-align:center;
                                      {Font}'>
                                Continue setup
                            </a>

                            <p style='margin:12px 0 0;
                                      font-size:14px;
                                      line-height:20px;
                                      color:#141414;
                                      {Font}'>
                                This link expires on {expiryLabel}. If it expires, you can request a new setup link.
                            </p>
                        </div>
                    </div>
                </div>";
        }
    }
}
