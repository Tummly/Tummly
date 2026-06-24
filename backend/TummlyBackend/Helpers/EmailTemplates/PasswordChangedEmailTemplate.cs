using System.Net;



namespace TummlyBackend.Helpers.EmailTemplates

{

    public static class PasswordChangedEmailTemplate

    {

        public const string Subject = "Your Tummly password was changed";



        private const string Title = Subject;

        private const string Font = BaseEmailTemplate.FontFamily;

        private const string SupportEmail = "support@tummly.com";



        public static string GenerateBody(string firstName)

        {

            var safeFirstName = WebUtility.HtmlEncode(firstName);



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

                            Your Tummly password was changed.

                        </p>



                        <p style='margin:0 0 14px;

                                  font-size:14px;

                                  line-height:20px;

                                  color:#141414;

                                  {Font}'>

                            If you made this change, no action is needed.

                        </p>



                        <p style='margin:0;

                                  font-size:14px;

                                  line-height:20px;

                                  color:#141414;

                                  {Font}'>

                            If you did not change your password,

                            <a href='mailto:{SupportEmail}'

                               style='color:#141414;text-decoration:underline;{Font}'>

                                contact Tummly support

                            </a>

                            immediately.

                        </p>

                    </div>

                </div>";

        }

    }

}


