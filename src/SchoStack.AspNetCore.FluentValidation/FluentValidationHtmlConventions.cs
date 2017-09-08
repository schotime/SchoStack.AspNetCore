using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Internal;
using FluentValidation.Validators;
using HtmlTags;
using SchoStack.AspNetCore.HtmlConventions.Core;

namespace SchoStack.AspNetCore.FluentValidation
{
    public class FluentValidationHtmlConventions : HtmlConvention
    {
        public List<Action<IEnumerable<PropertyValidatorResult>, HtmlTag, RequestData>> RuleProviders = new List<Action<IEnumerable<PropertyValidatorResult>, HtmlTag, RequestData>>();

        public FluentValidationHtmlConventions(FluentValidatorFinder validatorFinder)
        {
            RuleProviders.Add(AddLengthClasses);
            RuleProviders.Add(AddRequiredClass);
            RuleProviders.Add(AddCreditCardClass);
            RuleProviders.Add(AddEqualToDataAttr);
            RuleProviders.Add(AddRegexData);
            RuleProviders.Add(AddEmailData);

            Inputs.Always.Modify((h, r) =>
            {
                var propertyValidators = validatorFinder.FindValidators(r);
                foreach (var ruleProvider in RuleProviders)
                {
                    ruleProvider.Invoke(propertyValidators, h, r);
                }
            });
        }

        public static string GetMessage(RequestData requestData, PropertyValidatorResult propertyValidator)
        {
            MessageFormatter formatter = new MessageFormatter().AppendPropertyName(propertyValidator.DisplayName);
            string message = formatter.BuildMessage(propertyValidator.PropertyValidator.ErrorMessageSource.GetString(null));
            return message;
        }

        public void AddEqualToDataAttr(IEnumerable<PropertyValidatorResult> propertyValidators, HtmlTag htmlTag, RequestData request)
        {
            var result = propertyValidators.FirstOrDefault(x => x.PropertyValidator is EqualValidator);
            if (result != null)
            {
                var equal = result.PropertyValidator as EqualValidator;
                
                if (equal.MemberToCompare != null)
                {
                    MessageFormatter formatter = new MessageFormatter()
                        .AppendPropertyName(result.DisplayName)
                        .AppendArgument("ComparisonValue", equal.MemberToCompare.Name);
                    
                    string message = formatter.BuildMessage(equal.ErrorMessageSource.GetString(null));

                    htmlTag.Data("val", true);
                    htmlTag.Data("val-equalto", message);
                    if (request.Accessor.PropertyNames.Length > 1)
                        htmlTag.Data("val-equalto-other", request.Id.Replace("_" + request.Accessor.Name, "") + "_" + equal.MemberToCompare.Name);
                    else
                        htmlTag.Data("val-equalto-other", "*." + equal.MemberToCompare.Name);
                }
            }
        }

        public void AddRequiredClass(IEnumerable<PropertyValidatorResult> propertyValidators, HtmlTag htmlTag, RequestData requestData)
        {
            var result = propertyValidators.FirstOrDefault(x => x.PropertyValidator is NotEmptyValidator
                                                             || x.PropertyValidator is NotNullValidator);

            if (result != null)
            {
                htmlTag.Data("val", true).Data("val-required", GetMessage(requestData, result) ?? string.Empty);
            }
        }

        public void AddLengthClasses(IEnumerable<PropertyValidatorResult> propertyValidators, HtmlTag htmlTag, RequestData requestData)
        {
            var result = propertyValidators.FirstOrDefault(x => x.PropertyValidator is LengthValidator);
            if (result != null)
            {
                htmlTag.Attr("maxlength", ((LengthValidator)result.PropertyValidator).Max);
            }
        }

        public void AddCreditCardClass(IEnumerable<PropertyValidatorResult> propertyValidators, HtmlTag htmlTag, RequestData requestData)
        {
            var lengthValidator = propertyValidators.Select(x => x.PropertyValidator).OfType<CreditCardValidator>().FirstOrDefault();
            if (lengthValidator != null)
            {
                //if (!_msUnobtrusive)
                //{
                //    htmlTag.Data("rule-creditcard", true);
                //}
            }
        }

        public void AddRegexData(IEnumerable<PropertyValidatorResult> propertyValidators, HtmlTag htmlTag, RequestData requestData)
        {
            var result = propertyValidators.FirstOrDefault(x => x.PropertyValidator is RegularExpressionValidator);

            if (result != null)
            {
                var regex = result.PropertyValidator as RegularExpressionValidator;
                var msg = GetMessage(requestData, result) ?? string.Format("The value did not match the regular expression '{0}'", regex.Expression);
                htmlTag.Data("val", true).Data("val-regex", msg).Data("val-regex-pattern", regex.Expression);
            }
        }

        public void AddEmailData(IEnumerable<PropertyValidatorResult> propertyValidators, HtmlTag htmlTag, RequestData requestData)
        {
            var result = propertyValidators.FirstOrDefault(x => x.PropertyValidator is EmailValidator);
            if (result != null)
            {
                var msg = GetMessage(requestData, result) ?? string.Format("The value is not a valid email address");
                htmlTag.Data("val", true).Data("val-email", msg);
            }
        }
    }
}