using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FilelistGenerator {
    class Program {
        static void Main(string[] args) {
            Console.Write("Please enter in the SteamDB's patch note url: ");
            string urlString = Console.ReadLine();

            Uri uri;
            bool result =
                Uri.TryCreate(urlString, UriKind.Absolute, out uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

            Console.WriteLine();

            // Validate input url.
            if (!result) {
                WriteError(
                    "Invalid url detected (input: " + urlString + "). Please run again "
                        + "with a valid url.\n");
                Console.Write("Press [ENTER] to close.");
                Console.ReadLine();
                return;
            }

            Console.WriteLine("Opening Chrome: " + urlString);
            WriteImportant(
                "\nAfter the page has FULLY loaded, press [ENTER] to continue. Each "
                    + "section should be filled out with added, modified, and/or deleted "
                    + "files.");

            int numAdded = 0;
            int numModified = 0;
            int numDeleted = 0;
            List<string> files = new List<string>();
            FirefoxOptions options = new FirefoxOptions();
            FirefoxDriverService service = FirefoxDriverService.CreateDefaultService();
            service.SuppressInitialDiagnosticInformation = true;
            service.HideCommandPromptWindow = true;
            using (IWebDriver driver = new FirefoxDriver(service, options)) {
                driver.Navigate().GoToUrl(urlString);

                // Wait until page is fully loaded.
                Console.ReadLine();

                // Read in all files changed or modified. The HTML structure for SteamDB's
                // patch notes as of 1/6/2021 is:
                //
                // <ul class="app-history">
                //   <li>
                //     ...
                //     <i>file</i>
                //     <i>...</i>
                //   <li>
                // </ul>
                // 
                // Note: This may of course change and this will need to be modified 
                // accordingly. SteamDB currently uses <i>, <ins>, or <del> for the file.

                var ulElements = driver.FindElements(By.ClassName("app-history"));
                foreach (IWebElement ulElement in ulElements) {
                    var liElements = ulElement.FindElements(By.TagName("li"));
                    foreach (IWebElement liElement in liElements) {
                        var insElements = liElement.FindElements(By.TagName("ins"));
                        var delElements = liElement.FindElements(By.TagName("del"));
                        var iElements = liElement.FindElements(By.TagName("i"));
                        if (insElements.Count > 0) {
                            numAdded++;
                            files.Add(insElements[0].Text);
                        } else if (delElements.Count > 0) {
                            numModified++;
                            files.Add(delElements[0].Text);
                        } else if (iElements.Count > 0) {
                            numDeleted++;
                            files.Add(iElements[0].Text);
                        }
                    }
                }
            }

            // Generate filelist.
            Regex regex = new Regex(@"[a-zA-Z_]+\(*[\w-]*\)*\w*\.[^ ]*\w");
            string fileListPath = Directory.GetCurrentDirectory() + @"\filelist.txt";
            StreamWriter streamWriter = new StreamWriter(fileListPath, false);

            foreach (string file in files) {
                streamWriter.WriteLine(regex.Match(file).Value);
            }
            streamWriter.Flush();
            streamWriter.Close();

            Console.WriteLine(
                "Generated filelist (" + numAdded + " added, " + numModified 
                    + " modified, " + numDeleted + " deleted). Saved to " + fileListPath);

            WriteImportant("\nPress [ENTER] to close.");
            Console.ReadLine();
            return;
        }

        static void WriteError(string output) {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(output);
            Console.ResetColor();
        }

        static void WriteImportant(string output) {
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.WriteLine(output);
            Console.ResetColor();
        }


    }
}
