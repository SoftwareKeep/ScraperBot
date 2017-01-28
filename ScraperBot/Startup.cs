using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(ScraperBot.Startup))]
namespace ScraperBot
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
