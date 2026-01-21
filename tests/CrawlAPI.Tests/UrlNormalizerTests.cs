using Xunit;
using SharedDomain.Utilities;

namespace CrawlAPI.Tests;

public class UrlNormalizerTests
{
    [Fact]
    public void Normalize_WithHttpUrl_ReturnsNormalizedUrl()
    {
        var url = "http://example.com/path";
        var result = UrlNormalizer.Normalize(url);
        Assert.Equal("http://example.com/path", result);
    }

    [Fact]
    public void Normalize_WithHttpsUrl_ReturnsNormalizedUrl()
    {
        var url = "https://example.com/path";
        var result = UrlNormalizer.Normalize(url);
        Assert.Equal("https://example.com/path", result);
    }

    [Fact]
    public void Normalize_WithFragment_RemovesFragment()
    {
        var url = "https://example.com/page#section";
        var result = UrlNormalizer.Normalize(url);
        Assert.Equal("https://example.com/page", result);
    }

    [Fact]
    public void Normalize_WithTrailingSlash_RemovesIt()
    {
        var url = "https://example.com/path/";
        var result = UrlNormalizer.Normalize(url);
        Assert.Equal("https://example.com/path", result);
    }

    [Fact]
    public void Normalize_WithUpperCase_ReturnLowercase()
    {
        var url = "HTTPS://Example.COM/Path";
        var result = UrlNormalizer.Normalize(url);
        Assert.Equal("https://example.com/path", result);
    }

    [Fact]
    public void Normalize_WithInvalidUrl_ReturnsEmpty()
    {
        var result = UrlNormalizer.Normalize("not a url");
        Assert.Empty(result);
    }

    [Fact]
    public void ResolveRelativeUrl_WithAbsoluteUrl_ReturnsNormalized()
    {
        var baseUrl = "https://example.com/page";
        var relativeUrl = "https://other.com/path";
        var result = UrlNormalizer.ResolveRelativeUrl(baseUrl, relativeUrl);
        Assert.Equal("https://other.com/path", result);
    }

    [Fact]
    public void ResolveRelativeUrl_WithRelativePath_ResolvesCorrectly()
    {
        var baseUrl = "https://example.com/blog/post";
        var relativeUrl = "../about";
        var result = UrlNormalizer.ResolveRelativeUrl(baseUrl, relativeUrl);
        Assert.Equal("https://example.com/about", result);
    }

    [Fact]
    public void ResolveRelativeUrl_WithMailto_ReturnsNull()
    {
        var baseUrl = "https://example.com";
        var relativeUrl = "mailto:test@example.com";
        var result = UrlNormalizer.ResolveRelativeUrl(baseUrl, relativeUrl);
        Assert.Null(result);
    }

    [Fact]
    public void ResolveRelativeUrl_WithFragment_ReturnsNull()
    {
        var baseUrl = "https://example.com/page";
        var relativeUrl = "#section";
        var result = UrlNormalizer.ResolveRelativeUrl(baseUrl, relativeUrl);
        Assert.Null(result);
    }

    [Fact]
    public void IsSameDomain_WithSameDomain_ReturnsTrue()
    {
        var url1 = "https://example.com/page1";
        var url2 = "https://example.com/page2";
        var result = UrlNormalizer.IsSameDomain(url1, url2);
        Assert.True(result);
    }

    [Fact]
    public void IsSameDomain_WithDifferentDomain_ReturnsFalse()
    {
        var url1 = "https://example.com/page";
        var url2 = "https://other.com/page";
        var result = UrlNormalizer.IsSameDomain(url1, url2);
        Assert.False(result);
    }

    [Fact]
    public void IsSameDomain_WithSubdomain_ReturnsFalse()
    {
        var url1 = "https://example.com/page";
        var url2 = "https://sub.example.com/page";
        var result = UrlNormalizer.IsSameDomain(url1, url2);
        Assert.False(result);
    }
}
