/** 
  *    This file is part of Y messenger.
  *
  *    Y messenger is free software: you can redistribute it and/or modify
  *    it under the terms of the GNU Affero Public License as published by
  *    the Free Software Foundation, either version 3 of the License, or
  *    (at your option) any later version.
  *
  *    Y messenger is distributed in the hope that it will be useful,
  *    but WITHOUT ANY WARRANTY; without even the implied warranty of
  *    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  *    GNU Affero Public License for more details.
  *
  *    You should have received a copy of the GNU Affero Public License
  *    along with Y messenger.  If not, see <https://www.gnu.org/licenses/>.
  */
using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.Utils;
using System;
using System.IO;
using System.Threading.Tasks;

namespace NodeApp
{
    public static class EmailHandler
    {
        public static async void SendEmailAsync(string destinationEmail, string message)
        {
            try
            {
                var emailMessage = new MimeMessage();
                emailMessage.From.Add(new MailboxAddress("Y-Messenger", NodeSettings.Configs.SmtpClient.Email));
                emailMessage.To.Add(new MailboxAddress(destinationEmail));
                emailMessage.Body = new TextPart(MimeKit.Text.TextFormat.Html)
                {
                    Text = message
                };
                using (var client = new SmtpClient())
                {
                    client.ServerCertificateValidationCallback = delegate { return true; };
                    await client.ConnectAsync(NodeSettings.Configs.SmtpClient.Host, Convert.ToInt32(NodeSettings.Configs.SmtpClient.Port), MailKit.Security.SecureSocketOptions.StartTls).ConfigureAwait(false);
                    await client.AuthenticateAsync(NodeSettings.Configs.SmtpClient.Email, NodeSettings.Configs.SmtpClient.Password).ConfigureAwait(false);
                    await client.SendAsync(emailMessage).ConfigureAwait(false);
                    await client.DisconnectAsync(true).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex, $"{destinationEmail}:'{message}'");
            }
        }
        public static async Task SendEmailWithFileAsync(string receiverEmail, Stream stream, string fileName, string message)
        {
            var builder = new BodyBuilder();            
            var @object = builder.LinkedResources.Add(fileName, stream);
            @object.ContentId = MimeUtils.GenerateMessageId();
            builder.HtmlBody = $@"<p>{message}</p><img src=""cid:{@object.ContentId}"">";            
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress("Y-Messenger", NodeSettings.Configs.SmtpClient.Email));
            emailMessage.To.Add(new MailboxAddress(receiverEmail));
            emailMessage.Body = builder.ToMessageBody();
            using (var client = new SmtpClient())
            {
                client.ServerCertificateValidationCallback = delegate { return true; };
                await client.ConnectAsync(NodeSettings.Configs.SmtpClient.Host, Convert.ToInt32(NodeSettings.Configs.SmtpClient.Port), MailKit.Security.SecureSocketOptions.StartTls).ConfigureAwait(false);
                await client.AuthenticateAsync(NodeSettings.Configs.SmtpClient.Email, NodeSettings.Configs.SmtpClient.Password).ConfigureAwait(false);
                await client.SendAsync(emailMessage).ConfigureAwait(false);
                await client.DisconnectAsync(true).ConfigureAwait(false);
            }
        }
    }
}
