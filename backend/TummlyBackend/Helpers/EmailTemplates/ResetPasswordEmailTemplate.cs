using System.Net;



namespace TummlyBackend.Helpers.EmailTemplates

{

    public static class ResetPasswordEmailTemplate

    {

        public const string Subject = "Reset your Tummly password";



        private const string Title = Subject;

        private const string Font = BaseEmailTemplate.FontFamily;

        private const string SupportEmail = "support@tummly.com";

        private const string ExpiryText = "This link expires in 30 minutes.";



        public static string GenerateBody(string resetLink)

        {

            var safeLink = WebUtility.HtmlEncode(resetLink);



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

                        We received a request to reset the password for your Tummly account.

                    </p>



                    <div style='margin-bottom:32px;{Font}'>

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

                            Reset password

                        </a>



                        <p style='margin:12px 0 0;

                                  font-size:14px;

                                  line-height:20px;

                                  color:#141414;

                                  {Font}'>

                            {ExpiryText}

                        </p>

                    </div>



                    <p style='margin:0 0 14px;

                              font-size:14px;

                              line-height:20px;

                              color:#141414;

                              {Font}'>

                        If you did not request a password reset, ignore this email or

                        <a href='mailto:{SupportEmail}'

                           style='color:#141414;text-decoration:underline;{Font}'>

                            contact Tummly support

                        </a>.

                    </p>



                    <p style='margin:0;

                              font-size:14px;

                              line-height:20px;

                              color:#141414;

                              {Font}'>

                        Do not reply to this automated email.

                    </p>

                </div>";

        }

    }

}


