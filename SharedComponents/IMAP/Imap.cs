//using System;
//using System.Diagnostics;
//using System.Security.Authentication;
//using ImapX;
//using ImapX.Collections;
//using SharedComponents.EVE;

//namespace SharedComponents.IMAP
//{
//    public static class Imap
//    {
//        public static MessageCollection GetInboxEmails(string imapHost, int imapPort, SslProtocols sslProto, bool verifySSLCerts,
//            string socks5Host, int socks5Port, string socks5User, string socks5Password,
//            string emailUser, string emailPassword)
//        {
//            var client = new ImapClient();
//            if (client.Connect(imapHost, imapPort, sslProto, verifySSLCerts, socks5Host, socks5Port, socks5User, socks5Password))
//            {
//                if (client.Login(emailUser, emailPassword))
//                {
//                    Debug.WriteLine("Auth succesful.");
//                    client.Folders.Inbox.Messages.Download();
//                    client.Disconnect();
//                    client.Dispose();

//                    return client.Folders.Inbox.Messages;
//                }
//                else
//                {
//                    Debug.WriteLine("Auth/Connection failed.");
//                    throw new AuthenticationException("Wrong Socks5/Email authentication information.");
//                }
//            }
//            else
//            {
//                Debug.WriteLine("Connection failed.");
//                throw new AuthenticationException("Wrong Socks5/Email authentication information.");
//            }
//        }

//        public static MessageCollection GetInboxEmails(EveAccount eA)
//        {
//            if (eA != null && eA.HWSettings != null && eA.HWSettings.Proxy != null)
//            {
//                var p = eA.HWSettings.Proxy;
//                return GetInboxEmails(eA.IMAPHost, 993, SslProtocols.Default, true, p.Ip, Convert.ToInt32(p.Port),
//                    p.Username, p.Password, eA.Email, eA.Password);
//            }

//            Debug.WriteLine("HWSettings or Proxy == null.");
//            return null;
//        }
//    }
//}