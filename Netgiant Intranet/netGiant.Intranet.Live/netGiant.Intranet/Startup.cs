using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(netGiant.Intranet.Startup))]
namespace netGiant.Intranet
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
