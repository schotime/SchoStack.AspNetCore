using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace SchoStack.AspNetCore.Invoker
{
    public static class Extensions
    {
        public static void AddInvoker(this IServiceCollection serviceCollection)
        {
            serviceCollection.Add(new ServiceDescriptor(typeof(IInvoker), x => new Invoker(x.GetService), ServiceLifetime.Singleton));
            serviceCollection.Add(new ServiceDescriptor(typeof(IActionResultBuilder), typeof(ActionResultBuilder), ServiceLifetime.Scoped));
            serviceCollection.Add(new ServiceDescriptor(typeof(IActionContextAccessor), typeof(ActionContextAccessor), ServiceLifetime.Transient));
        }
    }
}
