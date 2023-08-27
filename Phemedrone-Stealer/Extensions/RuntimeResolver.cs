using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Phemedrone.Extensions
{
    public class RuntimeResolver
    {
        public static List<T> GetInheritedClasses<T>()
        {
            var objects = Assembly.GetAssembly(typeof(T))
                .GetTypes()
                .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(T)))
                .Select(type => (T)Activator.CreateInstance(type, null))
                .ToList();
            return objects;
        }
    }
}