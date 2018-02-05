using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace SchoStack.AspNetCore.ModelUrls
{
    public class TypedRoutingApplicationModelConvention : IApplicationModelConvention
    {
        public Dictionary<Type, RouteInformation> RouteInformations { get; }

        public TypedRoutingApplicationModelConvention(Dictionary<Type, RouteInformation> routeInformations)
        {
            RouteInformations = routeInformations;
        }

        public void Apply(ApplicationModel application)
        {
            foreach (var controller in application.Controllers)
            {
                foreach (var controllerAction in controller.Actions)
                {
                    var route = controllerAction.Selectors.Where(x => x.AttributeRouteModel != null).Select(x => x.AttributeRouteModel).FirstOrDefault();
                    var oneParam = controllerAction.Parameters.FirstOrDefault();
                    if (route != null && oneParam != null && oneParam.ParameterInfo.ParameterType.GetTypeInfo().IsClass && oneParam.ParameterInfo.ParameterType != typeof(string))
                    {
                        route.Name = oneParam.ParameterInfo.ParameterType.ToString();
                        RouteInformations[oneParam.ParameterInfo.ParameterType] = new RouteInformation
                        {
                            Method = controllerAction.Attributes.OfType<HttpPostAttribute>().Any() ? HttpMethod.Post : HttpMethod.Get,
                            ActionName = controllerAction.ActionName
                        };
                    }
                }
            }
        }
    }

    public class RouteInformation
    {
        public HttpMethod Method { get; set; }
        public string ActionName { get; set; }
    }
}