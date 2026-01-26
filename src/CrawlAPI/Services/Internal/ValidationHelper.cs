using CrawlAPI.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CrawlAPI.Services.Internal;

/// <summary>
/// Provides validation and normalization utilities for API inputs.
/// </summary>
internal static class ValidationHelper
{
    public static string NormalizeUrl(string url)
    {
        return url.Trim();
    }

    public static int NormalizeMaxDepth(int? maxDepth)
    {
        return Math.Max(1, Math.Min(maxDepth ?? 2, 5));
    }

    public static void ValidatePaginationInput(ref int page, ref int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;
    }
}
