using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace SystemInsta.AspNetCore
{
    internal static class ApplicationBuilderExtensions
    {
        public static void UseSymbolCollection(this IApplicationBuilder builder)
        {
            builder.UseMiddleware<SymbolCollectionMiddleware>();
        }

        private class SymbolCollectionMiddleware
        {
            private readonly ISystemImageRegistry _systemImageRegistry;
            private readonly IDeviceRegistry _deviceRegistry;
            private ILogger<SymbolCollectionMiddleware> _logger;

            public SymbolCollectionMiddleware(
                ISystemImageRegistry systemImageRegistry, 
                IDeviceRegistry deviceRegistry, 
                ILogger<SymbolCollectionMiddleware> logger)
            {
                _systemImageRegistry = systemImageRegistry;
                _deviceRegistry = deviceRegistry;
                _logger = logger;
            }

            public async Task Invoke(HttpContext context, RequestDelegate next)
            {
                var log = context.RequestServices.GetService<ILoggerFactory>()
                    .CreateLogger<Startup>();

                if (context.Request.Path == "/image")
                {
                    if (context.Request.Headers.TryGetValue("debug-id", out var debugId))
                    {
                        log.LogInformation("Incoming image with debug Id:{debugId}", debugId);

                        if (_systemImageRegistry.SymbolsWanted(debugId))
                        {
                            log.LogDebug($"Debug Id:{debugId} already exists.");
                            context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                        }
                        else
                        {
                            switch (context.Request.Method)
                            {
                                case "HEAD":
                                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                                    break;
                                case "POST":
                                    {
                                        await Add(debugId, context);
                                    }
                                    context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                                    break;
                                default:
                                    context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                                    break;
                            }
                        }
                    }
                    else if (context.Request.Method == "GET")
                    {
                        context.Response.ContentType = "text/plain";
//                        await context.Response.WriteAsync($@"Total images {store.Count}
//Total size in bytes: {store.Values.Sum(s => s.Length)}");
                    }
                    await context.Response.CompleteAsync();
                }
            }

            private async Task Add(StringValues debugId, HttpContext context)
            {
                using var _ = _logger.BeginScope(new { DebugId = debugId });

                await using var mem = new MemoryStream();
                var boundary = HeaderUtilities.RemoveQuotes(
                    MediaTypeHeaderValue.Parse(context.Request.ContentType).Boundary);

                if (boundary.Length < 10)
                {
                    return;
                }
                var reader = new MultipartReader(boundary.ToString(), context.Request.Body);

                var section = await reader.ReadNextSectionAsync();
                while (section != null)
                {
                    var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition);
                    if (hasContentDispositionHeader)
                    {
                        await section.Body.CopyToAsync(mem);
                        _logger.LogInformation("Size: " + mem.Length);
                        mem.Position = 0;
                        _systemImageRegistry.AddSystemImage(new SystemImage
                        {
                            DebugId = debugId,
                            //Data = 
                            //DeviceId = 
                            //HasDebugInformation = 
                            //HasUnwindInformation = 
                            //Hash = 
                            //Name = 
                            //Path = 
                            Data = mem
                        });
                    }

                    section = await reader.ReadNextSectionAsync();
                }
            }
        }
    }
}
