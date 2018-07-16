using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace SchoStack.AspNetCore.ModelUrls
{
    public static class UrlExtensions
    {
        public static string For<T>(this IUrlHelper urlHelper) where T : class, new()
        {
            return For(urlHelper, new T());
        }

        public static string For<T>(this IUrlHelper urlHelper, T obj) where T : class
        {
            return ForAsync(urlHelper, obj).Result;
        }

        public static string For<T>(this IUrlHelper urlHelper, params Action<T>[] modifiers) where T : class, new()
        {
            return ForAsync(urlHelper, modifiers).Result;
        }

        public static async Task<string> ForAsync<T>(this IUrlHelper urlHelper, params Action<T>[] modifiers) where T : class, new()
        {
            return await ForAsync(urlHelper, new T(), true, modifiers);
        }

        public static async Task<string> ForAsync<T>(this IUrlHelper urlHelper, T model, bool bindExistingQueryString = false, params Action<T>[] modifiers) where T : class
        {
            await BindModelFor(urlHelper, model, bindExistingQueryString, modifiers);

            var dictGenerator = new RouteValueDictionaryGenerator();
            var actionConventions = urlHelper.ActionContext.HttpContext.RequestServices.GetRequiredService<ActionConventionOptions>();
            var dict = dictGenerator.Generate(model, (t, o) => actionConventions.TypeFormatters.ContainsKey(t) ? actionConventions.TypeFormatters[t](o, urlHelper.ActionContext) : o, (propertyInfo, atts) => actionConventions.PropertyNameModifier.GetModifiedPropertyName(propertyInfo, atts));
            var url = urlHelper.RouteUrl(model.GetType().ToString(), dict);

            return url;
        }

        public static Task<T> BindModelFor<T>(this IUrlHelper urlHelper, params Action<T>[] modifiers) where T : class, new()
        {
            return BindModelFor(urlHelper, new T(), true, modifiers);
        }

        public static async Task<T> BindModelFor<T>(this IUrlHelper urlHelper, T model, bool bindExistingQueryString, params Action<T>[] modifiers) where T : class
        {
            if (bindExistingQueryString)
            {
                var meta = urlHelper.ActionContext.HttpContext.RequestServices.GetRequiredService<IModelMetadataProvider>();
                var modelfactory = urlHelper.ActionContext.HttpContext.RequestServices.GetRequiredService<IModelBinderFactory>();
                var validator = urlHelper.ActionContext.HttpContext.RequestServices.GetRequiredService<IObjectModelValidator>();

                var modelBound = await ModelBindingHelper.TryUpdateModelAsync((dynamic) model,
                    string.Empty,
                    new ControllerContext(urlHelper.ActionContext),
                    meta,
                    modelfactory,
                    new QueryStringValueProvider(BindingSource.Query, urlHelper.ActionContext.HttpContext.Request.Query, CultureInfo.CurrentCulture),
                    validator);
            }

            foreach (var modifier in modifiers)
            {
                modifier(model);
            }

            return model;
        }
    }
}