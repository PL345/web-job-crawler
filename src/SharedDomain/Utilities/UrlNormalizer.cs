namespace SharedDomain.Utilities;

public static class UrlNormalizer
{
    public static string Normalize(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;

        url = url.Trim();

        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            return string.Empty;

        try
        {
            var uri = new Uri(url);
            var builder = new UriBuilder(uri)
            {
                Fragment = string.Empty
            };

            var normalized = builder.Uri.ToString();
            if (normalized.EndsWith("/") && !normalized.Equals(builder.Uri.Scheme + "://"))
                normalized = normalized.TrimEnd('/');

            return normalized.ToLowerInvariant();
        }
        catch
        {
            return string.Empty;
        }
    }

    public static string? ResolveRelativeUrl(string baseUrl, string relativeOrAbsoluteUrl)
    {
        if (string.IsNullOrWhiteSpace(relativeOrAbsoluteUrl))
            return null;

        relativeOrAbsoluteUrl = relativeOrAbsoluteUrl.Trim();

        if (relativeOrAbsoluteUrl.StartsWith("mailto:") ||
            relativeOrAbsoluteUrl.StartsWith("tel:") ||
            relativeOrAbsoluteUrl.StartsWith("javascript:") ||
            relativeOrAbsoluteUrl.StartsWith("#"))
            return null;

        if (relativeOrAbsoluteUrl.StartsWith("http://") || relativeOrAbsoluteUrl.StartsWith("https://"))
            return Normalize(relativeOrAbsoluteUrl);

        try
        {
            var baseUri = new Uri(baseUrl);
            var resolved = new Uri(baseUri, relativeOrAbsoluteUrl);
            return Normalize(resolved.ToString());
        }
        catch
        {
            return null;
        }
    }

    public static string GetDomain(string url)
    {
        try
        {
            var uri = new Uri(url);
            return uri.Host.ToLowerInvariant();
        }
        catch
        {
            return string.Empty;
        }
    }

    public static bool IsSameDomain(string url1, string url2)
    {
        var domain1 = GetDomain(url1);
        var domain2 = GetDomain(url2);
        return !string.IsNullOrEmpty(domain1) && domain1 == domain2;
    }
}
