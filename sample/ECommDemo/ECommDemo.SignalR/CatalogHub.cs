using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SignalR.Hubs;
using SimpleCqrs.Commanding;

namespace ECommDemo.SignalR
{
	[HubName("catalogHub")]
	public class CatalogHub : Hub
	{
		public CatalogHub()
		{}

		public CatalogHub(ICommandBus commandBus)
		{
			CommandBus = commandBus;
		}

		private ICommandBus CommandBus { get; set; }

		public string Echo(string message)
		{
			return message;
		}
	}
}
