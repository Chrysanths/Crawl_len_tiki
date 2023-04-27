using System;
using System.Collections.Generic;
using System.Text;

namespace TikiCrawler
{
    public class Sanpham
    {
        public int ID { get; set; }
        public string type { get; set; }
        public string SKU { get; set; }
        public string title { get; set; }
        public string brand { get; set; }
        public string price { get; set; }
        public string img { get; set; }
        public string parent { get; set; }
        public string position { get; set; }
        public string attribute_1_name { get; set; }
        public string attribute_1_value { get; set; }
        public string attribute_1_visible { get; set; }
        public string attribute_1_global { get; set; }

        public string color { get; set; }
        public string detail { get; set; }
        public string description { get; set; }

        public void Print()
        {

            Console.WriteLine("\nINFOMATION OF PRODUCT\nID " + ID);
            Console.WriteLine("TYPE: " + type);
        }

    }
}
