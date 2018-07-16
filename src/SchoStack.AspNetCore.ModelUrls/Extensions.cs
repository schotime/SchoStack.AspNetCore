using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace SchoStack.AspNetCore.ModelUrls
{
    public static class Extensions
    {
        public static void AddStronglyTypedUrls(this IServiceCollection serviceCollection, Action<ActionConventionOptions> options = null)
        {
            var routeInformations = new Dictionary<Type, RouteInformation>();
            var convention = new TypedRoutingApplicationModelConvention(routeInformations);
            serviceCollection.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
            serviceCollection.AddScoped<IUrlHelper>(x => {
                var actionContext = x.GetRequiredService<IActionContextAccessor>().ActionContext;
                var factory = x.GetRequiredService<IUrlHelperFactory>();
                return factory.GetUrlHelper(actionContext);
            });
            serviceCollection.AddSingleton(convention);
            serviceCollection.Configure<MvcOptions>(x => x.Conventions.Add(convention));

            var actionConventions = new ActionConventionOptions();
            options?.Invoke(actionConventions);

            serviceCollection.Add(new ServiceDescriptor(typeof(ActionConventionOptions), actionConventions));
        }
    }
}
