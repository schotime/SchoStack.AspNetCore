using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace SchoStack.AspNetCore.FluentValidation
{
    public static class Extensions
    {
        public static void AddFluentValidationHtmlConventions(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton(x => new FluentValidatorFinder(y => (IValidator) x.GetService(y)));
        }
    }
}
