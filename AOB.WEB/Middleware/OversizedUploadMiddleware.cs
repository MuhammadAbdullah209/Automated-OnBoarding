using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace AOB.WEB.Middleware
{
    /// <summary>
    /// Catches a form post that is larger than its endpoint allows ([RequestSizeLimit] / [RequestFormLimits], or the
    /// server default) before anything reads it. Without this the request fails while the form is being read and the
    /// browser shows a bare "400" / "request entity too large" page. Instead the upload is discarded and the user is
    /// sent back to the page they came from with a message saying how large a file may be.
    /// The browser checks file sizes before posting (data-max-bytes in site.js); this covers everything else.
    /// </summary>
    public sealed class OversizedUploadMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<OversizedUploadMiddleware> logger)
    {
        // Beyond this the body is not read at all: the connection is simply refused with a 413.
        private const long MaxBytesToDiscard = 512L * 1024 * 1024;

        private readonly int _maxFileSizeMb = configuration.GetValue("FileStorage:MaxFileSizeMB", 10);

        public async Task InvokeAsync(HttpContext context)
        {
            if (!HttpMethods.IsPost(context.Request.Method)
                || context.Request.ContentLength is not long length
                || !context.Request.HasFormContentType)
            {
                await next(context);
                return;
            }

            var limit = LimitFor(context);
            if (limit is null || length <= limit)
            {
                await next(context);
                return;
            }

            logger.LogInformation("Rejected an upload of {Length} bytes to {Path} (limit {Limit} bytes).",
                length, context.Request.Path, limit);

            // Only a signed-in user gets the friendly redirect; there is nothing to go back to otherwise.
            if (context.User.Identity?.IsAuthenticated != true || length > MaxBytesToDiscard)
            {
                context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                return;
            }

            // Read and discard the body so the browser finishes sending it and then follows the redirect,
            // rather than reporting a reset connection.
            var sizeFeature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
            if (sizeFeature is { IsReadOnly: false })
            {
                sizeFeature.MaxRequestBodySize = MaxBytesToDiscard;
            }
            await context.Request.Body.CopyToAsync(Stream.Null, context.RequestAborted);

            var tempData = context.RequestServices.GetRequiredService<ITempDataDictionaryFactory>().GetTempData(context);
            tempData["Error"] = $"The selected file exceeds the maximum upload size of {_maxFileSizeMb} MB. Please upload a smaller file.";
            tempData.Save();

            context.Response.StatusCode = StatusCodes.Status303SeeOther;
            context.Response.Headers.Location = ReturnUrl(context);
        }

        /// <summary>The smallest of the request-size and multipart limits that apply to this endpoint.</summary>
        private static long? LimitFor(HttpContext context)
        {
            var metadata = context.GetEndpoint()?.Metadata;
            long? limit = metadata?.GetMetadata<IRequestSizeLimitMetadata>() is { } sizeLimit
                ? sizeLimit.MaxRequestBodySize
                : context.Features.Get<IHttpMaxRequestBodySizeFeature>()?.MaxRequestBodySize;

            if (metadata?.GetMetadata<IFormOptionsMetadata>()?.MultipartBodyLengthLimit is long formLimit)
            {
                limit = limit is null ? formLimit : Math.Min(limit.Value, formLimit);
            }

            return limit;
        }

        /// <summary>The page the form was posted from, when it is on this site; otherwise the same path (as a GET).</summary>
        private static string ReturnUrl(HttpContext context)
        {
            var request = context.Request;
            if (Uri.TryCreate(request.Headers.Referer.ToString(), UriKind.Absolute, out var referer)
                && string.Equals(referer.Host, request.Host.Host, StringComparison.OrdinalIgnoreCase)
                && (request.Host.Port is null || referer.Port == request.Host.Port))
            {
                return referer.PathAndQuery;
            }

            return (request.PathBase + request.Path).ToString();
        }
    }
}
