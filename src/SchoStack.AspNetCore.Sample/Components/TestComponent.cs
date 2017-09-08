using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SchoStack.AspNetCore.HtmlConventions;
using SchoStack.AspNetCore.MediatR;
using SchoStack.AspNetCore.Sample.Controllers;

namespace SchoStack.AspNetCore.Sample.Components
{
    public class TestComponent : ViewComponent, IViewComponent<TestComponent, ComponentQueryModel>
    {
        private readonly IAsyncViewComponentResultBuilder _builder;

        public TestComponent(IAsyncViewComponentResultBuilder builder)
        {
            _builder = builder;
        }

        public async Task<IViewComponentResult> InvokeAsync(ComponentQueryModel input) => await _builder
            .For(input)
            .Error(() => Content("errored"))
            .Success(x => Content(input.Name))
            .Send();
    }
}
