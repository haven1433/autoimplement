using System.Collections.Generic;

namespace System.Delegation {
   public class EventImplementation<TEventArgs> {
      public readonly List<EventHandler<TEventArgs>> handlers = new List<EventHandler<TEventArgs>>();

      public Action<EventHandler<TEventArgs>> add;

      public Action<EventHandler<TEventArgs>> remove;

      public EventImplementation() {
         add = handlers.Add;
         remove = value => handlers.Remove(value);
      }

      public static EventImplementation<TEventArgs> operator +(EventImplementation<TEventArgs> ev, EventHandler<TEventArgs> toAdd) {
         ev.add(toAdd);
         return ev;
      }

      public static EventImplementation<TEventArgs> operator -(EventImplementation<TEventArgs> ev, EventHandler<TEventArgs> toRemove) {
         ev.remove(toRemove);
         return ev;
      }

      public void Invoke(object sender, TEventArgs args) {
         foreach (var handler in handlers) {
            handler(sender, args);
         }
      }
   }
}
