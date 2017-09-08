using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace SchoStack.AspNetCore.ModelUrls
{
    public class TypedRoutingApplicationModelConvention : IApplicationModelConvention
    {
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
                    }
                }
            }
        }
    }
}