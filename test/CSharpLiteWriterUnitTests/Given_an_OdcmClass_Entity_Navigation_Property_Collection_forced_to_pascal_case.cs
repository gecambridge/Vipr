using System;
using System.Collections.Generic;
using System.Linq;
using Vipr.Writer.CSharp.Lite.Settings;
using Microsoft.Its.Recipes;
using Microsoft.MockService;
using Microsoft.MockService.Extensions.ODataV4;
using Microsoft.OData.ProxyExtensions.Lite;
using Moq;
using Vipr.Core;
using Xunit;
using System.Threading.Tasks;

namespace CSharpLiteWriterUnitTests
{
    public class Given_an_OdcmClass_Entity_Navigation_Property_Collection_forced_to_pascal_case : EntityTestBase
    {
        private MockService _mockedService;
        private string _camelCasedName;
        private readonly string _pascalCasedName;

        public Given_an_OdcmClass_Entity_Navigation_Property_Collection_forced_to_pascal_case()
        {
            SetConfiguration(new CSharpWriterSettings
            {
                ForcePropertyPascalCasing = true
            });

            Init(m =>
            {
                var property = Class.NavigationProperties().Where(p => p.IsCollection).RandomElement();

                _camelCasedName = Any.Char('a', 'z') + property.Name;

                property.Rename(_camelCasedName);

                Class.Properties.Remove(property);
            });

            _pascalCasedName = _camelCasedName.ToPascalCase();
        }

        [Fact(Skip = "Issue #24 https://github.com/Microsoft/vipr/issues/24")]
        public void When_retrieved_through_Concrete_ConcreteInterface_Property_then_request_is_sent_with_original_name()
        {
            var entityKeyValues = Class.GetSampleKeyArguments().ToArray();

            using (_mockedService = new MockService()
                .SetupPostEntity(TargetEntity, entityKeyValues)
                .SetupGetEntity(TargetEntity))
            {
                var instance = _mockedService
                    .GetDefaultContext(Model)
                    .CreateConcrete(ConcreteType);

                instance.SetPropertyValues(Class.GetSampleKeyArguments());

                var propertyValue = instance.GetPropertyValue<IPagedCollection>(ConcreteInterface,
                    _pascalCasedName);

                propertyValue.GetNextPageAsync().Wait();
            }
        }

        [Fact]
        public void When_retrieved_through_Fetcher_then_request_is_sent_to_server_with_original_name()
        {
            var entityPath = "/" + Any.UriPath(1);

            using (_mockedService = new MockService()
                    .OnGetEntityPropertyRequest(entityPath, _camelCasedName)
                    .RespondWithGetEntity(Class.GetDefaultEntitySetName(), Class.GetSampleJObject(Class.GetSampleKeyArguments())))
            {
                var fetcher = _mockedService
                    .GetDefaultContext(Model)
                    .CreateFetcher(FetcherType, entityPath);

                var propertyFetcher = fetcher.GetPropertyValue<ReadOnlyQueryableSetBase>(_pascalCasedName);

                propertyFetcher.ExecuteAsync().Wait();
            }
        }

        [Fact]
        public void When_updated_through_Concrete_accessor_then_request_is_sent_to_server_with_original_name()
        {
            var entitySetName = Class.Name + "s";
            var entitySetPath = "/" + entitySetName;
            var entityKeyValues = Class.GetSampleKeyArguments().ToArray();
            var entityPath = string.Format("{0}({1})", entitySetPath, ODataKeyPredicate.AsString(entityKeyValues));
            var expectedPath = entityPath + "/" + _camelCasedName;

            using (_mockedService = new MockService()
                .OnPostEntityRequest(entitySetPath)
                    .RespondWithCreateEntity(Class.Name + "s", Class.GetSampleJObject(entityKeyValues))
                .OnPostEntityRequest(expectedPath)
                    .RespondWithODataOk())
            {
                var context = _mockedService
                    .GetDefaultContext(Model);

                var instance = context
                    .CreateConcrete(ConcreteType);

                var fetcher = context.CreateFetcher(FetcherType, Class.GetDefaultEntityPath(entityKeyValues));

                var collectionFetcher = fetcher.GetPropertyValue(_pascalCasedName);

                var addMethod = "Add" + ConcreteType.Name + "Async";

                var relatedInstance = Activator.CreateInstance(ConcreteType);

                collectionFetcher.InvokeMethod<Task>(addMethod, new object[] { relatedInstance, System.Type.Missing }).Wait();
            }
        }
    }
}