﻿// ----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Microsoft.WindowsAzure.MobileServices.Test.Table
{
    [TestClass]
    public class MobileServiceTable_Test
    {
        [TestMethod]
        public async Task ReadAsync_DoesNotwrapResult_WhenParameterIsFalse()
        {
            JToken result = await TestReadResponse(@"[{
                                            ""id"":""abc"",
                                            ""String"":""Hey""
                                         }]", null, wrapResult: false);
            JToken[] items = result.ToArray();
            Assert.AreEqual(1, items.Count());
            Assert.AreEqual("abc", (string)items[0]["id"]);
            Assert.AreEqual("Hey", (string)items[0]["String"]);

            result = await TestReadResponse(@"{
                                            ""id"":""abc"",
                                            ""String"":""Hey""
                                         }", null, wrapResult: false);

            var item = result as JObject;
            Assert.IsNotNull(item);
            Assert.AreEqual("abc", (string)item["id"]);
            Assert.AreEqual("Hey", (string)item["String"]);

            result = await TestReadResponse(@"{
                                                ""count"": 53,
                                                ""results"": [
                                                {
                                                    ""id"":""abc"",
                                                    ""String"":""Hey""
                                                }]}", "http://contoso.com/tables/Todo?$top=1&$skip=2; rel=next", wrapResult: false);

            AssertResult(result, 53, null);
        }

        [TestMethod]
        public async Task ReadAsync_FormatsResult_WhenParameterIsTrue()
        {
            JToken result = await TestReadResponse(@"[{
                                                ""id"":""abc"",
                                                ""String"":""Hey""
                                                }]",
                                                   link: null,
                                                   wrapResult: true);
            AssertResult(result, -1, null);

            result = await TestReadResponse(@"{
                                            ""id"":""abc"",
                                            ""String"":""Hey""
                                            }",
                                              link: null,
                                              wrapResult: true);

            AssertResult(result, -1, null);


            result = await TestReadResponse(@"{
                                                ""count"": 53,
                                                ""results"": [
                                                {
                                                    ""id"":""abc"",
                                                    ""String"":""Hey""
                                                }]}",
                                                    link: "http://contoso.com/tables/Todo?$top=1&$skip=2; rel=next",
                                                    wrapResult: true);

            AssertResult(result, 53, "http://contoso.com/tables/Todo?$top=1&$skip=2");
        }

        private static void AssertResult(JToken result, long count, string link)
        {
            var item = result as JObject;
            Assert.IsNotNull(item);
            Assert.AreEqual(count, item["count"].Value<long>());
            Assert.AreEqual(link, (string)item["nextLink"]);
            var items = result["results"].ToArray();
            Assert.AreEqual("abc", (string)items[0]["id"]);
            Assert.AreEqual("Hey", (string)items[0]["String"]);
        }

        private static async Task<JToken> TestReadResponse(string response, string link, bool wrapResult)
        {
            TestHttpHandler hijack = new TestHttpHandler();
            hijack.SetResponseContent(response);
            if (!String.IsNullOrEmpty(link))
            {
                hijack.Responses[0].Headers.Add("Link", link);
            }
            IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);
            IMobileServiceTable table = service.GetTable("someTable");
            JToken result = await table.ReadAsync("this is a query", null, wrapResult);
            return result;
        }

        [TestMethod]
        public async Task ReadAsyncWithStringIdResponseContent()
        {
            string[] testIdData = IdTestData.ValidStringIds.Concat(
                                  IdTestData.EmptyStringIds).Concat(
                                  IdTestData.InvalidStringIds).ToArray();

            foreach (string testId in testIdData)
            {
                TestHttpHandler hijack = new TestHttpHandler();

                // Make the testId JSON safe
                string jsonTestId = testId.Replace("\\", "\\\\").Replace("\"", "\\\"");

                hijack.SetResponseContent("[{\"id\":\"" + jsonTestId + "\",\"String\":\"Hey\"}]");
                IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

                IMobileServiceTable table = service.GetTable("someTable");

                JToken results = await table.ReadAsync("");
                JToken[] items = results.ToArray();

                Assert.AreEqual(1, items.Count());
                Assert.AreEqual(testId, (string)items[0]["id"]);
                Assert.AreEqual("Hey", (string)items[0]["String"]);
            }
        }

        [TestMethod]
        public async Task ReadAsyncWithIntIdResponseContent()
        {
            long[] testIdData = IdTestData.ValidIntIds.Concat(
                                 IdTestData.InvalidIntIds).ToArray();

            foreach (long testId in testIdData)
            {
                string stringTestId = testId.ToString();

                TestHttpHandler hijack = new TestHttpHandler();
                hijack.SetResponseContent("[{\"id\":" + stringTestId + ",\"String\":\"Hey\"}]");
                IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

                IMobileServiceTable table = service.GetTable("someTable");

                JToken results = await table.ReadAsync("");
                JToken[] items = results.ToArray();

                Assert.AreEqual(1, items.Count());
                Assert.AreEqual(testId, (long)items[0]["id"]);
                Assert.AreEqual("Hey", (string)items[0]["String"]);
            }
        }

        [TestMethod]
        public async Task ReadAsyncWithNonStringAndNonIntIdResponseContent()
        {
            object[] testIdData = IdTestData.NonStringNonIntValidJsonIds;

            foreach (object testId in testIdData)
            {
                string stringTestId = testId.ToString().ToLower();

                TestHttpHandler hijack = new TestHttpHandler();
                hijack.SetResponseContent("[{\"id\":" + stringTestId + ",\"String\":\"Hey\"}]");
                IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

                IMobileServiceTable table = service.GetTable("someTable");

                JToken results = await table.ReadAsync("");
                JToken[] items = results.ToArray();

                Assert.AreEqual(1, items.Count());
                Assert.AreEqual(testId, items[0]["id"].ToObject(testId.GetType()));
                Assert.AreEqual("Hey", (string)items[0]["String"]);
            }
        }

        [TestMethod]
        public async Task ReadAsyncWithNullIdResponseContent()
        {
            TestHttpHandler hijack = new TestHttpHandler();
            hijack.SetResponseContent("[{\"id\":null,\"String\":\"Hey\"}]");
            IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

            IMobileServiceTable table = service.GetTable("someTable");

            JToken results = await table.ReadAsync("");
            JToken[] items = results.ToArray();
            JObject item0 = items[0] as JObject;

            Assert.AreEqual(1, items.Count());
            Assert.AreEqual(null, (string)items[0]["id"]);
            Assert.AreEqual("Hey", (string)items[0]["String"]);
        }

        [TestMethod]
        public async Task ReadAsyncWithNoIdResponseContent()
        {
            TestHttpHandler hijack = new TestHttpHandler();
            hijack.SetResponseContent("[{\"String\":\"Hey\"}]");
            IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

            IMobileServiceTable table = service.GetTable("someTable");

            JToken results = await table.ReadAsync("");
            JToken[] items = results.ToArray();
            JObject item0 = items[0] as JObject;

            Assert.AreEqual(1, items.Count());
            Assert.IsFalse(item0.Properties().Any(p => string.Equals(p.Name, "id", StringComparison.InvariantCultureIgnoreCase)));
            Assert.AreEqual("Hey", (string)items[0]["String"]);
        }

        [TestMethod]
        public async Task ReadAsync_WithAbsoluteUri()
        {
            var data = new[]
            {
                new
                {
                    ServiceUri = MobileAppUriValidator.DummyMobileAppWithoutTralingSlash,
                    QueryUri = MobileAppUriValidator.DummyMobileAppWithoutTralingSlash + "/about?$filter=a eq b&$orderby=c",
                    RequestUri = MobileAppUriValidator.DummyMobileAppWithoutTralingSlash + "/about?$filter=a eq b&$orderby=c"
                },
                new
                {
                    ServiceUri = MobileAppUriValidator.DummyMobileApp,
                    QueryUri = MobileAppUriValidator.DummyMobileApp + "about?$filter=a eq b&$orderby=c",
                    RequestUri = MobileAppUriValidator.DummyMobileApp + "about?$filter=a eq b&$orderby=c"
                },
                new
                {
                    ServiceUri = MobileAppUriValidator.DummyMobileAppUriWithFolder,
                    QueryUri = MobileAppUriValidator.DummyMobileAppUriWithFolder + "about?$filter=a eq b&$orderby=c",
                    RequestUri = MobileAppUriValidator.DummyMobileAppUriWithFolder + "about?$filter=a eq b&$orderby=c"
                },
                new
                {
                    ServiceUri = MobileAppUriValidator.DummyMobileAppUriWithFolderWithoutTralingSlash,
                    QueryUri = MobileAppUriValidator.DummyMobileAppUriWithFolderWithoutTralingSlash + "/about?$filter=a eq b&$orderby=c",
                    RequestUri = MobileAppUriValidator.DummyMobileAppUriWithFolderWithoutTralingSlash + "/about?$filter=a eq b&$orderby=c"
                }
            };

            foreach (var item in data)
            {
                var hijack = new TestHttpHandler();
                hijack.SetResponseContent("[{\"String\":\"Hey\"}]");
                IMobileServiceClient service = new MobileServiceClient(item.ServiceUri, hijack);

                IMobileServiceTable table = service.GetTable("someTable");

                await table.ReadAsync(item.QueryUri);

                Assert.AreEqual(item.RequestUri, hijack.Request.RequestUri.ToString());
            }
        }

        [TestMethod]
        public async Task ReadAsync_WithRelativeUri()
        {
            var data = new[]
            {
                new
                {
                    ServiceUri = MobileAppUriValidator.DummyMobileAppWithoutTralingSlash,
                    QueryUri = "/about?$filter=a eq b&$orderby=c",
                    RequestUri = MobileAppUriValidator.DummyMobileAppWithoutTralingSlash + "/about?$filter=a eq b&$orderby=c"
                },
                new
                {
                    ServiceUri = MobileAppUriValidator.DummyMobileApp,
                    QueryUri = "/about?$filter=a eq b&$orderby=c",
                    RequestUri = MobileAppUriValidator.DummyMobileApp + "about?$filter=a eq b&$orderby=c"
                },
                 new
                {
                    ServiceUri = MobileAppUriValidator.DummyMobileAppUriWithFolder,
                    QueryUri = "/about?$filter=a eq b&$orderby=c",
                    RequestUri = MobileAppUriValidator.DummyMobileAppUriWithFolder + "about?$filter=a eq b&$orderby=c"
                },
                new
                {
                    ServiceUri = MobileAppUriValidator.DummyMobileAppUriWithFolderWithoutTralingSlash,
                    QueryUri = "/about?$filter=a eq b&$orderby=c",
                    RequestUri = MobileAppUriValidator.DummyMobileAppUriWithFolderWithoutTralingSlash + "/about?$filter=a eq b&$orderby=c"
                }
            };

            foreach (var item in data)
            {
                var hijack = new TestHttpHandler();
                hijack.SetResponseContent("[{\"String\":\"Hey\"}]");
                IMobileServiceClient service = new MobileServiceClient(item.ServiceUri, hijack);

                IMobileServiceTable table = service.GetTable("someTable");

                await table.ReadAsync(item.QueryUri);

                Assert.AreEqual(item.RequestUri, hijack.Request.RequestUri.ToString());
            }
        }

        [TestMethod]
        public async Task ReadAsyncWithStringIdFilter()
        {
            string[] testIdData = IdTestData.ValidStringIds.Concat(
                                  IdTestData.EmptyStringIds).Concat(
                                  IdTestData.InvalidStringIds).ToArray();

            foreach (string testId in testIdData)
            {
                TestHttpHandler hijack = new TestHttpHandler();

                // Make the testId JSON safe
                string jsonTestId = testId.Replace("\\", "\\\\").Replace("\"", "\\\"");

                hijack.SetResponseContent("[{\"id\":\"" + jsonTestId + "\",\"String\":\"Hey\"}]");

                IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);
                MobileAppUriValidator mobileAppUriValidator = new MobileAppUriValidator(service);

                IMobileServiceTable table = service.GetTable("someTable");

                string idForOdataQuery = Uri.EscapeDataString(testId.Replace("'", "''"));
                JToken results = await table.ReadAsync(string.Format("$filter=id eq '{0}'", idForOdataQuery));
                JToken[] items = results.ToArray();
                JObject item0 = items[0] as JObject;

                Uri expectedUri = new Uri(string.Format(mobileAppUriValidator.GetTableUri("someTable?$filter=id eq '{0}'"), idForOdataQuery));

                Assert.AreEqual(1, items.Count());
                Assert.AreEqual(testId, (string)items[0]["id"]);
                Assert.AreEqual("Hey", (string)items[0]["String"]);
                Assert.AreEqual(hijack.Request.RequestUri.AbsoluteUri, expectedUri.AbsoluteUri);
            }
        }

        [TestMethod]
        public async Task ReadAsyncWithNullIdFilter()
        {
            TestHttpHandler hijack = new TestHttpHandler();
            hijack.SetResponseContent("[{\"id\":null,\"String\":\"Hey\"}]");

            IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);
            MobileAppUriValidator mobileAppUriValidator = new MobileAppUriValidator(service);

            IMobileServiceTable table = service.GetTable("someTable");

            JToken results = await table.ReadAsync("$filter=id eq null");
            JToken[] items = results.ToArray();
            JObject item0 = items[0] as JObject;

            Uri expectedUri = new Uri(mobileAppUriValidator.GetTableUri("someTable?$filter=id eq null"));

            Assert.AreEqual(1, items.Count());
            Assert.AreEqual(null, (string)items[0]["id"]);
            Assert.AreEqual("Hey", (string)items[0]["String"]);
            Assert.AreEqual(hijack.Request.RequestUri.AbsoluteUri, expectedUri.AbsoluteUri);
        }

        [TestMethod]
        public async Task ReadAsyncWithUserParameters()
        {
            TestHttpHandler hijack = new TestHttpHandler();
            hijack.SetResponseContent("{\"Count\":1, People: [{\"Id\":\"12\", \"String\":\"Hey\"}] }");
            IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

            var userDefinedParameters = new Dictionary<string, string>() { { "tags", "#pizza #beer" } };

            IMobileServiceTable table = service.GetTable("tests");

            JToken people = await table.ReadAsync("$filter=id eq 12", userDefinedParameters);

            StringAssert.Contains(hijack.Request.RequestUri.ToString(), "tests");
            StringAssert.Contains(hijack.Request.RequestUri.AbsoluteUri, "tags=%23pizza%20%23beer");
            StringAssert.Contains(hijack.Request.RequestUri.ToString(), "$filter=id eq 12");

            Assert.AreEqual(1, (int)people["Count"]);
            Assert.AreEqual(12, (int)people["People"][0]["Id"]);
            Assert.AreEqual("Hey", (string)people["People"][0]["String"]);
        }

        [TestMethod]
        public async Task ReadAsyncThrowsWithInvalidUserParameters()
        {
            var invalidUserDefinedParameters = new Dictionary<string, string>() { { "$this is invalid", "since it starts with a '$'" } };
            MobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp);
            IMobileServiceTable table = service.GetTable("test");
            ArgumentException expected = null;

            try
            {
                await table.ReadAsync("$filter=id eq 12", invalidUserDefinedParameters);
            }
            catch (ArgumentException e)
            {
                expected = e;
            }

            Assert.IsNotNull(expected);
        }

        [TestMethod]
        public async Task LookupAsyncWithStringIdResponseContent()
        {
            string[] testIdData = IdTestData.ValidStringIds.Concat(
                                  IdTestData.EmptyStringIds).Concat(
                                  IdTestData.InvalidStringIds).ToArray();

            foreach (string testId in testIdData)
            {
                TestHttpHandler hijack = new TestHttpHandler();

                // Make the testId JSON safe
                string jsonTestId = testId.Replace("\\", "\\\\").Replace("\"", "\\\"");

                hijack.SetResponseContent("{\"id\":\"" + jsonTestId + "\",\"String\":\"Hey\"}");
                IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

                IMobileServiceTable table = service.GetTable("someTable");

                JObject item = await table.LookupAsync("id") as JObject;

                Assert.AreEqual(testId, (string)item["id"]);
                Assert.AreEqual("Hey", (string)item["String"]);
            }
        }

        [TestMethod]
        public async Task LookupAsyncWithIntIdResponseContent()
        {
            long[] testIdData = IdTestData.ValidIntIds.Concat(
                                 IdTestData.InvalidIntIds).ToArray();

            foreach (long testId in testIdData)
            {
                string stringTestId = testId.ToString();

                TestHttpHandler hijack = new TestHttpHandler();
                hijack.SetResponseContent("{\"id\":" + stringTestId + ",\"String\":\"Hey\"}");
                IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

                IMobileServiceTable table = service.GetTable("someTable");

                JObject item = await table.LookupAsync("id") as JObject;

                Assert.AreEqual(testId, (long)item["id"]);
                Assert.AreEqual("Hey", (string)item["String"]);
            }
        }

        [TestMethod]
        public async Task LookupAsyncWithNonStringAndNonIntIdResponseContent()
        {
            object[] testIdData = IdTestData.NonStringNonIntValidJsonIds;

            foreach (object testId in testIdData)
            {
                string stringTestId = testId.ToString().ToLower();

                TestHttpHandler hijack = new TestHttpHandler();
                hijack.SetResponseContent("{\"id\":" + stringTestId + ",\"String\":\"Hey\"}");
                IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

                IMobileServiceTable table = service.GetTable("someTable");

                JObject item = await table.LookupAsync("id") as JObject;

                object value = item["id"].ToObject(testId.GetType());

                Assert.AreEqual(testId, value);
                Assert.AreEqual("Hey", (string)item["String"]);
            }
        }

        [TestMethod]
        public async Task LookupAsyncWithNullIdResponseContent()
        {
            TestHttpHandler hijack = new TestHttpHandler();
            hijack.SetResponseContent("{\"id\":null,\"String\":\"Hey\"}");
            IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

            IMobileServiceTable table = service.GetTable("someTable");

            JObject item = await table.LookupAsync("id") as JObject;

            Assert.AreEqual(null, (string)item["id"]);
            Assert.AreEqual("Hey", (string)item["String"]);
        }

        [TestMethod]
        public async Task LookupAsyncWithNoIdResponseContent()
        {
            TestHttpHandler hijack = new TestHttpHandler();
            hijack.SetResponseContent("{\"String\":\"Hey\"}");
            IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

            IMobileServiceTable table = service.GetTable("someTable");

            JObject item = await table.LookupAsync("id") as JObject;

            Assert.IsFalse(item.Properties().Any(p => string.Equals(p.Name, "id", StringComparison.InvariantCultureIgnoreCase)));
            Assert.AreEqual("Hey", (string)item["String"]);
        }

        [TestMethod]
        public async Task LookupAsyncWithStringIdParameter()
        {
            string[] testIdData = IdTestData.ValidStringIds;

            foreach (string testId in testIdData)
            {
                TestHttpHandler hijack = new TestHttpHandler();
                hijack.SetResponseContent("{\"id\":\"" + testId + "\",\"String\":\"Hey\"}");
                IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);
                MobileAppUriValidator mobileAppUriValidator = new MobileAppUriValidator(service);

                IMobileServiceTable table = service.GetTable("someTable");

                JToken item = await table.LookupAsync(testId);

                Uri expectedUri = new Uri(string.Format(mobileAppUriValidator.GetTableUri("someTable/{0}"), Uri.EscapeDataString(testId)));

                Assert.AreEqual(testId, (string)item["id"]);
                Assert.AreEqual("Hey", (string)item["String"]);
                Assert.AreEqual(hijack.Request.RequestUri.AbsoluteUri, expectedUri.AbsoluteUri);
            }
        }

        [TestMethod]
        public async Task LookupAsyncWithInvalidStringIdParameter()
        {
            string[] testIdData = IdTestData.EmptyStringIds.Concat(
                                  IdTestData.InvalidStringIds).ToArray();

            foreach (string testId in testIdData)
            {
                TestHttpHandler hijack = new TestHttpHandler();
                hijack.SetResponseContent("{\"id\":\"" + testId + "\",\"String\":\"Hey\"}");
                IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

                IMobileServiceTable table = service.GetTable("someTable");
                Exception exception = null;
                try
                {
                    JToken item = await table.LookupAsync(testId);
                }
                catch (Exception e)
                {
                    exception = e;
                }

                Assert.IsNotNull(exception);
                Assert.IsTrue(exception.Message.Contains("The id can not be null or an empty string.") ||
                              exception.Message.Contains("An id must not contain any control characters or the characters") ||
                              exception.Message.Contains("is longer than the max string id length of 255 characters"));
            }
        }

        [TestMethod]
        public async Task LookupAsyncWithIntIdParameter()
        {
            long[] testIdData = IdTestData.ValidIntIds;

            foreach (long testId in testIdData)
            {
                TestHttpHandler hijack = new TestHttpHandler();
                hijack.SetResponseContent("{\"id\":" + testId.ToString() + ",\"String\":\"Hey\"}");
                IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);
                MobileAppUriValidator mobileAppUriValidator = new MobileAppUriValidator(service);

                IMobileServiceTable table = service.GetTable("someTable");

                JToken item = await table.LookupAsync(testId);

                Uri expectedUri = new Uri(string.Format(mobileAppUriValidator.GetTableUri("someTable/{0}"), testId));

                Assert.AreEqual(testId, (long)item["id"]);
                Assert.AreEqual("Hey", (string)item["String"]);
                Assert.AreEqual(hijack.Request.RequestUri.AbsoluteUri, expectedUri.AbsoluteUri);
            }
        }

        [TestMethod]
        public async Task LookupAsyncWithNullIdParameter()
        {
            TestHttpHandler hijack = new TestHttpHandler();
            hijack.SetResponseContent("{\"id\":null,\"String\":\"Hey\"}");
            IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

            IMobileServiceTable table = service.GetTable("someTable");

            Exception exception = null;
            try
            {
                JToken item = await table.LookupAsync(null);
            }
            catch (Exception e)
            {
                exception = e;
            }

            Assert.IsNotNull(exception);
            Assert.IsTrue(exception.Message.Contains("The id can not be null or an empty string."));
        }

        [TestMethod]
        public async Task LookupAsyncWithZeroIdParameter()
        {
            TestHttpHandler hijack = new TestHttpHandler();
            hijack.SetResponseContent("{\"id\":null,\"String\":\"Hey\"}");
            IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

            IMobileServiceTable table = service.GetTable("someTable");

            Exception exception = null;
            try
            {
                JToken item = await table.LookupAsync(0L);
            }
            catch (Exception e)
            {
                exception = e;
            }

            Assert.IsNotNull(exception);
            Assert.IsTrue(exception.Message.Contains("Specified argument was out of the range of valid values") ||
                          exception.Message.Contains(" is not a positive integer value"));
        }

        [TestMethod]
        public async Task LookupAsyncWithUserParameters()
        {
            TestHttpHandler hijack = new TestHttpHandler();
            hijack.SetResponseContent("{\"Count\":1, People: [{\"Id\":\"12\", \"String\":\"Hey\"}] }");
            IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

            var userDefinedParameters = new Dictionary<string, string>() { { "tags", "#pizza #beer" } };

            IMobileServiceTable table = service.GetTable("tests");

            JToken people = await table.LookupAsync("id ", userDefinedParameters);

            StringAssert.Contains(hijack.Request.RequestUri.ToString(), "tests");
            StringAssert.Contains(hijack.Request.RequestUri.AbsoluteUri, "tags=%23pizza%20%23beer");

            Assert.AreEqual(1, (int)people["Count"]);
            Assert.AreEqual(12, (int)people["People"][0]["Id"]);
            Assert.AreEqual("Hey", (string)people["People"][0]["String"]);
        }

        [TestMethod]
        public async Task LookupAsyncThrowsWithInvalidUserParameters()
        {
            var invalidUserDefinedParameters = new Dictionary<string, string>() { { "$this is invalid", "since it starts with a '$'" } };
            MobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp);
            IMobileServiceTable table = service.GetTable("test");
            ArgumentException expected = null;

            try
            {
                await table.LookupAsync("$filter=id eq 12", invalidUserDefinedParameters);
            }
            catch (ArgumentException e)
            {
                expected = e;
            }

            Assert.IsNotNull(expected);
        }

        [TestMethod]
        public async Task InsertAsyncWithStringIdResponseContent()
        {
            string[] testIdData = IdTestData.ValidStringIds.Concat(
                                  IdTestData.EmptyStringIds).Concat(
                                  IdTestData.InvalidStringIds).ToArray();

            foreach (string testId in testIdData)
            {
                TestHttpHandler hijack = new TestHttpHandler();

                // Make the testId JSON safe
                string jsonTestId = testId.Replace("\\", "\\\\").Replace("\"", "\\\"");

                hijack.SetResponseContent("{\"id\":\"" + jsonTestId + "\",\"String\":\"Hey\"}");
                IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

                IMobileServiceTable table = service.GetTable("someTable");

                JObject obj = JToken.Parse("{\"value\":\"new\"}") as JObject;
                JObject item = await table.InsertAsync(obj) as JObject;

                Assert.AreEqual(testId, (string)item["id"]);
                Assert.AreEqual("Hey", (string)item["String"]);
            }
        }

        [TestMethod]
        public async Task InsertAsyncWithIntIdResponseContent()
        {
            long[] testIdData = IdTestData.ValidIntIds.Concat(
                                 IdTestData.InvalidIntIds).ToArray();

            foreach (long testId in testIdData)
            {
                string stringTestId = testId.ToString();

                TestHttpHandler hijack = new TestHttpHandler();
                hijack.SetResponseContent("{\"id\":" + stringTestId + ",\"String\":\"Hey\"}");
                IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

                IMobileServiceTable table = service.GetTable("someTable");

                JObject obj = JToken.Parse("{\"value\":\"new\"}") as JObject;
                JObject item = await table.InsertAsync(obj) as JObject;

                Assert.AreEqual(testId, (long)item["id"]);
                Assert.AreEqual("Hey", (string)item["String"]);
            }
        }

        [TestMethod]
        public async Task InsertAsyncWithNonStringAndNonIntIdResponseContent()
        {
            object[] testIdData = IdTestData.NonStringNonIntValidJsonIds;

            foreach (object testId in testIdData)
            {
                string stringTestId = testId.ToString().ToLower();

                TestHttpHandler hijack = new TestHttpHandler();
                hijack.SetResponseContent("{\"id\":" + stringTestId + ",\"String\":\"Hey\"}");
                IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

                IMobileServiceTable table = service.GetTable("someTable");

                JObject obj = JToken.Parse("{\"value\":\"new\"}") as JObject;
                JObject item = await table.InsertAsync(obj) as JObject;

                object value = item["id"].ToObject(testId.GetType());

                Assert.AreEqual(testId, value);
                Assert.AreEqual("Hey", (string)item["String"]);
            }
        }

        [TestMethod]
        public async Task InsertAsyncWithNullIdResponseContent()
        {
            TestHttpHandler hijack = new TestHttpHandler();
            hijack.SetResponseContent("{\"id\":null,\"String\":\"Hey\"}");
            IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

            IMobileServiceTable table = service.GetTable("someTable");

            JObject obj = JToken.Parse("{\"value\":\"new\"}") as JObject;
            JObject item = await table.InsertAsync(obj) as JObject;

            Assert.AreEqual(null, (string)item["id"]);
            Assert.AreEqual("Hey", (string)item["String"]);
        }

        [TestMethod]
        public async Task InsertAsyncWithNoIdResponseContent()
        {
            TestHttpHandler hijack = new TestHttpHandler();
            hijack.SetResponseContent("{\"String\":\"Hey\"}");
            IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

            IMobileServiceTable table = service.GetTable("someTable");

            JObject obj = JToken.Parse("{\"value\":\"new\"}") as JObject;
            JObject item = await table.InsertAsync(obj) as JObject;

            Assert.IsFalse(item.Properties().Any(p => string.Equals(p.Name, "id", StringComparison.InvariantCultureIgnoreCase)));
            Assert.AreEqual("Hey", (string)item["String"]);
        }

        [TestMethod]
        public async Task InsertAsyncWithStringIdItem()
        {
            string[] testIdData = IdTestData.ValidStringIds;

            foreach (string testId in testIdData)
            {
                TestHttpHandler hijack = new TestHttpHandler();
                hijack.SetResponseContent("{\"id\":\"" + testId + "\",\"String\":\"Hey\"}");
                IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

                IMobileServiceTable table = service.GetTable("someTable");

                JObject obj = JToken.Parse("{\"id\":\"" + testId + "\",\"String\":\"what?\"}") as JObject;
                JToken item = await table.InsertAsync(obj);

                Assert.AreEqual(testId, (string)item["id"]);
                Assert.AreEqual("Hey", (string)item["String"]);
            }
        }

        [TestMethod]
        public async Task InsertAsyncWithEmptyStringIdItem()
        {
            string[] testIdData = IdTestData.EmptyStringIds;

            foreach (string testId in testIdData)
            {
                TestHttpHandler hijack = new TestHttpHandler();
                hijack.SetResponseContent("{\"id\":\"an id\",\"String\":\"Hey\"}");
                IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

                IMobileServiceTable table = service.GetTable("someTable");

                JObject obj = JToken.Parse("{\"id\":\"" + testId + "\",\"String\":\"what?\"}") as JObject;
                JToken item = await table.InsertAsync(obj);

                Assert.AreEqual("an id", (string)item["id"]);
                Assert.AreEqual("Hey", (string)item["String"]);
            }
        }

        [TestMethod]
        public async Task InsertAsyncWithInvalidStringIdItem()
        {
            string[] testIdData = IdTestData.InvalidStringIds;

            foreach (string testId in testIdData)
            {
                TestHttpHandler hijack = new TestHttpHandler();

                // Make the testId JSON safe
                string jsonTestId = testId.Replace("\\", "\\\\").Replace("\"", "\\\"");

                hijack.SetResponseContent("{\"id\":\"" + jsonTestId + "\",\"String\":\"Hey\"}");
                IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

                IMobileServiceTable table = service.GetTable("someTable");
                Exception exception = null;
                try
                {
                    JObject obj = JToken.Parse("{\"id\":\"" + jsonTestId + "\",\"String\":\"what?\"}") as JObject;
                    JToken item = await table.InsertAsync(obj);
                }
                catch (Exception e)
                {
                    exception = e;
                }

                Assert.IsNotNull(exception);
                Assert.IsTrue(exception.Message.Contains("is longer than the max string id length of 255 characters") ||
                             exception.Message.Contains("An id must not contain any control characters or the characters"));
            }
        }

        [TestMethod]
        public async Task InsertAsyncWithIntIdItem()
        {
            long[] testIdData = IdTestData.ValidIntIds;

            foreach (long testId in testIdData)
            {
                TestHttpHandler hijack = new TestHttpHandler();
                hijack.SetResponseContent("{\"id\":" + testId.ToString() + ",\"String\":\"Hey\"}");
                IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

                IMobileServiceTable table = service.GetTable("someTable");
                Exception exception = null;

                try
                {
                    JObject obj = JToken.Parse("{\"id\":" + testId.ToString() + ",\"String\":\"what?\"}") as JObject;
                    JToken item = await table.InsertAsync(obj);
                }
                catch (Exception e)
                {
                    exception = e;
                }

                Assert.IsNotNull(exception);
                Assert.IsTrue(exception.Message.Contains("Cannot insert if the id member is already set.") ||
                              exception.Message.Contains("for member id is outside the valid range for numeric columns"));
            }
        }

        [TestMethod]
        public async Task InsertAsyncWithNullIdItem()
        {
            TestHttpHandler hijack = new TestHttpHandler();
            hijack.SetResponseContent("{\"id\":5,\"String\":\"Hey\"}");
            IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

            IMobileServiceTable table = service.GetTable("someTable");

            JObject obj = JToken.Parse("{\"id\":null,\"String\":\"what?\"}") as JObject;
            JToken item = await table.InsertAsync(obj);

            Assert.AreEqual(5L, (long)item["id"]);
            Assert.AreEqual("Hey", (string)item["String"]);
        }

        [TestMethod]
        public async Task InsertAsyncWithNoIdItem()
        {
            TestHttpHandler hijack = new TestHttpHandler();
            hijack.SetResponseContent("{\"id\":5,\"String\":\"Hey\"}");
            IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

            IMobileServiceTable table = service.GetTable("someTable");

            JObject obj = JToken.Parse("{\"String\":\"what?\"}") as JObject;
            JToken item = await table.InsertAsync(obj);

            Assert.AreEqual(5L, (long)item["id"]);
            Assert.AreEqual("Hey", (string)item["String"]);
        }

        [TestMethod]
        public async Task InsertAsyncWithZeroIdItem()
        {
            TestHttpHandler hijack = new TestHttpHandler();
            hijack.SetResponseContent("{\"id\":5,\"String\":\"Hey\"}");
            IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

            IMobileServiceTable table = service.GetTable("someTable");

            JObject obj = JToken.Parse("{\"id\":0,\"String\":\"what?\"}") as JObject;
            JToken item = await table.InsertAsync(obj);

            Assert.AreEqual(5L, (long)item["id"]);
            Assert.AreEqual("Hey", (string)item["String"]);
        }

        [TestMethod]
        public async Task InsertAsyncWithUserParameters()
        {
            var userDefinedParameters = new Dictionary<string, string>() { { "state", "AL" } };

            TestHttpHandler hijack = new TestHttpHandler();
            IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);
            IMobileServiceTable table = service.GetTable("tests");

            JObject obj = JToken.Parse("{\"value\":\"new\"}") as JObject;
            hijack.SetResponseContent("{\"id\":12,\"value\":\"new\"}");
            JToken newObj = await table.InsertAsync(obj, userDefinedParameters);

            Assert.AreEqual(12, (int)newObj["id"]);
            Assert.AreNotEqual(newObj, obj);
            StringAssert.Contains(hijack.Request.RequestUri.ToString(), "tests");
            StringAssert.Contains(hijack.Request.RequestUri.Query, "state=AL");
        }

        [TestMethod]
        public async Task InsertAsyncThrowsWhenIdIsID()
        {
            MobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp);
            IMobileServiceTable table = service.GetTable("tests");
            ArgumentException expected = null;

            JObject obj = JToken.Parse("{\"ID\":\"an id\"}") as JObject;
            try
            {
                await table.InsertAsync(obj);
            }
            catch (ArgumentException e)
            {
                expected = e;
            }

            Assert.IsNotNull(expected);
            Assert.IsTrue(expected.Message.Contains("The casing of the 'id' property is invalid."));
        }

        [TestMethod]
        public async Task InsertAsyncThrowsWhenIdIsId()
        {
            MobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp);
            IMobileServiceTable table = service.GetTable("tests");
            ArgumentException expected = null;

            JObject obj = JToken.Parse("{\"Id\":\"an id\"}") as JObject;
            try
            {
                await table.InsertAsync(obj);
            }
            catch (ArgumentException e)
            {
                expected = e;
            }

            Assert.IsNotNull(expected);
            Assert.IsTrue(expected.Message.Contains("The casing of the 'id' property is invalid."));
        }

        [TestMethod]
        public async Task InsertAsyncThrowsWhenIdIsiD()
        {
            MobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp);
            IMobileServiceTable table = service.GetTable("tests");
            ArgumentException expected = null;

            JObject obj = JToken.Parse("{\"iD\":\"an id\"}") as JObject;
            try
            {
                await table.InsertAsync(obj);
            }
            catch (ArgumentException e)
            {
                expected = e;
            }

            Assert.IsNotNull(expected);
            Assert.IsTrue(expected.Message.Contains("The casing of the 'id' property is invalid."));
        }

        [TestMethod]
        public async Task InsertAsyncWithStringId_KeepsSystemProperties()
        {
            var userDefinedParameters = new Dictionary<string, string>() { { "__systemproperties", "createdAt" } };

            TestHttpHandler hijack = new TestHttpHandler();
            IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);
            IMobileServiceTable table = service.GetTable("tests");

            JObject obj = JToken.Parse("{\"value\":\"new\"}") as JObject;
            hijack.SetResponseContent("{\"id\":\"A\",\"value\":\"new\", \"version\":\"XYZ\",\"__unknown\":12,\"CREATEDat\":\"12-02-02\"}");
            JToken newObj = await table.InsertAsync(obj);

            Assert.AreEqual("A", (string)newObj["id"]);
            Assert.AreNotEqual(newObj, obj);
            Assert.IsNotNull(newObj["version"]);
            Assert.IsNotNull(newObj["__unknown"]);
            Assert.IsNotNull(newObj["CREATEDat"]);
        }

        [TestMethod]
        public async Task InsertAsyncWithIntId_DoesNotStripSystemProperties()
        {
            var userDefinedParameters = new Dictionary<string, string>() { { "state", "AL" } };

            TestHttpHandler hijack = new TestHttpHandler();
            IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);
            IMobileServiceTable table = service.GetTable("tests");

            JObject obj = JToken.Parse("{\"value\":\"new\"}") as JObject;
            hijack.SetResponseContent("{\"id\":12,\"value\":\"new\", \"version\":\"XYZ\",\"__unknown\":12,\"CREATEDat\":\"12-02-02\"}");
            JToken newObj = await table.InsertAsync(obj, userDefinedParameters);

            Assert.AreEqual(12, (int)newObj["id"]);
            Assert.AreNotEqual(newObj, obj);
            Assert.IsNotNull(newObj["version"]);
            Assert.IsNotNull(newObj["__unknown"]);
            Assert.IsNotNull(newObj["CREATEDat"]);
        }

        [TestMethod]
        public async Task UpdateAsyncWithStringIdResponseContent()
        {
            string[] testIdData = IdTestData.ValidStringIds.Concat(
                                  IdTestData.EmptyStringIds).Concat(
                                  IdTestData.InvalidStringIds).ToArray();

            foreach (string testId in testIdData)
            {
                TestHttpHandler hijack = new TestHttpHandler();

                // Make the testId JSON safe
                string jsonTestId = testId.Replace("\\", "\\\\").Replace("\"", "\\\"");

                hijack.SetResponseContent("{\"id\":\"" + jsonTestId + "\",\"String\":\"Hey\"}");
                IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

                IMobileServiceTable table = service.GetTable("someTable");

                JObject obj = JToken.Parse("{\"id\":\"id\",\"value\":\"new\"}") as JObject;
                JObject item = await table.UpdateAsync(obj) as JObject;

                Assert.AreEqual(testId, (string)item["id"]);
                Assert.AreEqual("Hey", (string)item["String"]);
            }
        }

        [TestMethod]
        public async Task UpdateAsyncWithIntIdResponseContent()
        {
            long[] testIdData = IdTestData.ValidIntIds.Concat(
                                 IdTestData.InvalidIntIds).ToArray();

            foreach (long testId in testIdData)
            {
                string stringTestId = testId.ToString();

                TestHttpHandler hijack = new TestHttpHandler();
                hijack.SetResponseContent("{\"id\":" + stringTestId + ",\"String\":\"Hey\"}");
                IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

                IMobileServiceTable table = service.GetTable("someTable");

                JObject obj = JToken.Parse("{\"id\":\"id\",\"value\":\"new\"}") as JObject;
                JObject item = await table.UpdateAsync(obj) as JObject;

                Assert.AreEqual(testId, (long)item["id"]);
                Assert.AreEqual("Hey", (string)item["String"]);
            }
        }

        [TestMethod]
        public async Task UpdateAsyncWithNonStringAndNonIntIdResponseContent()
        {
            object[] testIdData = IdTestData.NonStringNonIntValidJsonIds;

            foreach (object testId in testIdData)
            {
                string stringTestId = testId.ToString().ToLower();

                TestHttpHandler hijack = new TestHttpHandler();
                hijack.SetResponseContent("{\"id\":" + stringTestId + ",\"String\":\"Hey\"}");
                IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

                IMobileServiceTable table = service.GetTable("someTable");

                JObject obj = JToken.Parse("{\"id\":\"id\",\"value\":\"new\"}") as JObject;
                JObject item = await table.UpdateAsync(obj) as JObject;

                object value = item["id"].ToObject(testId.GetType());

                Assert.AreEqual(testId, value);
                Assert.AreEqual("Hey", (string)item["String"]);
            }
        }

        [TestMethod]
        public async Task UpdateAsyncWithNullIdResponseContent()
        {
            TestHttpHandler hijack = new TestHttpHandler();
            hijack.SetResponseContent("{\"id\":null,\"String\":\"Hey\"}");
            IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

            IMobileServiceTable table = service.GetTable("someTable");

            JObject obj = JToken.Parse("{\"id\":\"id\",\"value\":\"new\"}") as JObject;
            JObject item = await table.UpdateAsync(obj) as JObject;

            Assert.AreEqual(null, (string)item["id"]);
            Assert.AreEqual("Hey", (string)item["String"]);
        }

        [TestMethod]
        public async Task UpdateAsyncWithNoIdResponseContent()
        {
            TestHttpHandler hijack = new TestHttpHandler();
            hijack.SetResponseContent("{\"String\":\"Hey\"}");
            IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

            IMobileServiceTable table = service.GetTable("someTable");

            JObject obj = JToken.Parse("{\"id\":\"id\",\"value\":\"new\"}") as JObject;
            JObject item = await table.UpdateAsync(obj) as JObject;

            Assert.IsFalse(item.Properties().Any(p => string.Equals(p.Name, "id", StringComparison.InvariantCultureIgnoreCase)));
            Assert.AreEqual("Hey", (string)item["String"]);
        }

        [TestMethod]
        public async Task UpdateAsyncWithStringIdItem()
        {
            string[] testIdData = IdTestData.ValidStringIds;

            foreach (string testId in testIdData)
            {
                TestHttpHandler hijack = new TestHttpHandler();
                hijack.SetResponseContent("{\"id\":\"" + testId + "\",\"String\":\"Hey\"}");
                IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

                IMobileServiceTable table = service.GetTable("someTable");

                JObject obj = JToken.Parse("{\"id\":\"" + testId + "\",\"String\":\"what?\"}") as JObject;
                JToken item = await table.UpdateAsync(obj);

                Assert.AreEqual(testId, (string)item["id"]);
                Assert.AreEqual("Hey", (string)item["String"]);
            }
        }

        [TestMethod]
        public async Task UpdateAsyncWithEmptyStringIdItem()
        {
            string[] testIdData = IdTestData.EmptyStringIds;

            foreach (string testId in testIdData)
            {
                TestHttpHandler hijack = new TestHttpHandler();
                hijack.SetResponseContent("{\"id\":\"an id\",\"String\":\"Hey\"}");
                IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

                IMobileServiceTable table = service.GetTable("someTable");
                Exception exception = null;
                try
                {
                    JObject obj = JToken.Parse("{\"id\":\"" + testId + "\",\"String\":\"what?\"}") as JObject;
                    JToken item = await table.UpdateAsync(obj);
                }
                catch (Exception e)
                {
                    exception = e;
                }

                Assert.IsNotNull(exception);
                Assert.IsTrue(exception.Message.Contains("The id can not be null or an empty string."));
            }
        }

        [TestMethod]
        public async Task UpdateAsyncWithInvalidStringIdItem()
        {
            string[] testIdData = IdTestData.InvalidStringIds;

            foreach (string testId in testIdData)
            {
                TestHttpHandler hijack = new TestHttpHandler();

                // Make the testId JSON safe
                string jsonTestId = testId.Replace("\\", "\\\\").Replace("\"", "\\\"");

                hijack.SetResponseContent("{\"id\":\"" + jsonTestId + "\",\"String\":\"Hey\"}");
                IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

                IMobileServiceTable table = service.GetTable("someTable");
                Exception exception = null;
                try
                {
                    JObject obj = JToken.Parse("{\"id\":\"" + jsonTestId + "\",\"String\":\"what?\"}") as JObject;
                    JToken item = await table.UpdateAsync(obj);
                }
                catch (Exception e)
                {
                    exception = e;
                }

                Assert.IsNotNull(exception);
                Assert.IsTrue(exception.Message.Contains("is longer than the max string id length of 255 characters") ||
                              exception.Message.Contains("An id must not contain any control characters or the characters"));
            }
        }

        [TestMethod]
        public async Task UpdateAsyncWithIntIdItem()
        {
            long[] testIdData = IdTestData.ValidIntIds;

            foreach (long testId in testIdData)
            {
                TestHttpHandler hijack = new TestHttpHandler();
                hijack.SetResponseContent("{\"id\":" + testId.ToString() + ",\"String\":\"Hey\"}");
                IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

                IMobileServiceTable table = service.GetTable("someTable");
                JObject obj = JToken.Parse("{\"id\":" + testId.ToString() + ",\"String\":\"what?\"}") as JObject;
                JToken item = await table.UpdateAsync(obj);

                Assert.AreEqual(testId, (long)item["id"]);
                Assert.AreEqual("Hey", (string)item["String"]);
            }
        }

        [TestMethod]
        public async Task UpdateAsyncWithNullIdItem()
        {
            TestHttpHandler hijack = new TestHttpHandler();
            hijack.SetResponseContent("{\"id\":5,\"String\":\"Hey\"}");
            IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

            IMobileServiceTable table = service.GetTable("someTable");
            Exception exception = null;
            try
            {
                JObject obj = JToken.Parse("{\"id\":null,\"String\":\"what?\"}") as JObject;
                JToken item = await table.UpdateAsync(obj);
            }
            catch (Exception e)
            {
                exception = e;
            }

            Assert.IsNotNull(exception);
            Assert.IsTrue(exception.Message.Contains("The id can not be null or an empty string"));
        }

        [TestMethod]
        public async Task UpdateAsyncWithNoIdItem()
        {
            TestHttpHandler hijack = new TestHttpHandler();
            hijack.SetResponseContent("{\"id\":5,\"String\":\"Hey\"}");
            IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

            IMobileServiceTable table = service.GetTable("someTable");
            Exception exception = null;
            try
            {
                JObject obj = JToken.Parse("{\"String\":\"what?\"}") as JObject;
                JToken item = await table.UpdateAsync(obj);
            }
            catch (Exception e)
            {
                exception = e;
            }

            Assert.IsNotNull(exception);
            Assert.IsTrue(exception.Message.Contains("Expected id member not found."));
        }

        [TestMethod]
        public async Task UpdateAsyncWithZeroIdItem()
        {
            TestHttpHandler hijack = new TestHttpHandler();
            hijack.SetResponseContent("{\"id\":5,\"String\":\"Hey\"}");
            IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

            IMobileServiceTable table = service.GetTable("someTable");
            Exception exception = null;
            try
            {
                JObject obj = JToken.Parse("{\"id\":0,\"String\":\"what?\"}") as JObject;
                JToken item = await table.UpdateAsync(obj);
            }
            catch (Exception e)
            {
                exception = e;
            }

            Assert.IsNotNull(exception);
            Assert.IsTrue(exception.Message.Contains("The integer id '0' is not a positive integer value."));
        }

        [TestMethod]
        public async Task UpdateAsyncWithUserParameters()
        {
            var userDefinedParameters = new Dictionary<string, string>() { { "state", "FL" } };

            TestHttpHandler hijack = new TestHttpHandler();
            IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);
            IMobileServiceTable table = service.GetTable("tests");

            JObject obj = JToken.Parse("{\"id\":12,\"value\":\"new\"}") as JObject;
            hijack.SetResponseContent("{\"id\":12,\"value\":\"new\",\"other\":\"123\"}");
            JToken newObj = await table.UpdateAsync(obj, userDefinedParameters);

            Assert.AreEqual("123", (string)newObj["other"]);
            Assert.AreNotEqual(newObj, obj);
            StringAssert.Contains(hijack.Request.RequestUri.ToString(), "tests/12");
            StringAssert.Contains(hijack.Request.RequestUri.Query, "state=FL");
        }

        [TestMethod]
        public async Task UpdateAsyncThrowsWhenIdIsID()
        {
            MobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp);
            IMobileServiceTable table = service.GetTable("tests");
            ArgumentException expected = null;

            JObject obj = JToken.Parse("{\"ID\":5}") as JObject;
            try
            {
                await table.UpdateAsync(obj);
            }
            catch (ArgumentException e)
            {
                expected = e;
            }

            Assert.IsNotNull(expected);
            Assert.IsTrue(expected.Message.Contains("The casing of the 'id' property is invalid."));
        }

        [TestMethod]
        public async Task UpdateAsyncThrowsWhenIdIsId()
        {
            MobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp);
            IMobileServiceTable table = service.GetTable("tests");
            ArgumentException expected = null;

            JObject obj = JToken.Parse("{\"Id\":5}") as JObject;
            try
            {
                await table.UpdateAsync(obj);
            }
            catch (ArgumentException e)
            {
                expected = e;
            }

            Assert.IsNotNull(expected);
            Assert.IsTrue(expected.Message.Contains("The casing of the 'id' property is invalid."));
        }

        [TestMethod]
        public async Task UpdateAsyncThrowsWhenIdIsiD()
        {
            MobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp);
            IMobileServiceTable table = service.GetTable("tests");
            ArgumentException expected = null;

            JObject obj = JToken.Parse("{\"iD\":5}") as JObject;
            try
            {
                await table.UpdateAsync(obj);
            }
            catch (ArgumentException e)
            {
                expected = e;
            }

            Assert.IsNotNull(expected);
            Assert.IsTrue(expected.Message.Contains("The casing of the 'id' property is invalid."));
        }

        [TestMethod]
        public async Task UpdateAsyncWithStringId_KeepsSystemProperties()
        {
            TestHttpHandler hijack = new TestHttpHandler();
            IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);
            IMobileServiceTable table = service.GetTable("tests");

            JObject obj = JToken.Parse("{\"id\":\"A\",\"value\":\"new\"}") as JObject;
            hijack.SetResponseContent("{\"id\":\"A\",\"value\":\"new\", \"version\":\"XYZ\",\"__unknown\":12,\"CREATEDat\":\"12-02-02\"}");
            var newObj = await table.UpdateAsync(obj);

            Assert.AreEqual("A", (string)newObj["id"]);
            Assert.AreNotEqual(newObj, obj);
            Assert.IsNotNull(newObj["version"]);
            Assert.IsNotNull(newObj["__unknown"]);
            Assert.IsNotNull(newObj["CREATEDat"]);
        }

        [TestMethod]
        public async Task UpdateAsyncWithIntId_DoesNotStripSystemProperties()
        {
            var userDefinedParameters = new Dictionary<string, string>() { { "state", "AL" } };

            TestHttpHandler hijack = new TestHttpHandler();
            IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);
            IMobileServiceTable table = service.GetTable("tests");

            JObject obj = JToken.Parse("{\"id\":12,\"value\":\"new\"}") as JObject;
            hijack.SetResponseContent("{\"id\":12,\"value\":\"new\", \"version\":\"XYZ\",\"__unknown\":12,\"CREATEDat\":\"12-02-02\"}");
            JToken newObj = await table.UpdateAsync(obj, userDefinedParameters);

            Assert.AreEqual(12, (int)newObj["id"]);
            Assert.AreNotEqual(newObj, obj);
            Assert.IsNotNull(newObj["version"]);
            Assert.IsNotNull(newObj["__unknown"]);
            Assert.IsNotNull(newObj["CREATEDat"]);
        }

        [TestMethod]
        public async Task DeleteAsyncWithStringIdResponseContent()
        {
            string[] testIdData = IdTestData.ValidStringIds.Concat(
                                  IdTestData.EmptyStringIds).Concat(
                                  IdTestData.InvalidStringIds).ToArray();

            foreach (string testId in testIdData)
            {
                TestHttpHandler hijack = new TestHttpHandler();

                // Make the testId JSON safe
                string jsonTestId = testId.Replace("\\", "\\\\").Replace("\"", "\\\"");

                hijack.SetResponseContent("{\"id\":\"" + jsonTestId + "\",\"String\":\"Hey\"}");
                IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

                IMobileServiceTable table = service.GetTable("someTable");

                JObject obj = JToken.Parse("{\"id\":\"id\",\"value\":\"new\"}") as JObject;
                JObject item = await table.DeleteAsync(obj) as JObject;

                Assert.AreEqual(testId, (string)item["id"]);
                Assert.AreEqual("Hey", (string)item["String"]);
            }
        }

        [TestMethod]
        public async Task DeleteAsyncWithIntIdResponseContent()
        {
            long[] testIdData = IdTestData.ValidIntIds.Concat(
                                 IdTestData.InvalidIntIds).ToArray();

            foreach (long testId in testIdData)
            {
                string stringTestId = testId.ToString();

                TestHttpHandler hijack = new TestHttpHandler();
                hijack.SetResponseContent("{\"id\":" + stringTestId + ",\"String\":\"Hey\"}");
                IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

                IMobileServiceTable table = service.GetTable("someTable");

                JObject obj = JToken.Parse("{\"id\":\"id\",\"value\":\"new\"}") as JObject;
                JObject item = await table.DeleteAsync(obj) as JObject;

                Assert.AreEqual(testId, (long)item["id"]);
                Assert.AreEqual("Hey", (string)item["String"]);
            }
        }

        [TestMethod]
        public async Task DeleteAsyncWithNonStringAndNonIntIdResponseContent()
        {
            object[] testIdData = IdTestData.NonStringNonIntValidJsonIds;

            foreach (object testId in testIdData)
            {
                string stringTestId = testId.ToString().ToLower();

                TestHttpHandler hijack = new TestHttpHandler();
                hijack.SetResponseContent("{\"id\":" + stringTestId + ",\"String\":\"Hey\"}");
                IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

                IMobileServiceTable table = service.GetTable("someTable");

                JObject obj = JToken.Parse("{\"id\":\"id\",\"value\":\"new\"}") as JObject;
                JObject item = await table.DeleteAsync(obj) as JObject;

                object value = item["id"].ToObject(testId.GetType());

                Assert.AreEqual(testId, value);
                Assert.AreEqual("Hey", (string)item["String"]);
            }
        }

        [TestMethod]
        public async Task DeleteAsyncWithNullIdResponseContent()
        {
            TestHttpHandler hijack = new TestHttpHandler();
            hijack.SetResponseContent("{\"id\":null,\"String\":\"Hey\"}");
            IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

            IMobileServiceTable table = service.GetTable("someTable");

            JObject obj = JToken.Parse("{\"id\":\"id\",\"value\":\"new\"}") as JObject;
            JObject item = await table.DeleteAsync(obj) as JObject;

            Assert.AreEqual(null, (string)item["id"]);
            Assert.AreEqual("Hey", (string)item["String"]);
        }

        [TestMethod]
        public async Task DeleteAsyncWithNoIdResponseContent()
        {
            TestHttpHandler hijack = new TestHttpHandler();
            hijack.SetResponseContent("{\"String\":\"Hey\"}");
            IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

            IMobileServiceTable table = service.GetTable("someTable");

            JObject obj = JToken.Parse("{\"id\":\"id\",\"value\":\"new\"}") as JObject;
            JObject item = await table.DeleteAsync(obj) as JObject;

            Assert.IsFalse(item.Properties().Any(p => string.Equals(p.Name, "id", StringComparison.InvariantCultureIgnoreCase)));
            Assert.AreEqual("Hey", (string)item["String"]);
        }

        [TestMethod]
        public async Task DeleteAsyncWithStringIdItem()
        {
            string[] testIdData = IdTestData.ValidStringIds;

            foreach (string testId in testIdData)
            {
                TestHttpHandler hijack = new TestHttpHandler();
                hijack.SetResponseContent("{\"id\":\"" + testId + "\",\"String\":\"Hey\"}");
                IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

                IMobileServiceTable table = service.GetTable("someTable");

                JObject obj = JToken.Parse("{\"id\":\"" + testId + "\",\"String\":\"what?\"}") as JObject;
                JToken item = await table.DeleteAsync(obj);

                Assert.AreEqual(testId, (string)item["id"]);
                Assert.AreEqual("Hey", (string)item["String"]);
            }
        }

        [TestMethod]
        public async Task DeleteAsyncWithEmptyStringIdItem()
        {
            string[] testIdData = IdTestData.EmptyStringIds;

            foreach (string testId in testIdData)
            {
                TestHttpHandler hijack = new TestHttpHandler();
                hijack.SetResponseContent("{\"id\":\"an id\",\"String\":\"Hey\"}");
                IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

                IMobileServiceTable table = service.GetTable("someTable");
                Exception exception = null;
                try
                {
                    JObject obj = JToken.Parse("{\"id\":\"" + testId + "\",\"String\":\"what?\"}") as JObject;
                    JToken item = await table.DeleteAsync(obj);
                }
                catch (Exception e)
                {
                    exception = e;
                }

                Assert.IsNotNull(exception);
                Assert.IsTrue(exception.Message.Contains("The id can not be null or an empty string."));
            }
        }

        [TestMethod]
        public async Task DeleteAsyncWithInvalidStringIdItem()
        {
            string[] testIdData = IdTestData.InvalidStringIds;

            foreach (string testId in testIdData)
            {
                TestHttpHandler hijack = new TestHttpHandler();

                // Make the testId JSON safe
                string jsonTestId = testId.Replace("\\", "\\\\").Replace("\"", "\\\"");

                hijack.SetResponseContent("{\"id\":\"" + jsonTestId + "\",\"String\":\"Hey\"}");
                IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

                IMobileServiceTable table = service.GetTable("someTable");
                Exception exception = null;
                try
                {
                    JObject obj = JToken.Parse("{\"id\":\"" + jsonTestId + "\",\"String\":\"what?\"}") as JObject;
                    JToken item = await table.DeleteAsync(obj);
                }
                catch (Exception e)
                {
                    exception = e;
                }

                Assert.IsNotNull(exception);
                Assert.IsTrue(exception.Message.Contains("is longer than the max string id length of 255 characters") ||
                              exception.Message.Contains("An id must not contain any control characters or the characters"));
            }
        }

        [TestMethod]
        public async Task DeleteAsyncWithIntIdItem()
        {
            long[] testIdData = IdTestData.ValidIntIds;

            foreach (long testId in testIdData)
            {
                TestHttpHandler hijack = new TestHttpHandler();
                hijack.SetResponseContent("{\"id\":" + testId.ToString() + ",\"String\":\"Hey\"}");
                IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

                IMobileServiceTable table = service.GetTable("someTable");
                JObject obj = JToken.Parse("{\"id\":" + testId.ToString() + ",\"String\":\"what?\"}") as JObject;
                JToken item = await table.DeleteAsync(obj);

                Assert.AreEqual(testId, (long)item["id"]);
                Assert.AreEqual("Hey", (string)item["String"]);
            }
        }

        [TestMethod]
        public async Task DeleteAsyncWithNullIdItem()
        {
            TestHttpHandler hijack = new TestHttpHandler();
            hijack.SetResponseContent("{\"id\":5,\"String\":\"Hey\"}");
            IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

            IMobileServiceTable table = service.GetTable("someTable");
            Exception exception = null;
            try
            {
                JObject obj = JToken.Parse("{\"id\":null,\"String\":\"what?\"}") as JObject;
                JToken item = await table.DeleteAsync(obj);
            }
            catch (Exception e)
            {
                exception = e;
            }

            Assert.IsNotNull(exception);
            Assert.IsTrue(exception.Message.Contains("The id can not be null or an empty string."));
        }

        [TestMethod]
        public async Task DeleteAsyncWithNoIdItem()
        {
            TestHttpHandler hijack = new TestHttpHandler();
            hijack.SetResponseContent("{\"id\":5,\"String\":\"Hey\"}");
            IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

            IMobileServiceTable table = service.GetTable("someTable");
            Exception exception = null;
            try
            {
                JObject obj = JToken.Parse("{\"String\":\"what?\"}") as JObject;
                JToken item = await table.DeleteAsync(obj);
            }
            catch (Exception e)
            {
                exception = e;
            }

            Assert.IsNotNull(exception);
            Assert.IsTrue(exception.Message.Contains("Expected id member not found."));
        }

        [TestMethod]
        public async Task DeleteAsyncWithZeroIdItem()
        {
            TestHttpHandler hijack = new TestHttpHandler();
            hijack.SetResponseContent("{\"id\":5,\"String\":\"Hey\"}");
            IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

            IMobileServiceTable table = service.GetTable("someTable");
            Exception exception = null;
            try
            {
                JObject obj = JToken.Parse("{\"id\":0,\"String\":\"what?\"}") as JObject;
                JToken item = await table.DeleteAsync(obj);
            }
            catch (Exception e)
            {
                exception = e;
            }

            Assert.IsNotNull(exception);
            Assert.IsTrue(exception.Message.Contains("The integer id '0' is not a positive integer value"));
        }

        [TestMethod]
        public async Task DeleteAsyncWithUserParameters()
        {
            var userDefinedParameters = new Dictionary<string, string>() { { "state", "FL" } };

            TestHttpHandler hijack = new TestHttpHandler();
            IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);
            IMobileServiceTable table = service.GetTable("tests");

            JObject obj = JToken.Parse("{\"id\":12,\"value\":\"new\"}") as JObject;
            hijack.SetResponseContent("{\"id\":12,\"value\":\"new\",\"other\":\"123\"}");
            JToken newObj = await table.DeleteAsync(obj, userDefinedParameters);

            Assert.AreEqual("123", (string)newObj["other"]);
            Assert.AreNotEqual(newObj, obj);
            StringAssert.Contains(hijack.Request.RequestUri.ToString(), "tests/12");
            StringAssert.Contains(hijack.Request.RequestUri.Query, "state=FL");
        }

        [TestMethod]
        public async Task DeleteAsyncThrowsWhenIdIsID()
        {
            MobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp);
            IMobileServiceTable table = service.GetTable("tests");
            ArgumentException expected = null;

            JObject obj = JToken.Parse("{\"ID\":5}") as JObject;
            try
            {
                await table.DeleteAsync(obj);
            }
            catch (ArgumentException e)
            {
                expected = e;
            }

            Assert.IsNotNull(expected);
            Assert.IsTrue(expected.Message.Contains("The casing of the 'id' property is invalid."));
        }

        [TestMethod]
        public async Task DeleteAsyncThrowsWhenIdIsId()
        {
            MobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp);
            IMobileServiceTable table = service.GetTable("tests");
            ArgumentException expected = null;

            JObject obj = JToken.Parse("{\"Id\":5}") as JObject;
            try
            {
                await table.DeleteAsync(obj);
            }
            catch (ArgumentException e)
            {
                expected = e;
            }

            Assert.IsNotNull(expected);
            Assert.IsTrue(expected.Message.Contains("The casing of the 'id' property is invalid."));
        }

        [TestMethod]
        public async Task DeleteAsyncThrowsWhenIdIsiD()
        {
            MobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp);
            IMobileServiceTable table = service.GetTable("tests");
            ArgumentException expected = null;

            JObject obj = JToken.Parse("{\"iD\":5}") as JObject;
            try
            {
                await table.DeleteAsync(obj);
            }
            catch (ArgumentException e)
            {
                expected = e;
            }

            Assert.IsNotNull(expected);
            Assert.IsTrue(expected.Message.Contains("The casing of the 'id' property is invalid."));
        }

        [TestMethod]
        public async Task UndeleteAsync()
        {
            await TestUndeleteAsync("", null);
        }

        [TestMethod]
        public async Task UndeleteAsyncWithParameters()
        {
            await TestUndeleteAsync("?custom=value", new Dictionary<string, string>() { { "custom", "value" } });
        }

        private static async Task TestUndeleteAsync(string query, IDictionary<string, string> parameters)
        {
            TestHttpHandler hijack = new TestHttpHandler();

            hijack.SetResponseContent("{\"id\":\"an id\",\"String\":\"Hey\"}");
            hijack.OnSendingRequest = req =>
            {
                Assert.AreEqual(req.RequestUri.Query, query);
                Assert.AreEqual(req.Method, HttpMethod.Post);

                // only id and version should be sent
                Assert.IsNull(req.Content);
                Assert.AreEqual(req.Headers.IfMatch.First().Tag, "\"abc\"");
                return Task.FromResult(req);
            };
            IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

            IMobileServiceTable table = service.GetTable("someTable");

            var obj = JToken.Parse("{\"id\":\"id\",\"value\":\"new\", \"blah\":\"doh\", \"version\": \"abc\"}") as JObject;
            JObject item = await table.UndeleteAsync(obj, parameters) as JObject;

            Assert.AreEqual("an id", (string)item["id"]);
            Assert.AreEqual("Hey", (string)item["String"]);
        }

        [TestMethod]
        public async Task DeleteAsyncWithStringId_KeepsSystemProperties()
        {
            var userDefinedParameters = new Dictionary<string, string>() { { "__systemproperties", "createdAt" } };

            TestHttpHandler hijack = new TestHttpHandler();
            IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);
            IMobileServiceTable table = service.GetTable("tests");

            JObject obj = JToken.Parse("{\"id\":\"A\",\"value\":\"new\"}") as JObject;
            hijack.SetResponseContent("{\"id\":\"A\",\"value\":\"new\", \"version\":\"XYZ\",\"__unknown\":12,\"CREATEDat\":\"12-02-02\"}");
            JToken newObj = await table.DeleteAsync(obj);

            Assert.AreEqual("A", (string)newObj["id"]);
            Assert.AreNotEqual(newObj, obj);
            Assert.IsNotNull(newObj["version"]);
            Assert.IsNotNull(newObj["__unknown"]);
            Assert.IsNotNull(newObj["CREATEDat"]);
        }

        [TestMethod]
        public async Task DeleteAsyncWithIntId_DoesNotStripSystemProperties()
        {
            var userDefinedParameters = new Dictionary<string, string>() { { "state", "AL" } };

            TestHttpHandler hijack = new TestHttpHandler();
            IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);
            IMobileServiceTable table = service.GetTable("tests");

            JObject obj = JToken.Parse("{\"id\":12,\"value\":\"new\"}") as JObject;
            hijack.SetResponseContent("{\"id\":12,\"value\":\"new\", \"version\":\"XYZ\",\"__unknown\":12,\"CREATEDat\":\"12-02-02\"}");
            JToken newObj = await table.DeleteAsync(obj, userDefinedParameters);

            Assert.AreEqual(12, (int)newObj["id"]);
            Assert.AreNotEqual(newObj, obj);
            Assert.IsNotNull(newObj["version"]);
            Assert.IsNotNull(newObj["__unknown"]);
            Assert.IsNotNull(newObj["CREATEDat"]);
        }

        [TestMethod]
        public async Task InsertAsync_RemovesSystemProperties_WhenIdIsString_Generic()
        {
            string[] testSystemProperties = SystemPropertiesTestData.ValidSystemProperties;

            foreach (string testSystemProperty in testSystemProperties)
            {
                TestHttpHandler hijack = new TestHttpHandler();
                hijack.SetResponseContent("{\"id\":\"an id\",\"String\":\"Hey\"}");
                var service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);
                hijack.OnSendingRequest = async (request) =>
                {
                    string content = await request.Content.ReadAsStringAsync();
                    JObject obj = JToken.Parse(content) as JObject;
                    Assert.IsTrue(obj.Properties().Where(p => p.Name == "id").Any());
                    Assert.IsTrue(obj.Properties().Where(p => p.Name == "String").Any());
                    Assert.IsFalse(obj.Properties().Where(p => p.Name == testSystemProperty).Any());
                    return request;
                };
                var table = service.GetTable<ToDoWithSystemPropertiesType>();

                JObject itemToInsert = new JObject();
                if (testSystemProperty.Contains("deleted"))
                {
                    itemToInsert = JToken.Parse("{\"id\":\"an id\",\"String\":\"what?\",\"" + testSystemProperty + "\":\"false\"}") as JObject;
                }
                else
                {
                    itemToInsert = JToken.Parse("{\"id\":\"an id\",\"String\":\"what?\",\"" + testSystemProperty + "\":\"2015-09-26\"}") as JObject;
                }
                var typedItemToInsert = itemToInsert.ToObject<ToDoWithSystemPropertiesType>();
                await table.InsertAsync(typedItemToInsert);
            }
        }

        [TestMethod]
        public async Task InsertAsync_DoesNotRemoveSystemProperties_WhenIdIsString()
        {
            await InsertAsync_DoesNotRemoveSystemPropertiesTest(client => client.GetTable("some"));
        }

        [TestMethod]
        public async Task InsertAsync_JToken_DoesNotRemoveSystemProperties_WhenIdIsString_Generic()
        {
            await InsertAsync_DoesNotRemoveSystemPropertiesTest(client => client.GetTable<ToDoWithSystemPropertiesType>());
        }

        private static async Task InsertAsync_DoesNotRemoveSystemPropertiesTest(Func<IMobileServiceClient, IMobileServiceTable> getTable)
        {
            string[] testSystemProperties = SystemPropertiesTestData.ValidSystemProperties;

            foreach (string testSystemProperty in testSystemProperties)
            {
                TestHttpHandler hijack = new TestHttpHandler();
                hijack.SetResponseContent("{\"id\":\"an id\",\"String\":\"Hey\"}");
                IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);
                IMobileServiceTable table = getTable(service);
                hijack.OnSendingRequest = async (request) =>
                {
                    string content = await request.Content.ReadAsStringAsync();
                    JObject obj = JToken.Parse(content) as JObject;
                    Assert.IsTrue(obj.Properties().Where(p => p.Name == "id").Any());
                    Assert.IsTrue(obj.Properties().Where(p => p.Name == "String").Any());
                    Assert.IsTrue(obj.Properties().Where(p => p.Name == testSystemProperty).Any());
                    return request;
                };

                JObject itemToInsert = JToken.Parse("{\"id\":\"an id\",\"String\":\"what?\",\"" + testSystemProperty + "\":\"a value\"}") as JObject;
                await table.InsertAsync(itemToInsert);
            }
        }

        [TestMethod]
        public async Task InsertAsyncStringIdNonSystemPropertiesNotRemovedFromRequest()
        {
            string[] testSystemProperties = SystemPropertiesTestData.NonSystemProperties;

            foreach (string testSystemProperty in testSystemProperties)
            {
                TestHttpHandler hijack = new TestHttpHandler();
                hijack.SetResponseContent("{\"id\":\"an id\",\"String\":\"Hey\",\"" + testSystemProperty + "\":\"a value\"}");
                IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

                IMobileServiceTable table = service.GetTable("someTable");

                hijack.OnSendingRequest = async (request) =>
                {
                    string content = await request.Content.ReadAsStringAsync();
                    JObject obj = JToken.Parse(content) as JObject;
                    Assert.IsTrue(obj.Properties().Where(p => p.Name == "id").Any());
                    Assert.IsTrue(obj.Properties().Where(p => p.Name == "String").Any());
                    Assert.IsTrue(obj.Properties().Where(p => p.Name == testSystemProperty).Any());
                    return request;
                };

                JObject itemToInsert = JToken.Parse("{\"id\":\"an id\",\"String\":\"what?\",\"" + testSystemProperty + "\":\"a value\"}") as JObject;
                await table.InsertAsync(itemToInsert);
            }
        }

        [TestMethod]
        public async Task InsertAsync_DoesNotRemoveSystemProperties_WhenIdIsNull()
        {
            string[] testSystemProperties = SystemPropertiesTestData.ValidSystemProperties;

            foreach (string testSystemProperty in testSystemProperties)
            {
                TestHttpHandler hijack = new TestHttpHandler();
                hijack.SetResponseContent("{\"id\":\"an id\",\"String\":\"Hey\"}");
                IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

                IMobileServiceTable table = service.GetTable("someTable");

                hijack.OnSendingRequest = async (request) =>
                {
                    string content = await request.Content.ReadAsStringAsync();
                    JObject obj = JToken.Parse(content) as JObject;
                    Assert.IsTrue(obj.Properties().Where(p => p.Name == "id").Any());
                    Assert.IsTrue(obj.Properties().Where(p => p.Name == "String").Any());
                    Assert.IsTrue(obj.Properties().Where(p => p.Name == testSystemProperty).Any());
                    return request;
                };

                JObject itemToInsert = JToken.Parse("{\"id\":null,\"String\":\"what?\",\"" + testSystemProperty + "\":\"a value\"}") as JObject;
                await table.InsertAsync(itemToInsert);
            }
        }

        [TestMethod]
        public async Task InsertAsyncNullIdNonSystemPropertiesNotRemovedFromRequest()
        {
            string[] testSystemProperties = SystemPropertiesTestData.NonSystemProperties;

            foreach (string testSystemProperty in testSystemProperties)
            {
                TestHttpHandler hijack = new TestHttpHandler();
                hijack.SetResponseContent("{\"id\":\"an id\",\"String\":\"Hey\",\"" + testSystemProperty + "\":\"a value\"}");
                IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

                IMobileServiceTable table = service.GetTable("someTable");

                hijack.OnSendingRequest = async (request) =>
                {
                    string content = await request.Content.ReadAsStringAsync();
                    JObject obj = JToken.Parse(content) as JObject;
                    Assert.IsTrue(obj.Properties().Where(p => p.Name == "id").Any());
                    Assert.IsTrue(obj.Properties().Where(p => p.Name == "String").Any());
                    Assert.IsTrue(obj.Properties().Where(p => p.Name == testSystemProperty).Any());
                    return request;
                };

                JObject itemToInsert = JToken.Parse("{\"id\":null,\"String\":\"what?\",\"" + testSystemProperty + "\":\"a value\"}") as JObject;
                await table.InsertAsync(itemToInsert);
            }
        }

        [TestMethod]
        public async Task UpdateAsyncStringIdSystemPropertiesRemovedFromRequest()
        {
            string[] testSystemProperties = SystemPropertiesTestData.ValidSystemProperties;

            foreach (string testSystemProperty in testSystemProperties)
            {
                TestHttpHandler hijack = new TestHttpHandler();
                hijack.SetResponseContent("{\"id\":\"an id\",\"String\":\"Hey\"}");
                IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

                IMobileServiceTable table = service.GetTable("someTable");

                hijack.OnSendingRequest = async (request) =>
                {
                    string content = await request.Content.ReadAsStringAsync();
                    JObject obj = JToken.Parse(content) as JObject;
                    Assert.IsTrue(obj.Properties().Where(p => p.Name == "id").Any());
                    Assert.IsTrue(obj.Properties().Where(p => p.Name == "String").Any());
                    Assert.IsFalse(obj.Properties().Where(p => p.Name == testSystemProperty).Any());
                    return request;
                };

                JObject itemToUpdate = JToken.Parse("{\"id\":\"an id\",\"String\":\"what?\",\"" + testSystemProperty + "\":\"a value\"}") as JObject;
                await table.UpdateAsync(itemToUpdate);
            }
        }

        [TestMethod]
        public async Task UpdateAsyncStringIdNonSystemPropertiesNotRemovedFromRequest()
        {
            string[] testSystemProperties = SystemPropertiesTestData.NonSystemProperties;

            foreach (string testSystemProperty in testSystemProperties)
            {
                TestHttpHandler hijack = new TestHttpHandler();
                hijack.SetResponseContent("{\"id\":\"an id\",\"String\":\"Hey\",\"" + testSystemProperty + "\":\"a value\"}");
                IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);

                IMobileServiceTable table = service.GetTable("someTable");

                hijack.OnSendingRequest = async (request) =>
                {
                    string content = await request.Content.ReadAsStringAsync();
                    JObject obj = JToken.Parse(content) as JObject;
                    Assert.IsTrue(obj.Properties().Where(p => p.Name == "id").Any());
                    Assert.IsTrue(obj.Properties().Where(p => p.Name == "String").Any());
                    Assert.IsTrue(obj.Properties().Where(p => p.Name == testSystemProperty).Any());
                    return request;
                };

                JObject itemToUpdate = JToken.Parse("{\"id\":\"an id\",\"String\":\"what?\",\"" + testSystemProperty + "\":\"a value\"}") as JObject;
                await table.UpdateAsync(itemToUpdate);
            }
        }

        [TestMethod]
        public Task FeatureHeaderValidation_UntypedTableRead()
        {
            return this.ValidateFeaturesHeader("TU", true, t => t.ReadAsync(""));
        }

        [TestMethod]
        public Task FeatureHeaderValidation_UntypedTableLookup()
        {
            return this.ValidateFeaturesHeader("TU", false, t => t.LookupAsync("id"));
        }

        [TestMethod]
        public Task FeatureHeaderValidation_UntypedTableInsert()
        {
            JObject obj = JObject.Parse("{\"id\":\"the id\",\"value\":\"new\"}");
            return this.ValidateFeaturesHeader("TU", false, t => t.InsertAsync(obj));
        }

        [TestMethod]
        public Task FeatureHeaderValidation_UntypedTableUpdate()
        {
            JObject obj = JObject.Parse("{\"id\":\"the id\",\"value\":\"new\"}");
            return this.ValidateFeaturesHeader("TU", false, t => t.UpdateAsync(obj));
        }

        [TestMethod]
        public Task FeatureHeaderValidation_UntypedTableDelete()
        {
            JObject obj = JObject.Parse("{\"id\":\"the id\",\"value\":\"new\"}");
            return this.ValidateFeaturesHeader("TU", false, t => t.DeleteAsync(obj));
        }

        [TestMethod]
        public Task FeatureHeaderValidation_UntypedTableReadWithQuery()
        {
            return this.ValidateFeaturesHeader("TU,QS", true, t => t.ReadAsync("", new Dictionary<string, string> { { "a", "b" } }));
        }

        [TestMethod]
        public Task FeatureHeaderValidation_UntypedTableLookupWithQuery_FeaturesHeaderIsCorrect()
        {
            return this.ValidateFeaturesHeader("TU,QS", false, t => t.LookupAsync("id", new Dictionary<string, string> { { "a", "b" } }));
        }

        [TestMethod]
        public Task FeatureHeaderValidation_UntypedTableInsertWithQuery()
        {
            JObject obj = JObject.Parse("{\"id\":\"the id\",\"value\":\"new\"}");
            return this.ValidateFeaturesHeader("TU,QS", false, t => t.InsertAsync(obj, new Dictionary<string, string> { { "a", "b" } }));
        }

        [TestMethod]
        public Task FeatureHeaderValidation_UntypedTableUpdateWithQuery()
        {
            JObject obj = JObject.Parse("{\"id\":\"the id\",\"value\":\"new\"}");
            return this.ValidateFeaturesHeader("TU,QS", false, t => t.UpdateAsync(obj, new Dictionary<string, string> { { "a", "b" } }));
        }

        [TestMethod]
        public Task FeatureHeaderValidation_UntypedTableDeleteWithQuery()
        {
            JObject obj = JObject.Parse("{\"id\":\"the id\",\"value\":\"new\"}");
            return this.ValidateFeaturesHeader("TU,QS", false, t => t.DeleteAsync(obj, new Dictionary<string, string> { { "a", "b" } }));
        }

        private async Task ValidateFeaturesHeader(string expectedFeaturesHeader, bool arrayResponse, Func<IMobileServiceTable, Task> operation)
        {
            TestHttpHandler hijack = new TestHttpHandler();
            bool validationDone = false;
            hijack.OnSendingRequest = (request) =>
            {
                Assert.AreEqual(expectedFeaturesHeader, request.Headers.GetValues("X-ZUMO-FEATURES").First());
                validationDone = true;
                return Task.FromResult(request);
            };

            IMobileServiceClient service = new MobileServiceClient(MobileAppUriValidator.DummyMobileApp, hijack);
            IMobileServiceTable table = service.GetTable("someTable");

            var responseContent = "{\"id\":\"the id\",\"String\":\"Hey\"}";
            if (arrayResponse)
            {
                responseContent = "[" + responseContent + "]";
            }

            hijack.SetResponseContent(responseContent);
            await operation(table);
            Assert.IsTrue(validationDone);
        }
    }
}
