using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlTags;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using SchoStack.AspNetCore.HtmlConventions.Core;

namespace SchoStack.AspNetCore.HtmlConventions
{
    public class DefaultHtmlConventions : HtmlConvention
    {
        public DefaultHtmlConventions()
        {
            Displays.Always.BuildBy(req => new HtmlTag("span").Text(req.GetValue<string>()));
            Labels.Always.BuildBy(req => new HtmlTag("label").Attr("for", req.Id).Text(req.Accessor.InnerProperty.Name.BreakUpCamelCase()));
            Inputs.Always.BuildBy(req =>
            {
                return new TextboxTag().Attr("value", req.GetAttemptedValue() ?? req.GetValue<string>());
            });

            Inputs.If<bool>().BuildBy(req =>
            {
                var attemptedVal = req.GetAttemptedValue();
                var isChecked = attemptedVal != null ? attemptedVal.Split(',').First() == Boolean.TrueString: req.GetValue<bool>();
                var check = new CheckboxTag(isChecked).Attr("value", true);
                var hidden = new HiddenTag().Attr("name", req.Name).Attr("value", false);
                return check.After(hidden);
            });

            Inputs.If<IEnumerable<SelectListItem>>().BuildBy(BuildSelectList);
            Inputs.If<MultiSelectList>().BuildBy(BuildSelectList);
            
            All.Always.Modify((h, r) =>
            {
                h.Id((string.IsNullOrEmpty(r.Id) ? null : r.Id) ?? (string.IsNullOrEmpty(h.Id()) ? null : h.Id()));
                if (h.IsInputElement())
                {
                    h.Attr("name", r.Name ?? (string.IsNullOrEmpty(h.Attr("name")) ? null : h.Attr("name")));
                }
            });

            Labels.Always.Modify((h, r) => h.Id(r.Id + "_" + "Label"));
            Displays.Always.Modify((h, r) => h.Id(r.Id + "_" + "Display"));

            Inputs.Always.Modify((h, r) =>
            {
                //Validation class
                ModelStateEntry modelState;
                if (r.ViewContext.ViewData.ModelState.TryGetValue(r.Name, out modelState) && modelState.Errors.Count > 0)
                {
                    h.AddClass(HtmlHelper.ValidationInputCssClassName);
                }
            });
        }

        private static HtmlTag BuildSelectList(RequestData req)
        {
            return new DefaultSelectListBuilder<SelectListItem>().Build(req);
        }
    }

    public static class MoreStringExtensions
    {
        /// <summary>
        /// Answers true if this String is neither null or empty.
        /// </summary>
        /// <remarks>I'm also tired of typing !String.IsNullOrEmpty(s)</remarks>
        public static bool HasValue(this string s)
        {
            return !String.IsNullOrEmpty(s);
        }

        public static string BreakUpCamelCase(this string s)
        {
            var patterns = new[]
            {
                "([a-z])([A-Z])",
                "([0-9])([a-zA-Z])",
                "([a-zA-Z])([0-9])"
            };
            var output = patterns.Aggregate(s, (current, pattern) => Regex.Replace(current, pattern, "$1 $2", RegexOptions.IgnorePatternWhitespace));
            return output.Replace('_', ' ');
        }
    }
}