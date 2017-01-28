using ScraperBot.Models;
using ScraperBotModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ScraperBot.Controllers
{
    public class ProductController : Controller
    {
        // GET: Product
        public ActionResult Index()
        {
            string productName = Request["productName"];
            
            string eProductName = Request["eProductName"];
            ModelContainer context = new ModelContainer();

            IQueryable<Product> products = context.Products;
            if (!string.IsNullOrEmpty(productName))
            {
                List<string> terms = productName.Split(' ').ToList();
                foreach (string term in terms)
                {
                    products = products.Where(x => x.title.Contains(term));
                }
                
            }
            if (!string.IsNullOrEmpty(eProductName))
            {
                List<string> terms = eProductName.Split(' ').ToList();
                foreach (string term in terms)
                {
                    products = products.Where(x => x.title.Contains(term));
                }
            }
            List<ProductTable> productsTable = products.GroupBy(x => x.title).Select(n => new ProductTable
            {
                productName = n.Key,
                lastCrawl = n.OrderByDescending(x => x.date).Select(x => x.date).FirstOrDefault(),
                store = n.FirstOrDefault().Store,
                prices = n.Select(x => new Price
                {
                    date = x.date,
                    price = ((x.salePrice == null || x.salePrice == 0) ? x.originalPrice : x.salePrice) ?? 0
                }).ToList()
            }).ToList();
            
            return View(productsTable);
        }
    }
}