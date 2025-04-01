using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.Extensions;
using OpenQA.Selenium.Support.UI;
using SharedComponents.EVE;
using SharedComponents.Utility;
using Proxy = SharedComponents.EVE.Proxy;

namespace SharedComponents.EVEAccountCreator
{
    public class Outlook : IDisposable
    {
        private volatile FirefoxDriver driver;

        #region IDisposable

        public void Dispose()
        {
            if (driver != null)
                driver.Dispose();

            #endregion
        }

        public static void KillGeckoDrivers()
        {
            try
            {
                foreach (var p in Process.GetProcesses().Where(x => x.ProcessName.ToLower().Contains("geckodriver")))
                {
                    if (p != null)
                        Util.TaskKill(p.Id, true);
                    Thread.Sleep(1000);
                }

                while (Process.GetProcesses().Any(x => x.ProcessName.ToLower().Contains("geckodriver")))
                    Thread.Sleep(100);
            }
            catch (Exception)
            {
            }
        }


        //        public boolean waitForJSandJQueryToLoad()
        //        {
        //
        //            WebDriverWait wait = new WebDriverWait(driver, 30);
        //
        //            // wait for jQuery to load
        //            
        //            ExpectedCondition jQueryLoad = new ExpectedCondition<Boolean>() {
        //                    @Override
        //                    public Boolean apply(WebDriver driver)
        //                    {
        //                    try
        //                    {
        //                    return ((Long)((JavascriptExecutor)getDriver()).executeScript("return jQuery.active") == 0);
        //                }
        //                catch (Exception e)
        //            {
        //                // no jQuery present
        //                return true;
        //            }
        //            }
        //            };
        //
        //            // wait for Javascript to load
        //            ExpectedCondition<Boolean> jsLoad = new ExpectedCondition<Boolean>() {
        //                @Override
        //                public Boolean apply(WebDriver driver)
        //                {
        //                return ((JavascriptExecutor)getDriver()).executeScript("return document.readyState")
        //                .toString().equals("complete");
        //            }
        //            };
        //
        //            return wait.until(jQueryLoad) && wait.until(jsLoad);
        //        }



        public void WaitForJSandJQueryToLoad()
        {

            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
            wait.Until((d) =>
            {
                var r = d.ExecuteJavaScript<string>("return document.readyState").ToString().Equals("complete");
                var state = d.ExecuteJavaScript<string>("return document.readyState");
                Debug.WriteLine("Page state: " + state);
                return r;
            });

            wait.Until((d) =>
            {
                try
                {
                    var r = d.ExecuteJavaScript<long>("return jQuery.active") == 0;
                    Debug.WriteLine("Jquery non active: " + r);
                    return r;
                }
                catch (Exception)
                {
                    return true;
                }
            });


        }

        public IWebElement WaitForElementPresentAndEnabled(By locator, int secondsToWait = 30)
        {
            WaitForJSandJQueryToLoad();
            new WebDriverWait(driver, new TimeSpan(0, 0, secondsToWait))
                .Until(d => d.FindElement(locator).Enabled
                            && d.FindElement(locator).Displayed
                            && d.FindElement(locator).GetAttribute("aria-disabled") == null
                );

            return driver.FindElement(locator);
        }


        public void CreateOutlookEmail(string username, string password, Proxy proxy, string recoveryEmailAddress)
        {
            KillGeckoDrivers();

            try
            {
                if (driver == null)
                {
                    var driverService = FirefoxDriverService.CreateDefaultService();
                    var options = new FirefoxOptions();
                    options.SetPreference("browser.private.browsing.autostart", true);
                    options.SetPreference("network.proxy.type", 1);
                    options.SetPreference("network.proxy.socks", "127.0.0.1");
                    options.SetPreference("network.proxy.socks_port", 41337);
                    options.SetPreference("network.proxy.socks_version", 5);
                    options.SetPreference("plugin.state.flash", 0);
                    options.SetPreference("media.peerconnection.enabled", false);


                    driverService.HideCommandPromptWindow = false;
                    driver = new FirefoxDriver(driverService, options, TimeSpan.FromSeconds(60));
                }

                driver.Url = "https://signup.live.com/signup.aspx";
                driver.Navigate();

                Debug.WriteLine("Page loaded...");


                var rnd = new Random();
                var firstName = UserPassGen.Instance.GenerateFirstname();
                var lastName = UserPassGen.Instance.GenerateFirstname();

                //WaitForElementPresentAndEnabled(new ByIdOrName("FirstName")).SendKeys(firstName);
                //WaitForElementPresentAndEnabled(new ByIdOrName("LastName")).SendKeys(lastName);
                //WaitForElementPresentAndEnabled(new ByIdOrName("Password")).SendKeys(password);
                //WaitForElementPresentAndEnabled(new ByIdOrName("RetypePassword")).SendKeys(password);

                //var birthDaySelect = new SelectElement(WaitForElementPresentAndEnabled(new ByIdOrName("BirthDay")));
                //birthDaySelect.SelectByIndex(rnd.Next(1, birthDaySelect.Options.Count - 1));
                //var birthMonthSelect = new SelectElement(WaitForElementPresentAndEnabled(new ByIdOrName("BirthMonth")));
                //birthMonthSelect.SelectByIndex(rnd.Next(1, birthMonthSelect.Options.Count - 1));
                //var birthYearSelect = new SelectElement(WaitForElementPresentAndEnabled(new ByIdOrName("BirthYear")));
                //birthYearSelect.SelectByIndex(rnd.Next(1, birthYearSelect.Options.Count - 1));
                //var genderSelect = new SelectElement(WaitForElementPresentAndEnabled(new ByIdOrName("Gender")));
                //genderSelect.SelectByIndex(rnd.Next(1, genderSelect.Options.Count - 1));
                //WaitForElementPresentAndEnabled(new ByIdOrName("iAltEmail")).SendKeys(recoveryEmailAddress);
                //WaitForElementPresentAndEnabled(new ByIdOrName("MemberName")).SendKeys(username);

            }
            catch (Exception ex)
            {
                Cache.Instance.Log("Exception: " + ex.ToString());
            }
        }
    }
}