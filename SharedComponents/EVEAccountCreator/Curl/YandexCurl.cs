using System;
using System.Diagnostics;
using System.Security.Authentication;
using System.Threading;
using System.Windows.Forms;
using HtmlAgilityPack;
//using ImapX.Collections;
using SharedComponents.CurlUtil;
using SharedComponents.EVE;
//using SharedComponents.IMAP;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace SharedComponents.EVEAccountCreator.Curl
{
    public class YandexCurl
    {
        public String Email { get; set; }

        public String Password { get; set; }
        public bool EmailCreatedSuccessfully { get; set; }

        public bool CreateYandexEmail(string username, string password, Proxy p)
        {
            using (var curlWorker = new CurlWorker())
            {
                // load the initial registration page
                var result =
                    curlWorker.GetPostPage(
                        "https://passport.yandex.com/registration/mail?from=mail&require_hint=1&origin=hostroot_com_nol_mobile_left&retpath=", string.Empty,
                        p.GetIpPort(),
                        p.GetUserPassword());

                // retrieve information to build post data
                var trackId = ExtractTrackId(result);
                var captchaUrl = GetCaptchaUrl(result);
                var captchaKey = ExtractCaptchaKey(captchaUrl);
                //var img = curlWorker.RetrieveImage(captchaUrl, p.GetIpPort(), p.GetUserPassword());
                var hintAnswer = new Random(new Random().Next(9999, 999999)).Next(10001, 99999).ToString();


                curlWorker.GetPostPage(
                    "https://passport.yandex.com/registration-validations/checkjsload", "track_id=" + trackId +
                                                                                        "f&language=en", p.GetIpPort(),
                    p.GetUserPassword());

                var captchaSolve = String.Empty;
                // show captcha form
                var t = new Thread(() =>
                {
                    //var f = new CaptchaResponseForm(img);
                    //f.ShowDialog();
                    //captchaSolve = f.GetCaptchaResponse;
                });

                t.Start();

                while (t.IsAlive)
                {
                    Debug.WriteLine("Waiting for captcha response.");
                    Thread.Sleep(1000);
                    Application.DoEvents();
                }

                Debug.WriteLine("Captcha response: " + captchaSolve);
                Debug.WriteLine("TrackId: " + trackId);
                Debug.WriteLine("CaptchaKey: " + captchaKey);

                // build post data
                var postData =
                    "track_id=" + trackId +
                    "&language=en&firstname=" + UserPassGen.Instance.GenerateFirstname() +
                    "&lastname=" + UserPassGen.Instance.GenerateFirstname() +
                    "&login=" + username +
                    "&fake-passwd=&password=" + password +
                    "&password_confirm=" + password +
                    "&human-confirmation=captcha&phone-confirm-state=&phone_number_confirmed=&phone_number=&fake-login=&phone-confirm-password=&hint_question_id=3&hint_question=&hint_answer=" +
                    hintAnswer +
                    "&answer=" + captchaSolve +
                    "&key=" + captchaKey +
                    "&captcha_mode=text&eula_accepted=on";


                result = curlWorker.GetPostPage(
                    "https://passport.yandex.com/registration/mail?from=mail&require_hint=1&origin=hostroot_com_nol_mobile_left&retpath=", postData,
                    p.GetIpPort(),
                    p.GetUserPassword());

                var email = username + "@yandex.com";
                if (result.Contains(email))
                {
                    Email = email;
                    Password = password;
                    EmailCreatedSuccessfully = true;
                    var msg = string.Format("Email account created [{0}]", email);
                    Debug.WriteLine(msg);
                    Cache.Instance.Log(msg);
                    return true;
                }

                return false;
            }
        }

        //public MessageCollection GetEmails(Proxy p)
        //{
        //    return Imap.GetInboxEmails("imap.yandex.com", 993, SslProtocols.Default, true, p.Ip, Convert.ToInt32(p.Port), p.Username, p.Password, Email,
        //        Password);
        //}

        public String ExtractTrackId(string source)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(source);
            HtmlNode.ElementsFlags.Remove("form");
            var trackId = String.Empty;
            if (htmlDoc.DocumentNode != null)
                foreach (var text in htmlDoc.DocumentNode.SelectNodes("//@value"))
                    if (text.Id == "track_id")
                        trackId = text.GetAttributeValue("value", "");
            return trackId;
        }

        public String GetCaptchaUrl(string source)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(source);
            HtmlNode.ElementsFlags.Remove("form");
            var url = String.Empty;
            if (htmlDoc.DocumentNode != null)
                foreach (var img in htmlDoc.DocumentNode.SelectNodes("//img[@class='captcha__captcha__text']"))
                    url = img.GetAttributeValue("src", "");
            return url;
        }

        public String ExtractCaptchaKey(string url)
        {
            return url.Split('=')[1];
        }
    }
}