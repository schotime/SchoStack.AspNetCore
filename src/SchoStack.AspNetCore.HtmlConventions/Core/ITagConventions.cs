using System;

namespace SchoStack.AspNetCore.HtmlConventions.Core
{
    public interface ITagConventions
    {
        IConventionAction Always { get; }
        IConventionAction If(Func<RequestData, bool> condition);
        IConventionAction If<T>();
        IConventionActionAttribute<TAttribute> IfAttribute<TAttribute>() where TAttribute : Attribute;
    }
}