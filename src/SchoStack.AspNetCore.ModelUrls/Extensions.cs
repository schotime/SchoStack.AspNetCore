using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace SchoStack.AspNetCore.ModelUrls
{
    public static class Extensions
    {
        public static void AddStronglyTypedUrls(this IServiceCollection serviceCollection, Action<ActionConventionOptions> options = null)
        {
            serviceCollection.Configure<MvcOptions>(x => x.Conventions.Add(new TypedRoutingApplicationModelConvention()));

            var actionConventions = new ActionConventionOptions();
            options?.Invoke(actionConventions);

            serviceCollection.Add(new ServiceDescriptor(typeof(ActionConventionOptions), actionConventions));
        }
    }
}
