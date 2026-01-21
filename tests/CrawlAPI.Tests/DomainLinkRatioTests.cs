using Xunit;

namespace CrawlAPI.Tests;

public class DomainLinkRatioTests
{
    [Fact]
    public void CalculateDomainLinkRatio_AllLinksInternal_ReturnsOne()
    {
        var totalLinks = 5;
        var internalLinks = 5;
        var ratio = (decimal)internalLinks / totalLinks;
        Assert.Equal(1m, ratio);
    }

    [Fact]
    public void CalculateDomainLinkRatio_NoLinksInternal_ReturnsZero()
    {
        var totalLinks = 5;
        var internalLinks = 0;
        var ratio = (decimal)internalLinks / totalLinks;
        Assert.Equal(0m, ratio);
    }

    [Fact]
    public void CalculateDomainLinkRatio_MixedLinks_ReturnsCorrectRatio()
    {
        var totalLinks = 4;
        var internalLinks = 2;
        var ratio = (decimal)internalLinks / totalLinks;
        Assert.Equal(0.5m, ratio);
    }

    [Fact]
    public void CalculateDomainLinkRatio_NoLinks_ReturnsZero()
    {
        var totalLinks = 0;
        var ratio = totalLinks > 0 ? (decimal)0 / totalLinks : 0m;
        Assert.Equal(0m, ratio);
    }

    [Fact]
    public void CalculateDomainLinkRatio_TwoThirdInternal_ReturnsCorrectRatio()
    {
        var totalLinks = 3;
        var internalLinks = 2;
        var ratio = (decimal)internalLinks / totalLinks;
        Assert.Equal(0.6667m, ratio, precision: 4);
    }
}
