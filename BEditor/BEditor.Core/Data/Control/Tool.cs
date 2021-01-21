﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BEditor.Core.Data.Property;

namespace BEditor.Core.Data
{
    public interface IParent<out T>
    {
        /// <summary>
        /// 子要素を取得します
        /// </summary>
        public IEnumerable<T> Children { get; }
    }

    public interface IChild<out T>
    {
        /// <summary>
        /// 親要素を取得します
        /// </summary>
        public T Parent { get; }
    }

    public static class Tool
    {
        #region GetParents

        [Pure] public static T GetParent<T>(this IChild<T> self) => self.Parent;
        [Pure] public static T GetParent2<T>(this IChild<IChild<T>> self) => self.Parent.Parent;
        [Pure] public static T GetParent3<T>(this IChild<IChild<IChild<T>>> self) => self.Parent.Parent.Parent;
        [Pure] public static T GetParent4<T>(this IChild<IChild<IChild<IChild<T>>>> self) => self.Parent.Parent.Parent.Parent;
        //[Pure] public static T GetParent5<T>(this IChild<IChild<IChild<IChild<IChild<T>>>>> self) => self.Parent.Parent.Parent.Parent.Parent;
        //[Pure] public static T GetParent6<T>(this IChild<IChild<IChild<IChild<IChild<IChild<T>>>>>> self) => self.Parent.Parent.Parent.Parent.Parent.Parent;
        //[Pure] public static T GetParent7<T>(this IChild<IChild<IChild<IChild<IChild<IChild<IChild<T>>>>>>> self) => self.Parent.Parent.Parent.Parent.Parent.Parent.Parent;
        //[Pure] public static T GetParent8<T>(this IChild<IChild<IChild<IChild<IChild<IChild<IChild<IChild<T>>>>>>>> self) => self.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent;
        //[Pure] public static T GetParent9<T>(this IChild<IChild<IChild<IChild<IChild<IChild<IChild<IChild<IChild<T>>>>>>>>> self) => self.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent;
        //[Pure] public static T GetParent10<T>(this IChild<IChild<IChild<IChild<IChild<IChild<IChild<IChild<IChild<IChild<T>>>>>>>>>> self) => self.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent;
        //[Pure] public static T GetParent11<T>(this IChild<IChild<IChild<IChild<IChild<IChild<IChild<IChild<IChild<IChild<IChild<T>>>>>>>>>>> self) => self.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent;
        //[Pure] public static T GetParent12<T>(this IChild<IChild<IChild<IChild<IChild<IChild<IChild<IChild<IChild<IChild<IChild<IChild<T>>>>>>>>>>>> self) => self.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent;
        //[Pure] public static T GetParent13<T>(this IChild<IChild<IChild<IChild<IChild<IChild<IChild<IChild<IChild<IChild<IChild<IChild<IChild<T>>>>>>>>>>>>> self) => self.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent;
        //[Pure] public static T GetParent14<T>(this IChild<IChild<IChild<IChild<IChild<IChild<IChild<IChild<IChild<IChild<IChild<IChild<IChild<IChild<T>>>>>>>>>>>>>> self) => self.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent;
        //[Pure] public static T GetParent15<T>(this IChild<IChild<IChild<IChild<IChild<IChild<IChild<IChild<IChild<IChild<IChild<IChild<IChild<IChild<IChild<T>>>>>>>>>>>>>>> self) => self.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent;
        //[Pure] public static T GetParent16<T>(this IChild<IChild<IChild<IChild<IChild<IChild<IChild<IChild<IChild<IChild<IChild<IChild<IChild<IChild<IChild<IChild<T>>>>>>>>>>>>>>>> self) => self.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent.Parent;

        #endregion
        
        //public static void LoadAll<T, T2>(this T self)
        //    where T : IElementObject, IParent<T2>
        //    where T2 : IElementObject
        //{
        //    self.Load();
        //    foreach(var e in self.Children)
        //    {
        //        e.Load();
        //    }
        //}
        //public static void LoadAll<T1, T2, T3>(this T1 self)
        //    where T1 : IElementObject, IParent<T2>
        //    where T2 : IElementObject, IParent<T3>
        //    where T3 : IElementObject
        //{
        //    self.Load();
        //    foreach (var e in self.Children)
        //    {
        //        e.LoadAll<T2, T3>();
        //    }
        //}
        //public static void LoadAll<T1, T2, T3, T4>(this T1 self)
        //    where T1 : IElementObject, IParent<T2>
        //    where T2 : IElementObject, IParent<T3>
        //    where T3 : IElementObject, IParent<T4>
        //    where T4 : IElementObject
        //{
        //    self.Load();
        //    foreach (var e in self.Children)
        //    {
        //        e.LoadAll<T2, T3, T4>();
        //    }
        //}
        //public static void LoadAll<T1, T2, T3, T4, T5>(this T1 self)
        //    where T1 : IElementObject, IParent<T2>
        //    where T2 : IElementObject, IParent<T3>
        //    where T3 : IElementObject, IParent<T4>
        //    where T4 : IElementObject, IParent<T5>
        //    where T5 : IElementObject
        //{
        //    self.Load();
        //    foreach (var e in self.Children)
        //    {
        //        e.LoadAll<T2, T3, T4, T5>();
        //    }
        //}
        
        //public static void UnloadAll<T, T2>(this T self)
        //    where T : IElementObject, IParent<T2>
        //    where T2 : IElementObject
        //{
        //    self.Unload();
        //    foreach(var e in self.Children)
        //    {
        //        e.Unload();
        //    }
        //}
        //public static void UnloadAll<T1, T2, T3>(this T1 self)
        //    where T1 : IElementObject, IParent<T2>
        //    where T2 : IElementObject, IParent<T3>
        //    where T3 : IElementObject
        //{
        //    self.Unload();
        //    foreach (var e in self.Children)
        //    {
        //        e.UnloadAll<T2, T3>();
        //    }
        //}
        //public static void UnloadAll<T1, T2, T3, T4>(this T1 self)
        //    where T1 : IElementObject, IParent<T2>
        //    where T2 : IElementObject, IParent<T3>
        //    where T3 : IElementObject, IParent<T4>
        //    where T4 : IElementObject
        //{
        //    self.Unload();
        //    foreach (var e in self.Children)
        //    {
        //        e.UnloadAll<T2, T3, T4>();
        //    }
        //}
        //public static void UnloadAll<T1, T2, T3, T4, T5>(this T1 self)
        //    where T1 : IElementObject, IParent<T2>
        //    where T2 : IElementObject, IParent<T3>
        //    where T3 : IElementObject, IParent<T4>
        //    where T4 : IElementObject, IParent<T5>
        //    where T5 : IElementObject
        //{
        //    self.Unload();
        //    foreach (var e in self.Children)
        //    {
        //        e.UnloadAll<T2, T3, T4, T5>();
        //    }
        //}

        #region Find

        [Pure]
        [return: MaybeNull]
        public static T? Find<T>(this IParent<T> self, int id) where T : IHasId
        {
            return self.Children.ToList().Find(item => item.Id == id);
        }
        [Pure]
        [return: MaybeNull]
        public static T? Find<T>(this IParent<T> self, string name) where T : IHasName
        {
            return self.Children.ToList().Find(item => item.Name == name);
        }
        [Pure]
        [return: MaybeNull]
        public static bool Contains<T>(this IParent<T> self, T item)
        {
            return self.Children.ToList().Contains(item);
        }


        #endregion
    }
}
