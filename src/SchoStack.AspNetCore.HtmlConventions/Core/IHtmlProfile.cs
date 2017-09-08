using System.Collections.Generic;

namespace SchoStack.AspNetCore.HtmlConventions.Core
{
    public interface IHtmlProfile
    {
        List<HtmlConvention> HtmlConventions { get; }
    }

    public abstract class HtmlProfile : IHtmlProfile
    {
        protected HtmlProfile()
        {
            HtmlConventions = new List<HtmlConvention>();
        }

        public List<HtmlConvention> HtmlConventions { get; private set; }
    }
}