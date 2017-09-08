using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SchoStack.AspNetCore.HtmlConventions.Core
{
    public class InputTypeMvcForm : MvcForm
    {
        private readonly ViewContext _viewContext;

        public InputTypeMvcForm(ViewContext viewContext) : base(viewContext, HtmlEncoder.Default)
        {
            _viewContext = viewContext;
        }

        protected override void GenerateEndForm()
        {
            _viewContext.HttpContext.Items.Remove(TagGenerator.FORMINPUTTYPE);
            base.GenerateEndForm();
        }
    }
}