using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HtmlTags;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SchoStack.AspNetCore.FluentValidation;
using SchoStack.AspNetCore.HtmlConventions;
using SchoStack.AspNetCore.Invoker;
using SchoStack.AspNetCore.MediatR;
using SchoStack.AspNetCore.ModelUrls;
using StructureMap;

namespace SchoStack.AspNetCore.Sample
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            services.AddInvoker();
            services.AddMediatrActionBuilder();

            services.AddStronglyTypedUrls();
            services.AddFluentValidationHtmlConventions();

            services.AddHtmlConventions(opt =>
            {
                opt.AddConventions<DefaultHtmlConventions>();
                opt.AddConventions<DataAnnotationValidationHtmlConventions>();
                opt.AddConventions<FluentValidationHtmlConventions>();
            });
        }

        public void ConfigureContainer(Registry registry)
        {
            registry.Scan(x =>
            {
                x.AssemblyContainingType<Startup>();
                x.ConnectImplementationsToTypesClosing(typeof(IHandler<,>));
                x.ConnectImplementationsToTypesClosing(typeof(IRequestHandler<,>));

            });

            registry.For<IMediator>().Use<Mediator>();
            registry.For<ServiceFactory>().Use<ServiceFactory>(ctx => ctx.GetInstance);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
