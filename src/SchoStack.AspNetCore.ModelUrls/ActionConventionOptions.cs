using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace SchoStack.AspNetCore.ModelUrls
{
    public delegate string TypeFormatterDelegate(object value, ActionContext context);

    public class ActionConventionOptions
    {
        public Dictionary<Type, TypeFormatterDelegate> TypeFormatters { get; set; } = new();
        public DefaultPropertyNameModfier PropertyNameModifier { get; set; } = new DefaultPropertyNameModfier();
        public void AddTypeFormatter<T>(TypeFormatterDelegate formatter)
        {
            TypeFormatters.Add(typeof(T), formatter);
        }
    }
}