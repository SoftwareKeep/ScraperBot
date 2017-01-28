using ScraperBotModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ScraperBot.Models
{
    public class ProductTable
    {
        public string productName { get; set; }
        public List<Price> prices { get; set; }
        public DateTime? lastCrawl { get; set; }
        public Store store { get; set; }
    }
    
}