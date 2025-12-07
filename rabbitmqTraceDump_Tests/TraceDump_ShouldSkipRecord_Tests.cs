using System;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using rabbitmq_trace_dump;

namespace rabbitmqTraceDump_Tests
{
    [TestFixture]
    public class TraceDump_ShouldSkipRecord_Tests
    {
        #region No Filter Tests

        [Test]
        public void ShouldSkipRecord_NoSearchKey_ReturnsFalse()
        {
            var options = new RunSettings { SearchKey = null };
            var traceDump = new TraceDump(options);
            var jobject = JObject.Parse("""{ "exchange": "my-exchange" }""");

            var result = traceDump.ShouldSkipRecord(jobject, out bool payloadDecoded);

            result.Should().BeFalse();
            payloadDecoded.Should().BeFalse();
        }

        #endregion

        #region Equals Operator Tests

        [Test]
        public void ShouldSkipRecord_EqualsOperator_MatchingValue_ReturnsFalse()
        {
            var options = new RunSettings
            {
                SearchKey = "exchange",
                SearchValue = "my-exchange",
                SearchOp = SearchOperator.Equals
            };
            var traceDump = new TraceDump(options);
            var jobject = JObject.Parse("""{ "exchange": "my-exchange" }""");

            var result = traceDump.ShouldSkipRecord(jobject, out bool payloadDecoded);

            result.Should().BeFalse();
            payloadDecoded.Should().BeFalse();
        }

        [Test]
        public void ShouldSkipRecord_EqualsOperator_NonMatchingValue_ReturnsTrue()
        {
            var options = new RunSettings
            {
                SearchKey = "exchange",
                SearchValue = "other-exchange",
                SearchOp = SearchOperator.Equals
            };
            var traceDump = new TraceDump(options);
            var jobject = JObject.Parse("""{ "exchange": "my-exchange" }""");

            var result = traceDump.ShouldSkipRecord(jobject, out bool payloadDecoded);

            result.Should().BeTrue();
        }

        [Test]
        public void ShouldSkipRecord_EqualsOperator_CaseInsensitive_ReturnsFalse()
        {
            var options = new RunSettings
            {
                SearchKey = "exchange",
                SearchValue = "MY-EXCHANGE",
                SearchOp = SearchOperator.Equals
            };
            var traceDump = new TraceDump(options);
            var jobject = JObject.Parse("""{ "exchange": "my-exchange" }""");

            var result = traceDump.ShouldSkipRecord(jobject, out bool payloadDecoded);

            result.Should().BeFalse();
        }

        [Test]
        public void ShouldSkipRecord_EqualsOperator_KeyNotFound_ReturnsTrue()
        {
            var options = new RunSettings
            {
                SearchKey = "nonexistent",
                SearchValue = "value",
                SearchOp = SearchOperator.Equals
            };
            var traceDump = new TraceDump(options);
            var jobject = JObject.Parse("""{ "exchange": "my-exchange" }""");

            var result = traceDump.ShouldSkipRecord(jobject, out bool payloadDecoded);

            result.Should().BeTrue();
        }

        #endregion

        #region NotEquals Operator Tests

        [Test]
        public void ShouldSkipRecord_NotEqualsOperator_DifferentValue_ReturnsFalse()
        {
            var options = new RunSettings
            {
                SearchKey = "type",
                SearchValue = "deliver",
                SearchOp = SearchOperator.NotEquals
            };
            var traceDump = new TraceDump(options);
            var jobject = JObject.Parse("""{ "type": "publish" }""");

            var result = traceDump.ShouldSkipRecord(jobject, out bool payloadDecoded);

            result.Should().BeFalse();
        }

        [Test]
        public void ShouldSkipRecord_NotEqualsOperator_SameValue_ReturnsTrue()
        {
            var options = new RunSettings
            {
                SearchKey = "type",
                SearchValue = "publish",
                SearchOp = SearchOperator.NotEquals
            };
            var traceDump = new TraceDump(options);
            var jobject = JObject.Parse("""{ "type": "publish" }""");

            var result = traceDump.ShouldSkipRecord(jobject, out bool payloadDecoded);

            result.Should().BeTrue();
        }

        #endregion

        #region Contains Operator Tests

        [Test]
        public void ShouldSkipRecord_ContainsOperator_SubstringMatch_ReturnsFalse()
        {
            var options = new RunSettings
            {
                SearchKey = "routing_key",
                SearchValue = "order",
                SearchOp = SearchOperator.Contains
            };
            var traceDump = new TraceDump(options);
            var jobject = JObject.Parse("""{ "routing_key": "orders.created" }""");

            var result = traceDump.ShouldSkipRecord(jobject, out bool payloadDecoded);

            result.Should().BeFalse();
        }

        [Test]
        public void ShouldSkipRecord_ContainsOperator_NoMatch_ReturnsTrue()
        {
            var options = new RunSettings
            {
                SearchKey = "routing_key",
                SearchValue = "product",
                SearchOp = SearchOperator.Contains
            };
            var traceDump = new TraceDump(options);
            var jobject = JObject.Parse("""{ "routing_key": "orders.created" }""");

            var result = traceDump.ShouldSkipRecord(jobject, out bool payloadDecoded);

            result.Should().BeTrue();
        }

        [Test]
        public void ShouldSkipRecord_ContainsOperator_CaseInsensitive_ReturnsFalse()
        {
            var options = new RunSettings
            {
                SearchKey = "routing_key",
                SearchValue = "ORDER",
                SearchOp = SearchOperator.Contains
            };
            var traceDump = new TraceDump(options);
            var jobject = JObject.Parse("""{ "routing_key": "orders.created" }""");

            var result = traceDump.ShouldSkipRecord(jobject, out bool payloadDecoded);

            result.Should().BeFalse();
        }

        #endregion

        #region StartsWith Operator Tests

        [Test]
        public void ShouldSkipRecord_StartsWithOperator_MatchingPrefix_ReturnsFalse()
        {
            var options = new RunSettings
            {
                SearchKey = "routing_key",
                SearchValue = "orders.",
                SearchOp = SearchOperator.StartsWith
            };
            var traceDump = new TraceDump(options);
            var jobject = JObject.Parse("""{ "routing_key": "orders.created" }""");

            var result = traceDump.ShouldSkipRecord(jobject, out bool payloadDecoded);

            result.Should().BeFalse();
        }

        [Test]
        public void ShouldSkipRecord_StartsWithOperator_NonMatchingPrefix_ReturnsTrue()
        {
            var options = new RunSettings
            {
                SearchKey = "routing_key",
                SearchValue = "products.",
                SearchOp = SearchOperator.StartsWith
            };
            var traceDump = new TraceDump(options);
            var jobject = JObject.Parse("""{ "routing_key": "orders.created" }""");

            var result = traceDump.ShouldSkipRecord(jobject, out bool payloadDecoded);

            result.Should().BeTrue();
        }

        #endregion

        #region EndsWith Operator Tests

        [Test]
        public void ShouldSkipRecord_EndsWithOperator_MatchingSuffix_ReturnsFalse()
        {
            var options = new RunSettings
            {
                SearchKey = "routing_key",
                SearchValue = ".created",
                SearchOp = SearchOperator.EndsWith
            };
            var traceDump = new TraceDump(options);
            var jobject = JObject.Parse("""{ "routing_key": "orders.created" }""");

            var result = traceDump.ShouldSkipRecord(jobject, out bool payloadDecoded);

            result.Should().BeFalse();
        }

        [Test]
        public void ShouldSkipRecord_EndsWithOperator_NonMatchingSuffix_ReturnsTrue()
        {
            var options = new RunSettings
            {
                SearchKey = "routing_key",
                SearchValue = ".updated",
                SearchOp = SearchOperator.EndsWith
            };
            var traceDump = new TraceDump(options);
            var jobject = JObject.Parse("""{ "routing_key": "orders.created" }""");

            var result = traceDump.ShouldSkipRecord(jobject, out bool payloadDecoded);

            result.Should().BeTrue();
        }

        #endregion

        #region Regex Operator Tests

        [Test]
        public void ShouldSkipRecord_RegexOperator_MatchingPattern_ReturnsFalse()
        {
            var options = new RunSettings
            {
                SearchKey = "routing_key",
                SearchValue = """^orders\.\w+$""",
                SearchOp = SearchOperator.Regex
            };
            var traceDump = new TraceDump(options);
            var jobject = JObject.Parse("""{ "routing_key": "orders.created" }""");

            var result = traceDump.ShouldSkipRecord(jobject, out bool payloadDecoded);

            result.Should().BeFalse();
        }

        [Test]
        public void ShouldSkipRecord_RegexOperator_NonMatchingPattern_ReturnsTrue()
        {
            var options = new RunSettings
            {
                SearchKey = "routing_key",
                SearchValue = """^products\.\d+$""",
                SearchOp = SearchOperator.Regex
            };
            var traceDump = new TraceDump(options);
            var jobject = JObject.Parse("""{ "routing_key": "orders.created" }""");

            var result = traceDump.ShouldSkipRecord(jobject, out bool payloadDecoded);

            result.Should().BeTrue();
        }

        [Test]
        public void ShouldSkipRecord_RegexOperator_InvalidPattern_ReturnsTrue()
        {
            var options = new RunSettings
            {
                SearchKey = "routing_key",
                SearchValue = "[invalid(regex",
                SearchOp = SearchOperator.Regex
            };
            var traceDump = new TraceDump(options);
            var jobject = JObject.Parse("""{ "routing_key": "orders.created" }""");

            var result = traceDump.ShouldSkipRecord(jobject, out bool payloadDecoded);

            result.Should().BeTrue();
        }

        #endregion

        #region JSONPath / Nested Property Tests

        [Test]
        public void ShouldSkipRecord_NestedProperty_UsingSelectToken_ReturnsFalse()
        {
            var options = new RunSettings
            {
                SearchKey = "properties.headers.x-custom",
                SearchValue = "test-value",
                SearchOp = SearchOperator.Equals
            };
            var traceDump = new TraceDump(options);
            var jobject = JObject.Parse("""
                { 
                    "exchange": "my-exchange",
                    "properties": {
                        "headers": {
                            "x-custom": "test-value"
                        }
                    }
                }
                """);

            var result = traceDump.ShouldSkipRecord(jobject, out bool payloadDecoded);

            result.Should().BeFalse();
        }

        [Test]
        public void ShouldSkipRecord_PayloadKey_SetsPayloadDecodedTrue()
        {
            var options = new RunSettings
            {
                SearchKey = "payload.someField",
                SearchValue = "value",
                SearchOp = SearchOperator.Equals
            };
            var traceDump = new TraceDump(options);
            var jobject = JObject.Parse("""{ "payload": "" }""");

            traceDump.ShouldSkipRecord(jobject, out bool payloadDecoded);

            payloadDecoded.Should().BeTrue();
        }

        #endregion

        #region Edge Cases

        [Test]
        public void ShouldSkipRecord_EmptyStringValue_EqualsEmpty_ReturnsFalse()
        {
            var options = new RunSettings
            {
                SearchKey = "exchange",
                SearchValue = "",
                SearchOp = SearchOperator.Equals
            };
            var traceDump = new TraceDump(options);
            var jobject = JObject.Parse("""{ "exchange": "" }""");

            var result = traceDump.ShouldSkipRecord(jobject, out bool payloadDecoded);

            result.Should().BeFalse();
        }

        [Test]
        public void ShouldSkipRecord_NullTokenValue_WithEmptyExpected_ReturnsFalse()
        {
            var options = new RunSettings
            {
                SearchKey = "exchange",
                SearchValue = "",
                SearchOp = SearchOperator.Equals
            };
            var traceDump = new TraceDump(options);
            var jobject = JObject.Parse("""{ "exchange": null }""");

            var result = traceDump.ShouldSkipRecord(jobject, out bool payloadDecoded);

            result.Should().BeFalse();
        }

        [Test]
        public void ShouldSkipRecord_NullTokenValue_WithNonEmptyExpected_ReturnsTrue()
        {
            var options = new RunSettings
            {
                SearchKey = "exchange",
                SearchValue = "value",
                SearchOp = SearchOperator.Equals
            };
            var traceDump = new TraceDump(options);
            var jobject = JObject.Parse("""{ "exchange": null }""");

            var result = traceDump.ShouldSkipRecord(jobject, out bool payloadDecoded);

            result.Should().BeTrue();
        }

        #endregion
    }
}