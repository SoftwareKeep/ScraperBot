using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ScraperBot.Models
{
    public class StoreTable
    {
       public string storeName { get; set; }
       public string baseURL { get; set; }
       public string titleSelector { get; set; }
       public string originalPriceSelector { get; set; }
       public string salePriceSelector { get; set; }
       public string currency { get; set; }
       public List<string> badwords { get; set; }
       public decimal crawled { get; set; }
       public int productsFound { get; set; }
    }
}