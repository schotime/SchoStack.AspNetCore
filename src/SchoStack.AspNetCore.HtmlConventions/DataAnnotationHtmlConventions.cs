﻿using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using HtmlTags;
using Microsoft.AspNetCore.Mvc;
using SchoStack.AspNetCore.HtmlConventions.Core;

namespace SchoStack.AspNetCore.HtmlConventions
{
    public class DataAnnotationHtmlConventions : HtmlConvention
    {
        public DataAnnotationHtmlConventions()
        {
            Labels.IfAttribute<DisplayNameAttribute>().Modify((h, r, a) => h.Text(a.DisplayName));

            Inputs.IfAttribute<DataTypeAttribute>().BuildBy((r, a) =>
            {
                if (a.DataType == DataType.Text)
                    return new HtmlTag("textarea").Text(r.GetAttemptedValue() ?? r.GetValue<string>());
                
                return null;
            });

            Inputs.IfAttribute<DataTypeAttribute>().Modify((h, r, a) =>
            {
                if (a.DataType == DataType.Password)
                    h.Attr("type", "password").Attr("value", null).Attr("autocomplete", "off");
            });
            
            Inputs.IfAttribute<HiddenInputAttribute>().BuildBy((r, a) =>
            {
                var hidden = new HiddenTag().Attr("value", r.GetAttemptedValue() ?? r.GetValue<object>());
                return hidden;
            });

            Inputs.IfAttribute<StringLengthAttribute>().Modify((h, r, a) =>
            {
                h.Attr("maxlength", a.MaximumLength);
                if (a.MinimumLength > 0)
                    h.Attr("minlength", a.MinimumLength);
            });

            Inputs.Always.Modify((h, r) => AddNumberClasses(r, h));
        }
        
        private static void AddNumberClasses(RequestData r, HtmlTag h)
        {
            if (r.Accessor.PropertyType == typeof(int) || r.Accessor.PropertyType == typeof(int?)
                || r.Accessor.PropertyType == typeof(uint) || r.Accessor.PropertyType == typeof(uint?)
                || r.Accessor.PropertyType == typeof(long) || r.Accessor.PropertyType == typeof(long?)
                || r.Accessor.PropertyType == typeof(ulong) || r.Accessor.PropertyType == typeof(ulong?)
                || r.Accessor.PropertyType == typeof(short) || r.Accessor.PropertyType == typeof(short?)
                || r.Accessor.PropertyType == typeof(ushort) || r.Accessor.PropertyType == typeof(ushort?))
            {
                h.AddClass("digits");
            }
            else if (r.Accessor.PropertyType == typeof(double) || r.Accessor.PropertyType == typeof(double?)
                || r.Accessor.PropertyType == typeof(decimal) || r.Accessor.PropertyType == typeof(decimal?)
                || r.Accessor.PropertyType == typeof(float) || r.Accessor.PropertyType == typeof(float?))
            {
                h.AddClass("number");
            }
        }

    }
}