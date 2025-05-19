using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using MailKit.Net.Smtp;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using MimeKit;
using Newtonsoft.Json.Linq;

public class EmailChatbotFunction
{
    private readonly ILogger _logger;

    public EmailChatbotFunction(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<EmailChatbotFunction>();
    }

    [Function("EmailChatbotFunction")]
    public async Task Run([TimerTrigger("0 */5 * * * *")] MyInfo myTimer, FunctionContext context)
    {
        var log = _logger;
        var imapHost = Environment.GetEnvironmentVariable("IMAP_Host");
        var imapPort = int.Parse(Environment.GetEnvironmentVariable("IMAP_Port") ?? "993");
        var imapUser = Environment.GetEnvironmentVariable("IMAP_Username");
        var imapPass = Environment.GetEnvironmentVariable("IMAP_Password");

        var smtpHost = Environment.GetEnvironmentVariable("SMTP_Host");
        var smtpPort = int.Parse(Environment.GetEnvironmentVariable("SMTP_Port") ?? "587");
        var smtpUser = Environment.GetEnvironmentVariable("SMTP_Username");
        var smtpPass = Environment.GetEnvironmentVariable("SMTP_Password");

        var openAiEndpoint = Environment.GetEnvironmentVariable("OPENAI_Endpoint");
        var openAiKey = Environment.GetEnvironmentVariable("OPENAI_Key");

        log.LogInformation($"EmailChatbotFunction running at: {DateTime.Now}");

        try
        {
            using var imapClient = new ImapClient();
            await imapClient.ConnectAsync(imapHost, imapPort, SecureSocketOptions.SslOnConnect);
            await imapClient.AuthenticateAsync(imapUser, imapPass);

            var inbox = imapClient.Inbox;
            await inbox.OpenAsync(MailKit.FolderAccess.ReadWrite);

            var results = await inbox.SearchAsync(SearchQuery.NotSeen);
            log.LogInformation($"Found {results.Count} unread messages.");

            foreach (var uid in results)
            {
                var message = await inbox.GetMessageAsync(uid);

                log.LogInformation($"Processing email from: {message.From} Subject: {message.Subject}");

                var senderEmail = message.From.Mailboxes.FirstOrDefault()?.Address;
                var questionText = message.TextBody ?? message.HtmlBody ?? "";

                if (string.IsNullOrWhiteSpace(senderEmail))
                {
                    log.LogWarning("Email has no sender address, skipping.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(questionText))
                {
                    log.LogWarning("Email body is empty, skipping.");
                    continue;
                }

                var answer = await GetOpenAIResponse(questionText, openAiEndpoint, openAiKey, log);

                if (string.IsNullOrWhiteSpace(answer))
                {
                    answer = "Sorry, I couldn't generate a response at this time.";
                }

                await SendReplyEmail(smtpHost, smtpPort, smtpUser, smtpPass, imapUser, senderEmail, $"Re: {message.Subject}", answer, log);

                await inbox.AddFlagsAsync(uid, MailKit.MessageFlags.Seen, true);

                log.LogInformation($"Replied to {senderEmail}");
            }

            await imapClient.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            log.LogError($"Error in EmailChatbotFunction: {ex}");
        }
    }

    private static async Task<string> GetOpenAIResponse(string question, string endpoint, string apiKey, ILogger log)
    {
        try
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var payload = new
            {
                messages = new[]
                {
                    new { role = "user", content = question }
                }
            };

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(endpoint, content);

            if (!response.IsSuccessStatusCode)
            {
                log.LogWarning($"OpenAI API returned status {response.StatusCode}");
                return null;
            }

            var responseString = await response.Content.ReadAsStringAsync();
            var jsonResponse = JObject.Parse(responseString);

            var answer = jsonResponse["choices"]?[0]?["message"]?["content"]?.ToString();

            return answer?.Trim();
        }
        catch (Exception e)
        {
            log.LogError($"Error calling OpenAI API: {e}");
            return null;
        }
    }

    private static async Task SendReplyEmail(string smtpHost, int smtpPort, string smtpUser, string smtpPass, string fromEmail, string toEmail, string subject, string body, ILogger log)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(fromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            message.Body = new TextPart("plain")
            {
                Text = body
            };

            using var smtpClient = new SmtpClient();
            await smtpClient.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.SslOnConnect);
            await smtpClient.AuthenticateAsync(smtpUser, smtpPass);
            await smtpClient.SendAsync(message);
            await smtpClient.DisconnectAsync(true);

            log.LogInformation($"Sent reply to {toEmail}");
        }
        catch (Exception e)
        {
            log.LogError($"Failed to send email to {toEmail}: {e}");
        }
    }
}

public class MyInfo
{
    public MyScheduleStatus ScheduleStatus { get; set; }
    public bool IsPastDue { get; set; }
}

public class MyScheduleStatus
{
    public DateTime Last { get; set; }
    public DateTime Next { get; set; }
    public DateTime LastUpdated { get; set; }
}