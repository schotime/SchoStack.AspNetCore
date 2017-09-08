using FluentValidation.Validators;

namespace SchoStack.AspNetCore.FluentValidation
{
    public class PropertyValidatorResult
    {
        public PropertyValidatorResult(IPropertyValidator propertyValidator, string displayName)
        {
            PropertyValidator = propertyValidator;
            DisplayName = displayName;
        }

        public IPropertyValidator PropertyValidator { get; set; }
        public string DisplayName { get; set; }
    }
}