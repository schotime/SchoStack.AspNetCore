using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using SchoStack.AspNetCore.HtmlConventions.Core;
using SchoStack.AspNetCore.ModelUrls;

namespace SchoStack.AspNetCore.HtmlConventions
{
    public static class Extensions
    {
        public static void AddHtmlConventions(this IServiceCollection serviceCollection, Action<HtmlConventionOptions> options = null)
        {
            var htmlConventionOptions = new HtmlConventionOptions();
            options?.Invoke(htmlConventionOptions);

            serviceCollection.Add(new ServiceDescriptor(typeof(HtmlConventionOptions), htmlConventionOptions));

            foreach (var convention in htmlConventionOptions.Conventions)
            {
                serviceCollection.AddSingleton(convention);
            }
        }
    }

    public class HtmlConventionOptions
    {
        public List<Type> Conventions { get; set; } = new List<Type>();

        public void AddConventions<T>() where T : HtmlConvention
        {
            Conventions.Add(typeof(T));
        }
    }
}
