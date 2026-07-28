using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(DP001Website.Startup))]
namespace DP001Website
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
