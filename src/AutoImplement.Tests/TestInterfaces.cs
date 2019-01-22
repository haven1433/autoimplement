using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;

namespace HavenSoft.AutoImplement.Tests {
   public interface IEmptyInterface { }

   public delegate void CustomEventHandler(object sender, int args);

   public interface IHasCustomEvent {
      event CustomEventHandler CustomEvent;
   }

   public interface IHasNormalEvent {
      event EventHandler<string> NormalEvent;
   }

   public interface IUseEvent { event EventHandler NormalEvent; }

   public interface IHaveGetProperty {
      string Name { get; }
   }

   public interface IHaveSetProperty {
      string Name { set; }
   }

   public interface IHaveGenericProperty<T> {
      T Value { get; set; }
   }

   public interface IHaveConflictingMethods {
      void Method1(int a);
      int Method1(string s);
   }

   public interface IHaveOutMethods {
      void Method(int inParam, out int outParam);
      string Method2(ref int p1, out double p2);
   }

   public interface IHaveGenericMethods {
      void MethodWithGenericInput<T>(T input);
      T MethodWithGenericOutput<T>(int input);
      A MethodWithGenericInputAndOutput<A, B>(B input);
      void MethodWithNestedGenericTypeInParameter<T>(IList<T> list);
      void MethodWithGenericOutParameter<T>(out T value);
      void MethodWithGenericRefParameter<T>(ref T value);
   }

   public interface IInputInterface<in T> { }

   public interface IOutputInterface<out T> { }

   public interface IInterfaceWithMultipleConstraints<in T> where T : IEquatable<T>, new() { }

   public interface IInterfaceWithTypeConstrainedMethods {
      T Create<T>() where T : new();
      void DoStuff1<T>(T argument) where T : class;
      void DoStuff2<T>(T argument) where T : struct;
      void DoStuff3<T>(T argument) where T : IEnumerable<T>;
      void DoStuff4<T>(T argument) where T : Type;
   }

   public interface IInterfaceWithBaseEventInterface : INotifyPropertyChanged {
      ICommand SomeProperty { get; }
   }

   public interface IInterfaceThatOverridesProperty : IReadOnlyList<int> {
      new int this[int index] { get; set; }
      new int Count { get; set; }
   }
}
