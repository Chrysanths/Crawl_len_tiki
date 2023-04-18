using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace TikiCrawler
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //Create an instance of Chrome driver
            IWebDriver browser = new ChromeDriver();

            //path
            string path = @"D:\Thiết kế hệ thống\TikiCrawler\import.txt";

            //header
            string format = "@ID@Type@SKU@Name@Published@Is featured?@Visibility in catalog@Short description@Description@Date sale price starts@Date sale price ends@Tax status@Tax class@In stock?@Stock@Low stock amount@Backorders allowed?@Sold individually?@Weight(kg)@Length(cm)@Width(cm)@Height(cm)@Allow customer reviews?@Purchase note@Sale price@Regular price@Categories@Tags@Shipping class@Images@Download limit@Download expiry days@Parent@Grouped products@Upsells@Cross-sells@External URL@Button text@Position@Attribute 1 name@Attribute 1 value(s)@Attribute 1 visible@Attribute 1 global@Attribute 1 default\n";
            string header = format;

            await File.WriteAllTextAsync(path, header.ToString());

            List<Sanpham> AllSanpham = new List<Sanpham>();
            List<string> listProductLink = new List<string>();
            int dem = 1;

            do
            {
                browser.Navigate().GoToUrl("https://tiki.vn/search?q=len");
                if(dem >50)
                    browser.Navigate().GoToUrl("https://tiki.vn/search?q=len&page=2");

                //Select all product items by CSS Selector
                var products = browser.FindElements(By.CssSelector(".product-item"));

                foreach (var product in products)
                {
                    try
                    {
                        string productLink = product.GetAttribute("href");
                        listProductLink.Add(productLink);
                        Console.WriteLine(dem);
                        dem++;
                        Console.WriteLine(productLink);
                    }
                    catch
                    { continue; }
                }
            }
            while(dem <100);
            //Go to each product link
            for (int i = 0; i < listProductLink.Count; i++)
            {
                Console.WriteLine(i);
                Sanpham product = new Sanpham();
                Console.WriteLine("DEBUG: " + listProductLink[i].ToString());
                browser.Navigate().GoToUrl(listProductLink[i]);

                //Extract product title
                product.title = browser.FindElements(By.CssSelector(".title"))[1].Text.ToString();
                Console.WriteLine("DEBUG TITLE: " + product.title);

                //Extract product brand
                product.brand = browser.FindElements(By.XPath("//a[@data-view-id='pdp_details_view_brand']"))[0].Text.ToString();
                Console.WriteLine("DEBUG BRAND: " + product.brand);

                //Extract product price
                product.price = "";
                try
                {
                    product.price = browser.FindElement(By.CssSelector(".product-price__current-price")).Text.ToString();
                }
                catch
                {
                    product.price = browser.FindElements(By.CssSelector(".styles__Price-sc-6hj7z9-1"))[0].Text.ToString();
                }
                product.price = Regex.Match(product.price, "^[\\d|\\.|\\,]+").Value;
                Console.WriteLine("DEBUG PRICE: " + product.price);

                //Extract product images
                int countImg = browser.FindElements(By.XPath("//a[@data-view-id='pdp_main_view_photo']")).Count;
                product.img = browser.FindElements(By.ClassName("fWjUGo"))[2].GetAttribute("src") + ", ";

                for (int x = 0; x < countImg; x++)
                {
                    string altImg = "product-img-" + x;
                    string xp = "//img[@alt='" + altImg + "']";
                    product.img += browser.FindElements(By.XPath(xp))[0].GetAttribute("src");
                    if (x + 1 < countImg)
                        product.img += ", ";
                }
                Console.WriteLine(countImg + " IMG: " + product.img);

                //Extract colors
                product.color = "";
                int countColor = browser.FindElements(By.ClassName("option-label")).Count;

                if (countColor == 0)
                {
                    countColor = browser.FindElements(By.XPath("//button[@data-view-id='pdp_main_select_configuration_item']")).Count;

                    for (int j = 0; j < countColor; j++)
                    {
                        product.color += browser.FindElements(By.XPath("//button[@data-view-id='pdp_main_select_configuration_item']"))[j].Text.ToString();
                        if (j + 1 < countColor)
                            product.color += ", ";
                    }
                }
                else
                {
                    for (int j = 0; j < countColor; j++)
                    {
                        product.color += browser.FindElements(By.ClassName("option-label"))[j].Text.ToString();
                        if (j + 1 < countColor)
                            product.color += ", ";
                    }
                }
                Console.WriteLine(countColor + " COLOR: " + product.color);

                //Extract product details
                string findDetails = browser.FindElement(By.ClassName("has-table")).GetAttribute("outerHTML");
                try
                {
                    Match match = Regex.Match(findDetails, "<td>(.*?)</td>", RegexOptions.Singleline);
                    int index = 1;
                    while (match.Success)
                    {
                        product.detail += match.Groups[1].Value;

                        if (index % 2 != 0)
                            product.detail += ": ";
                        else
                            product.detail += "\n";

                        index++;

                        match = match.NextMatch();
                    }
                }
                catch (RegexMatchTimeoutException){}
                Console.WriteLine("DETAIL: " + product.detail);

                //Extract product description
                string findDescription = browser.FindElement(By.ClassName("wyACs")).GetAttribute("outerHTML");
                product.description = Regex.Match(findDescription, "wyACs(.*?)>([\\s\\S]*)</div>").Groups[2].Value;
                Console.WriteLine("DESCRIPTION: "+ product.description);

                AllSanpham.Add(product);

                //write data
                string data = format.Replace("@ID", "@\"\"");
                data = data.Replace("@Type", "@simple");
                data = data.Replace("@SKU", "@\"\"");
                data = data.Replace("@Name", "@\"" + product.title + "\"");
                data = data.Replace("@Published", "@1");
                data = data.Replace("@Is featured?", "@\"0\"");
                data = data.Replace("@Visibility in catalog", "@visible");
                data = data.Replace("@Short description", "@\"" + product.detail + "\"");
                data = data.Replace("@Description", "@\"" + product.description + "\"");
                data = data.Replace("@Date sale price starts", "@\"\"");
                data = data.Replace("@Date sale price ends", "@\"\"");
                data = data.Replace("@Tax status", "@taxable");
                data = data.Replace("@Tax class", "@\"\"");
                data = data.Replace("@In stock?", "@\"1\"");
                data = data.Replace("@Stock", "@\"\"");
                data = data.Replace("@Low stock amount", "@\"\"");
                data = data.Replace("@Backorders allowed?", "@\"0\"");
                data = data.Replace("@Sold individually?", "@\"0\"");
                data = data.Replace("@Weight(kg)", "@\"\"");
                data = data.Replace("@Length(cm)", "@\"\"");
                data = data.Replace("@Width(cm)", "@\"\"");
                data = data.Replace("@Height(cm)", "@\"\"");
                data = data.Replace("@Allow customer reviews?", "@\"1\"");
                data = data.Replace("@Purchase note", "@\"\"");
                data = data.Replace("@Sale price", "@\"\"");
                data = data.Replace("@Regular price", "@\"" + product.price + "\"");
                data = data.Replace("@Categories", "@\"\"");
                data = data.Replace("@Tags", "@\"\"");
                data = data.Replace("@Shipping class", "@\"\"");
                data = data.Replace("@Images", "@\"" + product.img + "\"");
                data = data.Replace("@Download limit", "@\"\"");
                data = data.Replace("@Download expiry days", "@\"\"");
                data = data.Replace("@Parent", "@\"\"");
                data = data.Replace("@Grouped products", "@\"\"");
                data = data.Replace("@Upsells", "@\"\"");
                data = data.Replace("@Cross-sells", "@\"\"");
                data = data.Replace("@External URL", "@\"\"");
                data = data.Replace("@Button text", "@\"\"");
                data = data.Replace("@Position", "@\"0\"");
                data = data.Replace("@Attribute 1 name", "@\"\"");
                data = data.Replace("@Attribute 1 value(s)", "@\"\"");
                data = data.Replace("@Attribute 1 visible", "@\"\"");
                data = data.Replace("@Attribute 1 global", "@\"\"");
                data = data.Replace("@Attribute 1 default", "@\"\"" + "\n");

                File.AppendAllText(path, data);

                System.Threading.Thread.Sleep(5000);
            }

            Console.WriteLine("The data has been successfully saved to the CSV file");

        }
    }
}
