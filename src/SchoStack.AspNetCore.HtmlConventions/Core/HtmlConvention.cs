namespace SchoStack.AspNetCore.HtmlConventions.Core
{
    public class HtmlConvention
    {
        public HtmlConvention()
        {
            Inputs = new TagConventions(this);
            Labels = new TagConventions(this);
            Displays = new TagConventions(this);
            All = new TagConventions(this, true);
        }

        public ITagConventions All { get; private set; }
        public ITagConventions Inputs { get; private set; }
        public ITagConventions Labels { get; private set; }
        public ITagConventions Displays { get; private set; }

        public bool UsePropertyValueType { get; set; }
    }
}