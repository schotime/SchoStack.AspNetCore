using Microsoft.AspNetCore.Mvc;
using SchoStack.AspNetCore.ModelUrls;

namespace SchoStack.AspNetCore.Tests.ModelUrls
{
    [TestClass]
    public class DefaultPropertyNameModifierTests
    {
        private class TestModel
        {
            public string NoAttribute { get; set; }

            [FromQuery(Name = "customName")]
            public string WithFromQuery { get; set; }
        }

        [TestMethod]
        public void ReturnsPropertyName_WhenNoFromQueryAttribute()
        {
            var property = typeof(TestModel).GetProperty(nameof(TestModel.NoAttribute));
            var attributes = Attribute.GetCustomAttributes(property);
            var modifier = new DefaultPropertyNameModfier();

            var result = modifier.GetModifiedPropertyName(property, attributes);

            Assert.AreEqual("NoAttribute", result);
        }

        [TestMethod]
        public void ReturnsFromQueryName_WhenFromQueryAttributePresent()
        {
            var property = typeof(TestModel).GetProperty(nameof(TestModel.WithFromQuery));
            var attributes = Attribute.GetCustomAttributes(property);
            var modifier = new DefaultPropertyNameModfier();

            var result = modifier.GetModifiedPropertyName(property, attributes);

            Assert.AreEqual("customName", result);
        }
    }
}
