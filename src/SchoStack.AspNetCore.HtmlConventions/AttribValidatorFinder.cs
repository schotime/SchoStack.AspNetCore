using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using SchoStack.AspNetCore.HtmlConventions.Core;

namespace SchoStack.AspNetCore.HtmlConventions
{
    public class AttribValidatorFinder
    {
        public IEnumerable<ValidationAttribute> FindAttributeValidators(RequestData requestData)
        {
            if (requestData.InputType == null)
                return new List<ValidationAttribute>();

            var properties = InputPropertyMatcher.FindPropertyData(requestData);

            return properties.SelectMany(propertyInfo => propertyInfo.GetCustomAttributes(typeof(ValidationAttribute), true).Cast<ValidationAttribute>());
        }
    }
}
