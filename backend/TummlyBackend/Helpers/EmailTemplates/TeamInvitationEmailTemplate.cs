using System.Net;
using System.Text.RegularExpressions;

namespace TummlyBackend.Helpers.EmailTemplates
{
    /// <summary>
    /// Team invite HTML from React Email
    /// (<c>emails/emails/team-invitation.tsx</c> →
    /// <c>Assets/emails/templates/team-invitation.html</c>).
    /// Re-export with <c>npm run email:export</c> after template edits.
    /// </summary>
    public static class TeamInvitationEmailTemplate
    {
        private const string TemplateRelativePath =
            "Assets/emails/templates/team-invitation.html";

        private static readonly Regex InvitationMessageParagraph = new(
            @"<p[^>]*data-slot=[""']invitation-message[""'][^>]*>[\s\S]*?</p>",
            RegexOptions.CultureInvariant | RegexOptions.Compiled
        );

        private static string? _templateHtml;

        public static string Subject(string workspaceName)
        {
            return $"You've been invited to {workspaceName} on Tummly";
        }

        /// <summary>
        /// Full HTML document (not a body fragment).
        /// </summary>
        public static string Generate(
            IWebHostEnvironment environment,
            string greetingName,
            string inviterName,
            string workspaceName,
            string roleName,
            string locationScope,
            string? invitationMessage,
            string acceptUrl,
            string helpCentreUrl,
            string logoUrl
        )
        {
            var html = LoadTemplate(environment);

            var greetingLine = string.IsNullOrWhiteSpace(greetingName)
                ? "Hi,"
                : $"Hi {WebUtility.HtmlEncode(greetingName.Trim())},";

            html = html
                .Replace("{{greeting_line}}", greetingLine, StringComparison.Ordinal)
                .Replace(
                    "{{inviter_name}}",
                    WebUtility.HtmlEncode(inviterName),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{workspace_name}}",
                    WebUtility.HtmlEncode(workspaceName),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{role_name}}",
                    WebUtility.HtmlEncode(roleName),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{location_scope}}",
                    WebUtility.HtmlEncode(locationScope),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{accept_url}}",
                    WebUtility.HtmlEncode(acceptUrl),
                    StringComparison.Ordinal
                )
                .Replace(
                    "{{help_centre_url}}",
                    WebUtility.HtmlEncode(helpCentreUrl),
                    StringComparison.Ordinal
                )
                .Replace("{{logo_url}}", logoUrl, StringComparison.Ordinal);

            if (string.IsNullOrWhiteSpace(invitationMessage))
            {
                html = InvitationMessageParagraph.Replace(html, string.Empty);
            }
            else
            {
                html = html.Replace(
                    "{{invitation_message}}",
                    WebUtility.HtmlEncode(invitationMessage.Trim()),
                    StringComparison.Ordinal
                );
            }

            return html;
        }

        private static string LoadTemplate(IWebHostEnvironment environment)
        {
            if (_templateHtml != null)
            {
                return _templateHtml;
            }

            var path = Path.Combine(
                environment.ContentRootPath,
                TemplateRelativePath.Replace('/', Path.DirectorySeparatorChar)
            );

            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    "Team invitation email template was not found. Run `npm run email:export` from the repo root.",
                    path
                );
            }

            _templateHtml = File.ReadAllText(path);
            return _templateHtml;
        }
    }
}
