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
using System.Threading;
using FluentValidation;
using FluentValidation.Results;
using FluentValidation.Validators;
using HtmlTags.Reflection;
using SchoStack.AspNetCore.FluentValidation;
using SchoStack.AspNetCore.HtmlConventions.Core;

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
            return Task.FromResult((dynamic)new { what = "the" });
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

            var model = new AboutViewModel() { Name = "Name"};
            var finder = new FluentValidatorFinder(x =>
            {                
                var res = (IValidator) HttpContext.RequestServices.GetService(x);
                return res;
            });
            var result = finder.FindValidators(RequestData.BuildRequestData( ReflectionHelper.GetAccessor<AboutViewModel>(x => x.NestedModel.NameNested), typeof(AboutViewModel)));


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
        public Task<Unit> Handle(ComponentQueryModel message, CancellationToken token)
        {
            return Unit.Task;
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
        public NestedModel NestedModel { get; set; }
    }

    public class NestedModel
    {
        public string NameNested { get; set; }
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

    public class AboutCommandHandler : IRequestHandler<AboutInputModel, AboutResponseModel>
    {
        public Task<AboutResponseModel> Handle(AboutInputModel message, CancellationToken token)
        {
            return Task.FromResult(new AboutResponseModel() { RedirectUrl = "http://www.google.com" });
        }
    }

    public class AboutResponseModel
    {
        public string RedirectUrl { get; set; }
    }

    public class AboutViewModelValidator : AbstractValidator<AboutViewModel>
    {
        public AboutViewModelValidator()
        {
            RuleFor(x => x.Name).SetValidator(new NameValidator());
            RuleFor(x => x.NestedModel).SetValidator(new NestedModelValidator());
        }
    }

    public class NestedModelValidator : AbstractValidator<NestedModel>
    {
        public NestedModelValidator()
        {
            RuleFor(x => x.NameNested).NotEmpty();
        }
    }

    public class NameValidator : IPropertyValidator
    {
        public IEnumerable<ValidationFailure> Validate(PropertyValidatorContext context)
        {
            if (!(context.InstanceToValidate is string s) || s != "Name")
            {
                yield return new ValidationFailure(context.PropertyName, "The Error");
            }
        }

        public Task<IEnumerable<ValidationFailure>> ValidateAsync(PropertyValidatorContext context, CancellationToken cancellation)
        {
            return Task.FromResult(Validate(context));
        }

        public bool ShouldValidateAsynchronously(IValidationContext context)
        {
            return false;
        }

        public PropertyValidatorOptions Options { get; }
    }
}
