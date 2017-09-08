using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace SchoStack.AspNetCore.MediatR
{
    public static class Extensions
    {
        public static void AddMediatrActionBuilder(this IServiceCollection serviceCollection)
        {
            serviceCollection.Add(new ServiceDescriptor(typeof(IAsyncActionResultBuilder), typeof(MediatrResultBuilder), ServiceLifetime.Scoped));
            serviceCollection.Add(new ServiceDescriptor(typeof(IAsyncViewComponentResultBuilder), typeof(MediatrResultBuilder), ServiceLifetime.Scoped));
            serviceCollection.Add(new ServiceDescriptor(typeof(IActionContextAccessor), typeof(ActionContextAccessor), ServiceLifetime.Transient));
        }
    }
}
