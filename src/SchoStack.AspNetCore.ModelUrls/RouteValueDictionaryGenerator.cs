using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace SchoStack.AspNetCore.ModelUrls
{
    public class RouteValueDictionaryGenerator
    {
        public enum PropType
        {
            Simple,
            Enumerable,
            Complex,
            Unknown
        }

        public class Cache
        {
            public PropType PropType { get; set; }
            public Attribute[] Attributes { get; set; }
            public MemberAccessor MemberAccessor { get; set; }
        }

        public static ConcurrentDictionary<string, Cache> cache = new ConcurrentDictionary<string, Cache>();
        public static ConcurrentDictionary<Type, PropertyInfo[]> propCaches = new ConcurrentDictionary<Type, PropertyInfo[]>();

        public RouteValueDictionary Generate(object o, Func<Type, object, object> typeFormatter, Func<PropertyInfo, Attribute[], string> propertyNameFormatter, string prefix = "", RouteValueDictionary dict = null)
        {
            if (o == null)
                return dict;

            Type t = o.GetType();

            dict = dict ?? new RouteValueDictionary();

            foreach (PropertyInfo p in propCaches.GetOrAdd(t, t1 => TypeExtensions.GetProperties(t1)))
            {
                var theCache = cache.GetOrAdd(t.FullName + prefix + p.Name, _ =>
                {
                    return new Cache
                    {
                        Attributes = p.GetCustomAttributes().ToArray(),
                        PropType = IsEnum(p.PropertyType) || IsConvertible(p.PropertyType) ? PropType.Simple : (IsEnumerable(p.PropertyType) ? PropType.Enumerable : (SimpleGetter(p) ? PropType.Complex : PropType.Unknown)),
                        MemberAccessor = new MemberAccessor(t, p.Name)
                    };
                });

                var propType = theCache.PropType;
                var attributes = theCache.Attributes;
                var accessor = theCache.MemberAccessor;

                if (propType == PropType.Simple)
                {
                    var val = accessor.Get(o);
                    if (val == null || (Equals(val, GetDefault(p.PropertyType))
                                        && attributes.OfType<FromRouteAttribute>().Any() == false))
                    {
                        continue;
                    }

                    dict.Add(prefix + propertyNameFormatter(p, attributes), typeFormatter(p.PropertyType, val));
                }
                else if (propType == PropType.Enumerable)
                {
                    var i = 0;
                    foreach (object sub in (IEnumerable)accessor.Get(o) ?? new object[0])
                    {
                        if (sub == null)
                            continue;

                        var listCache = cache.GetOrAdd(t.FullName + prefix + p.Name + "_list", _ => new Cache
                        {
                            PropType = IsEnum(p.PropertyType) || IsConvertible(p.PropertyType) ? PropType.Simple : PropType.Unknown,
                        });
                        
                        if (listCache.PropType == PropType.Simple)
                        {
                            var subType = sub.GetType();
                            dict.Add(prefix + propertyNameFormatter(p, attributes) + "[" + (i++) + "]", typeFormatter(subType, sub));
                        }
                        else
                        {
                            Generate(sub, typeFormatter, propertyNameFormatter, prefix + propertyNameFormatter(p, attributes) + "[" + (i++) + "].", dict);
                        }
                    }
                }
                else if (propType == PropType.Complex)
                {
                    Generate(accessor.Get(o), typeFormatter, propertyNameFormatter, prefix + propertyNameFormatter(p, attributes) + ".", dict);
                }
            }

            return dict;
        }

        #region HelperMethods
        private static bool IsEnum(Type t)
        {
            if (t.GetTypeInfo().IsEnum)
                return true;
            var nullableType = Nullable.GetUnderlyingType(t);
            return nullableType != null && nullableType.GetTypeInfo().IsEnum;
        }

        private static List<Type> ConvertibleTypes = new List<Type>
        {
            typeof (bool), typeof (byte), typeof (char),
            typeof (DateTime), typeof(DateTimeOffset), typeof (decimal), typeof (double), typeof (float), typeof (int),
            typeof (long), typeof (sbyte), typeof (short), typeof (string), typeof (uint),
            typeof (ulong), typeof (ushort), typeof(Guid), typeof(TimeSpan)
        };

        /// <summary>
        /// Returns true if this Type matches any of a set of Types.
        /// </summary>
        /// <param name="type">This type.</param>
        /// <param name="types">The Types to compare this Type to.</param>
        private static bool In(Type type, IEnumerable<Type> types)
        {
            foreach (Type t in types)
            {
                if (t.IsAssignableFrom(type) || (Nullable.GetUnderlyingType(type) != null && t.IsAssignableFrom(Nullable.GetUnderlyingType(type))))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Returns true if this Type is one of the types accepted by Convert.ToString() 
        /// (other than object).
        /// </summary>
        private static bool IsConvertible(Type t)
        {
            return In(t, ConvertibleTypes);
        }

        /// <summary>
        /// Gets whether this type is enumerable.
        /// </summary>
        private static bool IsEnumerable(Type t)
        {
            return typeof(IEnumerable).IsAssignableFrom(t);
        }

        /// <summary>
        /// Returns true if this property's getter is public, has no arguments, and has no 
        /// generic type parameters.
        /// </summary>
        private static bool SimpleGetter(PropertyInfo info)
        {
            MethodInfo method = info.GetGetMethod(false);
            return method != null && method.GetParameters().Length == 0 && method.GetGenericArguments().Length == 0;
        }

        private static readonly Dictionary<Type, object> Defaults = new Dictionary<Type, object>()
        {
            {typeof (bool), false},
            {typeof (int), new int()},
            {typeof (DateTime), new DateTime()},
            {typeof (decimal), new decimal()},
            {typeof (float), new float()},
            {typeof (double), new double()},
        };

        private static object GetDefault(Type type)
        {
            if (type.GetTypeInfo().IsValueType)
            {
                object val;
                if (Defaults.TryGetValue(type, out val))
                    return val;
                return Activator.CreateInstance(type);
            }
            return null;
        }
        #endregion
    }
}