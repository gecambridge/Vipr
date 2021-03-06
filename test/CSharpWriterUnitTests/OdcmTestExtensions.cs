﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Its.Recipes;
using Newtonsoft.Json.Linq;
using Vipr.Core.CodeModel;

namespace CSharpWriterUnitTests
{
    public static class OdcmTestExtensions
    {
        public static IEnumerable<OdcmProperty> GetProperties(this OdcmModel model)
        {
            return
                model.Namespaces.SelectMany(n => n.Classes.SelectMany(c => c.Properties));
        }

        public static IEnumerable<Tuple<string, object>> GetSampleKeyArguments(this OdcmEntityClass entityClass)
        {
            return entityClass.Key.Select(p => new Tuple<string, object>(p.Name, Any.CSharpIdentifier(1)));
        }

        public static JObject GetSampleJObject(this OdcmEntityClass entityClass, bool dontSalt = false)
        {
            return entityClass.GetSampleJObject(entityClass.GetSampleKeyArguments(), dontSalt);
        }

        public static JObject GetSampleJObject(this OdcmEntityClass entityClass, IEnumerable<Tuple<string, object>> keyArguments, bool dontSalt = false)
        {
            var retVal = new JObject();

            foreach (var keyArgument in keyArguments)
            {
                retVal.Add(keyArgument.Item1, new JValue(keyArgument.Item2));
            }

            foreach (var collectionProperty in entityClass.Properties.Where(p => p.IsCollection))
            {
                retVal.Add(collectionProperty.Name, new JArray());
            }

            if (!dontSalt)
            {
                retVal.Add(Any.Word(), new JValue(Any.Int()));
            }

            return retVal;
        }

        public static IEnumerable<Tuple<string, object>> GetSampleArguments(this OdcmMethod method)
        {
            return method.Parameters.Select(p => new Tuple<string, object>(p.Name, Any.Paragraph(5)));
        }

        public static OdcmProperty Rename(this OdcmProperty originalProperty, string newName)
        {
            var index = originalProperty.Class.Properties.IndexOf(originalProperty);

            originalProperty.Class.Properties[index] =
                new OdcmProperty(newName)
                {
                    Class = originalProperty.Class,
                    ReadOnly = originalProperty.ReadOnly,
                    Projection = originalProperty.Type.DefaultProjection,
                    ContainsTarget = originalProperty.ContainsTarget,
                    IsCollection = originalProperty.IsCollection,
                    IsLink = originalProperty.IsLink,
                    IsNullable = originalProperty.IsNullable,
                    IsRequired = originalProperty.IsRequired
                };

            if (originalProperty.Class is OdcmEntityClass && ((OdcmEntityClass)originalProperty.Class).Key.Contains(originalProperty))
            {
                var keyIndex = ((OdcmEntityClass)originalProperty.Class).Key.IndexOf(originalProperty);
                ((OdcmEntityClass)originalProperty.Class).Key[keyIndex] = originalProperty.Class.Properties[index];
            }

            return originalProperty.Class.Properties[index];
        }

        public static string GetDefaultEntitySetName(this OdcmEntityClass odcmClass)
        {
            return odcmClass.Name + "s";
        }

        public static string GetDefaultEntitySetPath(this OdcmEntityClass odcmClass)
        {
            return "/" + odcmClass.GetDefaultEntitySetName();
        }

        public static string GetDefaultSingletonName(this OdcmEntityClass odcmClass)
        {
            return odcmClass.Name;
        }

        public static string GetDefaultSingletonPath(this OdcmEntityClass odcmClass)
        {
            return "/" + odcmClass.GetDefaultSingletonName();
        }

        public static string GetDefaultEntityPath(this OdcmEntityClass odcmClass, IEnumerable<Tuple<string, object>> keyValues = null)
        {
            keyValues = keyValues ?? odcmClass.GetSampleKeyArguments().ToArray();
            
            return string.Format("{0}({1})", odcmClass.GetDefaultEntitySetPath(), ODataKeyPredicate.AsString(keyValues.ToArray()));
        }

        public static string GetDefaultEntityPropertyPath(this OdcmEntityClass odcmClass, OdcmProperty property, IEnumerable<Tuple<string, object>> keyValues = null)
        {
            return odcmClass.GetDefaultEntityPropertyPath(property.Name, keyValues);
        }

        public static string GetDefaultEntityPropertyPath(this OdcmEntityClass odcmClass, string propertyName, IEnumerable<Tuple<string, object>> keyValues = null)
        {
            return string.Format("{0}/{1}", odcmClass.GetDefaultEntityPath(keyValues), propertyName);
        }

        public static string GetDefaultEntityMethodPath(this OdcmEntityClass odcmClass, IEnumerable<Tuple<string, object>> keyValues, string propertyName)
        {
            return string.Format("{0}/{1}", odcmClass.GetDefaultEntityPath(keyValues), propertyName);
        }

        public static EntityArtifacts GetArtifactsFrom(this OdcmEntityClass Class, Assembly Proxy)
        {
            var retVal = new EntityArtifacts
            {
                Class = Class,
                ConcreteType = Proxy.GetClass(Class.Namespace, Class.Name),
                ConcreteInterface = Proxy.GetInterface(Class.Namespace, "I" + Class.Name),
                FetcherType = Proxy.GetClass(Class.Namespace, Class.Name + "Fetcher"),
                FetcherInterface = Proxy.GetInterface(Class.Namespace, "I" + Class.Name + "Fetcher"),
                CollectionType = Proxy.GetClass(Class.Namespace, Class.Name + "Collection"),
                CollectionInterface = Proxy.GetInterface(Class.Namespace, "I" + Class.Name + "Collection")
            };

            return retVal;
        }
    }
}
