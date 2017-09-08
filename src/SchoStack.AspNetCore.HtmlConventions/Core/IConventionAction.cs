using System;
using HtmlTags;

namespace SchoStack.AspNetCore.HtmlConventions.Core
{
    public interface IConventionAction
    {
        void BuildBy(Func<RequestData, HtmlTag> builder);
        void BuildBy(IHtmlBuilder builder);
        void BuildBy(Func<RequestData, IConventionPipeline, HtmlTag> builder);
        void Modify(Action<HtmlTag, RequestData> modifier);
    }
}