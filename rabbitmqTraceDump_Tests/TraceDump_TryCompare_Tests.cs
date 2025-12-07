using System;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using rabbitmq_trace_dump;

namespace rabbitmqTraceDump_Tests
{
    [TestFixture]
    public class TraceDump_TryCompare_Tests
    {
        private TraceDump _traceDump;

        [SetUp]
        public void SetUp()
        {
            _traceDump = new TraceDump(new ProgramOptions());
        }

        #region String Type Tests

        [Test]
        public void TryCompare_StringEquals_Match_ReturnsZero()
        {
            var token = JToken.FromObject("hello");

            var result = _traceDump.TryCompare(token, "hello", SearchOperator.Equals);

            result.Should().Be(0);
        }

        [Test]
        public void TryCompare_StringEquals_CaseInsensitive_ReturnsZero()
        {
            var token = JToken.FromObject("Hello");

            var result = _traceDump.TryCompare(token, "HELLO", SearchOperator.Equals);

            result.Should().Be(0);
        }

        [Test]
        public void TryCompare_StringEquals_NoMatch_ReturnsNonZero()
        {
            var token = JToken.FromObject("hello");

            var result = _traceDump.TryCompare(token, "world", SearchOperator.Equals);

            result.Should().NotBe(0);
        }

        [Test]
        public void TryCompare_StringContains_Match_ReturnsZero()
        {
            var token = JToken.FromObject("hello world");

            var result = _traceDump.TryCompare(token, "world", SearchOperator.Contains);

            result.Should().Be(0);
        }

        [Test]
        public void TryCompare_StringContains_NoMatch_ReturnsNonZero()
        {
            var token = JToken.FromObject("hello world");

            var result = _traceDump.TryCompare(token, "foo", SearchOperator.Contains);

            result.Should().NotBe(0);
        }

        [Test]
        public void TryCompare_StringNotEquals_Different_ReturnsZero()
        {
            var token = JToken.FromObject("hello");

            var result = _traceDump.TryCompare(token, "world", SearchOperator.NotEquals);

            result.Should().Be(0);
        }

        [Test]
        public void TryCompare_StringNotEquals_Same_ReturnsNonZero()
        {
            var token = JToken.FromObject("hello");

            var result = _traceDump.TryCompare(token, "hello", SearchOperator.NotEquals);

            result.Should().NotBe(0);
        }

        [Test]
        public void TryCompare_StringStartsWith_Match_ReturnsZero()
        {
            var token = JToken.FromObject("hello world");

            var result = _traceDump.TryCompare(token, "hello", SearchOperator.StartsWith);

            result.Should().Be(0);
        }

        [Test]
        public void TryCompare_StringStartsWith_NoMatch_ReturnsNonZero()
        {
            var token = JToken.FromObject("hello world");

            var result = _traceDump.TryCompare(token, "world", SearchOperator.StartsWith);

            result.Should().NotBe(0);
        }

        [Test]
        public void TryCompare_StringEndsWith_Match_ReturnsZero()
        {
            var token = JToken.FromObject("hello world");

            var result = _traceDump.TryCompare(token, "world", SearchOperator.EndsWith);

            result.Should().Be(0);
        }

        [Test]
        public void TryCompare_StringEndsWith_NoMatch_ReturnsNonZero()
        {
            var token = JToken.FromObject("hello world");

            var result = _traceDump.TryCompare(token, "hello", SearchOperator.EndsWith);

            result.Should().NotBe(0);
        }

        [Test]
        public void TryCompare_StringRegex_Match_ReturnsZero()
        {
            var token = JToken.FromObject("order-12345");

            var result = _traceDump.TryCompare(token, """^order-\d+$""", SearchOperator.Regex);

            result.Should().Be(0);
        }

        [Test]
        public void TryCompare_StringRegex_NoMatch_ReturnsNonZero()
        {
            var token = JToken.FromObject("order-abc");

            var result = _traceDump.TryCompare(token, """^order-\d+$""", SearchOperator.Regex);

            result.Should().NotBe(0);
        }

        [Test]
        public void TryCompare_StringRegex_InvalidPattern_ReturnsNonZero()
        {
            var token = JToken.FromObject("test");

            var result = _traceDump.TryCompare(token, "[invalid(", SearchOperator.Regex);

            result.Should().NotBe(0);
        }

        #endregion

        #region Integer Type Tests

        [Test]
        public void TryCompare_IntegerEquals_Match_ReturnsZero()
        {
            var token = JToken.FromObject(42);

            var result = _traceDump.TryCompare(token, "42", SearchOperator.Equals);

            result.Should().Be(0);
        }

        [Test]
        public void TryCompare_IntegerEquals_NoMatch_ReturnsNonZero()
        {
            var token = JToken.FromObject(42);

            var result = _traceDump.TryCompare(token, "99", SearchOperator.Equals);

            result.Should().NotBe(0);
        }

        [Test]
        public void TryCompare_IntegerContains_Match_ReturnsZero()
        {
            var token = JToken.FromObject(12345);

            var result = _traceDump.TryCompare(token, "234", SearchOperator.Contains);

            result.Should().Be(0);
        }

        #endregion

        #region Float Type Tests

        [Test]
        public void TryCompare_FloatEquals_Match_ReturnsZero()
        {
            var token = JToken.FromObject(3.14);

            var result = _traceDump.TryCompare(token, "3.14", SearchOperator.Equals);

            result.Should().Be(0);
        }

        [Test]
        public void TryCompare_FloatContains_Match_ReturnsZero()
        {
            var token = JToken.FromObject(3.14159);

            var result = _traceDump.TryCompare(token, "14", SearchOperator.Contains);

            result.Should().Be(0);
        }

        #endregion

        #region Boolean Type Tests

        [Test]
        public void TryCompare_BooleanTrue_EqualsTrue_ReturnsZero()
        {
            var token = JToken.FromObject(true);

            var result = _traceDump.TryCompare(token, "True", SearchOperator.Equals);

            result.Should().Be(0);
        }

        [Test]
        public void TryCompare_BooleanFalse_EqualsFalse_ReturnsZero()
        {
            var token = JToken.FromObject(false);

            var result = _traceDump.TryCompare(token, "False", SearchOperator.Equals);

            result.Should().Be(0);
        }

        [Test]
        public void TryCompare_BooleanTrue_EqualsFalse_ReturnsNonZero()
        {
            var token = JToken.FromObject(true);

            var result = _traceDump.TryCompare(token, "False", SearchOperator.Equals);

            result.Should().NotBe(0);
        }

        #endregion

        #region Null Type Tests

        [Test]
        public void TryCompare_NullToken_EmptyExpected_ReturnsZero()
        {
            var token = JValue.CreateNull();

            var result = _traceDump.TryCompare(token, "", SearchOperator.Equals);

            result.Should().Be(0);
        }

        [Test]
        public void TryCompare_NullToken_NullExpected_ReturnsZero()
        {
            var token = JValue.CreateNull();

            var result = _traceDump.TryCompare(token, null, SearchOperator.Equals);

            result.Should().Be(0);
        }

        [Test]
        public void TryCompare_NullToken_NonEmptyExpected_ReturnsNonZero()
        {
            var token = JValue.CreateNull();

            var result = _traceDump.TryCompare(token, "value", SearchOperator.Equals);

            result.Should().NotBe(0);
        }

        #endregion

        #region Unsupported Type Tests

        [Test]
        public void TryCompare_ObjectToken_ThrowsInvalidOperationException()
        {
            var token = JObject.Parse("""{ "key": "value" }""");

            Action act = () => _traceDump.TryCompare(token, "value", SearchOperator.Equals);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Unexpected JTokenType*");
        }

        [Test]
        public void TryCompare_ArrayToken_ThrowsInvalidOperationException()
        {
            var token = JArray.Parse("[1, 2, 3]");

            Action act = () => _traceDump.TryCompare(token, "value", SearchOperator.Equals);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Unexpected JTokenType*");
        }

        #endregion

        #region Default Operator Tests

        [Test]
        public void TryCompare_DefaultOperator_UsesEquals()
        {
            var token = JToken.FromObject("hello");

            var result = _traceDump.TryCompare(token, "hello", SearchOperator.None);

            result.Should().Be(0);
        }

        #endregion
    }
}