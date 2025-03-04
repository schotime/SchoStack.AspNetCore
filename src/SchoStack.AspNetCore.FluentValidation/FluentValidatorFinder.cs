using System;
using System.Collections.Generic;
using System.Reflection;
using FluentValidation;
using FluentValidation.Validators;
using SchoStack.AspNetCore.HtmlConventions;
using SchoStack.AspNetCore.HtmlConventions.Core;

namespace SchoStack.AspNetCore.FluentValidation
{
#pragma warning disable CS0612 // Type or member is obsolete
    public interface IValidatorFinder
    {
        IEnumerable<PropertyValidatorResult> FindValidators(RequestData r);
    }

    public class FluentValidatorFinder : IValidatorFinder
    {
        private readonly Func<Type, IValidator> _resolver;

        public FluentValidatorFinder(Func<Type, IValidator> resolver)
        {
            _resolver = resolver;
        }

        public IEnumerable<PropertyValidatorResult> FindValidators(RequestData requestData)
        {
            if (requestData.InputType == null)
                return Array.Empty<PropertyValidatorResult>();

            var baseValidator = ResolveValidator(requestData.InputType);
            if (baseValidator == null)
                return Array.Empty<PropertyValidatorResult>();

            var properties = InputPropertyMatcher.FindPropertyData(requestData);
            var validators = GetPropertyValidators(baseValidator, properties);
            return validators;
        }

        private IEnumerable<PropertyValidatorResult> GetPropertyValidators(IValidator baseValidator, List<PropertyInfo> properties)
        {
            var desc = baseValidator.CreateDescriptor();
            var validators = GetNestedPropertyValidators(desc, properties, 0);
            return validators;
        }

        private IEnumerable<PropertyValidatorResult> GetNestedPropertyValidators(IValidatorDescriptor desc, List<PropertyInfo> propertyInfo, int i)
        {
            if (i == propertyInfo.Count)
                yield break;

            var vals = desc.GetValidatorsForMember(propertyInfo[i].Name);
            var name = desc.GetName(propertyInfo[i].Name);

            foreach ((dynamic inlineval, _) in vals)
            {
                if (i == propertyInfo.Count - 1)
                {
                    yield return new PropertyValidatorResult(inlineval, name);
                }

                IValidator val = GetValidator(inlineval);
                if (val == null)
                    continue;

                var nestedPropertyValidators = GetNestedPropertyValidators(val.CreateDescriptor(), propertyInfo, i + 1);
                foreach (var validator in nestedPropertyValidators)
                {
                    yield return validator;
                }
            }
        }

        private static IValidator GetChildValidator<T>(IChildValidatorAdaptor adaptor)
        {
            var validatorContext = new ValidationContext<T>(default);
            return ((dynamic)adaptor).GetValidator(validatorContext, null);
        }

        private static IValidator GetValidator<T, TProperty>(PropertyValidator<T, TProperty> propertyValidator)
        {
            if (propertyValidator is IChildValidatorAdaptor child)
                return GetChildValidator<T>(child);

            return null;
        }

        private IValidator ResolveValidator(Type modelType)
        {
            var gentype = typeof(IValidator<>).MakeGenericType(modelType);
            var validator = _resolver(gentype);
            return validator;
        }
    }
#pragma warning restore CS0612 // Type or member is obsolete
}
