using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace SystemInsta.AspNetCore
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

//            app.UseHttpsRedirection();
            app.UseRouting();

            var store = new ConcurrentDictionary<string, byte[]>();

            app.Use(async (context, next) =>
            {
                var log = context.RequestServices.GetService<ILoggerFactory>()
                    .CreateLogger<Startup>();

                if (context.Request.Path == "/image")
                {
                    log.LogInformation("Incoming image.");
                    if (context.Request.Headers.TryGetValue("debug-id", out var debugId))
                    {
                        log.LogTrace($"Debug Id:{debugId}");

                        if (store.ContainsKey(debugId))
                        {
                            log.LogDebug($"Debug Id:{debugId} already exists.");
                            context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                            return;
                        }

                        switch (context.Request.Method)
                        {
                            case "HEAD":
                                break;
                            case "POST":
                            {
                                await Add(log, debugId, context, store);
                            }
                                break;
                            default:
                                return;
                        }

                        context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                        await context.Response.CompleteAsync();
                    }
                }
            });
        }

        private static async Task Add(ILogger<Startup> log, StringValues debugId, HttpContext context, ConcurrentDictionary<string, byte[]> store)
        {
            using var _ = log.BeginScope(new { DebugId = debugId});

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
                    log.LogInformation("Size: " + mem.Length);
                    mem.Position = 0;
                    store.TryAdd(debugId, mem.ToArray());
                }

                section = await reader.ReadNextSectionAsync();
            }
        }
    }
}
