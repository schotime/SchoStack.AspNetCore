using System.Collections.Generic;

namespace SchoStack.AspNetCore.HtmlConventions.Core
{
    public interface IConventionAccessor
    {
        IList<Modifier> Modifiers { get; }
        IList<Builder> Builders { get; }
        bool IsAll { get; }
    }
}