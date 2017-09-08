using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace SchoStack.AspNetCore.ModelUrls
{
    public class ActionConventionOptions
    {
        public Dictionary<Type, Func<object, ActionContext, string>> TypeFormatters { get; set; } = new Dictionary<Type, Func<object, ActionContext, string>>();
        public DefaultPropertyNameModfier PropertyNameModifier { get; set; } = new DefaultPropertyNameModfier();
        public void AddTypeFormatter<T>(Func<object, ActionContext, string> formatter)
        {
            TypeFormatters.Add(typeof(T), formatter);
        }
    }
}