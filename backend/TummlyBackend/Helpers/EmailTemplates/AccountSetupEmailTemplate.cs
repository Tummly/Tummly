using System.Net;

namespace TummlyBackend.Helpers.EmailTemplates
{
    public static class AccountSetupEmailTemplate
    {
        public const string Subject =
            "Create your account and start your Tummly trial";

        private const string Title = Subject;
        private const string Font = BaseEmailTemplate.FontFamily;

        public static string GenerateBody(string setupLink)
        {
            var safeLink = WebUtility.HtmlEncode(setupLink);

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
                            Good news. Your restaurant has been approved for a guided Tummly trial.
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
                                Start trial setup
                            </a>

                            <p style='margin:12px 0 0;
                                      font-size:14px;
                                      line-height:20px;
                                      color:#141414;
                                      {Font}'>
                                This secure link expires in 14 days. If it expires, you can request a new setup link.
                            </p>
                        </div>
                    </div>

                    <div style='margin-top:32px;{Font}'>
                        {BaseEmailTemplate.RenderDivider()}
                    </div>

                    <div style='margin-top:32px;{Font}'>
                        <p style='margin:0 0 12px;
                                  font-size:14px;
                                  font-weight:600;
                                  line-height:20px;
                                  color:#141414;
                                  {Font}'>
                            What happens next
                        </p>

                        <p style='margin:0;
                                  font-size:14px;
                                  line-height:24px;
                                  color:#141414;
                                  {Font}'>
                            1. Create your account.<br />
                            2. Confirm your restaurant details.<br />
                            3. Open your Tummly workspace.
                        </p>
                    </div>
                </div>";
        }
    }
}
