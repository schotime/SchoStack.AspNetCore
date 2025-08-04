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

        public RouteValueDictionaryGenerator(ActionContext actionContext, ActionConventionOptions conventionOptions)
        {
            _actionContext = actionContext;
            _conventionOptions = conventionOptions;
        }

        /// <summary>
        /// Generates a <see cref="RouteValueDictionary"/> from the public properties of the given object.
        /// Handles simple, enumerable, and complex property types recursively, applying <see cref="ActionConventionOptions"/>.
        /// Value types, types implementing <see cref="IFormattable"/> or types with a <see cref="ActionConventionOptions.TypeFormatters"/> are treated as simple.
        /// Properties with default values or nulls are skipped unless marked with <see cref="FromRouteAttribute"/>.
        /// </summary>
        /// <param name="o">The object to generate route values from.</param>
        /// <param name="prefix">An optional prefix for each top level property name.</param>
        /// <param name="dict">An optional existing <see cref="RouteValueDictionary"/> to add values to; if null, a new dictionary is created.</param>
        public RouteValueDictionary Generate(object o, string prefix = "", RouteValueDictionary dict = null)
        {
            if (o == null)
                return dict;

            Type t = o.GetType();

            dict = dict ?? new RouteValueDictionary();

            foreach (PropertyInfo p in propCaches.GetOrAdd(t, t1 => TypeExtensions.GetProperties(t1)))
            {
                if (p.GetGetMethod()?.IsStatic == true)
                    continue;

                var theCache = cache.GetOrAdd($"{t.FullName}{prefix}{p.Name}", _ =>
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
                var propertyName = _conventionOptions.PropertyNameModifier.GetModifiedPropertyName(p, attributes);
                var prefixedPropertyName = $"{prefix}{propertyName}";

                if (propType == PropType.Simple)
                {
                    var val = accessor.Get(o);
                    if (val == null || (Equals(val, GetDefault(p.PropertyType))
                                        && attributes.OfType<FromRouteAttribute>().Any() == false))
                    {
                        continue;
                    }

                    dict.Add(prefixedPropertyName, ConvertTypeValue(p.PropertyType, val));
                }
                else if (propType == PropType.Enumerable)
                {
                    var i = 0;
                    foreach (object sub in (IEnumerable)accessor.Get(o) ?? new object[0])
                    {
                        if (sub == null)
                            continue;

                        var subType = sub.GetType();
                        var subPropType = IsEnum(subType) || IsConvertible(subType) ? PropType.Simple : PropType.Unknown;
                        var indexedPropertyName = $"{prefixedPropertyName}[{i++}]";

                        if (subPropType == PropType.Simple)
                        {
                            dict.Add(indexedPropertyName, ConvertTypeValue(subType, sub));
                        }
                        else
                        {
                            Generate(sub, $"{indexedPropertyName}.", dict);
                        }
                    }
                }
                else if (propType == PropType.Complex)
                {
                    Generate(accessor.Get(o), $"{prefixedPropertyName}.", dict);
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

        private object ConvertTypeValue(Type t, object value)
        {
            if (_conventionOptions.TypeFormatters.ContainsKey(t))
            {
                return _conventionOptions.TypeFormatters[t](value, _actionContext);
            }

            return value;
        }

        /// <summary>
        /// Returns true if this Type is one of the types accepted by Convert.ToString(), IFormattable or one of the <see cref="ActionConventionOptions.TypeFormatters"/>
        /// (other than object).
        /// </summary>
        private bool IsConvertible(Type t)
        {
            return IsConvertibleByMvc(t) || _conventionOptions.TypeFormatters.ContainsKey(t);
        }

        /// <summary>
        /// Returns true if this Type is one of the types accepted by Convert.ToString() or IFormattable
        /// (other than object).
        /// </summary>
        private static bool IsConvertibleByMvc(Type t)
        {
            return In(t, ConvertibleTypes) || IsFormattable(t);
        }

        /// <summary>
        /// Gets whether this type is IFormattable.
        /// </summary>
        private static bool IsFormattable(Type t)
        {
            return typeof(IFormattable).IsAssignableFrom(t);
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
        private readonly ActionContext _actionContext;
        private readonly ActionConventionOptions _conventionOptions;

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