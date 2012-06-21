using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.MicroKernel.SubSystems.Configuration;
using SignalR.Hubs;

namespace ECommDemo.SignalR
{
	public class HubsInstallers : IWindsorInstaller
	{
		public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                AllTypes.FromThisAssembly()
                    .BasedOn(typeof (Hub))
                );
        }
	}
}
