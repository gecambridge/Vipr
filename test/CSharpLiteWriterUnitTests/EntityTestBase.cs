﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Vipr.Writer.CSharp.Lite.Settings;
using Microsoft.Its.Recipes;
using System;
using System.Reflection;
using Moq;
using Vipr.Core;
using Vipr.Core.CodeModel;
using Vipr.Writer.CSharp.Lite;
using Type = System.Type;

namespace CSharpLiteWriterUnitTests
{
    public class EntityTestBase : CodeGenTestBase
    {
        protected OdcmModel Model;
        protected OdcmNamespace Namespace;
        protected OdcmEntityClass Class;
        protected Assembly Proxy;
        protected EntityArtifacts TargetEntity;
        protected Type ConcreteType;
        protected Type ConcreteInterface;
        protected Type FetcherType;
        protected Type FetcherInterface;
        protected Type CollectionType;
        protected Type CollectionInterface;
        protected OdcmClass OdcmContainer;
        protected Type EntityContainerType;
        protected Type EntityContainerInterface;
        protected IConfigurationProvider ConfigurationProvider;

        public void Init(Action<OdcmModel> config = null, bool generateMocks = false)
        {
            Model = new OdcmModel(Any.ServiceMetadata());

            Namespace = Any.EmptyOdcmNamespace();

            Model.Namespaces.Add(Namespace);

            Class = Any.OdcmEntityClass(Namespace);

            Model.AddType(Class);

            OdcmContainer = Any.ServiceOdcmClass(Namespace);

            Model.AddType(OdcmContainer);

            if (config != null) config(Model);

            Model.ServiceMetadata["$metadata"] = Model.ToEdmx(true);

            Proxy = GetProxy(Model, ConfigurationProvider, generateMocks ? new[] { "DynamicProxyGenAssembly2" } : null);

            ConcreteType = Proxy.GetClass(Class.Namespace, Class.Name);

            ConcreteInterface = Proxy.GetInterface(Class.Namespace, "I" + Class.Name);

            FetcherType = Proxy.GetClass(Class.Namespace, Class.Name + "Fetcher");

            var identifier = NamesService.GetFetcherInterfaceName(Class);
            FetcherInterface = Proxy.GetInterface(Class.Namespace, identifier.Name);

            CollectionType = Proxy.GetClass(Class.Namespace, Class.Name + "Collection");

            identifier = NamesService.GetCollectionInterfaceName(Class);
            CollectionInterface = Proxy.GetInterface(Class.Namespace, identifier.Name);

            EntityContainerType = Proxy.GetClass(Model.EntityContainer.Namespace, Model.EntityContainer.Name);

            EntityContainerInterface = Proxy.GetInterface(Model.EntityContainer.Namespace, "I" + Model.EntityContainer.Name);

            TargetEntity = new EntityArtifacts()
            {
                Class = Class,
                ConcreteType = ConcreteType,
                ConcreteInterface = ConcreteInterface,
                FetcherType = FetcherType,
                FetcherInterface = FetcherInterface,
                CollectionType = CollectionType,
                CollectionInterface = CollectionInterface
            };
        }
        protected void SetConfiguration(CSharpWriterSettings config)
        {
            var configurationProviderMock = new Mock<IConfigurationProvider>();
            configurationProviderMock
                .Setup(c => c.GetConfiguration<CSharpWriterSettings>())
                .Returns(config);
            ConfigurationProvider = configurationProviderMock.Object;
        }
    }
}
