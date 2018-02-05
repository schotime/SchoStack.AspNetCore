using System;
using System.Collections.Generic;
using FluentValidation;
using HtmlTags;
using Microsoft.Extensions.DependencyInjection;
using SchoStack.AspNetCore.HtmlConventions.Core;

namespace SchoStack.AspNetCore.FluentValidation
{
    public static class Extensions
    {
        public static void AddFluentValidationHtmlConventions(this IServiceCollection serviceCollection, Action<FluentValidationOptions> options = null)
        {
            var fluentValidationOptions = new FluentValidationOptions();
            options?.Invoke(fluentValidationOptions);
            
            serviceCollection.AddSingleton<IValidatorFinder>(x => new FluentValidatorFinder(y => (IValidator) x.GetService(y)));
            serviceCollection.AddSingleton(new ServiceDescriptor(typeof(FluentValidationOptions), fluentValidationOptions));
        }
    }

    public class FluentValidationOptions
    {
        public List<Action<IEnumerable<PropertyValidatorResult>, HtmlTag, RequestData>> RuleProviders = new List<Action<IEnumerable<PropertyValidatorResult>, HtmlTag, RequestData>>();

        public void AddRuleProviders(Action<IEnumerable<PropertyValidatorResult>, HtmlTag, RequestData> ruleProvider)
        {
            RuleProviders.Add(ruleProvider);
        }
    }
}
