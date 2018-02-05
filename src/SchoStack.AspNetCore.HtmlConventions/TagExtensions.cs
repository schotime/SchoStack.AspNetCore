using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using HtmlTags;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using SchoStack.AspNetCore.HtmlConventions.Core;
using SchoStack.AspNetCore.ModelUrls;

namespace SchoStack.AspNetCore.HtmlConventions
{
    public static class TagExtensions
    {
        public static IEnumerable<LoopItem<TModel, TData>> Loop<TModel, TData>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, IEnumerable<TData>>> listExpression) where TModel : class
        {
            var enumerable = listExpression.Compile().Invoke(htmlHelper.ViewData.Model);
            var listFunc = LoopItem<TModel, TData>.GetCurrentIndexedExpressionWithIntParam(listExpression).Compile();
            return LoopItem<TModel, TData>.LoopItems(htmlHelper, listExpression, listFunc, enumerable);
        }

        public static HtmlProfileContext Profile(this IHtmlHelper helper, IHtmlProfile profile)
        {
            var existingContext = helper.ViewContext.HttpContext.Items[HtmlProfileContext.SchostackWebProfile] as HtmlProfileContext;
            var htmlProfileContext = new HtmlProfileContext(helper, profile, existingContext);
            helper.ViewContext.HttpContext.Items[HtmlProfileContext.SchostackWebProfile] = htmlProfileContext;
            return htmlProfileContext;
        }

        public static HtmlTag Input<TModel>(this IHtmlHelper<TModel> helper, Expression<Func<TModel, object>> expression)
        {
            var options = helper.ViewContext.HttpContext.RequestServices.GetService<HtmlConventionOptions>();
            var tag = new TagGenerator(options.Conventions.Select(x => (HtmlConvention) helper.ViewContext.HttpContext.RequestServices.GetService(x)).ToList());
            return tag.GenerateInputFor(helper.ViewContext, expression);
        }

        public static HtmlTag Display<TModel>(this IHtmlHelper<TModel> helper, Expression<Func<TModel, object>> expression)
        {
            var options = helper.ViewContext.HttpContext.RequestServices.GetService<HtmlConventionOptions>();
            var tag = new TagGenerator(options.Conventions.Select(x => (HtmlConvention) helper.ViewContext.HttpContext.RequestServices.GetService(x)).ToList());
            return tag.GenerateDisplayFor(helper.ViewContext, expression);
        }

        public static HtmlTag Label<TModel>(this IHtmlHelper<TModel> helper, Expression<Func<TModel, object>> expression)
        {
            var options = helper.ViewContext.HttpContext.RequestServices.GetService<HtmlConventionOptions>();
            var tag = new TagGenerator(options.Conventions.Select(x => (HtmlConvention) helper.ViewContext.HttpContext.RequestServices.GetService(x)).ToList());
            return tag.GenerateLabelFor(helper.ViewContext, expression);
        }

        public static LiteralTag ValidationMessage<TModel, TProperty>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression)
        {
            return ValidationMessage(htmlHelper, expression, null);
        }

        public static LiteralTag ValidationMessage<TModel, TProperty>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, string message)
        {
            var reqName = RequestData.GetName(ReflectionHelper.GetAccessor(expression));
            var errors = htmlHelper.ViewData.ModelState.ContainsKey(reqName) && htmlHelper.ViewData.ModelState[reqName].Errors.Any();
            var val = HtmlHelperValidationExtensions.ValidationMessage(htmlHelper, reqName, message, errors ? new {role = "alert"} : null);
            if (val != null)
            {
                var sb = new StringBuilder();
                using (var stringWriter = new StringWriter(sb))
                    val.WriteTo(stringWriter, HtmlEncoder.Default);
                return new LiteralTag(sb.ToString());
            }
            return new LiteralTag("");
        }

        public static HtmlTag Submit(this IHtmlHelper htmlHelper, string text)
        {
            var tag = TagGen(htmlHelper).GenerateTagFor(htmlHelper.ViewContext, () => new HtmlTag("input").Attr("type", "submit").Attr("value", text));
            return tag;
        }

        public static HtmlTag Tag(this IHtmlHelper htmlHelper, string tagName)
        {
            var tag = TagGen(htmlHelper).GenerateTagFor(htmlHelper.ViewContext, () => new HtmlTag(tagName));
            return tag;
        }
        
        public static Task<IHtmlContent> PartialAsync(this IHtmlHelper htmlHelper, string partial)
        {
            return HtmlHelperPartialExtensions.PartialAsync(htmlHelper, partial);
        }

        public static Task<IHtmlContent> PartialAsync(this IHtmlHelper htmlHelper, string partial, object model)
        {
            return HtmlHelperPartialExtensions.PartialAsync(htmlHelper, partial, model);
        }

        public static string Class(this IHtmlHelper htmlHelper, bool condition, string className)
        {
            return condition ? className : null;
        }

        public static TagGenerator TagGen(IHtmlHelper htmlHelper)
        {
            var options = htmlHelper.ViewContext.HttpContext.RequestServices.GetService<HtmlConventionOptions>();
            return new TagGenerator(options.Conventions.Select(x => (HtmlConvention) htmlHelper.ViewContext.HttpContext.RequestServices.GetService(x)).ToList());
        }

        public static LinkTag Link<T>(this IHtmlHelper htmlHelper, T model, string text) where T : class, new()
        {
            var urlHelper = new UrlHelper(htmlHelper.ViewContext);
            var tag = TagGen(htmlHelper).GenerateTagFor(htmlHelper.ViewContext, () => new LinkTag(text, urlHelper.For(model)));
            return tag;
        }

        public static LinkTag Link(this IHtmlHelper htmlHelper, string text, string action)
        {
            var urlHelper = new UrlHelper(htmlHelper.ViewContext);
            var tag = TagGen(htmlHelper).GenerateTagFor(htmlHelper.ViewContext, () => new LinkTag(text, urlHelper.Action(action)));
            return tag;
        }

        public static MvcForm Form<TInput>(this IHtmlHelper htmlHelper) where TInput : class, new()
        {
            return Form(htmlHelper, new TInput());
        }

        public static MvcForm Form<TInput>(this IHtmlHelper htmlHelper, Action<FormTag> modifier) where TInput : class, new()
        {
            return Form(htmlHelper, new TInput(), modifier);
        }

        public static MvcForm Form<TInput>(this IHtmlHelper htmlHelper, TInput model) where TInput : class
        {
            return Form(htmlHelper, model, begin => { });
        }

        public static MvcForm Form<TInput>(this IHtmlHelper htmlHelper, TInput model, Action<FormTag> modifier) where TInput : class
        {
            var urlHelper = new UrlHelper(htmlHelper.ViewContext);
            var url = urlHelper.For(model);
            return GenerateForm(model.GetType(), htmlHelper.ViewContext, modifier, url);
        }

        public static HtmlTag FormEnd(this IHtmlHelper htmlHelper)
        {
            htmlHelper.ViewContext.HttpContext.Items.Remove(TagGenerator.FORMINPUTTYPE);
            return new LiteralTag("</form>");
        }

        public static MvcForm GenerateForm(Type inputType, ViewContext viewContext, Action<FormTag> modifier, string url)
        {
            var options = viewContext.HttpContext.RequestServices.GetService<HtmlConventionOptions>();
            viewContext.HttpContext.Items[TagGenerator.FORMINPUTTYPE] = inputType;
            var tagGenerator = new TagGenerator(options.Conventions.Select(x => (HtmlConvention) viewContext.HttpContext.RequestServices.GetService(x)).ToList());
            var tag = tagGenerator.GenerateTagFor(viewContext, () => (FormTag) new FormTag(url).NoClosingTag());
            modifier(tag);
            viewContext.Writer.WriteLine(tag);
            return new InputTypeMvcForm(viewContext);
        }
    }
}