using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using NSubstitute;
using SchoStack.AspNetCore.ModelUrls;
using System.Reflection;

namespace SchoStack.AspNetCore.Tests.ModelUrls
{
    [TestClass]
    public class RouteValueDictionaryGeneratorTests
    {
        private class TestModel
        {
            public int IntValue { get; set; }
            public double DoubleValue { get; set; }
            public DateTime DateValue { get; set; }
            public string StringValue { get; set; }
            public Guid GuidValue { get; set; }
            public CustomFormattable Formattable { get; set; }
            public string SomeProperty { get; set; }
            public NestedModel Nested { get; set; }
            public List<NestedModel> Items { get; set; }
        }

        private class CustomFormattable : IFormattable
        {
            public int A { get; set; }
            public int B { get; set; }
            public string ToString(string format, IFormatProvider formatProvider) => $"A={A},B={B}";
            public override string ToString() => ToString(null, null);
        }

        private class NestedModel
        {
            public string NestedValue { get; set; }
            public int NestedInt { get; set; }
            public NestedModel Deeper { get; set; }
        }

        private RouteValueDictionary GenerateRouteValues(object model, ActionConventionOptions options = null, string prefix = "", RouteValueDictionary dict = null)
        {
            var actionContext = new ActionContext();
            options ??= new ActionConventionOptions();
            var generator = new RouteValueDictionaryGenerator(actionContext, options);
            return generator.Generate(model, prefix, dict);
        }

        [TestMethod]
        public void ConvertibleTypes_Are_Added_Without_Expansion_Or_Conversion()
        {
            var now = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc);
            var model = new TestModel
            {
                IntValue = 42,
                DoubleValue = 3.14,
                DateValue = now,
                StringValue = "hello"
            };

            var dict = GenerateRouteValues(model);

            Assert.AreEqual(42, dict["IntValue"]);
            Assert.AreEqual(3.14, dict["DoubleValue"]);
            Assert.AreEqual(now, dict["DateValue"]);
            Assert.AreEqual("hello", dict["StringValue"]);
            Assert.IsFalse(dict.ContainsKey("DateValue.Day"));
            Assert.IsFalse(dict.ContainsKey("StringValue.Length"));
        }

        [TestMethod]
        public void IFormattable_Is_Added_Without_Expansion()
        {
            var model = new TestModel
            {
                Formattable = new CustomFormattable { A = 1, B = 2 }
            };

            var dict = GenerateRouteValues(model);

            Assert.IsInstanceOfType(dict["Formattable"], typeof(CustomFormattable));
            Assert.IsFalse(dict.ContainsKey("Formattable.A"));
            Assert.IsFalse(dict.ContainsKey("Formattable.B"));
            Assert.AreEqual(1, dict.Count);
        }

        [TestMethod]
        public void TypeFormatter_Is_Used_And_Value_Is_Converted()
        {
            var guid = Guid.NewGuid();
            var model = new TestModel { GuidValue = guid };
            var options = new ActionConventionOptions();
            options.AddTypeFormatter<Guid>((value, ctx) => ((Guid)value).ToString("N"));

            var dict = GenerateRouteValues(model, options);

            Assert.AreEqual(guid.ToString("N"), dict["GuidValue"]);
        }

        [TestMethod]
        public void TypeFormatter_Takes_Precedence_Over_Convertible()
        {
            var model = new TestModel { IntValue = 123 };
            var options = new ActionConventionOptions();
            options.AddTypeFormatter<int>((value, ctx) => $"formatted-{value}");

            var dict = GenerateRouteValues(model, options);

            Assert.AreEqual("formatted-123", dict["IntValue"]);
            Assert.AreEqual(1, dict.Count);
        }

        [TestMethod]
        public void TypeFormatter_Takes_Precedence_Over_IFormattable()
        {
            var model = new TestModel
            {
                Formattable = new CustomFormattable { A = 5, B = 6 }
            };
            var options = new ActionConventionOptions();
            options.AddTypeFormatter<CustomFormattable>((value, ctx) => "formatter-wins");

            var dict = GenerateRouteValues(model, options);

            Assert.AreEqual("formatter-wins", dict["Formattable"]);
            Assert.IsFalse(dict.ContainsKey("Formattable.A"));
            Assert.IsFalse(dict.ContainsKey("Formattable.B"));
            Assert.AreEqual(1, dict.Count);
        }

        [TestMethod]
        public void ComplexType_NotConvertible_ExpandsProperties()
        {
            var model = new { X = 1, Y = "abc" };
            var dict = GenerateRouteValues(model);

            Assert.AreEqual(1, dict["X"]);
            Assert.AreEqual("abc", dict["Y"]);
        }

        [TestMethod]
        public void Recursion_Expands_Nested_Complex_Properties()
        {
            var model = new TestModel
            {
                StringValue = "parent",
                Nested = new NestedModel { NestedValue = "child", NestedInt = 99 }
            };

            var dict = GenerateRouteValues(model);

            Assert.AreEqual("parent", dict["StringValue"]);
            Assert.AreEqual("child", dict["Nested.NestedValue"]);
            Assert.AreEqual(99, dict["Nested.NestedInt"]);
        }

        [TestMethod]
        public void Recursion_Expands_Enumerable_Of_Complex_Properties()
        {
            var model = new TestModel
            {
                Items = new List<NestedModel>
                {
                    new NestedModel { NestedValue = "a", NestedInt = 1 },
                    new NestedModel { NestedValue = "b", NestedInt = 2 }
                }
            };

            var dict = GenerateRouteValues(model);

            Assert.AreEqual("a", dict["Items[0].NestedValue"]);
            Assert.AreEqual(1, dict["Items[0].NestedInt"]);
            Assert.AreEqual("b", dict["Items[1].NestedValue"]);
            Assert.AreEqual(2, dict["Items[1].NestedInt"]);
        }

        [TestMethod]
        public void Recursion_Expands_Deeply_Nested_Properties()
        {
            var model = new TestModel
            {
                Nested = new NestedModel
                {
                    NestedValue = "level1",
                    Deeper = new NestedModel
                    {
                        NestedValue = "level2",
                        Deeper = new NestedModel
                        {
                            NestedValue = "deep"
                        }
                    }
                }
            };

            var dict = GenerateRouteValues(model);

            Assert.AreEqual("level1", dict["Nested.NestedValue"]);
            Assert.AreEqual("level2", dict["Nested.Deeper.NestedValue"]);
            Assert.AreEqual("deep", dict["Nested.Deeper.Deeper.NestedValue"]);
        }

        [TestMethod]
        public void PropertyNameModifier_Is_Called_And_Output_Used()
        {
            var modifier = Substitute.For<DefaultPropertyNameModfier>();
            var model = new TestModel { SomeProperty = "value" };
            var options = new ActionConventionOptions { PropertyNameModifier = modifier };
            modifier.GetModifiedPropertyName(Arg.Any<PropertyInfo>(), Arg.Any<Attribute[]>()).Returns("custom_key");

            var dict = GenerateRouteValues(model, options);

            modifier.Received().GetModifiedPropertyName(
                Arg.Is<PropertyInfo>(pi => pi.Name == "SomeProperty"),
                Arg.Any<Attribute[]>()
            );
            Assert.IsTrue(dict.ContainsKey("custom_key"));
            Assert.AreEqual("value", dict["custom_key"]);
        }

        [TestMethod]
        public void Prefix_Is_Only_Applied_To_Top_Level()
        {
            var model = new TestModel
            {
                IntValue = 1,
                Nested = new NestedModel
                {
                    NestedValue = "inner",
                    NestedInt = 2,
                    Deeper = new NestedModel
                    {
                        NestedValue = "deep"
                    }
                }
            };

            var dict = GenerateRouteValues(model, prefix: "top.");

            Assert.AreEqual(1, dict["top.IntValue"]);
            Assert.AreEqual("inner", dict["top.Nested.NestedValue"]);
            Assert.AreEqual(2, dict["top.Nested.NestedInt"]);
            Assert.AreEqual("deep", dict["top.Nested.Deeper.NestedValue"]);
            Assert.IsFalse(dict.ContainsKey("top.top.Nested.NestedValue"));
            Assert.IsFalse(dict.ContainsKey("top.Nested.top.NestedValue"));
        }

        [TestMethod]
        public void Existing_Dictionary_Is_Used()
        {
            var model = new TestModel
            {
                IntValue = 5,
                StringValue = "abc"
            };

            var existing = new RouteValueDictionary
            {
                { "existing", 123 }
            };

            var dict = GenerateRouteValues(model, dict: existing);

            Assert.AreSame(existing, dict);
            Assert.AreEqual(123, dict["existing"]);
            Assert.AreEqual(5, dict["IntValue"]);
            Assert.AreEqual("abc", dict["StringValue"]);
        }
    }
}
