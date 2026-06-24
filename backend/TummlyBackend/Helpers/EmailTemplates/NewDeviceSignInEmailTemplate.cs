using System.Net;

namespace TummlyBackend.Helpers.EmailTemplates
{
    public static class NewDeviceSignInEmailTemplate
    {
        public const string Subject =
            "New sign-in to your Tummly account";

        private const string Title = Subject;
        private const string Font = BaseEmailTemplate.FontFamily;

        public static string GenerateBody(
            string firstName,
            string signInTime,
            string deviceSummary,
            string locationSummary
        )
        {
            var safeFirstName = WebUtility.HtmlEncode(firstName);
            var safeSignInTime = WebUtility.HtmlEncode(signInTime);
            var safeDeviceSummary = WebUtility.HtmlEncode(deviceSummary);
            var safeLocationSummary = WebUtility.HtmlEncode(locationSummary);

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
                            Your Tummly account was used to sign in from a new device.
                        </p>

                        <p style='margin:0 0 14px;
                                  font-size:14px;
                                  line-height:20px;
                                  color:#141414;
                                  {Font}'>
                            Time: {safeSignInTime}<br />
                            Device: {safeDeviceSummary}<br />
                            Approximate location: {safeLocationSummary}
                        </p>

                        <p style='margin:0 0 14px;
                                  font-size:14px;
                                  line-height:20px;
                                  color:#141414;
                                  {Font}'>
                            If this was you, no action is needed.
                        </p>

                        <p style='margin:0;
                                  font-size:14px;
                                  line-height:20px;
                                  color:#141414;
                                  {Font}'>
                            If this was not you, reset your password and contact Tummly support.
                        </p>
                    </div>
                </div>";
        }
    }
}
