using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimpleCqrs.Domain;
using SimpleCqrs.Eventing;

namespace ECommDemo.Domain.ShopContext
{
    public class ShopItemCreatedEvent : DomainEvent
    {
        public string ItemId { get; protected set; }
        public string Description { get; protected set; }

        public ShopItemCreatedEvent(string itemId, string description)
        {
            ItemId = itemId;
            Description = description;
        }
    }

    public class ShopItem : AggregateRoot
    {
        public string ItemId { get; protected set; }
        public string Description { get; protected set; }
        public decimal BasePrice { get; protected set; }

        public ShopItem()
        {
        }

        public ShopItem(string id, string description)
        {
            RaiseEvent(
                new ShopItemCreatedEvent(id, description)
                      {
                          AggregateRootId = Guid.NewGuid()
                      }
             );
        }

        /// <summary>
        /// This use the new Applyer based on LCG, it is better then relaying on 
        /// the convention OnEventName, that is too sensitive to domain event refactoring
        /// during developement.
        /// </summary>
        /// <param name="e"></param>
        protected void Apply(ShopItemCreatedEvent e)
        {
            this.Id = e.AggregateRootId;
            this.ItemId = e.ItemId;
            this.Description = e.Description;
        }
    }
}
