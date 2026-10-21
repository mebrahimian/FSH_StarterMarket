using FSH.Framework.Storage.Local;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;

namespace FSH.Modules.Files.Features.v1.LocalUpload;

public static class LocalUploadEndpoint
{
    internal static RouteHandlerBuilder MapLocalUploadEndpoint(
        this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut(
            "/local-upload/{token}",
            async (
                string token,
                HttpContext context,
                IConfiguration configuration,
                IWebHostEnvironment environment,
                CancellationToken cancellationToken) =>
            {
                if (!string.Equals(
                    configuration["Storage:Provider"],
                    "local",
                    StringComparison.OrdinalIgnoreCase))
                {
                    return Results.NotFound();
                }

                var upload = LocalStorageService.SharedTokenStore.Consume(token);

                if (upload is null)
                    return Results.BadRequest("Invalid or expired upload token.");

                if (!string.Equals(
                    context.Request.ContentType,
                    upload.ContentType,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return Results.BadRequest("Invalid content type.");
                }

                if (upload.MaxBytes <= 0 ||
                    context.Request.ContentLength > upload.MaxBytes)
                {
                    return Results.BadRequest("File exceeds the allowed size.");
                }

                var root = Path.GetFullPath(
                    environment.WebRootPath ??
                    Path.Combine(environment.ContentRootPath, "wwwroot"));

                var fullPath = Path.GetFullPath(
                    Path.Combine(root, upload.StorageKey));

                var rootPrefix =
                    root.TrimEnd(Path.DirectorySeparatorChar) +
                    Path.DirectorySeparatorChar;

                if (!fullPath.StartsWith(
                    rootPrefix,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return Results.BadRequest("Invalid storage path.");
                }

                Directory.CreateDirectory(
                    Path.GetDirectoryName(fullPath)!);

                try
                {
                    await using var destination = new FileStream(
                        fullPath,
                        FileMode.CreateNew,
                        FileAccess.Write,
                        FileShare.None,
                        81920,
                        FileOptions.Asynchronous);

                    var buffer = new byte[81920];
                    long total = 0;

                    while (true)
                    {
                        var read = await context.Request.Body.ReadAsync(
                            buffer.AsMemory(),
                            cancellationToken);

                        if (read == 0)
                            break;

                        total += read;

                        if (total > upload.MaxBytes)
                        {
                            await destination.DisposeAsync();
                            File.Delete(fullPath);
                            return Results.BadRequest("File exceeds the allowed size.");
                        }

                        await destination.WriteAsync(
                            buffer.AsMemory(0, read),
                            cancellationToken);
                    }

                    if (total == 0)
                    {
                        await destination.DisposeAsync();
                        File.Delete(fullPath);
                        return Results.BadRequest("Empty file.");
                    }
                }
                catch
                {
                    if (File.Exists(fullPath))
                        File.Delete(fullPath);

                    throw;
                }

                return Results.NoContent();
            })
            .WithName("LocalFileUpload")
            .WithSummary("Uploads file bytes using a one-time local storage token")
            .AllowAnonymous();
    }
}