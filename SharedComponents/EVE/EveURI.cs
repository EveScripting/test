using SharedComponents.Extensions;
using SharedComponents.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace SharedComponents.EVE
{
    public static class EveUri
    {
        //Base Url locations
        private static readonly Uri baseUriTQ = new Uri("https://login.eveonline.com", UriKind.Absolute);
        private static readonly Uri baseUriSISI = new Uri("https://sisilogin.testeveonline.com", UriKind.Absolute);

        public static class Endpoints
        {
            public static readonly Uri Logoff = new Uri("/account/logoff", UriKind.Relative);
            public static readonly Uri Auth = new Uri("/v2/oauth/authorize", UriKind.Relative);
            public static readonly Uri Eula = new Uri("/v2/oauth/eula", UriKind.Relative);
            public static readonly Uri Logon = new Uri("/account/logon", UriKind.Relative);
            public static readonly Uri Token = new Uri("/v2/oauth/token", UriKind.Relative);
            public static readonly Uri TwoFactorVerification = new Uri("/account/verifytwofactor", UriKind.Relative);
            public static readonly Uri Authenticator = new Uri("/account/authenticator", UriKind.Relative);
            public static readonly Uri CharacterChallenge = new Uri("/account/character", UriKind.Relative);
            
            public static readonly Uri Launcher = new Uri("/launcher", UriKind.Relative);
        }
        //public const string logonRetURI = "ReturnUrl=/v2/oauth/authorize?client_id=eveLauncherTQ&response_type=code&scope=eveClientLogin%20cisservice.customerRead.v1%20cisservice.customerWrite.v1";
        //public const string logonRedirectURI = "redirect_uri={0}/launcher?client_id=eveLauncherTQ&state={1}&code_challenge_method=S256&code_challenge={2}&ignoreClientStyle=true&showRemember=true";

        public const string originUri = "https://launcher.eveonline.com";
        public const string refererUri = "https://launcher.eveonline.com/6-0-x/6.0.22/";

        /*
         * 
         * /account/logoff?
         * lang=en
         * &ReturnUrl=
         * %2Fv2
         * %2Foauth
         * %2Fauthorize
         * %3Fclient_id%3DeveLauncherTQ%26response_type%3Dcode%26scope%3DeveClientLogin%2520cisservice.customerRead.v1%2520cisservice.customerWrite.v1%26redirect_uri%3Dhttps%253A%252F%252Flogin.eveonline.com%252Flauncher%253Fclient_id%253DeveLauncherTQ%26state%3D1cde5a8c-8198-4ce0-a1a6-516d02d78c95%26code_challenge_method%3DS256%26code_challenge%3DOqxpDQcXNazmz2noSSLffifJ0gjJ5LERZLOw-n3VKD4%26ignoreClientStyle%3Dtrue%26showRemember%3Dtrue HTTP/1.1
         */

        private static Uri GetBaseUri(bool sisi=false)
        {
            return sisi ? baseUriSISI : baseUriTQ;
        }

        public static Uri GetTokenUri(bool sisi)
        {
            return new Uri(GetBaseUri(sisi), Endpoints.Token);
        }

        public static Uri GetLogoffUri(bool sisi, string state, string challengeHash)
        {
            return new Uri(GetBaseUri(sisi),
                Endpoints.Logoff
                .AddQuery("lang", "en")
                .AddQuery("ReturnUrl", 
                    Endpoints.Auth
                        .AddQuery("client_id", "eveLauncherTQ")
                        .AddQuery("response_type", "code")
                        .AddQuery("scope", "eveClientLogin cisservice.customerRead.v1 cisservice.customerWrite.v1")
                        .AddQuery("redirect_uri", new Uri(GetBaseUri(sisi), Endpoints.Launcher)
                            .AddQuery("client_id", "eveLauncherTQ").ToString())
                        .AddQuery("state", state)
                        .AddQuery("code_challenge_method", "S256")
                        .AddQuery("code_challenge", challengeHash)
                        .AddQuery("ignoreClientStyle", "true")
                        .AddQuery("showRemember", "true")
                        .ToString()
                    ));
        }

        public static Uri GetLoginUri(bool sisi, string state, string challengeHash)
        {
            return new Uri(GetBaseUri(sisi),
                Endpoints.Logon
                .AddQuery("ReturnUrl",
                    Endpoints.Auth
                        .AddQuery("client_id", "eveLauncherTQ")
                        .AddQuery("response_type", "code")
                        .AddQuery("scope", "eveClientLogin cisservice.customerRead.v1 cisservice.customerWrite.v1")
                        .AddQuery("redirect_uri", new Uri(GetBaseUri(sisi), Endpoints.Launcher)
                            .AddQuery("client_id", "eveLauncherTQ").ToString())
                        .AddQuery("state", state)
                        .AddQuery("code_challenge_method", "S256")
                        .AddQuery("code_challenge", challengeHash)
                        .AddQuery("ignoreClientStyle", "true")
                        .AddQuery("showRemember", "true").ToString()));
        }



        public static Uri GetSecurityWarningChallenge(bool sisi, string state, string challengeHash)
        {
            //https://login.eveonline.com/v2/oauth/authorize?
            //client_id =eveLauncherTQ
            //&amp;response_type=code
            //&amp;scope=eveClientLogin%20cisservice.customerRead.v1%20cisservice.customerWrite.v1
            //&amp;redirect_uri=https%3A%2F%2Flogin.eveonline.com%2Flauncher%3Fclient_id%3DeveLauncherTQ
            //&amp;state=5617f90c-efdb-41a1-b00d-6f4f24bbeee4
            //&amp;code_challenge_method=S256
            //&amp;code_challenge=nC-B19HKX8ZZYfOEN_bg-YZSjVAMieqEB3nJXFyfQQc
            //&amp;ignoreClientStyle=true
            //&amp;showRemember=true
            return new Uri(GetBaseUri(sisi), Endpoints.Auth
                        .AddQuery("client_id", "eveLauncherTQ")
                        .AddQuery("response_type", "code")
                        .AddQuery("scope", "eveClientLogin cisservice.customerRead.v1 cisservice.customerWrite.v1")
                        .AddQuery("redirect_uri", new Uri(GetBaseUri(sisi), Endpoints.Launcher)
                            .AddQuery("client_id", "eveLauncherTQ").ToString())
                        .AddQuery("state", state)
                        .AddQuery("code_challenge_method", "S256")
                        .AddQuery("code_challenge", challengeHash)
                        .AddQuery("ignoreClientStyle", "true")
                        .AddQuery("showRemember", "true"));
        }

        public static Uri GetVerifyTwoFactorUri(bool sisi, string state, string challengeHash)
        {
            return new Uri(GetBaseUri(sisi), Endpoints.TwoFactorVerification
                .AddQuery("ReturnUrl",
                    Endpoints.Auth
                        .AddQuery("client_id", "eveLauncherTQ")
                        .AddQuery("response_type", "code")
                        .AddQuery("scope", "eveClientLogin cisservice.customerRead.v1 cisservice.customerWrite.v1")
                        .AddQuery("redirect_uri", new Uri(GetBaseUri(sisi), Endpoints.Launcher)
                            .AddQuery("client_id", "eveLauncherTQ").ToString())
                        .AddQuery("state", state)
                        .AddQuery("code_challenge_method", "S256")
                        .AddQuery("code_challenge", challengeHash)
                        .AddQuery("ignoreClientStyle", "true")
                        .AddQuery("showRemember", "true").ToString()));
        }

        public static Uri GetAuthenticatorUri(bool sisi, string state, string challengeHash)
        {
            return new Uri(GetBaseUri(sisi), Endpoints.Authenticator
                .AddQuery("ReturnUrl",
                    Endpoints.Auth
                        .AddQuery("client_id", "eveLauncherTQ")
                        .AddQuery("response_type", "code")
                        .AddQuery("scope", "eveClientLogin cisservice.customerRead.v1 cisservice.customerWrite.v1")
                        .AddQuery("redirect_uri", new Uri(GetBaseUri(sisi), Endpoints.Launcher)
                            .AddQuery("client_id", "eveLauncherTQ").ToString())
                        .AddQuery("state", state)
                        .AddQuery("code_challenge_method", "S256")
                        .AddQuery("code_challenge", challengeHash)
                        .AddQuery("ignoreClientStyle", "true")
                        .AddQuery("showRemember", "true").ToString()));
        }

        public static Uri GetEulaUri(bool sisi, string state, string challengeHash)
        {
            return new Uri(GetBaseUri(sisi), Endpoints.Eula
                .AddQuery("ReturnUrl",
                    Endpoints.Auth
                        .AddQuery("client_id", "eveLauncherTQ")
                        .AddQuery("response_type", "code")
                        .AddQuery("scope", "eveClientLogin cisservice.customerRead.v1 cisservice.customerWrite.v1")
                        .AddQuery("redirect_uri", new Uri(GetBaseUri(sisi), Endpoints.Launcher)
                            .AddQuery("client_id", "eveLauncherTQ").ToString())
                        .AddQuery("state", state)
                        .AddQuery("code_challenge_method", "S256")
                        .AddQuery("code_challenge", challengeHash)
                        .AddQuery("ignoreClientStyle", "true")
                        .AddQuery("showRemember", "true").ToString()));
        }


        public static Uri GetCharacterChallengeUri(bool sisi, string state, string challengeHash)
        {
            return new Uri(GetBaseUri(sisi), Endpoints.CharacterChallenge
                .AddQuery("ReturnUrl",
                    Endpoints.Auth
                        .AddQuery("client_id", "eveLauncherTQ")
                        .AddQuery("response_type", "code")
                        .AddQuery("scope", "eveClientLogin cisservice.customerRead.v1 cisservice.customerWrite.v1")
                        .AddQuery("redirect_uri", new Uri(GetBaseUri(sisi), Endpoints.Launcher)
                            .AddQuery("client_id", "eveLauncherTQ").ToString())
                        .AddQuery("state", state)
                        .AddQuery("code_challenge_method", "S256")
                        .AddQuery("code_challenge", challengeHash)
                        .AddQuery("ignoreClientStyle", "true")
                        .AddQuery("showRemember", "true").ToString()));
        }
    }
}
