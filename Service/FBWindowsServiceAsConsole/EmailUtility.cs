using Azure.Identity;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Mail;
using System.Net.Mime;
using LinkedResource = System.Net.Mail.LinkedResource;

namespace FBWindowsServiceAsConsole
{
    public class EmailUtility
    {
        public string SendEmail(string fromAddress, string toAddress, string CcAddress, string subject, string message)
        {
            try
            {

                string tenanatID = ConfigurationManager.AppSettings["Email_TenantId"].ToString();
                string clientID = ConfigurationManager.AppSettings["Email_ClientId"].ToString();
                string clientSecret = ConfigurationManager.AppSettings["Email_ClientSecret"].ToString();
                System.Net.ServicePointManager.SecurityProtocol = (System.Net.SecurityProtocolType)3072;
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

                var credentials = new ClientSecretCredential(
                                    tenanatID, clientID, clientSecret,
                                new TokenCredentialOptions { AuthorityHost = AzureAuthorityHosts.AzurePublicCloud });
                GraphServiceClient graphServiceClient = new GraphServiceClient(credentials);


                string[] toMail = toAddress.Split(';');
                List<Recipient> toRecipients = new List<Recipient>();
                int i = 0;
                for (i = 0; i < toMail.Count(); i++)
                {
                    if (string.IsNullOrEmpty(toMail[i])) continue;

                    Recipient toRecipient = new Recipient();
                    EmailAddress toEmailAddress = new EmailAddress();

                    toEmailAddress.Address = toMail[i];
                    toRecipient.EmailAddress = toEmailAddress;
                    toRecipients.Add(toRecipient);
                }

                List<Recipient> ccRecipients = new List<Recipient>();
                if (!string.IsNullOrEmpty(CcAddress))
                {
                    string[] ccMail = CcAddress.Split(';');
                    int j = 0;
                    for (j = 0; j < ccMail.Count(); j++)
                    {
                        if (string.IsNullOrEmpty(ccMail[j])) continue;

                        Recipient ccRecipient = new Recipient();
                        EmailAddress ccEmailAddress = new EmailAddress();

                        ccEmailAddress.Address = ccMail[j];
                        ccRecipient.EmailAddress = ccEmailAddress;
                        ccRecipients.Add(ccRecipient);
                    }
                }

                string contentId = Guid.NewGuid().ToString();
                string logoPath = System.AppDomain.CurrentDomain.BaseDirectory.ToString() + "img\\mainlogo.jpg";

                message = message.Replace("cid:logo", "cid:" + contentId);

                byte[] imageArray = System.IO.File.ReadAllBytes(logoPath);
                var attachments = new MessageAttachmentsCollectionPage()
                {
                    new FileAttachment{
                        ContentType= "image/jpeg",
                        ContentBytes = imageArray,
                        ContentId = contentId,
                        Name= "test-image"
                    }
                };

                string replyToAddress = ConfigurationManager.AppSettings["DispatchMailReplyToAddress"].ToString();
                List<Recipient> replyToRecipients = new List<Recipient>();
                if (!string.IsNullOrEmpty(replyToAddress))
                {
                    string[] replyToMail = replyToAddress.Split(';');
                    int j = 0;
                    for (j = 0; j < replyToMail.Count(); j++)
                    {
                        if (string.IsNullOrEmpty(replyToMail[j])) continue;

                        Recipient replyToRecipient = new Recipient();
                        EmailAddress replyToEmailAddress = new EmailAddress();

                        replyToEmailAddress.Address = replyToMail[j];
                        replyToRecipient.EmailAddress = replyToEmailAddress;
                        replyToRecipients.Add(replyToRecipient);
                    }
                }

                var mailMessage = new Message
                {
                    Subject = subject,

                    Body = new ItemBody
                    {
                        ContentType = BodyType.Html,
                        Content = message
                    },
                    ToRecipients = toRecipients,
                    CcRecipients = ccRecipients,
                    Attachments = attachments,
                    ReplyTo = replyToRecipients

                };
                // Send mail as the given user. 
                graphServiceClient
                   .Users[fromAddress]
                    .SendMail(mailMessage, true)
                    .Request()
                    .PostAsync().Wait();

                return "Email successfully sent.";

            }
            catch (Exception ex)
            {

                return "Send Email Failed.\r\n" + ex.Message;
            }
        }

    }
}
