using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Castle.Windsor;
using ECommDemo.Domain.Support;
using ECommDemo.ViewModel.Support;
using ECommDemo.Web.Support;
using SignalR;
using ECommDemo.SignalR;

namespace ECommDemo.Web
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication
    {
        private RuntimeBootstrapper _bootstrapper;
        private IWindsorContainer Container { get; set; }

        protected void Application_Start()
        {
			Container = new WindsorContainer();
           
			// signalR configuration: must appear before everything else
			GlobalHost.DependencyResolver = new CastleWindsorDependencyResolver(Container);
			RouteTable.Routes.MapHubs();

            AreaRegistration.RegisterAllAreas();

            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            SetupContainer();
            _bootstrapper = new RuntimeBootstrapper(Container, AssemblyToScanProvider.List);
            _bootstrapper.Start();
        }

        protected void SetupContainer()
        {
            Container.Install(
                    new SupportInstaller(),
                    new HandlersInstaller(),
                    new DomainEventHandlersInstaller(),
					new HubsInstallers(),
                    new ViewModelInstaller().AddReaders().AddWriters()
                );
        
            ControllerBuilder.Current.SetControllerFactory(new WindsorControllerFactory(Container));
        }

        protected void Application_End()
        {
            if (_bootstrapper != null)
                _bootstrapper.Stop();

            if(Container != null)
                Container.Dispose();
        }
    }
}