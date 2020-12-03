using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace SchoStack.AspNetCore.ModelUrls
{
    internal static class ModelBindingHelper
    {
        /// <summary>
        /// Updates the specified <paramref name="model"/> instance using the specified
        /// <paramref name="modelBinderFactory"/> and the specified <paramref name="valueProvider"/> and executes
        /// validation using the specified <paramref name="objectModelValidator"/>.
        /// </summary>
        /// <typeparam name="TModel">The type of the model object.</typeparam>
        /// <param name="model">The model instance to update and validate.</param>
        /// <param name="prefix">The prefix to use when looking up values in the <paramref name="valueProvider"/>.
        /// </param>
        /// <param name="actionContext">The <see cref="ActionContext"/> for the current executing request.</param>
        /// <param name="metadataProvider">The provider used for reading metadata for the model type.</param>
        /// <param name="modelBinderFactory">The <see cref="IModelBinderFactory"/> used for binding.</param>
        /// <param name="valueProvider">The <see cref="IValueProvider"/> used for looking up values.</param>
        /// <param name="objectModelValidator">The <see cref="IObjectModelValidator"/> used for validating the
        /// bound values.</param>
        /// <returns>A <see cref="Task"/> that on completion returns <c>true</c> if the update is successful</returns>
        public static Task<bool> TryUpdateModelAsync<TModel>(
            TModel model,
            string prefix,
            ActionContext actionContext,
            IModelMetadataProvider metadataProvider,
            IModelBinderFactory modelBinderFactory,
            IValueProvider valueProvider,
            IObjectModelValidator objectModelValidator)
            where TModel : class
        {
            return TryUpdateModelAsync(
                model,
                prefix,
                actionContext,
                metadataProvider,
                modelBinderFactory,
                valueProvider,
                objectModelValidator,
                // Includes everything by default.
                propertyFilter: (m) => true);
        }

        /// <summary>
        /// Updates the specified <paramref name="model"/> instance using the specified <paramref name="modelBinderFactory"/>
        /// and the specified <paramref name="valueProvider"/> and executes validation using the specified
        /// <paramref name="objectModelValidator"/>.
        /// </summary>
        /// <typeparam name="TModel">The type of the model object.</typeparam>
        /// <param name="model">The model instance to update and validate.</param>
        /// <param name="prefix">The prefix to use when looking up values in the <paramref name="valueProvider"/>.
        /// </param>
        /// <param name="actionContext">The <see cref="ActionContext"/> for the current executing request.</param>
        /// <param name="metadataProvider">The provider used for reading metadata for the model type.</param>
        /// <param name="modelBinderFactory">The <see cref="IModelBinderFactory"/> used for binding.</param>
        /// <param name="valueProvider">The <see cref="IValueProvider"/> used for looking up values.</param>
        /// <param name="objectModelValidator">The <see cref="IObjectModelValidator"/> used for validating the
        /// bound values.</param>
        /// <param name="includeExpressions">Expression(s) which represent top level properties
        /// which need to be included for the current model.</param>
        /// <returns>A <see cref="Task"/> that on completion returns <c>true</c> if the update is successful</returns>
        public static Task<bool> TryUpdateModelAsync<TModel>(
            TModel model,
            string prefix,
            ActionContext actionContext,
            IModelMetadataProvider metadataProvider,
            IModelBinderFactory modelBinderFactory,
            IValueProvider valueProvider,
            IObjectModelValidator objectModelValidator,
            params Expression<Func<TModel, object>>[] includeExpressions)
            where TModel : class
        {
            if (includeExpressions == null)
            {
                throw new ArgumentNullException(nameof(includeExpressions));
            }

            var expression = GetPropertyFilterExpression(includeExpressions);
            var propertyFilter = expression.Compile();

            return TryUpdateModelAsync(
                model,
                prefix,
                actionContext,
                metadataProvider,
                modelBinderFactory,
                valueProvider,
                objectModelValidator,
                propertyFilter);
        }

        /// <summary>
        /// Updates the specified <paramref name="model"/> instance using the specified <paramref name="modelBinderFactory"/>
        /// and the specified <paramref name="valueProvider"/> and executes validation using the specified
        /// <paramref name="objectModelValidator"/>.
        /// </summary>
        /// <typeparam name="TModel">The type of the model object.</typeparam>
        /// <param name="model">The model instance to update and validate.</param>
        /// <param name="prefix">The prefix to use when looking up values in the <paramref name="valueProvider"/>.
        /// </param>
        /// <param name="actionContext">The <see cref="ActionContext"/> for the current executing request.</param>
        /// <param name="metadataProvider">The provider used for reading metadata for the model type.</param>
        /// <param name="modelBinderFactory">The <see cref="IModelBinderFactory"/> used for binding.</param>
        /// <param name="valueProvider">The <see cref="IValueProvider"/> used for looking up values.</param>
        /// <param name="objectModelValidator">The <see cref="IObjectModelValidator"/> used for validating the
        /// bound values.</param>
        /// <param name="propertyFilter">
        /// A predicate which can be used to filter properties(for inclusion/exclusion) at runtime.
        /// </param>
        /// <returns>A <see cref="Task"/> that on completion returns <c>true</c> if the update is successful</returns>
        public static Task<bool> TryUpdateModelAsync<TModel>(
            TModel model,
            string prefix,
            ActionContext actionContext,
            IModelMetadataProvider metadataProvider,
            IModelBinderFactory modelBinderFactory,
            IValueProvider valueProvider,
            IObjectModelValidator objectModelValidator,
            Func<ModelMetadata, bool> propertyFilter)
            where TModel : class
        {
            return TryUpdateModelAsync(
                model,
                typeof(TModel),
                prefix,
                actionContext,
                metadataProvider,
                modelBinderFactory,
                valueProvider,
                objectModelValidator,
                propertyFilter);
        }

        /// <summary>
        /// Updates the specified <paramref name="model"/> instance using the specified <paramref name="modelBinderFactory"/>
        /// and the specified <paramref name="valueProvider"/> and executes validation using the specified
        /// <paramref name="objectModelValidator"/>.
        /// </summary>
        /// <param name="model">The model instance to update and validate.</param>
        /// <param name="modelType">The type of model instance to update and validate.</param>
        /// <param name="prefix">The prefix to use when looking up values in the <paramref name="valueProvider"/>.
        /// </param>
        /// <param name="actionContext">The <see cref="ActionContext"/> for the current executing request.</param>
        /// <param name="metadataProvider">The provider used for reading metadata for the model type.</param>
        /// <param name="modelBinderFactory">The <see cref="IModelBinderFactory"/> used for binding.</param>
        /// <param name="valueProvider">The <see cref="IValueProvider"/> used for looking up values.</param>
        /// <param name="objectModelValidator">The <see cref="IObjectModelValidator"/> used for validating the
        /// bound values.</param>
        /// <returns>A <see cref="Task"/> that on completion returns <c>true</c> if the update is successful</returns>
        public static Task<bool> TryUpdateModelAsync(
            object model,
            Type modelType,
            string prefix,
            ActionContext actionContext,
            IModelMetadataProvider metadataProvider,
            IModelBinderFactory modelBinderFactory,
            IValueProvider valueProvider,
            IObjectModelValidator objectModelValidator)
        {
            return TryUpdateModelAsync(
                model,
                modelType,
                prefix,
                actionContext,
                metadataProvider,
                modelBinderFactory,
                valueProvider,
                objectModelValidator,
                // Includes everything by default.
                propertyFilter: (m) => true);
        }

        /// <summary>
        /// Updates the specified <paramref name="model"/> instance using the specified <paramref name="modelBinderFactory"/>
        /// and the specified <paramref name="valueProvider"/> and executes validation using the specified
        /// <paramref name="objectModelValidator"/>.
        /// </summary>
        /// <param name="model">The model instance to update and validate.</param>
        /// <param name="modelType">The type of model instance to update and validate.</param>
        /// <param name="prefix">The prefix to use when looking up values in the <paramref name="valueProvider"/>.
        /// </param>
        /// <param name="actionContext">The <see cref="ActionContext"/> for the current executing request.</param>
        /// <param name="metadataProvider">The provider used for reading metadata for the model type.</param>
        /// <param name="modelBinderFactory">The <see cref="IModelBinderFactory"/> used for binding.</param>
        /// <param name="valueProvider">The <see cref="IValueProvider"/> used for looking up values.</param>
        /// <param name="objectModelValidator">The <see cref="IObjectModelValidator"/> used for validating the
        /// bound values.</param>
        /// <param name="propertyFilter">A predicate which can be used to
        /// filter properties(for inclusion/exclusion) at runtime.</param>
        /// <returns>A <see cref="Task"/> that on completion returns <c>true</c> if the update is successful</returns>
        public static async Task<bool> TryUpdateModelAsync(
            object model,
            Type modelType,
            string prefix,
            ActionContext actionContext,
            IModelMetadataProvider metadataProvider,
            IModelBinderFactory modelBinderFactory,
            IValueProvider valueProvider,
            IObjectModelValidator objectModelValidator,
            Func<ModelMetadata, bool> propertyFilter)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (modelType == null)
            {
                throw new ArgumentNullException(nameof(modelType));
            }

            if (prefix == null)
            {
                throw new ArgumentNullException(nameof(prefix));
            }

            if (actionContext == null)
            {
                throw new ArgumentNullException(nameof(actionContext));
            }

            if (metadataProvider == null)
            {
                throw new ArgumentNullException(nameof(metadataProvider));
            }

            if (modelBinderFactory == null)
            {
                throw new ArgumentNullException(nameof(modelBinderFactory));
            }

            if (valueProvider == null)
            {
                throw new ArgumentNullException(nameof(valueProvider));
            }

            if (objectModelValidator == null)
            {
                throw new ArgumentNullException(nameof(objectModelValidator));
            }

            if (propertyFilter == null)
            {
                throw new ArgumentNullException(nameof(propertyFilter));
            }

            if (!modelType.IsAssignableFrom(model.GetType()))
            {
                var message = Resources.FormatModelType_WrongType(
                    model.GetType().FullName,
                    modelType.FullName);
                throw new ArgumentException(message, nameof(modelType));
            }

            var modelMetadata = metadataProvider.GetMetadataForType(modelType);

            //if (modelMetadata.BoundConstructor != null)
            //{
            //    throw new NotSupportedException(Resources.FormatTryUpdateModel_RecordTypeNotSupported(nameof(TryUpdateModelAsync), modelType));
            //}

            var modelState = actionContext.ModelState;

            var modelBindingContext = DefaultModelBindingContext.CreateBindingContext(
                actionContext,
                valueProvider,
                modelMetadata,
                bindingInfo: null,
                modelName: prefix);

            modelBindingContext.Model = model;
            modelBindingContext.PropertyFilter = propertyFilter;

            var factoryContext = new ModelBinderFactoryContext()
            {
                Metadata = modelMetadata,
                BindingInfo = new BindingInfo()
                {
                    BinderModelName = modelMetadata.BinderModelName,
                    BinderType = modelMetadata.BinderType,
                    BindingSource = modelMetadata.BindingSource,
                    PropertyFilterProvider = modelMetadata.PropertyFilterProvider,
                },

                // We're using the model metadata as the cache token here so that TryUpdateModelAsync calls
                // for the same model type can share a binder. This won't overlap with normal model binding
                // operations because they use the ParameterDescriptor for the token.
                CacheToken = modelMetadata,
            };
            var binder = modelBinderFactory.CreateBinder(factoryContext);

            await binder.BindModelAsync(modelBindingContext);
            var modelBindingResult = modelBindingContext.Result;
            if (modelBindingResult.IsModelSet)
            {
                objectModelValidator.Validate(
                    actionContext,
                    modelBindingContext.ValidationState,
                    modelBindingContext.ModelName,
                    modelBindingResult.Model);

                return modelState.IsValid;
            }

            return false;
        }

        // Internal for tests
        internal static string GetPropertyName(Expression expression)
        {
            if (expression.NodeType == ExpressionType.Convert ||
                expression.NodeType == ExpressionType.ConvertChecked)
            {
                // For Boxed Value Types
                expression = ((UnaryExpression)expression).Operand;
            }

            if (expression.NodeType != ExpressionType.MemberAccess)
            {
                throw new InvalidOperationException(
                    Resources.FormatInvalid_IncludePropertyExpression(expression.NodeType));
            }

            var memberExpression = (MemberExpression)expression;
            if (memberExpression.Member is PropertyInfo memberInfo)
            {
                if (memberExpression.Expression.NodeType != ExpressionType.Parameter)
                {
                    // Chained expressions and non parameter based expressions are not supported.
                    throw new InvalidOperationException(
                        Resources.FormatInvalid_IncludePropertyExpression(expression.NodeType));
                }

                return memberInfo.Name;
            }
            else
            {
                // Fields are also not supported.
                throw new InvalidOperationException(
                    Resources.FormatInvalid_IncludePropertyExpression(expression.NodeType));
            }
        }

        /// <summary>
        /// Creates an expression for a predicate to limit the set of properties used in model binding.
        /// </summary>
        /// <typeparam name="TModel">The model type.</typeparam>
        /// <param name="expressions">Expressions identifying the properties to allow for binding.</param>
        /// <returns>An expression which can be used with <see cref="IPropertyFilterProvider"/>.</returns>
        public static Expression<Func<ModelMetadata, bool>> GetPropertyFilterExpression<TModel>(
            Expression<Func<TModel, object>>[] expressions)
        {
            if (expressions.Length == 0)
            {
                // If nothing is included explicitly, treat everything as included.
                return (m) => true;
            }

            var firstExpression = GetPredicateExpression(expressions[0]);
            var orWrapperExpression = firstExpression.Body;
            foreach (var expression in expressions.Skip(1))
            {
                var predicate = GetPredicateExpression(expression);
                orWrapperExpression = Expression.OrElse(
                    orWrapperExpression,
                    Expression.Invoke(predicate, firstExpression.Parameters));
            }

            return Expression.Lambda<Func<ModelMetadata, bool>>(orWrapperExpression, firstExpression.Parameters);
        }

        private static Expression<Func<ModelMetadata, bool>> GetPredicateExpression<TModel>(
            Expression<Func<TModel, object>> expression)
        {
            var propertyName = GetPropertyName(expression.Body);

            return (metadata) => string.Equals(metadata.PropertyName, propertyName, StringComparison.Ordinal);
        }

        /// <summary>
        /// Clears <see cref="ModelStateDictionary"/> entries for <see cref="ModelMetadata"/>.
        /// </summary>
        /// <param name="modelType">The <see cref="Type"/> of the model.</param>
        /// <param name="modelState">The <see cref="ModelStateDictionary"/> associated with the model.</param>
        /// <param name="metadataProvider">The <see cref="IModelMetadataProvider"/>.</param>
        /// <param name="modelKey">The entry to clear. </param>
        public static void ClearValidationStateForModel(
            Type modelType,
            ModelStateDictionary modelState,
            IModelMetadataProvider metadataProvider,
            string modelKey)
        {
            if (modelType == null)
            {
                throw new ArgumentNullException(nameof(modelType));
            }

            if (modelState == null)
            {
                throw new ArgumentNullException(nameof(modelState));
            }

            if (metadataProvider == null)
            {
                throw new ArgumentNullException(nameof(metadataProvider));
            }

            ClearValidationStateForModel(metadataProvider.GetMetadataForType(modelType), modelState, modelKey);
        }

        /// <summary>
        /// Clears <see cref="ModelStateDictionary"/> entries for <see cref="ModelMetadata"/>.
        /// </summary>
        /// <param name="modelMetadata">The <see cref="ModelMetadata"/>.</param>
        /// <param name="modelState">The <see cref="ModelStateDictionary"/> associated with the model.</param>
        /// <param name="modelKey">The entry to clear. </param>
        public static void ClearValidationStateForModel(
            ModelMetadata modelMetadata,
            ModelStateDictionary modelState,
            string modelKey)
        {
            if (modelMetadata == null)
            {
                throw new ArgumentNullException(nameof(modelMetadata));
            }

            if (modelState == null)
            {
                throw new ArgumentNullException(nameof(modelState));
            }

            if (string.IsNullOrEmpty(modelKey))
            {
                // If model key is empty, we have to do a best guess to try and clear the appropriate
                // keys. Clearing the empty prefix would clear the state of ALL entries, which might wipe out
                // data from other models.
                if (modelMetadata.IsEnumerableType)
                {
                    // We expect that any key beginning with '[' is an index. We can't just infer the indexes
                    // used, so we clear all keys that look like <empty prefix -> index>.
                    //
                    // In the unlikely case that multiple top-level collections where bound to the empty prefix,
                    // you're just out of luck.
                    foreach (var kvp in modelState)
                    {
                        if (kvp.Key.Length > 0 && kvp.Key[0] == '[')
                        {
                            // Starts with an indexer
                            kvp.Value.Errors.Clear();
                            kvp.Value.ValidationState = ModelValidationState.Unvalidated;
                        }
                    }
                }
                else if (modelMetadata.IsComplexType)
                {
                    for (var i = 0; i < modelMetadata.Properties.Count; i++)
                    {
                        var property = modelMetadata.Properties[i];
                        modelState.ClearValidationState(property.BinderModelName ?? property.PropertyName);
                    }
                }
                else
                {
                    // Simple types bind to a single entry. So clear the entry with the empty-key, in the
                    // unlikely event that it has errors.
                    var entry = modelState[string.Empty];
                    if (entry != null)
                    {
                        entry.Errors.Clear();
                        entry.ValidationState = ModelValidationState.Unvalidated;
                    }
                }
            }
            else
            {
                // If model key is non-empty, we just want to clear all keys with that prefix. We expect
                // model binding to have only used this key (and suffixes) for all entries related to
                // this model.
                modelState.ClearValidationState(modelKey);
            }
        }

        internal static TModel CastOrDefault<TModel>(object model)
        {
            return (model is TModel) ? (TModel)model : default(TModel);
        }

        /// <summary>
        /// Gets an indication whether <see cref="M:GetCompatibleCollection{T}"/> is likely to return a usable
        /// non-<c>null</c> value.
        /// </summary>
        /// <typeparam name="T">The element type of the <see cref="ICollection{T}"/> required.</typeparam>
        /// <param name="bindingContext">The <see cref="ModelBindingContext"/>.</param>
        /// <returns>
        /// <c>true</c> if <see cref="M:GetCompatibleCollection{T}"/> is likely to return a usable non-<c>null</c>
        /// value; <c>false</c> otherwise.
        /// </returns>
        /// <remarks>"Usable" in this context means the property can be set or its value reused.</remarks>
        public static bool CanGetCompatibleCollection<T>(ModelBindingContext bindingContext)
        {
            var model = bindingContext.Model;
            var modelType = bindingContext.ModelType;

            if (typeof(T).IsAssignableFrom(modelType))
            {
                // Scalar case. Existing model is not relevant and property must always be set. Will use a List<T>
                // intermediate and set property to first element, if any.
                return true;
            }

            if (modelType == typeof(T[]))
            {
                // Can't change the length of an existing array or replace it. Will use a List<T> intermediate and set
                // property to an array created from that.
                return true;
            }

            if (!typeof(IEnumerable<T>).IsAssignableFrom(modelType))
            {
                // Not a supported collection.
                return false;
            }

            if (model is ICollection<T> collection && !collection.IsReadOnly)
            {
                // Can use the existing collection.
                return true;
            }

            // Most likely the model is null.
            // Also covers the corner case where the model implements IEnumerable<T> but not ICollection<T> e.g.
            //   public IEnumerable<T> Property { get; set; } = new T[0];
            if (modelType.IsAssignableFrom(typeof(List<T>)))
            {
                return true;
            }

            // Will we be able to activate an instance and use that?
            return modelType.GetTypeInfo().IsClass &&
                   !modelType.GetTypeInfo().IsAbstract &&
                   typeof(ICollection<T>).IsAssignableFrom(modelType);
        }

        /// <summary>
        /// Creates an <see cref="ICollection{T}"/> instance compatible with <paramref name="bindingContext"/>'s
        /// <see cref="ModelBindingContext.ModelType"/>.
        /// </summary>
        /// <typeparam name="T">The element type of the <see cref="ICollection{T}"/> required.</typeparam>
        /// <param name="bindingContext">The <see cref="ModelBindingContext"/>.</param>
        /// <returns>
        /// An <see cref="ICollection{T}"/> instance compatible with <paramref name="bindingContext"/>'s
        /// <see cref="ModelBindingContext.ModelType"/>.
        /// </returns>
        /// <remarks>
        /// Should not be called if <see cref="CanGetCompatibleCollection{T}"/> returned <c>false</c>.
        /// </remarks>
        public static ICollection<T> GetCompatibleCollection<T>(ModelBindingContext bindingContext)
        {
            return GetCompatibleCollection<T>(bindingContext, capacity: null);
        }

        /// <summary>
        /// Creates an <see cref="ICollection{T}"/> instance compatible with <paramref name="bindingContext"/>'s
        /// <see cref="ModelBindingContext.ModelType"/>.
        /// </summary>
        /// <typeparam name="T">The element type of the <see cref="ICollection{T}"/> required.</typeparam>
        /// <param name="bindingContext">The <see cref="ModelBindingContext"/>.</param>
        /// <param name="capacity">
        /// Capacity for use when creating a <see cref="List{T}"/> instance. Not used when creating another type.
        /// </param>
        /// <returns>
        /// An <see cref="ICollection{T}"/> instance compatible with <paramref name="bindingContext"/>'s
        /// <see cref="ModelBindingContext.ModelType"/>.
        /// </returns>
        /// <remarks>
        /// Should not be called if <see cref="CanGetCompatibleCollection{T}"/> returned <c>false</c>.
        /// </remarks>
        public static ICollection<T> GetCompatibleCollection<T>(ModelBindingContext bindingContext, int capacity)
        {
            return GetCompatibleCollection<T>(bindingContext, (int?)capacity);
        }

        private static ICollection<T> GetCompatibleCollection<T>(ModelBindingContext bindingContext, int? capacity)
        {
            var model = bindingContext.Model;
            var modelType = bindingContext.ModelType;

            // There's a limited set of collection types we can create here.
            //
            // For the simple cases: Choose List<T> if the destination type supports it (at least as an intermediary).
            //
            // For more complex cases: If the destination type is a class that implements ICollection<T>, then activate
            // an instance and return that.
            //
            // Otherwise just give up.
            if (typeof(T).IsAssignableFrom(modelType))
            {
                return CreateList<T>(capacity);
            }

            if (modelType == typeof(T[]))
            {
                return CreateList<T>(capacity);
            }

            // Does collection exist and can it be reused?
            if (model is ICollection<T> collection && !collection.IsReadOnly)
            {
                collection.Clear();

                return collection;
            }

            if (modelType.IsAssignableFrom(typeof(List<T>)))
            {
                return CreateList<T>(capacity);
            }

            return (ICollection<T>)Activator.CreateInstance(modelType);
        }

        private static List<T> CreateList<T>(int? capacity)
        {
            return capacity.HasValue ? new List<T>(capacity.Value) : new List<T>();
        }

        /// <summary>
        /// Converts the provided <paramref name="value"/> to a value of <see cref="Type"/> <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> for conversion.</typeparam>
        /// <param name="value">The value to convert."/></param>
        /// <param name="culture">The <see cref="CultureInfo"/> for conversion.</param>
        /// <returns>
        /// The converted value or the default value of <typeparamref name="T"/> if the value could not be converted.
        /// </returns>
        public static T ConvertTo<T>(object value, CultureInfo culture)
        {
            var converted = ConvertTo(value, typeof(T), culture);
            return converted == null ? default(T) : (T)converted;
        }

        /// <summary>
        /// Converts the provided <paramref name="value"/> to a value of <see cref="Type"/> <paramref name="type"/>.
        /// </summary>
        /// <param name="value">The value to convert."/></param>
        /// <param name="type">The <see cref="Type"/> for conversion.</param>
        /// <param name="culture">The <see cref="CultureInfo"/> for conversion.</param>
        /// <returns>
        /// The converted value or <c>null</c> if the value could not be converted.
        /// </returns>
        public static object ConvertTo(object value, Type type, CultureInfo culture)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (value == null)
            {
                // For value types, treat null values as though they were the default value for the type.
                return type.GetTypeInfo().IsValueType ? Activator.CreateInstance(type) : null;
            }

            if (type.IsAssignableFrom(value.GetType()))
            {
                return value;
            }

            var cultureToUse = culture ?? CultureInfo.InvariantCulture;
            return UnwrapPossibleArrayType(value, type, cultureToUse);
        }

        private static object UnwrapPossibleArrayType(object value, Type destinationType, CultureInfo culture)
        {
            // array conversion results in four cases, as below
            var valueAsArray = value as Array;
            if (destinationType.IsArray)
            {
                var destinationElementType = destinationType.GetElementType();
                if (valueAsArray != null)
                {
                    // case 1: both destination + source type are arrays, so convert each element
                    var converted = (IList)Array.CreateInstance(destinationElementType, valueAsArray.Length);
                    for (var i = 0; i < valueAsArray.Length; i++)
                    {
                        converted[i] = ConvertSimpleType(valueAsArray.GetValue(i), destinationElementType, culture);
                    }
                    return converted;
                }
                else
                {
                    // case 2: destination type is array but source is single element, so wrap element in
                    // array + convert
                    var element = ConvertSimpleType(value, destinationElementType, culture);
                    var converted = (IList)Array.CreateInstance(destinationElementType, 1);
                    converted[0] = element;
                    return converted;
                }
            }
            else if (valueAsArray != null)
            {
                // case 3: destination type is single element but source is array, so extract first element + convert
                if (valueAsArray.Length > 0)
                {
                    value = valueAsArray.GetValue(0);
                    return ConvertSimpleType(value, destinationType, culture);
                }
                else
                {
                    // case 3(a): source is empty array, so can't perform conversion
                    return null;
                }
            }

            // case 4: both destination + source type are single elements, so convert
            return ConvertSimpleType(value, destinationType, culture);
        }

        private static object ConvertSimpleType(object value, Type destinationType, CultureInfo culture)
        {
            if (value == null || destinationType.IsAssignableFrom(value.GetType()))
            {
                return value;
            }

            // In case of a Nullable object, we try again with its underlying type.
            destinationType = UnwrapNullableType(destinationType);

            // if this is a user-input value but the user didn't type anything, return no value
            if (value is string valueAsString && string.IsNullOrWhiteSpace(valueAsString))
            {
                return null;
            }

            var converter = TypeDescriptor.GetConverter(destinationType);
            var canConvertFrom = converter.CanConvertFrom(value.GetType());
            if (!canConvertFrom)
            {
                converter = TypeDescriptor.GetConverter(value.GetType());
            }
            if (!(canConvertFrom || converter.CanConvertTo(destinationType)))
            {
                // EnumConverter cannot convert integer, so we verify manually
                if (destinationType.GetTypeInfo().IsEnum &&
                    (value is int ||
                     value is uint ||
                     value is long ||
                     value is ulong ||
                     value is short ||
                     value is ushort ||
                     value is byte ||
                     value is sbyte))
                {
                    return Enum.ToObject(destinationType, value);
                }

                throw new InvalidOperationException(
                    Resources.FormatValueProviderResult_NoConverterExists(value.GetType(), destinationType));
            }

            try
            {
                return canConvertFrom
                    ? converter.ConvertFrom(null, culture, value)
                    : converter.ConvertTo(null, culture, value, destinationType);
            }
            catch (FormatException)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (ex.InnerException == null)
                {
                    throw;
                }
                else
                {
                    // TypeConverter throws System.Exception wrapping the FormatException,
                    // so we throw the inner exception.
                    ExceptionDispatchInfo.Capture(ex.InnerException).Throw();

                    // This code is never reached because the previous line will always throw.
                    throw;
                }
            }
        }

        private static Type UnwrapNullableType(Type destinationType)
        {
            return Nullable.GetUnderlyingType(destinationType) ?? destinationType;
        }
    }

    internal static partial class Resources
    {
        private static global::System.Resources.ResourceManager s_resourceManager;
        internal static global::System.Resources.ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new global::System.Resources.ResourceManager(typeof(Resources)));
        internal static global::System.Globalization.CultureInfo Culture { get; set; }
#if !NET20
        [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        internal static string GetResourceString(string resourceKey, string defaultValue = null) => ResourceManager.GetString(resourceKey, Culture);

        private static string GetResourceString(string resourceKey, string[] formatterNames)
        {
            var value = GetResourceString(resourceKey);
            if (formatterNames != null)
            {
                for (var i = 0; i < formatterNames.Length; i++)
                {
                    value = value.Replace("{" + formatterNames[i] + "}", "{" + i + "}");
                }
            }
            return value;
        }

        /// <summary>The argument '{0}' is invalid. Media types which match all types or match all subtypes are not supported.</summary>
        internal static string @MatchAllContentTypeIsNotAllowed => GetResourceString("MatchAllContentTypeIsNotAllowed");
        /// <summary>The argument '{0}' is invalid. Media types which match all types or match all subtypes are not supported.</summary>
        internal static string FormatMatchAllContentTypeIsNotAllowed(object p0)
           => string.Format(Culture, GetResourceString("MatchAllContentTypeIsNotAllowed"), p0);

        /// <summary>The content-type '{0}' added in the '{1}' property is invalid. Media types which match all types or match all subtypes are not supported.</summary>
        internal static string @ObjectResult_MatchAllContentType => GetResourceString("ObjectResult_MatchAllContentType");
        /// <summary>The content-type '{0}' added in the '{1}' property is invalid. Media types which match all types or match all subtypes are not supported.</summary>
        internal static string FormatObjectResult_MatchAllContentType(object p0, object p1)
           => string.Format(Culture, GetResourceString("ObjectResult_MatchAllContentType"), p0, p1);

        /// <summary>The method '{0}' on type '{1}' returned an instance of '{2}'. Make sure to call Unwrap on the returned value to avoid unobserved faulted Task.</summary>
        internal static string @ActionExecutor_WrappedTaskInstance => GetResourceString("ActionExecutor_WrappedTaskInstance");
        /// <summary>The method '{0}' on type '{1}' returned an instance of '{2}'. Make sure to call Unwrap on the returned value to avoid unobserved faulted Task.</summary>
        internal static string FormatActionExecutor_WrappedTaskInstance(object p0, object p1, object p2)
           => string.Format(Culture, GetResourceString("ActionExecutor_WrappedTaskInstance"), p0, p1, p2);

        /// <summary>The method '{0}' on type '{1}' returned a Task instance even though it is not an asynchronous method.</summary>
        internal static string @ActionExecutor_UnexpectedTaskInstance => GetResourceString("ActionExecutor_UnexpectedTaskInstance");
        /// <summary>The method '{0}' on type '{1}' returned a Task instance even though it is not an asynchronous method.</summary>
        internal static string FormatActionExecutor_UnexpectedTaskInstance(object p0, object p1)
           => string.Format(Culture, GetResourceString("ActionExecutor_UnexpectedTaskInstance"), p0, p1);

        /// <summary>An action invoker could not be created for action '{0}'.</summary>
        internal static string @ActionInvokerFactory_CouldNotCreateInvoker => GetResourceString("ActionInvokerFactory_CouldNotCreateInvoker");
        /// <summary>An action invoker could not be created for action '{0}'.</summary>
        internal static string FormatActionInvokerFactory_CouldNotCreateInvoker(object p0)
           => string.Format(Culture, GetResourceString("ActionInvokerFactory_CouldNotCreateInvoker"), p0);

        /// <summary>The action descriptor must be of type '{0}'.</summary>
        internal static string @ActionDescriptorMustBeBasedOnControllerAction => GetResourceString("ActionDescriptorMustBeBasedOnControllerAction");
        /// <summary>The action descriptor must be of type '{0}'.</summary>
        internal static string FormatActionDescriptorMustBeBasedOnControllerAction(object p0)
           => string.Format(Culture, GetResourceString("ActionDescriptorMustBeBasedOnControllerAction"), p0);

        /// <summary>Value cannot be null or empty.</summary>
        internal static string @ArgumentCannotBeNullOrEmpty => GetResourceString("ArgumentCannotBeNullOrEmpty");
        /// <summary>The '{0}' property of '{1}' must not be null.</summary>
        internal static string @PropertyOfTypeCannotBeNull => GetResourceString("PropertyOfTypeCannotBeNull");
        /// <summary>The '{0}' property of '{1}' must not be null.</summary>
        internal static string FormatPropertyOfTypeCannotBeNull(object p0, object p1)
           => string.Format(Culture, GetResourceString("PropertyOfTypeCannotBeNull"), p0, p1);

        /// <summary>The '{0}' method of type '{1}' cannot return a null value.</summary>
        internal static string @TypeMethodMustReturnNotNullValue => GetResourceString("TypeMethodMustReturnNotNullValue");
        /// <summary>The '{0}' method of type '{1}' cannot return a null value.</summary>
        internal static string FormatTypeMethodMustReturnNotNullValue(object p0, object p1)
           => string.Format(Culture, GetResourceString("TypeMethodMustReturnNotNullValue"), p0, p1);

        /// <summary>The value '{0}' is invalid.</summary>
        internal static string @ModelBinding_NullValueNotValid => GetResourceString("ModelBinding_NullValueNotValid");
        /// <summary>The value '{0}' is invalid.</summary>
        internal static string FormatModelBinding_NullValueNotValid(object p0)
           => string.Format(Culture, GetResourceString("ModelBinding_NullValueNotValid"), p0);

        /// <summary>The passed expression of expression node type '{0}' is invalid. Only simple member access expressions for model properties are supported.</summary>
        internal static string @Invalid_IncludePropertyExpression => GetResourceString("Invalid_IncludePropertyExpression");
        /// <summary>The passed expression of expression node type '{0}' is invalid. Only simple member access expressions for model properties are supported.</summary>
        internal static string FormatInvalid_IncludePropertyExpression(object p0)
           => string.Format(Culture, GetResourceString("Invalid_IncludePropertyExpression"), p0);

        /// <summary>No route matches the supplied values.</summary>
        internal static string @NoRoutesMatched => GetResourceString("NoRoutesMatched");
        /// <summary>If an {0} provides a result value by setting the {1} property of {2} to a non-null value, then it cannot call the next filter by invoking {3}.</summary>
        internal static string @AsyncActionFilter_InvalidShortCircuit => GetResourceString("AsyncActionFilter_InvalidShortCircuit");
        /// <summary>If an {0} provides a result value by setting the {1} property of {2} to a non-null value, then it cannot call the next filter by invoking {3}.</summary>
        internal static string FormatAsyncActionFilter_InvalidShortCircuit(object p0, object p1, object p2, object p3)
           => string.Format(Culture, GetResourceString("AsyncActionFilter_InvalidShortCircuit"), p0, p1, p2, p3);

        /// <summary>If an {0} cancels execution by setting the {1} property of {2} to 'true', then it cannot call the next filter by invoking {3}.</summary>
        internal static string @AsyncResultFilter_InvalidShortCircuit => GetResourceString("AsyncResultFilter_InvalidShortCircuit");
        /// <summary>If an {0} cancels execution by setting the {1} property of {2} to 'true', then it cannot call the next filter by invoking {3}.</summary>
        internal static string FormatAsyncResultFilter_InvalidShortCircuit(object p0, object p1, object p2, object p3)
           => string.Format(Culture, GetResourceString("AsyncResultFilter_InvalidShortCircuit"), p0, p1, p2, p3);

        /// <summary>The type provided to '{0}' must implement '{1}'.</summary>
        internal static string @FilterFactoryAttribute_TypeMustImplementIFilter => GetResourceString("FilterFactoryAttribute_TypeMustImplementIFilter");
        /// <summary>The type provided to '{0}' must implement '{1}'.</summary>
        internal static string FormatFilterFactoryAttribute_TypeMustImplementIFilter(object p0, object p1)
           => string.Format(Culture, GetResourceString("FilterFactoryAttribute_TypeMustImplementIFilter"), p0, p1);

        /// <summary>Cannot return null from an action method with a return type of '{0}'.</summary>
        internal static string @ActionResult_ActionReturnValueCannotBeNull => GetResourceString("ActionResult_ActionReturnValueCannotBeNull");
        /// <summary>Cannot return null from an action method with a return type of '{0}'.</summary>
        internal static string FormatActionResult_ActionReturnValueCannotBeNull(object p0)
           => string.Format(Culture, GetResourceString("ActionResult_ActionReturnValueCannotBeNull"), p0);

        /// <summary>The type '{0}' must derive from '{1}'.</summary>
        internal static string @TypeMustDeriveFromType => GetResourceString("TypeMustDeriveFromType");
        /// <summary>The type '{0}' must derive from '{1}'.</summary>
        internal static string FormatTypeMustDeriveFromType(object p0, object p1)
           => string.Format(Culture, GetResourceString("TypeMustDeriveFromType"), p0, p1);

        /// <summary>No encoding found for input formatter '{0}'. There must be at least one supported encoding registered in order for the formatter to read content.</summary>
        internal static string @InputFormatterNoEncoding => GetResourceString("InputFormatterNoEncoding");
        /// <summary>No encoding found for input formatter '{0}'. There must be at least one supported encoding registered in order for the formatter to read content.</summary>
        internal static string FormatInputFormatterNoEncoding(object p0)
           => string.Format(Culture, GetResourceString("InputFormatterNoEncoding"), p0);

        /// <summary>Unsupported content type '{0}'.</summary>
        internal static string @UnsupportedContentType => GetResourceString("UnsupportedContentType");
        /// <summary>Unsupported content type '{0}'.</summary>
        internal static string FormatUnsupportedContentType(object p0)
           => string.Format(Culture, GetResourceString("UnsupportedContentType"), p0);

        /// <summary>No supported media type registered for output formatter '{0}'. There must be at least one supported media type registered in order for the output formatter to write content.</summary>
        internal static string @OutputFormatterNoMediaType => GetResourceString("OutputFormatterNoMediaType");
        /// <summary>No supported media type registered for output formatter '{0}'. There must be at least one supported media type registered in order for the output formatter to write content.</summary>
        internal static string FormatOutputFormatterNoMediaType(object p0)
           => string.Format(Culture, GetResourceString("OutputFormatterNoMediaType"), p0);

        /// <summary>The following errors occurred with attribute routing information:{0}{0}{1}</summary>
        internal static string @AttributeRoute_AggregateErrorMessage => GetResourceString("AttributeRoute_AggregateErrorMessage");
        /// <summary>The following errors occurred with attribute routing information:{0}{0}{1}</summary>
        internal static string FormatAttributeRoute_AggregateErrorMessage(object p0, object p1)
           => string.Format(Culture, GetResourceString("AttributeRoute_AggregateErrorMessage"), p0, p1);

        /// <summary>The attribute route '{0}' cannot contain a parameter named '{{{1}}}'. Use '[{1}]' in the route template to insert the value '{2}'.</summary>
        internal static string @AttributeRoute_CannotContainParameter => GetResourceString("AttributeRoute_CannotContainParameter");
        /// <summary>The attribute route '{0}' cannot contain a parameter named '{{{1}}}'. Use '[{1}]' in the route template to insert the value '{2}'.</summary>
        internal static string FormatAttributeRoute_CannotContainParameter(object p0, object p1, object p2)
           => string.Format(Culture, GetResourceString("AttributeRoute_CannotContainParameter"), p0, p1, p2);

        /// <summary>For action: '{0}'{1}Error: {2}</summary>
        internal static string @AttributeRoute_IndividualErrorMessage => GetResourceString("AttributeRoute_IndividualErrorMessage");
        /// <summary>For action: '{0}'{1}Error: {2}</summary>
        internal static string FormatAttributeRoute_IndividualErrorMessage(object p0, object p1, object p2)
           => string.Format(Culture, GetResourceString("AttributeRoute_IndividualErrorMessage"), p0, p1, p2);

        /// <summary>An empty replacement token ('[]') is not allowed.</summary>
        internal static string @AttributeRoute_TokenReplacement_EmptyTokenNotAllowed => GetResourceString("AttributeRoute_TokenReplacement_EmptyTokenNotAllowed");
        /// <summary>Token delimiters ('[', ']') are imbalanced.</summary>
        internal static string @AttributeRoute_TokenReplacement_ImbalancedSquareBrackets => GetResourceString("AttributeRoute_TokenReplacement_ImbalancedSquareBrackets");
        /// <summary>The route template '{0}' has invalid syntax. {1}</summary>
        internal static string @AttributeRoute_TokenReplacement_InvalidSyntax => GetResourceString("AttributeRoute_TokenReplacement_InvalidSyntax");
        /// <summary>The route template '{0}' has invalid syntax. {1}</summary>
        internal static string FormatAttributeRoute_TokenReplacement_InvalidSyntax(object p0, object p1)
           => string.Format(Culture, GetResourceString("AttributeRoute_TokenReplacement_InvalidSyntax"), p0, p1);

        /// <summary>While processing template '{0}', a replacement value for the token '{1}' could not be found. Available tokens: '{2}'. To use a '[' or ']' as a literal string in a route or within a constraint, use '[[' or ']]' instead.</summary>
        internal static string @AttributeRoute_TokenReplacement_ReplacementValueNotFound => GetResourceString("AttributeRoute_TokenReplacement_ReplacementValueNotFound");
        /// <summary>While processing template '{0}', a replacement value for the token '{1}' could not be found. Available tokens: '{2}'. To use a '[' or ']' as a literal string in a route or within a constraint, use '[[' or ']]' instead.</summary>
        internal static string FormatAttributeRoute_TokenReplacement_ReplacementValueNotFound(object p0, object p1, object p2)
           => string.Format(Culture, GetResourceString("AttributeRoute_TokenReplacement_ReplacementValueNotFound"), p0, p1, p2);

        /// <summary>A replacement token is not closed.</summary>
        internal static string @AttributeRoute_TokenReplacement_UnclosedToken => GetResourceString("AttributeRoute_TokenReplacement_UnclosedToken");
        /// <summary>An unescaped '[' token is not allowed inside of a replacement token. Use '[[' to escape.</summary>
        internal static string @AttributeRoute_TokenReplacement_UnescapedBraceInToken => GetResourceString("AttributeRoute_TokenReplacement_UnescapedBraceInToken");
        /// <summary>Unable to find the required services. Please add all the required services by calling '{0}.{1}' inside the call to '{2}' in the application startup code.</summary>
        internal static string @UnableToFindServices => GetResourceString("UnableToFindServices");
        /// <summary>Unable to find the required services. Please add all the required services by calling '{0}.{1}' inside the call to '{2}' in the application startup code.</summary>
        internal static string FormatUnableToFindServices(object p0, object p1, object p2)
           => string.Format(Culture, GetResourceString("UnableToFindServices"), p0, p1, p2);

        /// <summary>Action: '{0}' - Template: '{1}'</summary>
        internal static string @AttributeRoute_DuplicateNames_Item => GetResourceString("AttributeRoute_DuplicateNames_Item");
        /// <summary>Action: '{0}' - Template: '{1}'</summary>
        internal static string FormatAttributeRoute_DuplicateNames_Item(object p0, object p1)
           => string.Format(Culture, GetResourceString("AttributeRoute_DuplicateNames_Item"), p0, p1);

        /// <summary>Attribute routes with the same name '{0}' must have the same template:{1}{2}</summary>
        internal static string @AttributeRoute_DuplicateNames => GetResourceString("AttributeRoute_DuplicateNames");
        /// <summary>Attribute routes with the same name '{0}' must have the same template:{1}{2}</summary>
        internal static string FormatAttributeRoute_DuplicateNames(object p0, object p1, object p2)
           => string.Format(Culture, GetResourceString("AttributeRoute_DuplicateNames"), p0, p1, p2);

        /// <summary>Error {0}:{1}{2}</summary>
        internal static string @AttributeRoute_AggregateErrorMessage_ErrorNumber => GetResourceString("AttributeRoute_AggregateErrorMessage_ErrorNumber");
        /// <summary>Error {0}:{1}{2}</summary>
        internal static string FormatAttributeRoute_AggregateErrorMessage_ErrorNumber(object p0, object p1, object p2)
           => string.Format(Culture, GetResourceString("AttributeRoute_AggregateErrorMessage_ErrorNumber"), p0, p1, p2);

        /// <summary>A method '{0}' must not define attribute routed actions and non attribute routed actions at the same time:{1}{2}{1}{1}Use 'AcceptVerbsAttribute' to create a single route that allows multiple HTTP verbs and defines a route, or set a route template in all at ...</summary>
        internal static string @AttributeRoute_MixedAttributeAndConventionallyRoutedActions_ForMethod => GetResourceString("AttributeRoute_MixedAttributeAndConventionallyRoutedActions_ForMethod");
        /// <summary>A method '{0}' must not define attribute routed actions and non attribute routed actions at the same time:{1}{2}{1}{1}Use 'AcceptVerbsAttribute' to create a single route that allows multiple HTTP verbs and defines a route, or set a route template in all at ...</summary>
        internal static string FormatAttributeRoute_MixedAttributeAndConventionallyRoutedActions_ForMethod(object p0, object p1, object p2)
           => string.Format(Culture, GetResourceString("AttributeRoute_MixedAttributeAndConventionallyRoutedActions_ForMethod"), p0, p1, p2);

        /// <summary>Action: '{0}' - Route Template: '{1}' - HTTP Verbs: '{2}'</summary>
        internal static string @AttributeRoute_MixedAttributeAndConventionallyRoutedActions_ForMethod_Item => GetResourceString("AttributeRoute_MixedAttributeAndConventionallyRoutedActions_ForMethod_Item");
        /// <summary>Action: '{0}' - Route Template: '{1}' - HTTP Verbs: '{2}'</summary>
        internal static string FormatAttributeRoute_MixedAttributeAndConventionallyRoutedActions_ForMethod_Item(object p0, object p1, object p2)
           => string.Format(Culture, GetResourceString("AttributeRoute_MixedAttributeAndConventionallyRoutedActions_ForMethod_Item"), p0, p1, p2);

        /// <summary>(none)</summary>
        internal static string @AttributeRoute_NullTemplateRepresentation => GetResourceString("AttributeRoute_NullTemplateRepresentation");
        /// <summary>Multiple actions matched. The following actions matched route data and had all constraints satisfied:{0}{0}{1}</summary>
        internal static string @DefaultActionSelector_AmbiguousActions => GetResourceString("DefaultActionSelector_AmbiguousActions");
        /// <summary>Multiple actions matched. The following actions matched route data and had all constraints satisfied:{0}{0}{1}</summary>
        internal static string FormatDefaultActionSelector_AmbiguousActions(object p0, object p1)
           => string.Format(Culture, GetResourceString("DefaultActionSelector_AmbiguousActions"), p0, p1);

        /// <summary>Could not find file: {0}</summary>
        internal static string @FileResult_InvalidPath => GetResourceString("FileResult_InvalidPath");
        /// <summary>Could not find file: {0}</summary>
        internal static string FormatFileResult_InvalidPath(object p0)
           => string.Format(Culture, GetResourceString("FileResult_InvalidPath"), p0);

        /// <summary>The input was not valid.</summary>
        internal static string @SerializableError_DefaultError => GetResourceString("SerializableError_DefaultError");
        /// <summary>If an {0} provides a result value by setting the {1} property of {2} to a non-null value, then it cannot call the next filter by invoking {3}.</summary>
        internal static string @AsyncResourceFilter_InvalidShortCircuit => GetResourceString("AsyncResourceFilter_InvalidShortCircuit");
        /// <summary>If an {0} provides a result value by setting the {1} property of {2} to a non-null value, then it cannot call the next filter by invoking {3}.</summary>
        internal static string FormatAsyncResourceFilter_InvalidShortCircuit(object p0, object p1, object p2, object p3)
           => string.Format(Culture, GetResourceString("AsyncResourceFilter_InvalidShortCircuit"), p0, p1, p2, p3);

        /// <summary>If the '{0}' property is not set to true, '{1}' property must be specified.</summary>
        internal static string @ResponseCache_SpecifyDuration => GetResourceString("ResponseCache_SpecifyDuration");
        /// <summary>If the '{0}' property is not set to true, '{1}' property must be specified.</summary>
        internal static string FormatResponseCache_SpecifyDuration(object p0, object p1)
           => string.Format(Culture, GetResourceString("ResponseCache_SpecifyDuration"), p0, p1);

        /// <summary>The action '{0}' has ApiExplorer enabled, but is using conventional routing. Only actions which use attribute routing support ApiExplorer.</summary>
        internal static string @ApiExplorer_UnsupportedAction => GetResourceString("ApiExplorer_UnsupportedAction");
        /// <summary>The action '{0}' has ApiExplorer enabled, but is using conventional routing. Only actions which use attribute routing support ApiExplorer.</summary>
        internal static string FormatApiExplorer_UnsupportedAction(object p0)
           => string.Format(Culture, GetResourceString("ApiExplorer_UnsupportedAction"), p0);

        /// <summary>The media type "{0}" is not valid. MediaTypes containing wildcards (*) are not allowed in formatter mappings.</summary>
        internal static string @FormatterMappings_NotValidMediaType => GetResourceString("FormatterMappings_NotValidMediaType");
        /// <summary>The media type "{0}" is not valid. MediaTypes containing wildcards (*) are not allowed in formatter mappings.</summary>
        internal static string FormatFormatterMappings_NotValidMediaType(object p0)
           => string.Format(Culture, GetResourceString("FormatterMappings_NotValidMediaType"), p0);

        /// <summary>The format provided is invalid '{0}'. A format must be a non-empty file-extension, optionally prefixed with a '.' character.</summary>
        internal static string @Format_NotValid => GetResourceString("Format_NotValid");
        /// <summary>The format provided is invalid '{0}'. A format must be a non-empty file-extension, optionally prefixed with a '.' character.</summary>
        internal static string FormatFormat_NotValid(object p0)
           => string.Format(Culture, GetResourceString("Format_NotValid"), p0);

        /// <summary>The '{0}' cache profile is not defined.</summary>
        internal static string @CacheProfileNotFound => GetResourceString("CacheProfileNotFound");
        /// <summary>The '{0}' cache profile is not defined.</summary>
        internal static string FormatCacheProfileNotFound(object p0)
           => string.Format(Culture, GetResourceString("CacheProfileNotFound"), p0);

        /// <summary>The model's runtime type '{0}' is not assignable to the type '{1}'.</summary>
        internal static string @ModelType_WrongType => GetResourceString("ModelType_WrongType");
        /// <summary>The model's runtime type '{0}' is not assignable to the type '{1}'.</summary>
        internal static string FormatModelType_WrongType(object p0, object p1)
           => string.Format(Culture, GetResourceString("ModelType_WrongType"), p0, p1);

        /// <summary>The type '{0}' cannot be activated by '{1}' because it is either a value type, an interface, an abstract class or an open generic type.</summary>
        internal static string @ValueInterfaceAbstractOrOpenGenericTypesCannotBeActivated => GetResourceString("ValueInterfaceAbstractOrOpenGenericTypesCannotBeActivated");
        /// <summary>The type '{0}' cannot be activated by '{1}' because it is either a value type, an interface, an abstract class or an open generic type.</summary>
        internal static string FormatValueInterfaceAbstractOrOpenGenericTypesCannotBeActivated(object p0, object p1)
           => string.Format(Culture, GetResourceString("ValueInterfaceAbstractOrOpenGenericTypesCannotBeActivated"), p0, p1);

        /// <summary>The type '{0}' must implement '{1}' to be used as a model binder.</summary>
        internal static string @BinderType_MustBeIModelBinder => GetResourceString("BinderType_MustBeIModelBinder");
        /// <summary>The type '{0}' must implement '{1}' to be used as a model binder.</summary>
        internal static string FormatBinderType_MustBeIModelBinder(object p0, object p1)
           => string.Format(Culture, GetResourceString("BinderType_MustBeIModelBinder"), p0, p1);

        /// <summary>The provided binding source '{0}' is a composite. '{1}' requires that the source must represent a single type of input.</summary>
        internal static string @BindingSource_CannotBeComposite => GetResourceString("BindingSource_CannotBeComposite");
        /// <summary>The provided binding source '{0}' is a composite. '{1}' requires that the source must represent a single type of input.</summary>
        internal static string FormatBindingSource_CannotBeComposite(object p0, object p1)
           => string.Format(Culture, GetResourceString("BindingSource_CannotBeComposite"), p0, p1);

        /// <summary>The provided binding source '{0}' is a greedy data source. '{1}' does not support greedy data sources.</summary>
        internal static string @BindingSource_CannotBeGreedy => GetResourceString("BindingSource_CannotBeGreedy");
        /// <summary>The provided binding source '{0}' is a greedy data source. '{1}' does not support greedy data sources.</summary>
        internal static string FormatBindingSource_CannotBeGreedy(object p0, object p1)
           => string.Format(Culture, GetResourceString("BindingSource_CannotBeGreedy"), p0, p1);

        /// <summary>The property {0}.{1} could not be found.</summary>
        internal static string @Common_PropertyNotFound => GetResourceString("Common_PropertyNotFound");
        /// <summary>The property {0}.{1} could not be found.</summary>
        internal static string FormatCommon_PropertyNotFound(object p0, object p1)
           => string.Format(Culture, GetResourceString("Common_PropertyNotFound"), p0, p1);

        /// <summary>The key '{0}' is invalid JQuery syntax because it is missing a closing bracket.</summary>
        internal static string @JQueryFormValueProviderFactory_MissingClosingBracket => GetResourceString("JQueryFormValueProviderFactory_MissingClosingBracket");
        /// <summary>The key '{0}' is invalid JQuery syntax because it is missing a closing bracket.</summary>
        internal static string FormatJQueryFormValueProviderFactory_MissingClosingBracket(object p0)
           => string.Format(Culture, GetResourceString("JQueryFormValueProviderFactory_MissingClosingBracket"), p0);

        /// <summary>A value is required.</summary>
        internal static string @KeyValuePair_BothKeyAndValueMustBePresent => GetResourceString("KeyValuePair_BothKeyAndValueMustBePresent");
        /// <summary>The binding context has a null Model, but this binder requires a non-null model of type '{0}'.</summary>
        internal static string @ModelBinderUtil_ModelCannotBeNull => GetResourceString("ModelBinderUtil_ModelCannotBeNull");
        /// <summary>The binding context has a null Model, but this binder requires a non-null model of type '{0}'.</summary>
        internal static string FormatModelBinderUtil_ModelCannotBeNull(object p0)
           => string.Format(Culture, GetResourceString("ModelBinderUtil_ModelCannotBeNull"), p0);

        /// <summary>The binding context has a Model of type '{0}', but this binder can only operate on models of type '{1}'.</summary>
        internal static string @ModelBinderUtil_ModelInstanceIsWrong => GetResourceString("ModelBinderUtil_ModelInstanceIsWrong");
        /// <summary>The binding context has a Model of type '{0}', but this binder can only operate on models of type '{1}'.</summary>
        internal static string FormatModelBinderUtil_ModelInstanceIsWrong(object p0, object p1)
           => string.Format(Culture, GetResourceString("ModelBinderUtil_ModelInstanceIsWrong"), p0, p1);

        /// <summary>The binding context cannot have a null ModelMetadata.</summary>
        internal static string @ModelBinderUtil_ModelMetadataCannotBeNull => GetResourceString("ModelBinderUtil_ModelMetadataCannotBeNull");
        /// <summary>A value for the '{0}' parameter or property was not provided.</summary>
        internal static string @ModelBinding_MissingBindRequiredMember => GetResourceString("ModelBinding_MissingBindRequiredMember");
        /// <summary>A value for the '{0}' parameter or property was not provided.</summary>
        internal static string FormatModelBinding_MissingBindRequiredMember(object p0)
           => string.Format(Culture, GetResourceString("ModelBinding_MissingBindRequiredMember"), p0);

        /// <summary>A non-empty request body is required.</summary>
        internal static string @ModelBinding_MissingRequestBodyRequiredMember => GetResourceString("ModelBinding_MissingRequestBodyRequiredMember");
        /// <summary>The parameter conversion from type '{0}' to type '{1}' failed because no type converter can convert between these types.</summary>
        internal static string @ValueProviderResult_NoConverterExists => GetResourceString("ValueProviderResult_NoConverterExists");
        /// <summary>The parameter conversion from type '{0}' to type '{1}' failed because no type converter can convert between these types.</summary>
        internal static string FormatValueProviderResult_NoConverterExists(object p0, object p1)
           => string.Format(Culture, GetResourceString("ValueProviderResult_NoConverterExists"), p0, p1);

        /// <summary>Path '{0}' was not rooted.</summary>
        internal static string @FileResult_PathNotRooted => GetResourceString("FileResult_PathNotRooted");
        /// <summary>Path '{0}' was not rooted.</summary>
        internal static string FormatFileResult_PathNotRooted(object p0)
           => string.Format(Culture, GetResourceString("FileResult_PathNotRooted"), p0);

        /// <summary>The supplied URL is not local. A URL with an absolute path is considered local if it does not have a host/authority part. URLs using virtual paths ('~/') are also local.</summary>
        internal static string @UrlNotLocal => GetResourceString("UrlNotLocal");
        /// <summary>The argument '{0}' is invalid. Empty or null formats are not supported.</summary>
        internal static string @FormatFormatterMappings_GetMediaTypeMappingForFormat_InvalidFormat => GetResourceString("FormatFormatterMappings_GetMediaTypeMappingForFormat_InvalidFormat");
        /// <summary>The argument '{0}' is invalid. Empty or null formats are not supported.</summary>
        internal static string FormatFormatFormatterMappings_GetMediaTypeMappingForFormat_InvalidFormat(object p0)
           => string.Format(Culture, GetResourceString("FormatFormatterMappings_GetMediaTypeMappingForFormat_InvalidFormat"), p0);

        /// <summary>"Invalid values '{0}'."</summary>
        internal static string @AcceptHeaderParser_ParseAcceptHeader_InvalidValues => GetResourceString("AcceptHeaderParser_ParseAcceptHeader_InvalidValues");
        /// <summary>"Invalid values '{0}'."</summary>
        internal static string FormatAcceptHeaderParser_ParseAcceptHeader_InvalidValues(object p0)
           => string.Format(Culture, GetResourceString("AcceptHeaderParser_ParseAcceptHeader_InvalidValues"), p0);

        /// <summary>The value '{0}' is not valid for {1}.</summary>
        internal static string @ModelState_AttemptedValueIsInvalid => GetResourceString("ModelState_AttemptedValueIsInvalid");
        /// <summary>The value '{0}' is not valid for {1}.</summary>
        internal static string FormatModelState_AttemptedValueIsInvalid(object p0, object p1)
           => string.Format(Culture, GetResourceString("ModelState_AttemptedValueIsInvalid"), p0, p1);

        /// <summary>The value '{0}' is not valid.</summary>
        internal static string @ModelState_NonPropertyAttemptedValueIsInvalid => GetResourceString("ModelState_NonPropertyAttemptedValueIsInvalid");
        /// <summary>The value '{0}' is not valid.</summary>
        internal static string FormatModelState_NonPropertyAttemptedValueIsInvalid(object p0)
           => string.Format(Culture, GetResourceString("ModelState_NonPropertyAttemptedValueIsInvalid"), p0);

        /// <summary>The supplied value is invalid for {0}.</summary>
        internal static string @ModelState_UnknownValueIsInvalid => GetResourceString("ModelState_UnknownValueIsInvalid");
        /// <summary>The supplied value is invalid for {0}.</summary>
        internal static string FormatModelState_UnknownValueIsInvalid(object p0)
           => string.Format(Culture, GetResourceString("ModelState_UnknownValueIsInvalid"), p0);

        /// <summary>The supplied value is invalid.</summary>
        internal static string @ModelState_NonPropertyUnknownValueIsInvalid => GetResourceString("ModelState_NonPropertyUnknownValueIsInvalid");
        /// <summary>The value '{0}' is invalid.</summary>
        internal static string @HtmlGeneration_ValueIsInvalid => GetResourceString("HtmlGeneration_ValueIsInvalid");
        /// <summary>The value '{0}' is invalid.</summary>
        internal static string FormatHtmlGeneration_ValueIsInvalid(object p0)
           => string.Format(Culture, GetResourceString("HtmlGeneration_ValueIsInvalid"), p0);

        /// <summary>The field {0} must be a number.</summary>
        internal static string @HtmlGeneration_ValueMustBeNumber => GetResourceString("HtmlGeneration_ValueMustBeNumber");
        /// <summary>The field {0} must be a number.</summary>
        internal static string FormatHtmlGeneration_ValueMustBeNumber(object p0)
           => string.Format(Culture, GetResourceString("HtmlGeneration_ValueMustBeNumber"), p0);

        /// <summary>The field must be a number.</summary>
        internal static string @HtmlGeneration_NonPropertyValueMustBeNumber => GetResourceString("HtmlGeneration_NonPropertyValueMustBeNumber");
        /// <summary>The list of '{0}' must not be empty. Add at least one supported encoding.</summary>
        internal static string @TextInputFormatter_SupportedEncodingsMustNotBeEmpty => GetResourceString("TextInputFormatter_SupportedEncodingsMustNotBeEmpty");
        /// <summary>The list of '{0}' must not be empty. Add at least one supported encoding.</summary>
        internal static string FormatTextInputFormatter_SupportedEncodingsMustNotBeEmpty(object p0)
           => string.Format(Culture, GetResourceString("TextInputFormatter_SupportedEncodingsMustNotBeEmpty"), p0);

        /// <summary>The list of '{0}' must not be empty. Add at least one supported encoding.</summary>
        internal static string @TextOutputFormatter_SupportedEncodingsMustNotBeEmpty => GetResourceString("TextOutputFormatter_SupportedEncodingsMustNotBeEmpty");
        /// <summary>The list of '{0}' must not be empty. Add at least one supported encoding.</summary>
        internal static string FormatTextOutputFormatter_SupportedEncodingsMustNotBeEmpty(object p0)
           => string.Format(Culture, GetResourceString("TextOutputFormatter_SupportedEncodingsMustNotBeEmpty"), p0);

        /// <summary>'{0}' is not supported by '{1}'. Use '{2}' instead.</summary>
        internal static string @TextOutputFormatter_WriteResponseBodyAsyncNotSupported => GetResourceString("TextOutputFormatter_WriteResponseBodyAsyncNotSupported");
        /// <summary>'{0}' is not supported by '{1}'. Use '{2}' instead.</summary>
        internal static string FormatTextOutputFormatter_WriteResponseBodyAsyncNotSupported(object p0, object p1, object p2)
           => string.Format(Culture, GetResourceString("TextOutputFormatter_WriteResponseBodyAsyncNotSupported"), p0, p1, p2);

        /// <summary>No media types found in '{0}.{1}'. Add at least one media type to the list of supported media types.</summary>
        internal static string @Formatter_NoMediaTypes => GetResourceString("Formatter_NoMediaTypes");
        /// <summary>No media types found in '{0}.{1}'. Add at least one media type to the list of supported media types.</summary>
        internal static string FormatFormatter_NoMediaTypes(object p0, object p1)
           => string.Format(Culture, GetResourceString("Formatter_NoMediaTypes"), p0, p1);

        /// <summary>Could not create a model binder for model object of type '{0}'.</summary>
        internal static string @CouldNotCreateIModelBinder => GetResourceString("CouldNotCreateIModelBinder");
        /// <summary>Could not create a model binder for model object of type '{0}'.</summary>
        internal static string FormatCouldNotCreateIModelBinder(object p0)
           => string.Format(Culture, GetResourceString("CouldNotCreateIModelBinder"), p0);

        /// <summary>'{0}.{1}' must not be empty. At least one '{2}' is required to bind from the body.</summary>
        internal static string @InputFormattersAreRequired => GetResourceString("InputFormattersAreRequired");
        /// <summary>'{0}.{1}' must not be empty. At least one '{2}' is required to bind from the body.</summary>
        internal static string FormatInputFormattersAreRequired(object p0, object p1, object p2)
           => string.Format(Culture, GetResourceString("InputFormattersAreRequired"), p0, p1, p2);

        /// <summary>'{0}.{1}' must not be empty. At least one '{2}' is required to model bind.</summary>
        internal static string @ModelBinderProvidersAreRequired => GetResourceString("ModelBinderProvidersAreRequired");
        /// <summary>'{0}.{1}' must not be empty. At least one '{2}' is required to model bind.</summary>
        internal static string FormatModelBinderProvidersAreRequired(object p0, object p1, object p2)
           => string.Format(Culture, GetResourceString("ModelBinderProvidersAreRequired"), p0, p1, p2);

        /// <summary>'{0}.{1}' must not be empty. At least one '{2}' is required to format a response.</summary>
        internal static string @OutputFormattersAreRequired => GetResourceString("OutputFormattersAreRequired");
        /// <summary>'{0}.{1}' must not be empty. At least one '{2}' is required to format a response.</summary>
        internal static string FormatOutputFormattersAreRequired(object p0, object p1, object p2)
           => string.Format(Culture, GetResourceString("OutputFormattersAreRequired"), p0, p1, p2);

        /// <summary>Multiple overloads of method '{0}' are not supported.</summary>
        internal static string @MiddewareFilter_ConfigureMethodOverload => GetResourceString("MiddewareFilter_ConfigureMethodOverload");
        /// <summary>Multiple overloads of method '{0}' are not supported.</summary>
        internal static string FormatMiddewareFilter_ConfigureMethodOverload(object p0)
           => string.Format(Culture, GetResourceString("MiddewareFilter_ConfigureMethodOverload"), p0);

        /// <summary>A public method named '{0}' could not be found in the '{1}' type.</summary>
        internal static string @MiddewareFilter_NoConfigureMethod => GetResourceString("MiddewareFilter_NoConfigureMethod");
        /// <summary>A public method named '{0}' could not be found in the '{1}' type.</summary>
        internal static string FormatMiddewareFilter_NoConfigureMethod(object p0, object p1)
           => string.Format(Culture, GetResourceString("MiddewareFilter_NoConfigureMethod"), p0, p1);

        /// <summary>Could not find '{0}' in the feature list.</summary>
        internal static string @MiddlewareFilterBuilder_NoMiddlewareFeature => GetResourceString("MiddlewareFilterBuilder_NoMiddlewareFeature");
        /// <summary>Could not find '{0}' in the feature list.</summary>
        internal static string FormatMiddlewareFilterBuilder_NoMiddlewareFeature(object p0)
           => string.Format(Culture, GetResourceString("MiddlewareFilterBuilder_NoMiddlewareFeature"), p0);

        /// <summary>The '{0}' property cannot be null.</summary>
        internal static string @MiddlewareFilterBuilder_NullApplicationBuilder => GetResourceString("MiddlewareFilterBuilder_NullApplicationBuilder");
        /// <summary>The '{0}' property cannot be null.</summary>
        internal static string FormatMiddlewareFilterBuilder_NullApplicationBuilder(object p0)
           => string.Format(Culture, GetResourceString("MiddlewareFilterBuilder_NullApplicationBuilder"), p0);

        /// <summary>The '{0}' method in the type '{1}' must have a return type of '{2}'.</summary>
        internal static string @MiddlewareFilter_InvalidConfigureReturnType => GetResourceString("MiddlewareFilter_InvalidConfigureReturnType");
        /// <summary>The '{0}' method in the type '{1}' must have a return type of '{2}'.</summary>
        internal static string FormatMiddlewareFilter_InvalidConfigureReturnType(object p0, object p1, object p2)
           => string.Format(Culture, GetResourceString("MiddlewareFilter_InvalidConfigureReturnType"), p0, p1, p2);

        /// <summary>Could not resolve a service of type '{0}' for the parameter '{1}' of method '{2}' on type '{3}'.</summary>
        internal static string @MiddlewareFilter_ServiceResolutionFail => GetResourceString("MiddlewareFilter_ServiceResolutionFail");
        /// <summary>Could not resolve a service of type '{0}' for the parameter '{1}' of method '{2}' on type '{3}'.</summary>
        internal static string FormatMiddlewareFilter_ServiceResolutionFail(object p0, object p1, object p2, object p3)
           => string.Format(Culture, GetResourceString("MiddlewareFilter_ServiceResolutionFail"), p0, p1, p2, p3);

        /// <summary>An {0} cannot be created without a valid instance of {1}.</summary>
        internal static string @AuthorizeFilter_AuthorizationPolicyCannotBeCreated => GetResourceString("AuthorizeFilter_AuthorizationPolicyCannotBeCreated");
        /// <summary>An {0} cannot be created without a valid instance of {1}.</summary>
        internal static string FormatAuthorizeFilter_AuthorizationPolicyCannotBeCreated(object p0, object p1)
           => string.Format(Culture, GetResourceString("AuthorizeFilter_AuthorizationPolicyCannotBeCreated"), p0, p1);

        /// <summary>The '{0}' cannot bind to a model of type '{1}'. Change the model type to '{2}' instead.</summary>
        internal static string @FormCollectionModelBinder_CannotBindToFormCollection => GetResourceString("FormCollectionModelBinder_CannotBindToFormCollection");
        /// <summary>The '{0}' cannot bind to a model of type '{1}'. Change the model type to '{2}' instead.</summary>
        internal static string FormatFormCollectionModelBinder_CannotBindToFormCollection(object p0, object p1, object p2)
           => string.Format(Culture, GetResourceString("FormCollectionModelBinder_CannotBindToFormCollection"), p0, p1, p2);

        /// <summary>'{0}' requires the response cache middleware.</summary>
        internal static string @VaryByQueryKeys_Requires_ResponseCachingMiddleware => GetResourceString("VaryByQueryKeys_Requires_ResponseCachingMiddleware");
        /// <summary>'{0}' requires the response cache middleware.</summary>
        internal static string FormatVaryByQueryKeys_Requires_ResponseCachingMiddleware(object p0)
           => string.Format(Culture, GetResourceString("VaryByQueryKeys_Requires_ResponseCachingMiddleware"), p0);

        /// <summary>A duplicate entry for library reference {0} was found. Please check that all package references in all projects use the same casing for the same package references.</summary>
        internal static string @CandidateResolver_DifferentCasedReference => GetResourceString("CandidateResolver_DifferentCasedReference");
        /// <summary>A duplicate entry for library reference {0} was found. Please check that all package references in all projects use the same casing for the same package references.</summary>
        internal static string FormatCandidateResolver_DifferentCasedReference(object p0)
           => string.Format(Culture, GetResourceString("CandidateResolver_DifferentCasedReference"), p0);

        /// <summary>Unable to create an instance of type '{0}'. The type specified in {1} must not be abstract and must have a parameterless constructor.</summary>
        internal static string @MiddlewareFilterConfigurationProvider_CreateConfigureDelegate_CannotCreateType => GetResourceString("MiddlewareFilterConfigurationProvider_CreateConfigureDelegate_CannotCreateType");
        /// <summary>Unable to create an instance of type '{0}'. The type specified in {1} must not be abstract and must have a parameterless constructor.</summary>
        internal static string FormatMiddlewareFilterConfigurationProvider_CreateConfigureDelegate_CannotCreateType(object p0, object p1)
           => string.Format(Culture, GetResourceString("MiddlewareFilterConfigurationProvider_CreateConfigureDelegate_CannotCreateType"), p0, p1);

        /// <summary>'{0}' and '{1}' are out of bounds for the string.</summary>
        internal static string @Argument_InvalidOffsetLength => GetResourceString("Argument_InvalidOffsetLength");
        /// <summary>'{0}' and '{1}' are out of bounds for the string.</summary>
        internal static string FormatArgument_InvalidOffsetLength(object p0, object p1)
           => string.Format(Culture, GetResourceString("Argument_InvalidOffsetLength"), p0, p1);

        /// <summary>Could not create an instance of type '{0}'. Model bound complex types must not be abstract or value types and must have a parameterless constructor.</summary>
        internal static string @ComplexTypeModelBinder_NoParameterlessConstructor_ForType => GetResourceString("ComplexTypeModelBinder_NoParameterlessConstructor_ForType");
        /// <summary>Could not create an instance of type '{0}'. Model bound complex types must not be abstract or value types and must have a parameterless constructor.</summary>
        internal static string FormatComplexTypeModelBinder_NoParameterlessConstructor_ForType(object p0)
           => string.Format(Culture, GetResourceString("ComplexTypeModelBinder_NoParameterlessConstructor_ForType"), p0);

        /// <summary>Could not create an instance of type '{0}'. Model bound complex types must not be abstract or value types and must have a parameterless constructor. Alternatively, set the '{1}' property to a non-null value in the '{2}' constructor.</summary>
        internal static string @ComplexTypeModelBinder_NoParameterlessConstructor_ForProperty => GetResourceString("ComplexTypeModelBinder_NoParameterlessConstructor_ForProperty");
        /// <summary>Could not create an instance of type '{0}'. Model bound complex types must not be abstract or value types and must have a parameterless constructor. Alternatively, set the '{1}' property to a non-null value in the '{2}' constructor.</summary>
        internal static string FormatComplexTypeModelBinder_NoParameterlessConstructor_ForProperty(object p0, object p1, object p2)
           => string.Format(Culture, GetResourceString("ComplexTypeModelBinder_NoParameterlessConstructor_ForProperty"), p0, p1, p2);

        /// <summary>No page named '{0}' matches the supplied values.</summary>
        internal static string @NoRoutesMatchedForPage => GetResourceString("NoRoutesMatchedForPage");
        /// <summary>No page named '{0}' matches the supplied values.</summary>
        internal static string FormatNoRoutesMatchedForPage(object p0)
           => string.Format(Culture, GetResourceString("NoRoutesMatchedForPage"), p0);

        /// <summary>The relative page path '{0}' can only be used while executing a Razor Page. Specify a root relative path with a leading '/' to generate a URL outside of a Razor Page. If you are using {1} then you must provide the current {2} to use relative pages.</summary>
        internal static string @UrlHelper_RelativePagePathIsNotSupported => GetResourceString("UrlHelper_RelativePagePathIsNotSupported");
        /// <summary>The relative page path '{0}' can only be used while executing a Razor Page. Specify a root relative path with a leading '/' to generate a URL outside of a Razor Page. If you are using {1} then you must provide the current {2} to use relative pages.</summary>
        internal static string FormatUrlHelper_RelativePagePathIsNotSupported(object p0, object p1, object p2)
           => string.Format(Culture, GetResourceString("UrlHelper_RelativePagePathIsNotSupported"), p0, p1, p2);

        /// <summary>One or more validation errors occurred.</summary>
        internal static string @ValidationProblemDescription_Title => GetResourceString("ValidationProblemDescription_Title");
        /// <summary>Action '{0}' does not have an attribute route. Action methods on controllers annotated with {1} must be attribute routed.</summary>
        internal static string @ApiController_AttributeRouteRequired => GetResourceString("ApiController_AttributeRouteRequired");
        /// <summary>Action '{0}' does not have an attribute route. Action methods on controllers annotated with {1} must be attribute routed.</summary>
        internal static string FormatApiController_AttributeRouteRequired(object p0, object p1)
           => string.Format(Culture, GetResourceString("ApiController_AttributeRouteRequired"), p0, p1);

        /// <summary>No file provider has been configured to process the supplied file.</summary>
        internal static string @VirtualFileResultExecutor_NoFileProviderConfigured => GetResourceString("VirtualFileResultExecutor_NoFileProviderConfigured");
        /// <summary>Type {0} specified by {1} is invalid. Type specified by {1} must derive from {2}.</summary>
        internal static string @ApplicationPartFactory_InvalidFactoryType => GetResourceString("ApplicationPartFactory_InvalidFactoryType");
        /// <summary>Type {0} specified by {1} is invalid. Type specified by {1} must derive from {2}.</summary>
        internal static string FormatApplicationPartFactory_InvalidFactoryType(object p0, object p1, object p2)
           => string.Format(Culture, GetResourceString("ApplicationPartFactory_InvalidFactoryType"), p0, p1, p2);

        /// <summary>{0} specified on {1} cannot be self referential.</summary>
        internal static string @RelatedAssemblyAttribute_AssemblyCannotReferenceSelf => GetResourceString("RelatedAssemblyAttribute_AssemblyCannotReferenceSelf");
        /// <summary>{0} specified on {1} cannot be self referential.</summary>
        internal static string FormatRelatedAssemblyAttribute_AssemblyCannotReferenceSelf(object p0, object p1)
           => string.Format(Culture, GetResourceString("RelatedAssemblyAttribute_AssemblyCannotReferenceSelf"), p0, p1);

        /// <summary>Related assembly '{0}' specified by assembly '{1}' could not be found in the directory {2}. Related assemblies must be co-located with the specifying assemblies.</summary>
        internal static string @RelatedAssemblyAttribute_CouldNotBeFound => GetResourceString("RelatedAssemblyAttribute_CouldNotBeFound");
        /// <summary>Related assembly '{0}' specified by assembly '{1}' could not be found in the directory {2}. Related assemblies must be co-located with the specifying assemblies.</summary>
        internal static string FormatRelatedAssemblyAttribute_CouldNotBeFound(object p0, object p1, object p2)
           => string.Format(Culture, GetResourceString("RelatedAssemblyAttribute_CouldNotBeFound"), p0, p1, p2);

        /// <summary>Each related assembly must be declared by exactly one assembly. The assembly '{0}' was declared as related assembly by the following:</summary>
        internal static string @ApplicationAssembliesProvider_DuplicateRelatedAssembly => GetResourceString("ApplicationAssembliesProvider_DuplicateRelatedAssembly");
        /// <summary>Each related assembly must be declared by exactly one assembly. The assembly '{0}' was declared as related assembly by the following:</summary>
        internal static string FormatApplicationAssembliesProvider_DuplicateRelatedAssembly(object p0)
           => string.Format(Culture, GetResourceString("ApplicationAssembliesProvider_DuplicateRelatedAssembly"), p0);

        /// <summary>Assembly '{0}' declared as a related assembly by assembly '{1}' cannot define additional related assemblies.</summary>
        internal static string @ApplicationAssembliesProvider_RelatedAssemblyCannotDefineAdditional => GetResourceString("ApplicationAssembliesProvider_RelatedAssemblyCannotDefineAdditional");
        /// <summary>Assembly '{0}' declared as a related assembly by assembly '{1}' cannot define additional related assemblies.</summary>
        internal static string FormatApplicationAssembliesProvider_RelatedAssemblyCannotDefineAdditional(object p0, object p1)
           => string.Format(Culture, GetResourceString("ApplicationAssembliesProvider_RelatedAssemblyCannotDefineAdditional"), p0, p1);

        /// <summary>Could not create an instance of type '{0}'. Model bound complex types must not be abstract or value types and must have a parameterless constructor. Alternatively, give the '{1}' parameter a non-null default value.</summary>
        internal static string @ComplexTypeModelBinder_NoParameterlessConstructor_ForParameter => GetResourceString("ComplexTypeModelBinder_NoParameterlessConstructor_ForParameter");
        /// <summary>Could not create an instance of type '{0}'. Model bound complex types must not be abstract or value types and must have a parameterless constructor. Alternatively, give the '{1}' parameter a non-null default value.</summary>
        internal static string FormatComplexTypeModelBinder_NoParameterlessConstructor_ForParameter(object p0, object p1)
           => string.Format(Culture, GetResourceString("ComplexTypeModelBinder_NoParameterlessConstructor_ForParameter"), p0, p1);

        /// <summary>Action '{0}' has more than one parameter that was specified or inferred as bound from request body. Only one parameter per action may be bound from body. Inspect the following parameters, and use '{1}' to specify bound from query, '{2}' to specify bound fr ...</summary>
        internal static string @ApiController_MultipleBodyParametersFound => GetResourceString("ApiController_MultipleBodyParametersFound");
        /// <summary>Action '{0}' has more than one parameter that was specified or inferred as bound from request body. Only one parameter per action may be bound from body. Inspect the following parameters, and use '{1}' to specify bound from query, '{2}' to specify bound fr ...</summary>
        internal static string FormatApiController_MultipleBodyParametersFound(object p0, object p1, object p2, object p3)
           => string.Format(Culture, GetResourceString("ApiController_MultipleBodyParametersFound"), p0, p1, p2, p3);

        /// <summary>API convention type '{0}' must be a static type.</summary>
        internal static string @ApiConventionMustBeStatic => GetResourceString("ApiConventionMustBeStatic");
        /// <summary>API convention type '{0}' must be a static type.</summary>
        internal static string FormatApiConventionMustBeStatic(object p0)
           => string.Format(Culture, GetResourceString("ApiConventionMustBeStatic"), p0);

        /// <summary>Invalid type parameter '{0}' specified for '{1}'.</summary>
        internal static string @InvalidTypeTForActionResultOfT => GetResourceString("InvalidTypeTForActionResultOfT");
        /// <summary>Invalid type parameter '{0}' specified for '{1}'.</summary>
        internal static string FormatInvalidTypeTForActionResultOfT(object p0, object p1)
           => string.Format(Culture, GetResourceString("InvalidTypeTForActionResultOfT"), p0, p1);

        /// <summary>Method {0} is decorated with the following attributes that are not allowed on an API convention method:{1}The following attributes are allowed on API convention methods: {2}.</summary>
        internal static string @ApiConvention_UnsupportedAttributesOnConvention => GetResourceString("ApiConvention_UnsupportedAttributesOnConvention");
        /// <summary>Method {0} is decorated with the following attributes that are not allowed on an API convention method:{1}The following attributes are allowed on API convention methods: {2}.</summary>
        internal static string FormatApiConvention_UnsupportedAttributesOnConvention(object p0, object p1, object p2)
           => string.Format(Culture, GetResourceString("ApiConvention_UnsupportedAttributesOnConvention"), p0, p1, p2);

        /// <summary>Method name '{0}' is ambiguous for convention type '{1}'. More than one method found with the name '{0}'.</summary>
        internal static string @ApiConventionMethod_AmbiguousMethodName => GetResourceString("ApiConventionMethod_AmbiguousMethodName");
        /// <summary>Method name '{0}' is ambiguous for convention type '{1}'. More than one method found with the name '{0}'.</summary>
        internal static string FormatApiConventionMethod_AmbiguousMethodName(object p0, object p1)
           => string.Format(Culture, GetResourceString("ApiConventionMethod_AmbiguousMethodName"), p0, p1);

        /// <summary>A method named '{0}' was not found on convention type '{1}'.</summary>
        internal static string @ApiConventionMethod_NoMethodFound => GetResourceString("ApiConventionMethod_NoMethodFound");
        /// <summary>A method named '{0}' was not found on convention type '{1}'.</summary>
        internal static string FormatApiConventionMethod_NoMethodFound(object p0, object p1)
           => string.Format(Culture, GetResourceString("ApiConventionMethod_NoMethodFound"), p0, p1);

        /// <summary>{0} exceeded the maximum configured validation depth '{1}' when validating type '{2}'.</summary>
        internal static string @ValidationVisitor_ExceededMaxDepth => GetResourceString("ValidationVisitor_ExceededMaxDepth");
        /// <summary>{0} exceeded the maximum configured validation depth '{1}' when validating type '{2}'.</summary>
        internal static string FormatValidationVisitor_ExceededMaxDepth(object p0, object p1, object p2)
           => string.Format(Culture, GetResourceString("ValidationVisitor_ExceededMaxDepth"), p0, p1, p2);

        /// <summary>This may indicate a very deep or infinitely recursive object graph. Consider modifying '{0}.{1}' or suppressing validation on the model type.</summary>
        internal static string @ValidationVisitor_ExceededMaxDepthFix => GetResourceString("ValidationVisitor_ExceededMaxDepthFix");
        /// <summary>This may indicate a very deep or infinitely recursive object graph. Consider modifying '{0}.{1}' or suppressing validation on the model type.</summary>
        internal static string FormatValidationVisitor_ExceededMaxDepthFix(object p0, object p1)
           => string.Format(Culture, GetResourceString("ValidationVisitor_ExceededMaxDepthFix"), p0, p1);

        /// <summary>{0} exceeded the maximum configured validation depth '{1}' when validating property '{2}' on type '{3}'.</summary>
        internal static string @ValidationVisitor_ExceededMaxPropertyDepth => GetResourceString("ValidationVisitor_ExceededMaxPropertyDepth");
        /// <summary>{0} exceeded the maximum configured validation depth '{1}' when validating property '{2}' on type '{3}'.</summary>
        internal static string FormatValidationVisitor_ExceededMaxPropertyDepth(object p0, object p1, object p2, object p3)
           => string.Format(Culture, GetResourceString("ValidationVisitor_ExceededMaxPropertyDepth"), p0, p1, p2, p3);

        /// <summary>Bad Request</summary>
        internal static string @ApiConventions_Title_400 => GetResourceString("ApiConventions_Title_400");
        /// <summary>Unauthorized</summary>
        internal static string @ApiConventions_Title_401 => GetResourceString("ApiConventions_Title_401");
        /// <summary>Forbidden</summary>
        internal static string @ApiConventions_Title_403 => GetResourceString("ApiConventions_Title_403");
        /// <summary>Not Found</summary>
        internal static string @ApiConventions_Title_404 => GetResourceString("ApiConventions_Title_404");
        /// <summary>Not Acceptable</summary>
        internal static string @ApiConventions_Title_406 => GetResourceString("ApiConventions_Title_406");
        /// <summary>Conflict</summary>
        internal static string @ApiConventions_Title_409 => GetResourceString("ApiConventions_Title_409");
        /// <summary>Unsupported Media Type</summary>
        internal static string @ApiConventions_Title_415 => GetResourceString("ApiConventions_Title_415");
        /// <summary>Unprocessable Entity</summary>
        internal static string @ApiConventions_Title_422 => GetResourceString("ApiConventions_Title_422");
        /// <summary>'{0}' requires a reference to '{1}'. Configure your application by adding a reference to the '{1}' package and calling '{2}.{3}' inside the call to '{4}' in the application startup code.</summary>
        internal static string @ReferenceToNewtonsoftJsonRequired => GetResourceString("ReferenceToNewtonsoftJsonRequired");
        /// <summary>'{0}' requires a reference to '{1}'. Configure your application by adding a reference to the '{1}' package and calling '{2}.{3}' inside the call to '{4}' in the application startup code.</summary>
        internal static string FormatReferenceToNewtonsoftJsonRequired(object p0, object p1, object p2, object p3, object p4)
           => string.Format(Culture, GetResourceString("ReferenceToNewtonsoftJsonRequired"), p0, p1, p2, p3, p4);

        /// <summary>Collection bound to '{0}' exceeded {1}.{2} ({3}). This limit is a safeguard against incorrect model binders and models. Address issues in '{4}'. For example, this type may have a property with a model binder that always succeeds. See the {1}.{2} documentat ...</summary>
        internal static string @ModelBinding_ExceededMaxModelBindingCollectionSize => GetResourceString("ModelBinding_ExceededMaxModelBindingCollectionSize");
        /// <summary>Collection bound to '{0}' exceeded {1}.{2} ({3}). This limit is a safeguard against incorrect model binders and models. Address issues in '{4}'. For example, this type may have a property with a model binder that always succeeds. See the {1}.{2} documentat ...</summary>
        internal static string FormatModelBinding_ExceededMaxModelBindingCollectionSize(object p0, object p1, object p2, object p3, object p4)
           => string.Format(Culture, GetResourceString("ModelBinding_ExceededMaxModelBindingCollectionSize"), p0, p1, p2, p3, p4);

        /// <summary>Model binding system exceeded {0}.{1} ({2}). Reduce the potential nesting of '{3}'. For example, this type may have a property with a model binder that always succeeds. See the {0}.{1} documentation for more information.</summary>
        internal static string @ModelBinding_ExceededMaxModelBindingRecursionDepth => GetResourceString("ModelBinding_ExceededMaxModelBindingRecursionDepth");
        /// <summary>Model binding system exceeded {0}.{1} ({2}). Reduce the potential nesting of '{3}'. For example, this type may have a property with a model binder that always succeeds. See the {0}.{1} documentation for more information.</summary>
        internal static string FormatModelBinding_ExceededMaxModelBindingRecursionDepth(object p0, object p1, object p2, object p3)
           => string.Format(Culture, GetResourceString("ModelBinding_ExceededMaxModelBindingRecursionDepth"), p0, p1, p2, p3);

        /// <summary>Property '{0}.{1}' must be an instance of type '{2}'.</summary>
        internal static string @Property_MustBeInstanceOfType => GetResourceString("Property_MustBeInstanceOfType");
        /// <summary>Property '{0}.{1}' must be an instance of type '{2}'.</summary>
        internal static string FormatProperty_MustBeInstanceOfType(object p0, object p1, object p2)
           => string.Format(Culture, GetResourceString("Property_MustBeInstanceOfType"), p0, p1, p2);

        /// <summary>'{0}' reached the configured maximum size of the buffer when enumerating a value of type '{1}'. This limit is in place to prevent infinite streams of 'IAsyncEnumerable&lt;&gt;' from continuing indefinitely. If this is not a programming mistake, consider ways to  ...</summary>
        internal static string @ObjectResultExecutor_MaxEnumerationExceeded => GetResourceString("ObjectResultExecutor_MaxEnumerationExceeded");
        /// <summary>'{0}' reached the configured maximum size of the buffer when enumerating a value of type '{1}'. This limit is in place to prevent infinite streams of 'IAsyncEnumerable&lt;&gt;' from continuing indefinitely. If this is not a programming mistake, consider ways to  ...</summary>
        internal static string FormatObjectResultExecutor_MaxEnumerationExceeded(object p0, object p1)
           => string.Format(Culture, GetResourceString("ObjectResultExecutor_MaxEnumerationExceeded"), p0, p1);

        /// <summary>Unexcepted end when reading JSON.</summary>
        internal static string @UnexpectedJsonEnd => GetResourceString("UnexpectedJsonEnd");
        /// <summary>An error occurred while processing your request.</summary>
        internal static string @ApiConventions_Title_500 => GetResourceString("ApiConventions_Title_500");
        /// <summary>Failed to read the request form. {0}</summary>
        internal static string @FailedToReadRequestForm => GetResourceString("FailedToReadRequestForm");
        /// <summary>Failed to read the request form. {0}</summary>
        internal static string FormatFailedToReadRequestForm(object p0)
           => string.Format(Culture, GetResourceString("FailedToReadRequestForm"), p0);

        /// <summary>A container cannot be specified when the ModelMetada is of kind '{0}'.</summary>
        internal static string @ValidationVisitor_ContainerCannotBeSpecified => GetResourceString("ValidationVisitor_ContainerCannotBeSpecified");
        /// <summary>A container cannot be specified when the ModelMetada is of kind '{0}'.</summary>
        internal static string FormatValidationVisitor_ContainerCannotBeSpecified(object p0)
           => string.Format(Culture, GetResourceString("ValidationVisitor_ContainerCannotBeSpecified"), p0);

        /// <summary>Transformer '{0}' was retrieved from dependency injection with a state value. State can only be specified when the dynamic route is mapped using MapDynamicControllerRoute's state argument together with transient lifetime transformer. Ensure that '{0}' does ...</summary>
        internal static string @StateShouldBeNullForRouteValueTransformers => GetResourceString("StateShouldBeNullForRouteValueTransformers");
        /// <summary>Transformer '{0}' was retrieved from dependency injection with a state value. State can only be specified when the dynamic route is mapped using MapDynamicControllerRoute's state argument together with transient lifetime transformer. Ensure that '{0}' does ...</summary>
        internal static string FormatStateShouldBeNullForRouteValueTransformers(object p0)
           => string.Format(Culture, GetResourceString("StateShouldBeNullForRouteValueTransformers"), p0);

        /// <summary>Could not create an instance of type '{0}'. Model bound complex types must not be abstract or value types and must have a parameterless constructor. Record types must have a single primary constructor. Alternatively, give the '{1}' parameter a non-null def ...</summary>
        internal static string @ComplexObjectModelBinder_NoSuitableConstructor_ForParameter => GetResourceString("ComplexObjectModelBinder_NoSuitableConstructor_ForParameter");
        /// <summary>Could not create an instance of type '{0}'. Model bound complex types must not be abstract or value types and must have a parameterless constructor. Record types must have a single primary constructor. Alternatively, give the '{1}' parameter a non-null def ...</summary>
        internal static string FormatComplexObjectModelBinder_NoSuitableConstructor_ForParameter(object p0, object p1)
           => string.Format(Culture, GetResourceString("ComplexObjectModelBinder_NoSuitableConstructor_ForParameter"), p0, p1);

        /// <summary>Could not create an instance of type '{0}'. Model bound complex types must not be abstract or value types and must have a parameterless constructor. Record types must have a single primary constructor. Alternatively, set the '{1}' property to a non-null va ...</summary>
        internal static string @ComplexObjectModelBinder_NoSuitableConstructor_ForProperty => GetResourceString("ComplexObjectModelBinder_NoSuitableConstructor_ForProperty");
        /// <summary>Could not create an instance of type '{0}'. Model bound complex types must not be abstract or value types and must have a parameterless constructor. Record types must have a single primary constructor. Alternatively, set the '{1}' property to a non-null va ...</summary>
        internal static string FormatComplexObjectModelBinder_NoSuitableConstructor_ForProperty(object p0, object p1, object p2)
           => string.Format(Culture, GetResourceString("ComplexObjectModelBinder_NoSuitableConstructor_ForProperty"), p0, p1, p2);

        /// <summary>Could not create an instance of type '{0}'. Model bound complex types must not be abstract or value types and must have a parameterless constructor. Record types must have a single primary constructor.</summary>
        internal static string @ComplexObjectModelBinder_NoSuitableConstructor_ForType => GetResourceString("ComplexObjectModelBinder_NoSuitableConstructor_ForType");
        /// <summary>Could not create an instance of type '{0}'. Model bound complex types must not be abstract or value types and must have a parameterless constructor. Record types must have a single primary constructor.</summary>
        internal static string FormatComplexObjectModelBinder_NoSuitableConstructor_ForType(object p0)
           => string.Format(Culture, GetResourceString("ComplexObjectModelBinder_NoSuitableConstructor_ForType"), p0);

        /// <summary>No property found that maps to constructor parameter '{0}' for type '{1}'. Validation requires that each bound parameter of a record type's primary constructor must have a property to read the value.</summary>
        internal static string @ValidationStrategy_MappedPropertyNotFound => GetResourceString("ValidationStrategy_MappedPropertyNotFound");
        /// <summary>No property found that maps to constructor parameter '{0}' for type '{1}'. Validation requires that each bound parameter of a record type's primary constructor must have a property to read the value.</summary>
        internal static string FormatValidationStrategy_MappedPropertyNotFound(object p0, object p1)
           => string.Format(Culture, GetResourceString("ValidationStrategy_MappedPropertyNotFound"), p0, p1);

        /// <summary>{0} cannot update a record type model. If a '{1}' must be updated, include it in an object type.</summary>
        internal static string @TryUpdateModel_RecordTypeNotSupported => GetResourceString("TryUpdateModel_RecordTypeNotSupported");
        /// <summary>{0} cannot update a record type model. If a '{1}' must be updated, include it in an object type.</summary>
        internal static string FormatTryUpdateModel_RecordTypeNotSupported(object p0, object p1)
           => string.Format(Culture, GetResourceString("TryUpdateModel_RecordTypeNotSupported"), p0, p1);


    }
}