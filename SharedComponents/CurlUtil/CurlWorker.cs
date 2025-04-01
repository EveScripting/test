/*
 * ---------------------------------------
 * User: duketwo
 * Date: 03.02.2014
 * Time: 11:32
 * 
 * ---------------------------------------
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using CurlSharp;
using SharedComponents.EVE;
using SharedComponents.Utility;

namespace SharedComponents.CurlUtil
{
    /// <summary>
    ///     Description of CurlWorker.
    /// </summary>
    public class CurlWorker : IDisposable
    {

        public static bool DisableSSLVerifcation;

        private const string UserAgent =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.3";

        private static Object Lock = new object();
        private bool _persistentCookie;

        static CurlWorker()
        {
            Curl.GlobalInit(CurlInitFlag.All);
        }

        public CurlWorker()
        {

            lock (Lock)
            {
                CreateDirectories();
                var path = Path.Combine(Utility.Util.AssemblyPath, "EveSharpSettings", "Cookies");
                CookieFile = Path.Combine(path, Guid.NewGuid().ToString("n") + DateTime.UtcNow.Ticks + Cnt.ToString() + ".cookie");
                Cnt++;
                while (File.Exists(CookieFile))
                {
                    Cnt++;
                    CookieFile = Path.Combine(path, Guid.NewGuid().ToString("n") + DateTime.UtcNow.Ticks + Cnt.ToString() + ".cookie");
                }
                try
                {
                    Util.Touch(CookieFile);
                }
                catch (Exception e)
                {
                    Cache.Instance.Log("Exception: " + e);
                }
            }
        }

        public CurlWorker(string cookieName)
        {
            lock (Lock)
            {
                CreateDirectories();
                var path = Path.Combine(Utility.Util.AssemblyPath, "EveSharpSettings", "Cookies");
                CookieFile = Path.Combine(path, cookieName + ".cookie");
                _persistentCookie = true;
                if (!File.Exists(CookieFile))
                {
                    try
                    {
                        Util.Touch(CookieFile);
                    }
                    catch (Exception e)
                    {
                        Cache.Instance.Log("Exception: " + e);
                    }
                }
            }
        }

        private void CreateDirectories()
        {
            var path = Utility.Util.AssemblyPath;
            if (!Directory.Exists(Path.Combine(path, "EveSharpSettings")))
            {
                Directory.CreateDirectory(Path.Combine(path, "EveSharpSettings"));
            }
            if (!Directory.Exists(Path.Combine(path, "EveSharpSettings", "Cookies")))
            {
                Directory.CreateDirectory(Path.Combine(path, "EveSharpSettings", "Cookies"));
            }
        }

        private string CookieFile { get; set; }
        private static int Cnt { get; set; }

        #region IDisposable

        public void Dispose()
        {
            DeleteCurrentSessionCookie();
        }

        #endregion

        ~CurlWorker()
        {
            DeleteCurrentSessionCookie();
        }

        public bool DeleteCurrentSessionCookie(bool force = false)
        {
            if (File.Exists(CookieFile) && (!_persistentCookie || force))
                try
                {
                    File.Delete(CookieFile);
                    //var msg = string.Format("Deleted session cookie '{0}' file.", CookieFile);
                    //Cache.Instance.Log(msg);
                    //Debug.WriteLine(msg);
                    return true;
                }
                catch (Exception)
                {
                    Cache.Instance.Log(string.Format("Error: Couldn't delete session cookie '{0}' file.", CookieFile));
                }

            return false;
        }

        public string GetPostPage(string url, string postData, string proxyPort, string userPassword, bool followLocation = true, bool includeHeader = false, long timeout = 60L)
        {
            var writer = new CurlWriter();
            try
            {
                using (var easy = new CurlEasy())
                {
                    easy.WriteFunction = writer.WriteData;
                    easy.SetOpt(CurlOption.Url, url);
                    easy.SetOpt(CurlOption.SslVerifyhost, 2);
                    var verifyPeer = DisableSSLVerifcation ? 0 : 1;
                    easy.SetOpt(CurlOption.SslVerifyPeer, verifyPeer);
                    easy.SetOpt(CurlOption.CaInfo, "curl-ca-bundle.crt");
                    if (!string.IsNullOrEmpty(proxyPort)) easy.SetOpt(CurlOption.Proxy, proxyPort);
                    if (!string.IsNullOrEmpty(userPassword)) easy.SetOpt(CurlOption.ProxyUserPwd, userPassword);
                    if (!string.IsNullOrEmpty(proxyPort)) easy.SetOpt(CurlOption.ProxyType, CurlProxyType.Socks5);
                    easy.SetOpt(CurlOption.UserAgent, UserAgent);
                    easy.SetOpt(CurlOption.CookieFile, CookieFile);
                    easy.SetOpt(CurlOption.CookieJar, CookieFile);
                    if (followLocation) easy.SetOpt(CurlOption.FollowLocation, 1);
                    easy.SetOpt(CurlOption.AutoReferer, 1);
                    easy.SetOpt(CurlOption.ConnectTimeout, timeout);
                    easy.SetOpt(CurlOption.Timeout, timeout);
                    if (includeHeader) easy.SetOpt(CurlOption.Header, true);
                    if (!string.IsNullOrEmpty(postData)) easy.SetOpt(CurlOption.PostFields, postData);
                    easy.Perform();
                    return writer.CurrentPage;
                }
            }
            catch (Exception exp)
            {
                if (exp is ThreadAbortException)
                    writer.CurrentPage = string.Empty;
                Cache.Instance.Log("Exception: " + exp.StackTrace);
            }
            return writer.CurrentPage;
        }

        public static bool CheckInternetConnectivity(string proxyPort, string userPassword)
        {
            var writer = new CurlWriter();
            try
            {
                using (var easy = new CurlEasy())
                {
                    easy.WriteFunction = writer.WriteData;
                    easy.SetOpt(CurlOption.Url, "http://www.google.com");
                    easy.SetOpt(CurlOption.SslVerifyhost, 2);
                    easy.SetOpt(CurlOption.SslVerifyPeer, 1);
                    easy.SetOpt(CurlOption.CaInfo, "curl-ca-bundle.crt");
                    easy.SetOpt(CurlOption.FollowLocation, 0);
                    easy.SetOpt(CurlOption.Header, 1);
                    easy.SetOpt(CurlOption.NoBody, 1);
                    if (!string.IsNullOrEmpty(proxyPort)) easy.SetOpt(CurlOption.Proxy, proxyPort);
                    if (!string.IsNullOrEmpty(userPassword)) easy.SetOpt(CurlOption.ProxyUserPwd, userPassword);
                    if (!string.IsNullOrEmpty(proxyPort)) easy.SetOpt(CurlOption.ProxyType, CurlProxyType.Socks5);
                    easy.SetOpt(CurlOption.UserAgent, UserAgent);
                    easy.SetOpt(CurlOption.AutoReferer, 1);
                    easy.SetOpt(CurlOption.ConnectTimeout, 5L);
                    easy.SetOpt(CurlOption.Timeout, 5L);
                    easy.Perform();
                    return writer.CurrentPage != null && (writer.CurrentPage.ToUpper().Contains("200 OK") || writer.CurrentPage.ToUpper().Contains("302 FOUND"));
                }
            }
            catch (Exception exp)
            {
                if (exp is ThreadAbortException)
                    writer.CurrentPage = string.Empty;
                Cache.Instance.Log("Exception: " + exp.StackTrace);
                return false;
            }
        }

        //public Byte[] RetrieveImage(string url, string proxyPort, string userPassword)
        //{
        //    Easy easy = null;
        //    var writer = new CurlWriter();
        //    try
        //    {
        //        easy = new Easy();
        //        Easy.WriteFunction wf = writer.WriteData;
        //        easy.SetOpt(CurlOption.CURLOPT_URL, url);
        //        easy.SetOpt(CurlOption.CURLOPT_WRITEFUNCTION, wf);
        //        easy.SetOpt(CurlOption.CURLOPT_SSL_VERIFYHOST, 2);
        //        easy.SetOpt(CurlOption.CURLOPT_SSL_VERIFYPEER, 1);
        //        easy.SetOpt(CurlOption.CURLOPT_CAINFO, "curl-ca-bundle.crt");
        //        easy.SetOpt(CurlOption.CURLOPT_FOLLOWLOCATION, 1);
        //        easy.SetOpt(CurlOption.CURLOPT_HEADER, 0);
        //        if (!string.IsNullOrEmpty(proxyPort)) easy.SetOpt(CurlOption.CURLOPT_PROXY, proxyPort);
        //        if (!string.IsNullOrEmpty(userPassword)) easy.SetOpt(CurlOption.CURLOPT_PROXYUSERPWD, userPassword);
        //        if (!string.IsNullOrEmpty(proxyPort)) easy.SetOpt(CurlOption.CURLOPT_PROXYTYPE, CURLproxyType.CURLPROXY_SOCKS5);
        //        easy.SetOpt(CurlOption.CURLOPT_USERAGENT, UserAgent);
        //        easy.SetOpt(CurlOption.CURLOPT_AUTOREFERER, 1);
        //        easy.SetOpt(CurlOption.CURLOPT_CONNECTTIMEOUT, 5L);
        //        easy.SetOpt(CurlOption.CURLOPT_TIMEOUT, 10L);
        //        var r = easy.Perform();

        //        if (r == CurlCode.Ok)
        //        {
        //            Debug.WriteLine("Code: CURLcode.CURLE_OK, Image downloaded successfully.");
        //            return writer.ByteArr;
        //        }

        //        Debug.WriteLine("Error downloading the image.");
        //        return new List<Byte>().ToArray();
        //    }
        //    catch (Exception exp)
        //    {
        //        if (exp is ThreadAbortException)
        //            writer.CurrentPage = string.Empty;
        //        Cache.Instance.Log("Exception: " + exp.StackTrace);
        //        return new List<Byte>().ToArray();
        //    }
        //    finally
        //    {
        //        try
        //        {
        //            if (easy != null)
        //                easy.Cleanup();
        //        }
        //        catch (Exception exp)
        //        {
        //            Cache.Instance.Log("Exception: " + exp.StackTrace);
        //        }
        //    }
        //}
    }
}