using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HtmlTags.Reflection;

namespace SchoStack.AspNetCore.HtmlConventions.Core
{
    public class TagConventions : ITagConventions, IConventionAccessor
    {
        private readonly HtmlConvention _htmlConvention;

        public bool IsAll { get; private set; }
        public IList<Modifier> Modifiers { get; private set; }
        public IList<Builder> Builders { get; private set; }

        public TagConventions(HtmlConvention htmlConvention) : this(htmlConvention, false) { }
        public TagConventions(HtmlConvention htmlConvention, bool isAll)
        {
            _htmlConvention = htmlConvention;
            IsAll = isAll;
            Modifiers = new List<Modifier>();
            Builders = new List<Builder>();
        }

        public IConventionAction Always
        {
            get { return new ConventionAction(x => true, Builders, Modifiers); }
        }

        public IConventionAction If(Func<RequestData, bool> condition)
        {
            return new ConventionAction(condition, Builders, Modifiers);
        }

        public IConventionAction If<T>()
        {
            return new ConventionAction(req => IsAssignable<T>(req, _htmlConvention.UsePropertyValueType), Builders, Modifiers);
        }

        public IConventionActionAttribute<TAttribute> IfAttribute<TAttribute>() where TAttribute : Attribute
        {
            return new ConventionActionAttribute<TAttribute>(Condition<TAttribute>(), Builders, Modifiers);
        }

        private static Func<RequestData, bool> Condition<TAttribute>() where TAttribute : Attribute
        {
            return req => GetPropertyInfo(req.Accessor).GetCustomAttributes(typeof (TAttribute), true).Length > 0;
        }
        
        public static PropertyInfo GetPropertyInfo(Accessor accessor)
        {
            if (accessor.InnerProperty != null)
                return accessor.InnerProperty;
            return accessor.Getters().OfType<PropertyValueGetter>().Last().PropertyInfo;
        }

        public static bool IsAssignable<TProperty>(RequestData x, bool usePropertyValueType)
        {
            if (x.Accessor == null)
                return false;
            var type = typeof(TProperty);
           
            if (usePropertyValueType && x.GetPropertyType() == typeof(object))
            {
                var val = x.Accessor.GetValue(x.GetModel());
                return type.IsInstanceOfType(val);
            }

            var assignable = type.IsAssignableFrom(x.GetPropertyType());

            //if (!assignable && type.IsValueType)
            //{
            //    assignable = typeof(Nullable<>).MakeGenericType(type).IsAssignableFrom(x.Accessor.PropertyType);
            //}
            return assignable;
        }
    }
}