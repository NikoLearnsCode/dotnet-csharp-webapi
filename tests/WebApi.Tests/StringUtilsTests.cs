using Shouldly;
using WebApi.Helpers;

namespace WebApi.Tests;

// Pure unit test - no web host or database needed.
public class StringUtilsTests
{
    [Theory]
    [InlineData("Black T-shirt", "black-t-shirt")]
    [InlineData("Blå Tröja", "bla-troja")] // å/ä → a, ö → o
    [InlineData("Hello   World", "hello-world")] // collapses repeated separators
    [InlineData("Ärtor & Bönor", "artor-bonor")] // strips non-alphanumeric
    [InlineData("", "")]
    [InlineData("   ", "")]
    public void GenerateSlug_ProducesExpectedSlug(string input, string expected) =>
        StringUtils.GenerateSlug(input).ShouldBe(expected);
}
