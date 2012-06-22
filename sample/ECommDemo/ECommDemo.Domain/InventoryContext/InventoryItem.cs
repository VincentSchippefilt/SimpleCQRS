using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimpleCqrs.Domain;

namespace ECommDemo.Domain.InventoryContext
{
    public class InventoryItemSnapshot : Snapshot
    {
        public string ItemId { get; set; }
        public string Description { get; set; }
    }

    public class InventoryItem : AggregateRoot, ISnapshotOriginator
    {
        public string ItemId { get; protected set; }

        public string Description { get; protected set; }

        public decimal Availability { get; protected set; }

        public string UnitOfMeasure { get; protected set; }

        public InventoryItem()
        {
        }

        public InventoryItem(string id, string description)
        {
            RaiseEvent(new InventoryItemCreatedEvent(id, description)
                      {
                          AggregateRootId = Guid.NewGuid()
                      });
        }

        
        protected void Apply(InventoryItemCreatedEvent e)
        {
            this.Id = e.AggregateRootId;
            this.ItemId = e.ItemId;
            this.Description = e.Description;
        }

        public Snapshot GetSnapshot()
        {
            return new InventoryItemSnapshot()
                       {
                           ItemId = this.ItemId,
                           Description = this.Description
                       };

        }

        public void LoadSnapshot(Snapshot snapshot)
        {
            var s = (InventoryItemSnapshot) snapshot;
            this.ItemId = s.ItemId;
            this.Description = s.Description;
        }

        public bool ShouldTakeSnapshot(Snapshot previousSnapshot)
        {
            return  previousSnapshot == null || 
                    previousSnapshot.LastEventSequence < this.LastEventSequence;
        }
    }
}
