using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using HtmlTags.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SchoStack.AspNetCore.HtmlConventions.Core
{
    public class RequestData
    {
        protected RequestData() {}

        private static readonly Regex IdRegex = new Regex(@"[\.\[\]]", RegexOptions.Compiled);

        public ViewContext ViewContext { get; set; }
        public Accessor Accessor { get; set; }

        private string _id;
        public string Id
        {
            get
            {
                if (_id != null)
                    return _id;

                _id = Name == null ? null : IdRegex.Replace(Name, "_");
                return _id;
            }
        }

        private string _name;
        public string Name
        {
            get
            {
                if (_name != null)
                    return _name;

                if (Accessor == null)
                    return null;

                _name = string.Join(".", GetPropertyNames()).Replace(".[", "[");
                return _name;
            }
        }

        private IEnumerable<string> GetPropertyNames()
        {
            var getters = Accessor.Getters().ToList();

            if (InputType != null)
            {
                var props = InputPropertyMatcher.FindPropertyData(this);
                var i = 0;
                return getters.Select(x =>
                {
                    if (i > props.Count - 1)
                        return x.Name;

                    var getter = x as PropertyValueGetter;
                    var prop = props[i];
                    if (getter != null && getter.Name == prop.Name)
                    {
                        i++;
                        var alias = prop.GetCustomAttribute(typeof(FromQueryAttribute)) as FromQueryAttribute;
                        return alias != null ? alias.Name : x.Name;
                    }
                    return x.Name;
                });
            }

            return getters.Select(x =>
            {
                var getter = x as PropertyValueGetter;
                if (getter != null)
                {
                    var alias = getter.PropertyInfo.GetCustomAttribute(typeof(FromQueryAttribute)) as FromQueryAttribute;
                    return alias != null ? alias.Name : getter.Name;
                }
                return x.Name;
            });
        }

        public virtual Type GetPropertyType()
        {
            return Accessor.PropertyType;
        }

        public Type InputType { get; set; }

        public virtual T TryGetValue<T>()
        {
            try
            {
                return GetValue<T>();
            }
            catch
            {
                return default(T);
            }
        }

        public virtual T GetValue<T>()
        {
            var model = GetModel();
            if (model == null)
                return default(T);

            var val = Accessor.GetValue(model);
            return ConvertValue<T>(this, val);
        }

        public object GetModel()
        {
            return ViewContext.ViewData.ContainsKey("SchoStack.Model")
                ? ViewContext.ViewData["SchoStack.Model"]
                : ViewContext.ViewData.Model;
        }

        public static T ConvertValue<T>(RequestData rd, object val)
        {
            if (TagConventions.IsAssignable<T>(rd, true))
                return (T)val;

            if (typeof(T) == typeof(string))
            {
                return (T)(val != null ? (object)val.ToString() : (object)string.Empty);
            }

            try
            {
                return (T)Convert.ChangeType(val, typeof(T));
            }
            catch (Exception ex)
            {
                throw new InvalidCastException(string.Format("Cannot convert '{0}' to '{1}'", (val == null ? "null" : val.GetType().ToString()), typeof(T)), ex);
            }
        }

        public string GetAttemptedValue()
        {
            return !ViewContext.ViewData.ModelState.IsValid && ViewContext.ViewData.ModelState.ContainsKey(Name)
                ? (ViewContext.ViewData.ModelState[Name].AttemptedValue != null ? ViewContext.ViewData.ModelState[Name].AttemptedValue : null)
                : null;
        }

        public static string GetName(Accessor accessor)
        {
            var requestData = new RequestData() {Accessor = accessor};
            return requestData.Name;
        }

        public static RequestData BuildRequestData(ViewContext viewContext, Accessor accessor, Type inputType = null)
        {
            var req = new RequestData()
            {
                ViewContext = viewContext,
                Accessor = accessor,
                InputType = inputType ?? (viewContext != null ? viewContext.HttpContext.Items[TagGenerator.FORMINPUTTYPE] : null) as Type
            };
            return req;
        }

        public static RequestData BuildRequestData(Accessor accessor, Type inputType = null)
        {
            var req = new RequestData()
            {
                Accessor = accessor,
                InputType = inputType
            };
            return req;
        }

        public static RequestData BuildRequestData<TModel, TProperty>(ViewContext viewContext, Expression<Func<TModel, TProperty>> expression)
        {
            var accessor = ReflectionHelper.GetAccessor(expression);
            return BuildRequestData(viewContext, accessor);
        }
    }

    public class ValueRequestData<T> : RequestData
    {
        private readonly object _value;

        public ValueRequestData(RequestData requestData, T value)
        {
            _value = value;
            this.Accessor = requestData.Accessor;
            this.ViewContext = requestData.ViewContext;
            this.InputType = requestData.InputType;
        }

        public override Type GetPropertyType()
        {
            return typeof(T);
        }

        public override TModel GetValue<TModel>()
        {
            return ConvertValue<TModel>(this, (TModel)_value);
        }
    }
}