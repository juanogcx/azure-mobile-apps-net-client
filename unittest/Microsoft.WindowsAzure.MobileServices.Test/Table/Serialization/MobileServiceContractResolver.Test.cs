﻿// ----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Serialization;

namespace Microsoft.WindowsAzure.MobileServices.Test.Table
{
    [TestClass]
    public class MobileServiceContractResolver_Test
    {
        [TestMethod]
        public void ResolveContractIsThreadSafe()
        {
            const int iterationCount = 100;

            for (int i = 0; i < iterationCount; i++)
            {
                MobileServiceContractResolver contractResolver = new MobileServiceContractResolver();
                Func<JsonContract> resolveContract = () => contractResolver.ResolveContract(typeof(PocoType));

                Task t1 = Task.Run(resolveContract);
                Task t2 = Task.Run(resolveContract);
                Task.WhenAll(t1, t2).Wait();
            }
        }

        [TestMethod]
        public void ResolveTableNameIsThreadSafe()
        {
            const int iterationCount = 100;

            for (int i = 0; i < iterationCount; i++)
            {
                MobileServiceContractResolver contractResolver = new MobileServiceContractResolver();
                Action resolveTableName = () => contractResolver.ResolveTableName(typeof(PocoType));

                Task t1 = Task.Run(resolveTableName);
                Task t2 = Task.Run(resolveTableName);
                Task.WhenAll(t1, t2).Wait();
            }
        }

        [TestMethod]
        public void ResolveTableName()
        {
            MobileServiceContractResolver contractResolver = new MobileServiceContractResolver();

            List<Tuple<Type, string>> testCases = new List<Tuple<Type, string>>() {
                new Tuple<Type, string>(typeof(PocoType), "PocoType"),
                new Tuple<Type, string>(typeof(DataContractType), "DataContractNameFromAttributeType"),
                new Tuple<Type, string>(typeof(DataTableType), "NamedDataTableType"),
                new Tuple<Type, string>(typeof(JsonContainerType), "NamedJsonContainerType"),
                new Tuple<Type, string>(typeof(UnnamedJsonContainerType), "UnnamedJsonContainerType"),
            };

            foreach (var testCase in testCases)
            {
                var input = testCase.Item1;
                string expected = testCase.Item2;

                var actual = contractResolver.ResolveTableName(input);

                Assert.AreEqual(actual, expected);
            }
        }
    }
}
