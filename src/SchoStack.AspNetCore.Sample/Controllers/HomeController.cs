using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using SchoStack.AspNetCore.Invoker;
using SchoStack.AspNetCore.ModelUrls;
using SchoStack.AspNetCore.Sample.Models;
using SchoStack.AspNetCore.MediatR;
using SchoStack.AspNetCore.HtmlConventions;
using SchoStack.AspNetCore.Sample.Components;

namespace SchoStack.AspNetCore.Sample.Controllers
{
    public class TestController
    {
        private readonly IActionContextAccessor _actionContextAccessor;

        public TestController(IActionContextAccessor actionContextAccessor)
        {
            _actionContextAccessor = actionContextAccessor;
        }

        [Route("test/it")]
        [HttpGet]
        public Task<dynamic> It()
        {
            return Task.FromResult((dynamic)new{ what = "the"});
        }
    }

    public class HomeController : Controller
    {
        private readonly IActionResultBuilder _actionResultBuilder;
        private readonly IAsyncActionResultBuilder _actionBuilder;
        private readonly IInvoker _invoker;

        public HomeController(IActionResultBuilder actionResultBuilder, IAsyncActionResultBuilder actionBuilder, IInvoker invoker)
        {
            _actionResultBuilder = actionResultBuilder;
            _actionBuilder = actionBuilder;
            _invoker = invoker;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [Route("home/about")]
        public async Task<ActionResult> About(AboutQueryModel query)
        {
            var requiredService = HttpContext.RequestServices.GetRequiredService<IUrlHelper>();
            ViewData["Message"] = await requiredService.ForAsync(new AboutQueryModel(), true);



            return new HandleActionBuilder<AboutQueryModel>(query, _invoker)
                .Returning<AboutViewModel>()
                .OnSuccess(View);
        }

        [HttpGet]
        [Route("home/about2")]
        public ActionResult About2(AboutQueryModel2 query)
        {
            ViewData["Message"] = "Your application description page.";

            return _actionResultBuilder.Build(new AboutQueryModel(), z => z
                .Returning<AboutViewModel>()
                .OnSuccess(x => View("About", x)));
        }

        [HttpPost]
        [Route("home/about")]
        public async Task<IActionResult> About(AboutInputModel input) => await _actionBuilder
            .For(input)
            .Error(async () => await About(new AboutQueryModel()))
            .On(y => !string.IsNullOrEmpty(y.RedirectUrl), y => Redirect(y.RedirectUrl))
            .Success(_ => Redirect(Url.For(new AboutQueryModel())))
            .Send();

        [Route("home/component")]
        public IActionResult Component(ComponentQueryModel query) => this.ViewComponent(query);

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    public class ComponentQueryModel : IRequest<Unit>, IComponentModel<TestComponent>
    {
        public string Name { get; set; }
    }

    public class ComponentHandler : IRequestHandler<ComponentQueryModel, Unit>
    {
        public Unit Handle(ComponentQueryModel message)
        {
            return Unit.Value;
        }
    }

    public class AboutQueryModel2 : AboutQueryModel
    {
    }

    public class AboutQueryModel
    {
        public string Name { get; set; }
        public string Name2 { get; set; }

        public List<string> WorkflowStates { get; set; } = new List<string>();
    }

    public class AboutInputModel : IRequest<AboutResponseModel>
    {
        [MinLength(4)]
        [StringLength(100)]
        [Required]
        public string Name { get; set; }
    }

    public class AboutViewModel
    {
        public string Name { get; set; }
    }

    public class AboutHandler : IHandler<AboutQueryModel, AboutViewModel>
    {
        public AboutViewModel Handle(AboutQueryModel input)
        {
            return new AboutViewModel()
            {
                Name = "Scho Stack"
            };
        }
    }

    public class AboutCommandHandler : IAsyncRequestHandler<AboutInputModel, AboutResponseModel>
    {
        public Task<AboutResponseModel> Handle(AboutInputModel message)
        {
            return Task.FromResult(new AboutResponseModel() { RedirectUrl = "http://www.google.com" });
        }
    }

    public class AboutResponseModel
    {
        public string RedirectUrl { get; set; }
    }
}
