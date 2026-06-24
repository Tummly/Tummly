using System.Net;

namespace TummlyBackend.Helpers.EmailTemplates
{
    public static class OtpEmailTemplate
    {
        public const string Subject =
            "Your Tummly verification code";

        private const string Title = Subject;
        private const string Font = BaseEmailTemplate.FontFamily;
        private const string ExpiryText =
            "This code expires in 10 minutes.";

        public static string GenerateBody(string otp)
        {
            var safeOtp = WebUtility.HtmlEncode(otp);

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

                    <p style='margin:32px 0;
                              font-size:14px;
                              line-height:20px;
                              color:#141414;
                              {Font}'>
                        Use this code to finish signing in to Tummly:
                    </p>

                    <p style='margin:0 0 32px;
                              font-size:30px;
                              font-weight:600;
                              line-height:28px;
                              color:#141414;
                              letter-spacing:0.02em;
                              {Font}'>
                        {safeOtp}
                    </p>

                    <p style='margin:0;
                              font-size:14px;
                              line-height:20px;
                              color:#141414;
                              {Font}'>
                        {ExpiryText}
                    </p>
                </div>";
        }
    }
}
