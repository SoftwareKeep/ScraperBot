using System.Linq;
using System.Web.Mvc;
using System.Threading.Tasks;
using ScraperBotModel;
using System;
using ScraperBot.Models;
using System.Collections.Generic;
using System.Data.Entity;

namespace ScraperBot.Controllers
{
    public class HomeController : Controller
    {
        public  ActionResult Index()
        {

            ModelContainer context = new ModelContainer();


            //getting configuration
            ScraperBotModel.Configuration configuration = context.Configurations.FirstOrDefault();

            DateTime outdatedDate = DateTime.Now.AddDays(-configuration.expirationTime);

            List<StoreTable> stores = context.Stores.Select(x => new StoreTable
            {
                storeName = x.name,
                baseURL = x.baseUrl,
                titleSelector = x.titleSelector,
                originalPriceSelector = x.originalPriceSelector,
                salePriceSelector = x.salePriceSelector,
                currency = x.currency,
                badwords = x.BadWords.Select(y => y.badWord).ToList(),
                productsFound = x.Product.Count,
                crawled = 100 - (100 * x.Link.Where(y => DbFunctions.TruncateTime(y.lastVisit) < DbFunctions.TruncateTime(outdatedDate)).Count() / x.Link.Count())

            }).ToList();

            return View(stores);
        }

    }
}