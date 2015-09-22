using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(InputRequiredExample.Startup))]
namespace InputRequiredExample
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
