using ScraperBot.Models;
using ScraperBotModel;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ScraperBot.Controllers
{
    public class StoreController : Controller
    {
        // GET: Store
        public ActionResult Index()
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
                badwords = x.BadWords.Select(y=>y.badWord).ToList(),
                productsFound = x.Product.Count,
                crawled = 100 - (100 * x.Link.Where(y=> DbFunctions.TruncateTime(y.lastVisit) < DbFunctions.TruncateTime(outdatedDate)).Count() / x.Link.Count())

            }).ToList();

            return View(stores);
        }

        [HttpPost]
        public ActionResult Add(StoreTable storeTable)
        {

            ModelContainer context = new ModelContainer();
            List<Link> link = new List<Link> {
                new Link
            {
                addDate = DateTime.Now,
                lastVisit = DateTime.Now.AddDays(-20),
                status = true,
                link = "http://" + storeTable.baseURL,

            }
                };

            List<BadWords> badwords = new List<BadWords>();
            List<string> bwords = storeTable.badwords[0].Split(',').ToList();

            foreach (string badword in bwords)
            {
                BadWords bw = new BadWords
                {
                    badWord = badword.Trim()

                };
                badwords.Add(bw);
            }

            Store store = new Store
            {
                name = storeTable.storeName,
                baseUrl = storeTable.baseURL,
                currency = storeTable.currency,
                titleSelector = storeTable.titleSelector,
                originalPriceSelector = storeTable.originalPriceSelector,
                salePriceSelector = storeTable.salePriceSelector,
                Link = link,
                BadWords = badwords
            };
            context.Stores.Add(store);
            context.SaveChanges();

            return RedirectToAction("Index");
        }
    }
}