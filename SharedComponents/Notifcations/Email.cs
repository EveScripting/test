/*
 * Created by SharpDevelop.
 * User: duketwo
 * Date: 17.09.2016
 * Time: 13:28
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Threading;
using SharedComponents.Events;
using SharedComponents.EVE;

namespace SharedComponents.Notifcations
{
    /// <summary>
    ///     Description of Email.
    /// </summary>
    public class Email
    {
        private static int THROTTLE_DELAY = 240;
        private static DateTime LAST_THROTTLED_EMAIL_SENT_ON = DateTime.MinValue;

        public Email()
        {
        }

        public static void onNewDirectEvent(string charName, DirectEvent directEvent)
        {
            var emailSettingsAvailable = !String.IsNullOrWhiteSpace(Cache.Instance.EveSettings.GmailPassword) &&
                                         !String.IsNullOrWhiteSpace(Cache.Instance.EveSettings.GmailUser) &&
                                         !String.IsNullOrWhiteSpace(Cache.Instance.EveSettings.ReceiverEmailAddress);

            if (directEvent.warning && emailSettingsAvailable)
            {
                var subject = "EVESharp Event: " + charName + " " + directEvent.type.ToString();
                var message = directEvent.message;
                new Thread(delegate()
                {
                    try
                    {
                        SendGmailThrottled(Cache.Instance.EveSettings.GmailPassword, Cache.Instance.EveSettings.GmailUser,
                            Cache.Instance.EveSettings.ReceiverEmailAddress, subject, message);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e);
                        Cache.Instance.Log(e.ToString());
                    }
                }).Start();
            }
        }

        public static void SendEmail(string smtpHost, int port, string password, string from, string to, string subject, string message, bool ssl = true)
        {
            try
            {
                var mail = new MailMessage(from, to);
                mail.Subject = subject;
                mail.Body = message;
                var client = new SmtpClient();
                client.Port = port;
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(from, password);
                client.Host = smtpHost;

                if (ssl)
                    client.EnableSsl = true;
                client.Send(mail);
            }

            catch (Exception e)
            {
                Cache.Instance.Log(e.ToString());
            }
        }

        public static void SendGmail(string password, string from, string to, string subject, string message)
        {
            SendEmail("smtp.gmail.com", 587, password, from, to, subject, message, true);
        }

        public static void SendGmailThrottled(string password, string from, string to, string subject, string message)
        {
            if (LAST_THROTTLED_EMAIL_SENT_ON.AddSeconds(THROTTLE_DELAY) < DateTime.UtcNow)
            {
                SendGmail(password, from, to, subject, message);
                LAST_THROTTLED_EMAIL_SENT_ON = DateTime.UtcNow;
            }
        }
    }
}