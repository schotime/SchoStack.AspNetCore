using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.DependencyInjection;

namespace SchoStack.AspNetCore.ModelUrls
{
    [HtmlTargetElement(Attributes = "model-url-*")]
    public class ModelUrlTagHelper : TagHelper
    {
        private readonly IHtmlGenerator _generator;
        private IDictionary<string, object> _modelUrls;

        public override int Order => -1001;

        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        /// <summary>An instance of the model to be used for URL generation.</summary>
        [HtmlAttributeName("model-all-url", DictionaryAttributePrefix = "model-url-")]
        public IDictionary<string, object> ModelUrls 
        {
            get => _modelUrls ?? (_modelUrls = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase));
            set => _modelUrls = value;
        }

        public ModelUrlTagHelper(IHtmlGenerator generator)
        {
            _generator = generator;
        }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var urlHelper = ViewContext.HttpContext.RequestServices.GetRequiredService<IUrlHelper>();
            var bindQuery = false;

            if (context.TagName == "form" && ModelUrls.ContainsKey("action"))
            {
                var modelType = ModelUrls["action"].GetType();
                ViewContext.HttpContext.Items["SchoStack.Model"] = modelType;
                var modelConventions = ViewContext.HttpContext.RequestServices.GetRequiredService<TypedRoutingApplicationModelConvention>();
                if (modelConventions.RouteInformations.ContainsKey(modelType) && modelConventions.RouteInformations[modelType].Method == HttpMethod.Post)
                {
                    output.Attributes.SetAttribute("method", "post");
                    var antiforgery = _generator.GenerateAntiforgery(ViewContext);
                    if (antiforgery != null)
                    {
                        output.PostContent.AppendHtml(antiforgery);
                    }
                }
                else
                {
                    bindQuery = true;
                }
            }

            foreach (var modelUrl in ModelUrls)
            {
                output.Attributes.SetAttribute(modelUrl.Key, await urlHelper.ForAsync(modelUrl.Value, bindQuery));
            }
        }
    }
}
