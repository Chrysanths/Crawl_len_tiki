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
            string path = @"D:\Thiết kế hệ thống\TikiCrawler\import.csv";

            //header
            string format = "@ID@Type@SKU@Name@Published@Is featured?@Visibility in catalog@Short description@Description@Date sale price starts@Date sale price ends@Tax status@Tax class@In stock?@Stock@Low stock amount@Backorders allowed?@Sold individually?@Weight (kg)@Length (cm)@Width (cm)@Height (cm)@Allow customer reviews?@Purchase note@Sale price@Regular price@Categories@Tags@Shipping class@Images@Download limit@Download expiry days@Parent@Grouped products@Upsells@Cross-sells@External URL@Button text@Position@Swatches Attributes@Attribute 1 name@Attribute 1 value(s)@Attribute 1 visible@Attribute 1 global@Attribute 2 name@Attribute 2 value(s)@Attribute 2 visible@Attribute 2 global@Attribute 3 name@Attribute 3 value(s)@Attribute 3 visible@Attribute 3 global@Attribute 4 name@Attribute 4 value(s)@Attribute 4 visible@Attribute 4 global@Attribute 1 default\n";
            string header = format;

            await File.WriteAllTextAsync(path, header.ToString());

            List<Sanpham> AllSanpham = new List<Sanpham>();
            List<string> listProductLink = new List<string>();
            int ID = 0;
            int dem = 0;
            
            browser.Navigate().GoToUrl("https://tiki.vn/search?q=len&page=2");
            //browser.Navigate().GoToUrl("https://tiki.vn/search?q=len");
            var products = browser.FindElements(By.ClassName("product-item"));
            foreach (var product in products)
            {
                try
                {
                    string productLink = product.GetAttribute("outerHTML");

                    productLink = product.GetAttribute("href");
                    listProductLink.Add(productLink);
                    dem++;
                    Console.WriteLine("\n" + dem + "\n" + productLink);
                }
                catch
                { continue; }
            }

            //Go to each product link
            for (int i = 0; i < listProductLink.Count; i++)
            {
                Console.WriteLine(i);
                Sanpham product = new Sanpham();
                Console.WriteLine("DEBUG: " + listProductLink[i].ToString());
                browser.Navigate().GoToUrl(listProductLink[i]);

                //ADD product id
                product.ID = ID +1;
                ID++;

                //ADD product type
                product.type = "simple";

                //Extract product title
                product.title = browser.FindElements(By.CssSelector(".title"))[1].Text.ToString();
                Console.WriteLine("DEBUG TITLE: " + product.title);

                //Extract product brand
                product.brand = browser.FindElements(By.XPath("//a[@data-view-id='pdp_details_view_brand']"))[0].Text.ToString();
                Console.WriteLine("DEBUG BRAND: " + product.brand);

                //Extract product price
                try
                {
                    product.price = browser.FindElement(By.CssSelector(".product-price__current-price")).Text.ToString();
                }
                catch
                {
                    try
                    {
                        product.price = browser.FindElements(By.CssSelector(".styles__Price-sc-6hj7z9-1"))[0].Text.ToString();
                    }
                    catch
                    {
                        product.price = browser.FindElement(By.ClassName("list-price")).Text.ToString();
                    }
                }
                
                product.price = Regex.Match(product.price, "^[\\d|\\.|\\,]+").Value;
                product.price = Regex.Replace(product.price, "\\.", "");
                Console.WriteLine("DEBUG PRICE: " + product.price);

                //Extract product images
                string htmlItem = browser.PageSource;
                MatchCollection matchesImage = Regex.Matches(htmlItem, "\"base_url\":\"(.*?)\"");
                int count = 0;
                int countImg = matchesImage.Count;

                foreach (Match matchImage in matchesImage)
                {
                    var temp = matchImage.Groups[1].Value;
                    product.img += temp.ToString();
                    if (count +1 < countImg)
                        product.img += ", ";
                    count++;
                }
                Console.WriteLine("\nIMG: " + product.img);

                //Extract colors
                List<string> color = new List<string>();
                product.color = "";
                int countColor = browser.FindElements(By.ClassName("option-label")).Count;

                if (countColor == 0)
                {
                    countColor = browser.FindElements(By.XPath("//button[@data-view-id='pdp_main_select_configuration_item']")).Count;

                    for (int j = 0; j < countColor; j++)
                    {
                        string temp_color = browser.FindElements(By.XPath("//button[@data-view-id='pdp_main_select_configuration_item']"))[j].Text.ToString();
                        product.color += temp_color;
                        
                        color.Add(temp_color);
                        if (j + 1 < countColor)
                            product.color += ", ";
                    }
                }
                else
                {
                    for (int j = 0; j < countColor; j++)
                    {
                        string temp_color = browser.FindElements(By.ClassName("option-label"))[j].Text.ToString();
                        product.color += temp_color;

                        color.Add(temp_color);
                        if (j + 1 < countColor)
                            product.color += ", ";
                    }
                }
                string price = product.price;
                
                if(countColor>0)
                {
                    product.type = "variable";
                    
                    product.price = "";
                    product.attribute_1_name = "Phân loại";
                    product.attribute_1_value = product.color;
                    product.attribute_1_visible = "1";
                    product.attribute_1_global = "1";
                }
                Console.WriteLine(countColor + " COLOR: " + product.color);

                //Extract product details
                try
                {
                    string findDetails = browser.FindElement(By.ClassName("has-table")).GetAttribute("outerHTML");
                    try
                    {
                        Match match = Regex.Match(findDetails, "<td>(.*?)</td>", RegexOptions.Singleline);
                        int index = 1;
                        while (match.Success)
                        {
                            product.detail += match.Groups[1].Value;

                            try
                            {
                                product.detail = Regex.Replace(product.detail, "<img.*?>", "");
                                product.detail = Regex.Replace(product.detail, "<div.*?>", "");
                                product.detail = Regex.Replace(product.detail, "<span.*?>", "");
                                product.detail = Regex.Replace(product.detail, "<a.*?>", "");
                                product.detail = Regex.Replace(product.detail, "<iframe.*?></iframe>", "");
                                product.detail = Regex.Replace(product.detail, "class=\".*?\"", "");
                                product.detail = Regex.Replace(product.detail, "style=\".*?\"", "");
                                product.detail = Regex.Replace(product.detail, "\"", "");
                            }
                            catch { continue; }

                            if (index % 2 != 0)
                                product.detail += ": ";
                            else
                                product.detail += "\n";

                            index++;

                            match = match.NextMatch();
                        }
                    }
                    catch (RegexMatchTimeoutException) { }
                }
                catch { continue; }
                
                Console.WriteLine("DETAIL: " + product.detail);

                //Extract product description
                string findDescription = browser.FindElement(By.ClassName("wyACs")).GetAttribute("outerHTML");
                product.description = Regex.Match(findDescription, "wyACs(.*?)>([\\s\\S]*)</div>").Groups[2].Value;

                try
                {
                    product.description = Regex.Replace(product.description, "<img.*?>", "");
                    product.description = Regex.Replace(product.description, "<div.*?>", "");
                    product.description = Regex.Replace(product.description, "<span.*?>", "");
                    product.description = Regex.Replace(product.description, "<a.*?>", "");
                    product.description = Regex.Replace(product.description, "<iframe.*?></iframe>", "");
                    product.description = Regex.Replace(product.description, "class=\".*?\"", "");
                    product.description = Regex.Replace(product.description, "style=\".*?\"", "");
                    product.description = Regex.Replace(product.description, "\"", "");
                }
                catch { continue; }

                Console.WriteLine("DESCRIPTION: "+ product.description);

                AllSanpham.Add(product);

                //write data
                string data = format.Replace("@ID", "@\"" + product.ID + "\"");
                data = data.Replace("@Type", "@\"" + product.type + "\"");
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
                data = data.Replace("@Weight (kg)", "@\"\"");
                data = data.Replace("@Length (cm)", "@\"\"");
                data = data.Replace("@Width (cm)", "@\"\"");
                data = data.Replace("@Height (cm)", "@\"\"");
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
                data = data.Replace("@Swatches Attributes", "@\"\"");
                data = data.Replace("@Attribute 1 name", "@\"" + product.attribute_1_name + "\"");
                data = data.Replace("@Attribute 1 value(s)", "@\"" + product.attribute_1_value + "\"");
                data = data.Replace("@Attribute 1 visible", "@\"" + product.attribute_1_visible + "\"");
                data = data.Replace("@Attribute 1 global", "@\"" + product.attribute_1_global + "\"");
                data = data.Replace("@Attribute 2 name", "@\"\"");
                data = data.Replace("@Attribute 2 value(s)", "@\"\"");
                data = data.Replace("@Attribute 2 visible", "@\"\"");
                data = data.Replace("@Attribute 2 global", "@\"\"");
                data = data.Replace("@Attribute 3 name", "@\"\"");
                data = data.Replace("@Attribute 3 value(s)", "@\"\"");
                data = data.Replace("@Attribute 3 visible", "@\"\"");
                data = data.Replace("@Attribute 3 global", "@\"\"");
                data = data.Replace("@Attribute 4 name", "@\"\"");
                data = data.Replace("@Attribute 4 value(s)", "@\"\"");
                data = data.Replace("@Attribute 4 visible", "@\"\"");
                data = data.Replace("@Attribute 4 global", "@\"\"");
                data = data.Replace("@Attribute 1 default", "@\"\"" + "\n");

                File.AppendAllText(path, data);

                //Append data for attibute color
                for (int temp1 = 0; temp1 < countColor; temp1++)
                {
                    Sanpham sp = new Sanpham();
                    sp.ID = ID + 1;
                    ID++;

                    sp.title = product.title;
                    sp.type = "variation";
                    sp.price = price;
                    sp.parent = "id:" + product.ID;
                    sp.position = (temp1 + 1).ToString();
                    sp.attribute_1_name = "Phân loại";
                    sp.attribute_1_value = color[temp1];
                    sp.attribute_1_visible = "";
                    sp.attribute_1_global = "1";

                    data = format.Replace("@ID", "@\"" + sp.ID + "\"");
                    data = data.Replace("@Type", "@\"" + sp.type + "\"");
                    data = data.Replace("@SKU", "@\"\"");
                    data = data.Replace("@Name", "@\"" + sp.title + "\"");
                    data = data.Replace("@Published", "@1");
                    data = data.Replace("@Is featured?", "@\"0\"");
                    data = data.Replace("@Visibility in catalog", "@visible");
                    data = data.Replace("@Short description", "@\"" + sp.detail + "\"");
                    data = data.Replace("@Description", "@\"" + sp.description + "\"");
                    data = data.Replace("@Date sale price starts", "@\"\"");
                    data = data.Replace("@Date sale price ends", "@\"\"");
                    data = data.Replace("@Tax status", "@taxable");
                    data = data.Replace("@Tax class", "@parent");
                    data = data.Replace("@In stock?", "@\"1\"");
                    data = data.Replace("@Stock", "@\"\"");
                    data = data.Replace("@Low stock amount", "@\"\"");
                    data = data.Replace("@Backorders allowed?", "@\"0\"");
                    data = data.Replace("@Sold individually?", "@\"0\"");
                    data = data.Replace("@Weight (kg)", "@\"\"");
                    data = data.Replace("@Length (cm)", "@\"\"");
                    data = data.Replace("@Width (cm)", "@\"\"");
                    data = data.Replace("@Height (cm)", "@\"\"");
                    data = data.Replace("@Allow customer reviews?", "@\"0\"");
                    data = data.Replace("@Purchase note", "@\"\"");
                    data = data.Replace("@Sale price", "@\"\"");
                    data = data.Replace("@Regular price", "@\"" + sp.price + "\"");
                    data = data.Replace("@Categories", "@\"\"");
                    data = data.Replace("@Tags", "@\"\"");
                    data = data.Replace("@Shipping class", "@\"\"");
                    data = data.Replace("@Images", "@\"" + sp.img + "\"");
                    data = data.Replace("@Download limit", "@\"\"");
                    data = data.Replace("@Download expiry days", "@\"\"");
                    data = data.Replace("@Parent", "@\"" + sp.parent + "\"");
                    data = data.Replace("@Grouped products", "@\"\"");
                    data = data.Replace("@Upsells", "@\"\"");
                    data = data.Replace("@Cross-sells", "@\"\"");
                    data = data.Replace("@External URL", "@\"\"");
                    data = data.Replace("@Button text", "@\"\"");
                    data = data.Replace("@Position", "@\"" + sp.position + "\"");
                    data = data.Replace("@Swatches Attributes", "@\"\"");
                    data = data.Replace("@Attribute 1 name", "@\"" + sp.attribute_1_name + "\"");
                    data = data.Replace("@Attribute 1 value(s)", "@\"" + sp.attribute_1_value + "\"");
                    data = data.Replace("@Attribute 1 visible", "@\"" + sp.attribute_1_visible + "\"");
                    data = data.Replace("@Attribute 1 global", "@\"" + sp.attribute_1_global + "\"");
                    data = data.Replace("@Attribute 2 name", "@\"\"");
                    data = data.Replace("@Attribute 2 value(s)", "@\"\"");
                    data = data.Replace("@Attribute 2 visible", "@\"\"");
                    data = data.Replace("@Attribute 2 global", "@\"\"");
                    data = data.Replace("@Attribute 3 name", "@\"\"");
                    data = data.Replace("@Attribute 3 value(s)", "@\"\"");
                    data = data.Replace("@Attribute 3 visible", "@\"\"");
                    data = data.Replace("@Attribute 3 global", "@\"\"");
                    data = data.Replace("@Attribute 4 name", "@\"\"");
                    data = data.Replace("@Attribute 4 value(s)", "@\"\"");
                    data = data.Replace("@Attribute 4 visible", "@\"\"");
                    data = data.Replace("@Attribute 4 global", "@\"\"");
                    data = data.Replace("@Attribute 1 default", "@\"\"" + "\n");

                    File.AppendAllText(path, data);
                }
                System.Threading.Thread.Sleep(1000);
            }
            Console.WriteLine("The data has been successfully saved to the CSV file");
        }
    }
}
