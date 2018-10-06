﻿using System.Collections.Generic;

namespace System.Delegation {
   /// <summary>
   /// EventImplementation can look like an event to the user, but allows you to add custom delegates for add/remove.
   /// It also keeps track of the added handlers in a list, so that you can query them as needed.
   /// </summary>
   /// <typeparam name="TEventArgs"></typeparam>
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
