using Owin;
using Microsoft.Owin;
using Ninject;
using AnagramSolverSite.DI;
using AnagramSolverSite.Services.Interfaces;
using AnagramSolverSite.Services;
using Microsoft.AspNet.SignalR;
using AnagramSolverLib;
[assembly: OwinStartup(typeof(AnagramSolverSite.Startup))]
namespace AnagramSolverSite
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var kernel = new StandardKernel();
            var resolver = new NinjectSignalRDependencyResolver(kernel);

            kernel.Bind<ICrosswordAnagramProcessService>()
                .To<CrosswordAnagramProcessService>().InSingletonScope().WithConstructorArgument("wordFrequencies", (context) =>
                {
                    var wordlistLoader = new WordListLoader();
                    return wordlistLoader.LoadWordFrequencies(System.Web.Hosting.HostingEnvironment.MapPath(@"~\Data\AllWords.zip"));
                });                

            var config = new HubConfiguration();
            config.Resolver = resolver;
            app.MapSignalR(config);

            GlobalHost.DependencyResolver = resolver;
        }
    }
}