using System.Net;

namespace TummlyBackend.Helpers.EmailTemplates
{
    public static class TeamInvitationEmailTemplate
    {
        public static string Subject(string workspaceName)
        {
            return $"You've been invited to {workspaceName} on Tummly";
        }

        private const string Font = BaseEmailTemplate.FontFamily;

        public static string GenerateBody(
            string greetingName,
            string inviterName,
            string workspaceName,
            string roleName,
            string locationScope,
            string? invitationMessage,
            string acceptUrl
        )
        {
            var greeting = string.IsNullOrWhiteSpace(greetingName)
                ? "Hi,"
                : $"Hi {WebUtility.HtmlEncode(greetingName)},";
            var safeInviter = WebUtility.HtmlEncode(inviterName);
            var safeWorkspace = WebUtility.HtmlEncode(workspaceName);
            var safeRole = WebUtility.HtmlEncode(roleName);
            var safeScope = WebUtility.HtmlEncode(locationScope);
            var safeLink = WebUtility.HtmlEncode(acceptUrl);
            var messageBlock = string.IsNullOrWhiteSpace(invitationMessage)
                ? string.Empty
                : $@"
                        <p style='margin:0 0 14px;
                                  font-size:14px;
                                  line-height:20px;
                                  color:#141414;
                                  {Font}'>
                            {WebUtility.HtmlEncode(invitationMessage)}
                        </p>";

            return $@"
                <div style='display:block;{Font}'>
                    <h1 style='margin:0;
                               font-size:34px;
                               font-weight:500;
                               line-height:42px;
                               color:#141414;
                               text-align:center;
                               {Font}'>
                        {WebUtility.HtmlEncode(Subject(workspaceName))}
                    </h1>

                    {BaseEmailTemplate.RenderDivider()}

                    <div style='margin-top:32px;{Font}'>
                        <p style='margin:0 0 14px;
                                  font-size:14px;
                                  line-height:20px;
                                  color:#141414;
                                  {Font}'>
                            {greeting}
                        </p>
                        <p style='margin:0 0 14px;
                                  font-size:14px;
                                  line-height:20px;
                                  color:#141414;
                                  {Font}'>
                            {safeInviter} has invited you to join {safeWorkspace} on Tummly.
                        </p>
                        <p style='margin:0 0 14px;
                                  font-size:14px;
                                  line-height:20px;
                                  color:#141414;
                                  {Font}'>
                            You'll have access based on the role and Locations assigned to you:
                        </p>
                        <p style='margin:0 0 14px;
                                  font-size:14px;
                                  line-height:20px;
                                  color:#141414;
                                  {Font}'>
                            Role: {safeRole}<br />
                            Location access: {safeScope}
                        </p>
                        {messageBlock}
                        <p style='margin:0 0 14px;
                                  font-size:14px;
                                  line-height:20px;
                                  color:#141414;
                                  {Font}'>
                            Accept your invitation:
                        </p>
                        <div style='margin:0 0 14px;{Font}'>
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
                                Accept invitation
                            </a>
                        </div>
                        <p style='margin:0 0 14px;
                                  font-size:14px;
                                  line-height:20px;
                                  color:#141414;
                                  {Font}'>
                            After accepting, you'll be able to sign in to Tummly and access the areas available to your role.
                        </p>
                        <p style='margin:0 0 14px;
                                  font-size:14px;
                                  line-height:20px;
                                  color:#141414;
                                  {Font}'>
                            If you already have a Tummly account, sign in with the Email address this invitation was sent to. If not, you'll be guided through creating your account.
                        </p>
                        <p style='margin:0 0 14px;
                                  font-size:14px;
                                  line-height:20px;
                                  color:#141414;
                                  {Font}'>
                            If you weren't expecting this invitation, you can ignore this Email.
                        </p>
                        <p style='margin:0;
                                  font-size:14px;
                                  line-height:20px;
                                  color:#141414;
                                  {Font}'>
                            Thanks,<br />
                            Tummly
                        </p>
                    </div>
                </div>";
        }
    }
}
