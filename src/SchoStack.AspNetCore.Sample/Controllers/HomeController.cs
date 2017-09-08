using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using SchoStack.AspNetCore.Invoker;
using SchoStack.AspNetCore.ModelUrls;
using SchoStack.AspNetCore.Sample.Models;
using SchoStack.AspNetCore.MediatR;

namespace SchoStack.AspNetCore.Sample.Controllers
{
    public class HomeController : Controller
    {
        private readonly IActionResultBuilder _actionResultBuilder;
        private readonly IAsyncActionResultBuilder _actionBuilder;

        public HomeController(IActionResultBuilder actionResultBuilder, IAsyncActionResultBuilder actionBuilder)
        {
            _actionResultBuilder = actionResultBuilder;
            _actionBuilder = actionBuilder;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [Route("home/about")]
        public IActionResult About(AboutQueryModel query)
        {
            ViewData["Message"] = "Your application description page.";

            return _actionResultBuilder.Build(query, z => z
                .Returning<AboutViewModel>()
                .OnSuccess(View));

            //var vm = new AboutViewModel();
            //return View(vm);
        }

        [HttpPost]
        [Route("home/about")]
        public async Task<IActionResult> About(AboutInputModel input) => await _actionBuilder
            .For(input)
            .Error(() => About(new AboutQueryModel()))
            .On(y => !string.IsNullOrEmpty(y.RedirectUrl), y => Redirect(y.RedirectUrl))
            .Success(_ => Redirect(Url.For(new AboutQueryModel())))
            .Send();

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

    public class AboutQueryModel
    {
    }

    public class AboutInputModel : IRequest<AboutResponseModel>
    {
        [MinLength(4)]
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
