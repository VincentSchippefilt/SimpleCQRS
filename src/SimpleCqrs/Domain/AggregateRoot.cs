using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using SimpleCqrs.Eventing;
using System.Reflection.Emit;

namespace SimpleCqrs.Domain
{
    public abstract class AggregateRoot
    {
        private readonly Queue<DomainEvent> uncommittedEvents = new Queue<DomainEvent>();
        private readonly List<Entity> entities = new List<Entity>(); 

        public Guid Id { get; protected internal set; }
        public int LastEventSequence { get; protected internal set; }

        public ReadOnlyCollection<DomainEvent> UncommittedEvents
        {
            get { return new ReadOnlyCollection<DomainEvent>(uncommittedEvents.ToList()); }
        }

        public void LoadFromHistoricalEvents(params DomainEvent[] domainEvents)
        {
            if(domainEvents.Length == 0) return;

            var domainEventList = domainEvents.OrderBy(domainEvent => domainEvent.Sequence).ToList();
            LastEventSequence = domainEventList.Last().Sequence;

            domainEventList.ForEach(ApplyEventToInternalState);
        }

        public void RaiseEvent(DomainEvent domainEvent)
        {
            domainEvent.Sequence = ++LastEventSequence;
            ApplyEventToInternalState(domainEvent);
            domainEvent.AggregateRootId = Id;
            domainEvent.EventDate = DateTime.Now;
            
            EventModifier.Modify(domainEvent);

            uncommittedEvents.Enqueue(domainEvent);
        }

        public void CommitEvents()
        {
            uncommittedEvents.Clear();
            entities.ForEach(entity => entity.CommitEvents());
        }

        public void RegisterEntity(Entity entity)
        {
            entity.AggregateRoot = this;
            entities.Add(entity);
        }

        private void ApplyEventToInternalState(DomainEvent domainEvent)
        {
            var domainEventType = domainEvent.GetType();
            var domainEventTypeName = domainEventType.Name;
            var aggregateRootType = GetType();

        	var eventHandlerMethodName = GetEventHandlerMethodName(domainEventTypeName);
        	var methodInfo = aggregateRootType.GetMethod(eventHandlerMethodName,
                                                         BindingFlags.Instance | BindingFlags.Public |
                                                         BindingFlags.NonPublic, null, new[] {domainEventType}, null);

            if (methodInfo != null && EventHandlerMethodInfoHasCorrectParameter(methodInfo, domainEventType))
            {
                methodInfo.Invoke(this, new[] { domainEvent });
            }
            else 
            { 
                //I've not found the event with the convention, use the LCG applyer
                applyInvokers.Invoke(this, domainEvent);
            }

            ApplyEventToEntities(domainEvent);
        }

        private void ApplyEventToEntities(DomainEvent domainEvent)
        {
            var entityDomainEvent = domainEvent as EntityDomainEvent;
            if (entityDomainEvent == null) return;

            var list = entities
                .Where(entity => entity.Id == entityDomainEvent.EntityId).ToList();
            list
                .ForEach(entity => entity.ApplyHistoricalEvents(entityDomainEvent));
        }

        private static string GetEventHandlerMethodName(string domainEventTypeName)
        {
            var eventIndex = domainEventTypeName.LastIndexOf("Event");
            return "On" + domainEventTypeName.Remove(eventIndex, 5);
        }

        private static bool EventHandlerMethodInfoHasCorrectParameter(MethodInfo eventHandlerMethodInfo, Type domainEventType)
        {
            var parameters = eventHandlerMethodInfo.GetParameters();
            return parameters.Length == 1 && parameters[0].ParameterType == domainEventType;
        }

        /// <summary>
        /// it is not thread safe for now, it is not a problem for this test.
        /// </summary>
        private static LcgApplyInvoker applyInvokers = new LcgApplyInvoker(); 

    }

    internal class LcgApplyInvoker 
    {
        /// <summary>
        /// Cache of appliers, for each domain object I have a dictionary of actions
        /// </summary>
        private Dictionary<Type, Dictionary<Type, Action<Object, Object>>> lcgCache =
            new Dictionary<Type, Dictionary<Type, Action<Object, Object>>>();

        public void Invoke(Object obj, DomainEvent domainEvent)
        {
            if (!lcgCache.ContainsKey(obj.GetType()))
            {
                var typeCache = new Dictionary<Type, Action<Object, Object>>();

                var applyMethods = obj.GetType().GetMethods(
                    BindingFlags.NonPublic |
                    BindingFlags.Instance);

                foreach (var item in applyMethods
                    .Where(am => am.Name.Equals("apply", StringComparison.OrdinalIgnoreCase))
                    .Select(am => new { parameters = am.GetParameters(), minfo = am })
                    .Where(p => p.parameters.Length == 1 &&
                        typeof(DomainEvent).IsAssignableFrom(p.parameters[0].ParameterType)))
                {
                    var localItem = item;
                    Action<Object, Object> applier = ReflectAction(obj.GetType(), item.minfo);
                    typeCache[localItem.parameters[0].ParameterType] = applier;
                }
                lcgCache[obj.GetType()] = typeCache;

            }
            var thisTypeCache = lcgCache[obj.GetType()];
            Action<Object, Object> invoker;
            if (thisTypeCache.TryGetValue(domainEvent.GetType(), out invoker))
            {
                invoker(obj, domainEvent);
            }

        }

        private static Action<Object, Object> ReflectAction(Type objType, MethodInfo methodinfo)
        {

            DynamicMethod retmethod = new DynamicMethod(
                "Invoker" + methodinfo.Name,
                (Type)null,
                new Type[] { typeof(Object), typeof(Object) },
                objType,
                true); //methodinfo.GetParameters().Single().ParameterType
            ILGenerator ilgen = retmethod.GetILGenerator();
            ilgen.Emit(OpCodes.Ldarg_0);
            ilgen.Emit(OpCodes.Castclass, objType);
            ilgen.Emit(OpCodes.Ldarg_1);
            ilgen.Emit(OpCodes.Callvirt, methodinfo);
            ilgen.Emit(OpCodes.Ret);
            return (Action<Object, Object>)retmethod.CreateDelegate(typeof(Action<Object, Object>));
        }

    }
}