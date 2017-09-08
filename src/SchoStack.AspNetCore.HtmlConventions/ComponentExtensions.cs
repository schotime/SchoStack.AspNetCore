using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;

namespace SchoStack.AspNetCore.HtmlConventions
{
    public static class ComponentExtensions
    {
        public static async Task<IHtmlContent> InvokeAsync<T>(this IViewComponentHelper viewComponentHelper, IComponentModel<T> arguments) 
        {
            return await viewComponentHelper.InvokeAsync(typeof(T), arguments);
        }

        public static ViewComponentResult ViewComponent<T>(this Controller controller, IComponentModel<T> arguments)
        {
            return controller.ViewComponent(typeof(T), arguments);
        }
    }

    public interface IComponentModel<T>
    {
    }

    public interface IViewComponent<TComponent, TModel> where TModel : IComponentModel<TComponent>
    {
        Task<IViewComponentResult> InvokeAsync(TModel input);
    }
}
