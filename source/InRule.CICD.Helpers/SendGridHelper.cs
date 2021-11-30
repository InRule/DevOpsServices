using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace InRule.CICD.Helpers
{
    public class SendGridHelper
    {
        private static readonly string moniker = "Email";
        //static string SendFromEmail = SettingsManager.Get($"{moniker}.EmailSendFrom");
        //static string SendFromName = SettingsManager.Get($"{moniker}.EmailSendFromName");
        //static string SendToEmail = SettingsManager.Get($"{moniker}.EmailSendTo");
        //static string SendToName = SettingsManager.Get($"{moniker}.EmailSendToName");
        //static string ApiKey = SettingsManager.Get($"{moniker}.SendGridApiKey");
        //static SendGridClient Client = new SendGridClient(ApiKey);
        //static EmailAddress From = new EmailAddress(SendFromEmail, SendFromName);
        public static async Task SendEmail(string subject, string plainTextContent, string htmlContent)
        {
            await SendEmail(subject, plainTextContent, htmlContent, moniker);
        }

        public static async Task SendEmail(string subject, string plainTextContent, string htmlContent, string moniker)
        {
            string SendFromEmail = SettingsManager.Get($"{moniker}.EmailSendFrom");
            string SendFromName = SettingsManager.Get($"{moniker}.EmailSendFromName");
            string SendToEmail = SettingsManager.Get($"{moniker}.EmailSendTo");
            string SendToName = SettingsManager.Get($"{moniker}.EmailSendToName");
            string ApiKey = SettingsManager.Get($"{moniker}.SendGridApiKey");
            SendGridClient Client = new SendGridClient(ApiKey);
            EmailAddress From = new EmailAddress(SendFromEmail, SendFromName);

            if (ApiKey.Length == 0 || SendFromEmail.Length == 0 || SendToEmail.Length == 0)
                return;

            SendGridMessage msg;

            if (SendToEmail.Contains(","))
            {
                var emails = SendToEmail.Split(',');
                var emailsList = new List<EmailAddress>();
                var subjects = new List<string>();
                var substitutions = new List<Dictionary<string, string>>();

                foreach (var email in emails)
                {
                    emailsList.Add(new EmailAddress(email.Trim()));
                    subjects.Add(subject);
                    var sub = new Dictionary<string, string>();
                    sub.Add("zzz", "zzz");
                    substitutions.Add(sub);
                }
                msg = MailHelper.CreateMultipleEmailsToMultipleRecipients(From, emailsList, subjects, plainTextContent, htmlContent, substitutions);
            }
            else
            {
                var to = new EmailAddress(SendToEmail, SendToName);
                msg = MailHelper.CreateSingleEmail(From, to, subject, plainTextContent, htmlContent);
            }

            var response = await Client.SendEmailAsync(msg).ConfigureAwait(false); ;
        }

        public static async Task SendEventToEmailAsync(string eventType, object data, string subjectSuffix, string htmlContent = "")
        {
            await SendEventToEmailAsync(eventType, data, subjectSuffix, moniker, htmlContent = "");
        }

        public static async Task SendEventToEmailAsync(string eventType, object data, string subjectSuffix, string moniker, string htmlContent = "")
        {
            try
            {
                var map = data as IDictionary<string, object>;

                if (htmlContent == "")
                    htmlContent = GetHtmlForEventData(data, string.Empty, string.Empty);

                await SendGridHelper.SendEmail($"{((dynamic)data).OperationName} by user {((dynamic)data).RequestorUsername}{subjectSuffix}", string.Empty, htmlContent, moniker);
            }
            catch (Exception ex)
            {
                await NotificationHelper.NotifyAsync($"Error writing {eventType} event out to email: {ex.Message}", "PUBLISH TO EMAIL", "Debug");
            }
        }

        public static string GetHtmlForEventData(object data, string appendPrefix, string appendSuffix)
        {
            var map = data as IDictionary<string, object>;
            StringBuilder sb = new StringBuilder();
            sb.Append("<table>");

            if (appendPrefix.Length > 0)
                sb.Append($"<tr><td>{appendPrefix}</td></tr>");

            if (map.ContainsKey("OperationName"))
                sb.Append($"<tr><td><b>{((dynamic)data).OperationName} by user</b> {((dynamic)data).RequestorUsername}</td></tr>");

            var repositoryUri = ((dynamic)data).RepositoryUri;
            sb.Append($"<tr><td><b>Catalog:</b> <a href='{repositoryUri}'>{repositoryUri}</a></td></tr>");

            string repositoryManagerUri = repositoryUri.Replace(repositoryUri.Substring(repositoryUri.LastIndexOf('/')), "/InRuleCatalogManager");
            sb.Append($"<tr><td><b>Catalog Manager (likely location):</b> <a href='{repositoryManagerUri}'>{repositoryManagerUri}</a></td></tr>");

            if (map.ContainsKey("Name"))
                sb.Append($"<tr><td><b>Rule application:</b> {((dynamic)data).Name}</td></tr>");

            if (map.ContainsKey("RuleAppRevision"))
                sb.Append($"<tr><td><b>Revision:</b> {((dynamic)data).RuleAppRevision}</td></tr>");

            if (map.ContainsKey("Label"))
                sb.Append($"<tr><td><b>Label:</b> { ((dynamic)data).Label}</td></tr>");

            if (map.ContainsKey("Comment"))
                sb.Append($"<tr><td><b>Comment:</b> { ((dynamic)data).Comment}</td></tr>"); ;

            if (appendSuffix.Length > 0)
                sb.Append($"<br><tr><td>{appendSuffix}</td></tr>");

            sb.Append("</table>");
            return sb.ToString();
        }
    }
}
