using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SharedComponents.EVE;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SharedComponents.SeleniumDriverHandler
{
    public class MailInboxHandler
    {
        public enum EmailProvider
        {
            WEB_DE,
            GMAIL_COM,
            OUTLOOK_COM,
            UNKNOWN,
        }

        public EveAccount EA { get; }
        public DriverHandler DriverHandler { get; }

        private String _emailRegex = @"(?<Name>[^\x00-\x20\x22\x28\x29\x2c\x2e\x3a-\x3c\x3e\x40\x5b-\x5d\x7f-\xff]+|\x22([^\x0d\x22\x5c\x80-\xff]|\x5c[\x00-\x7f])*\x22)(\x2e([^\x00-\x20\x22\x28\x29\x2c\x2e\x3a-\x3c\x3e\x40\x5b-\x5d\x7f-\xff]+|\x22([^\x0d\x22\x5c\x80-\xff]|\x5c[\x00-\x7f])*\x22))*\x40(?<Domain>[^\x00-\x20\x22\x28\x29\x2c\x2e\x3a-\x3c\x3e\x40\x5b-\x5d\x7f-\xff]+|\x5b(?<Unknown>[^\x0d\x5b-\x5d\x80-\xff]|\x5c[\x00-\x7f])*\x5d)(\x2e(?<TLD>[^\x00-\x20\x22\x28\x29\x2c\x2e\x3a-\x3c\x3e\x40\x5b-\x5d\x7f-\xff]+|\x5b([^\x0d\x5b-\x5d\x80-\xff]|\x5c[\x00-\x7f])*\x5d))*";

        public MailInboxHandler(EveAccount eA, DriverHandler driverHandler)
        {
            EA = eA;
            DriverHandler = driverHandler;
        }

        public void OpenMailInbox()
        {

            var provider = GetEmailProvider();
            var providerUrl = GetProviderInboxUrl(provider);
            Debug.WriteLine("Email provider: " + provider);
            Debug.WriteLine("Email provider url: " + providerUrl);

            var driver = DriverHandler.GetDriver();
            switch (provider)
            {

                case EmailProvider.WEB_DE:
                    // unfinished
                    driver.GoToUrl(providerUrl);
                    DriverHandler.WaitForPage();
                    Debug.WriteLine("#1");
                    DriverHandler.WaitForElement(() => {
                        driver.FindElement(By.XPath("/html/body/div/div[2]/div/div/div/div[1]/div[2]/div/div/button")).Submit();
                    }, 10);
                    Debug.WriteLine("#2");
                    DriverHandler.WaitForPage();

                    break;
                case EmailProvider.GMAIL_COM:
                    driver.GoToUrl(providerUrl);
                    DriverHandler.WaitForPage();
                    driver.FindElement(By.Name("identifier")).SendKeys(GetEmail());
                    DriverHandler.WaitForPage();
                    driver.FindElement(By.XPath("//span[text()='Next']")).Click();
                    DriverHandler.WaitForPage();


                    var to = DateTime.UtcNow.AddSeconds(8);

                    while (true)
                    {

                        if (to < DateTime.UtcNow)
                            break;

                        try
                        {
                            driver.FindElement(By.Name("Passwd")).SendKeys(EA.EmailPassword);
                        }
                        catch (StaleElementReferenceException ex)
                        {
                            continue;
                        }
                        catch (ElementNotInteractableException ex)
                        {
                            continue;
                        }
                        catch (NoSuchElementException)
                        {
                            continue;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex);
                            break;
                        }
                        break;
                    }

                    to = DateTime.UtcNow.AddSeconds(8);
                    while (true)
                    {

                        if (to < DateTime.UtcNow)
                            break;

                        try
                        {
                            driver.FindElement(By.XPath("//span[text()='Next']")).Click();
                        }
                        catch (StaleElementReferenceException ex)
                        {
                            continue;
                        }
                        catch (ElementNotInteractableException ex)
                        {
                            continue;
                        }
                        catch (NoSuchElementException)
                        {
                            continue;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex);
                            break;
                        }
                        break;
                    }
                    DriverHandler.WaitForPage();
                    
                    break;
                case EmailProvider.OUTLOOK_COM:
                    driver.GoToUrl(providerUrl);
                    DriverHandler.WaitForPage();
                    driver.FindElement(By.Name("loginfmt")).SendKeys(GetEmail());
                    driver.FindElement(By.ClassName("button_primary")).Click();
                    DriverHandler.WaitForPage();
                    driver.FindElement(By.Name("passwd")).SendKeys(EA.EmailPassword);


                    var timeout = DateTime.UtcNow.AddSeconds(8);
                    var n = 0;
                    while (true)
                    {

                        if (timeout < DateTime.UtcNow || n > 1)
                            break;

                        try
                        {
                            driver.FindElement(By.Id("idSIButton9")).Submit();
                        }
                        catch (StaleElementReferenceException ex)
                        {
                            continue;
                        }
                        catch (ElementNotInteractableException ex)
                        {
                            continue;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex);
                            break;
                        }
                        n++;
                    }


                    DriverHandler.WaitForPage();
                    break;
                case EmailProvider.UNKNOWN:
                    break;
                default:
                    break;
            }
        }

        private string GetProviderInboxUrl(EmailProvider provider)
        {

            switch (provider)
            {
                case EmailProvider.WEB_DE:
                    return "https://web.de";
                case EmailProvider.GMAIL_COM:
                    return "https://mail.google.com";
                case EmailProvider.OUTLOOK_COM:
                    return "https://outlook.live.com/owa/?nlp=1";
                default:
                    return "";
            }
        }

        private EmailProvider GetEmailProvider()
        {

            Regex regex = new Regex(_emailRegex);
            return regex.IsMatch(EA.Email) ? (EmailProvider)Enum.Parse(typeof(EmailProvider), regex.Match(EA.Email).Groups["Domain"].Value.ToUpper() + "_" + regex.Match(EA.Email).Groups["TLD"].Value.ToUpper()) : EmailProvider.UNKNOWN;
        }

        private String GetEmail()
        {
            Regex regex = new Regex(_emailRegex);
            var match = regex.Match(EA.Email);
            if (match.Success)
                return match.Captures[0].Value;

            return string.Empty;
        }

    }
}
