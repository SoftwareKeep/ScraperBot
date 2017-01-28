using AngleSharp;
using ScraperBotModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Entity;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ScraperBotService
{
    public partial class Service1 : ServiceBase
    {
        List<int> storeThreads = new List<int>();
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            StreamWriter vWriter = new StreamWriter(@"C:\scraperbotlog.txt", true);

            vWriter.WriteLine("Servico Iniciado: " + DateTime.Now.ToString());
            vWriter.Flush();
            vWriter.Close();


            ModelContainer context = new ModelContainer();

            List<int> idStores = context.Stores.Select(x => x.idStore).ToList();
            foreach (int idStore in idStores)
            {
                Thread t = new Thread(() => GetProducts(idStore));
                t.Start();
            }



        }
        protected override void OnStop()
        {
            StreamWriter vWriter = new StreamWriter(@"C:\scraperbotlog.txt", true);

            vWriter.WriteLine("Servico Parado: " + DateTime.Now.ToString());
            vWriter.Flush();
            vWriter.Close();
        }



        private async void GetProducts(int idStore)
        {
            List<string> log = new List<string>();
            log.Add("=================================");
            log.Add(DateTime.Now.ToString());
            //connecting to DB
            ModelContainer context = new ModelContainer();

            //getting configuration
            ScraperBotModel.Configuration configuration = context.Configurations.FirstOrDefault();


            //Getting last link from only expired links list
            DateTime daysBefore = DateTime.Now.AddDays(-configuration.expirationTime);
            Link link = context.Links.Where(x => x.lastVisit < daysBefore && x.idStore == idStore)
                                     .OrderByDescending(x => x.addDate)
                                     .FirstOrDefault();
            if (link != null)
            {
                //Getting store infos based on the link
                Store store = context.Stores.Where(x => x.idStore == idStore)
                                            .FirstOrDefault();

                log.Add("looking for a product at: " + link.link);


                //getting the document
                var document = await BrowsingContext.New(AngleSharp.Configuration.Default.WithDefaultLoader())
                                                    .OpenAsync(link.link);

                //getting title
                var titleSelector = document.QuerySelector(store.titleSelector);

                //List<string> productsType = new List<string> {
                //    "windows",
                //    "office",
                //    "microsoft",
                //    "word",
                //    "powerpoint",
                //    "excel",
                //    "outlook",
                //    "project",
                //    "visio",
                //    "SQL",
                //    "visual",
                //    "studio",
                //    "server",
                //    "access",
                //    "kaspersky",
                //    "mcafee",
                //    "antivirus",
                //    "parallels",
                //    "trend",
                //    "software",
                //    "license",
                //    "download",
                //    "norton"
                //};
                //bool isSoftware = false;

                //foreach (string type in productsType)
                //{
                //    if (titleSelector != null)
                //    {
                //        if (titleSelector.TextContent.Contains(type))
                //        {
                //            isSoftware = true;
                //        }
                //    }
                //}
                //get prices only if title has been found
                if (titleSelector != null)
                {

                    Product product = new Product
                    {
                        date = DateTime.Now,
                        idStore = idStore,

                    };

                    //Feeding title
                    product.title = titleSelector.TextContent;

                    Regex regex = new Regex(configuration.priceRegex);

                    //Feeding Original price
                    var originalPriceSelector = document.QuerySelectorAll(store.originalPriceSelector)
                                                .Where(x => regex.Match(x.TextContent)
                                                                 .Success)
                                                .Select(x => regex.Match(x.TextContent)
                                                                  .Groups[1].Value)
                                                .Min();

                    if (originalPriceSelector != null)
                    {
                        product.originalPrice = Parse(originalPriceSelector);
                    }

                    //Feeding Sale price
                    var salePriceSelector = document.QuerySelectorAll(store.salePriceSelector)
                                                .Where(x => regex.Match(x.TextContent)
                                                                 .Success)
                                                .Select(x => regex.Match(x.TextContent)
                                                                  .Groups[1].Value)
                                                .Min();

                    if (salePriceSelector != null)
                    {
                        product.salePrice = Parse(salePriceSelector) ?? 0;
                    }
                    DateTime date = DateTime.Now;
                    if (context.Products.Where(x => x.title == product.title && DbFunctions.TruncateTime(x.date) == DbFunctions.TruncateTime(date) && x.idStore == idStore).Count() == 0)
                    {
                        log.Add("Product has been found: " + product.title + " | Price: " + ((product.salePrice == 0 || product.salePrice == null) ? product.originalPrice : product.salePrice));
                        //adding this product to the database
                        context.Products.Add(product);
                    }
                }



                //getting all links from this page
                List<string> allLinks = context.Links.Select(x => x.link).ToList();
                Regex regexLink = new Regex(@"^(http:\/\/|https:\/\/)[w]{0,3}\.?" + store.baseUrl);
                List<Link> AllLinksFound = new List<Link>();

                List<string> badWords = context.BadWords.Where(x => x.idStore == store.idStore).Select(x => x.badWord).ToList();

                Regex regexBadWords = new Regex(string.Join("|", badWords));

                List<string> linksFound = document.QuerySelectorAll("a")
                                                  .Where(x => (!string.IsNullOrEmpty(x.GetAttribute("href"))
                                                            && regexLink.Match(x.GetAttribute("href")).Success
                                                            && x.GetAttribute("href").Contains(store.baseUrl)
                                                            && !regexBadWords.Match(x.GetAttribute("href")).Success)
                                                            || (!string.IsNullOrEmpty(x.GetAttribute("href"))
                                                                && !x.GetAttribute("href").Contains("http")
                                                                && !x.GetAttribute("href").Contains("//"))
                                                         )
                                                  .GroupBy(x => x.GetAttribute("href"))
                                                  .Select(x => x.Key.ToString())
                                                  .ToList();

                foreach (string linkFound in linksFound)
                {
                    string linkFormated = (!linkFound.Contains("http")) ? "https://" + store.baseUrl + linkFound : linkFound;

                    if (allLinks.Where(x => x == linkFormated) == null || allLinks.Where(x => x == linkFormated).Count() == 0)
                    {
                        Link newLink = new Link
                        {
                            addDate = DateTime.Now,
                            idStore = store.idStore,
                            lastVisit = DateTime.Now.AddDays(-5),
                            status = true,
                            link = linkFormated

                        };
                        AllLinksFound.Add(newLink);
                    }
                }
                if (AllLinksFound.Count > 0)
                {
                    //Adding all links that was found to the DB
                    context.Links.AddRange(AllLinksFound);
                }
                log.Add("New links found: " + AllLinksFound.Count);

                link.lastVisit = DateTime.Now;

                context.SaveChanges();

                Thread.Sleep(configuration.serviceInterval * 1000);
            }
            else
            {

                Thread.Sleep(60000);
            }
            log.Add(" ");
            Console.WriteLine(string.Join("\r\n",log));
            GetProducts(idStore);

        }
        public decimal? Parse(string incomingValue)
        {
            decimal val;
            if (!decimal.TryParse(incomingValue.Replace(",", "").Replace(".", ""), NumberStyles.Number, CultureInfo.InvariantCulture, out val))
                return null;
            return val / 100;
        }

        internal void TestStartupAndStop(string[] args)
        {
            this.OnStart(args);
            Console.ReadLine();
            this.OnStop();
        }
    }
}
