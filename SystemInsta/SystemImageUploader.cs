using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ELFSharp.ELF;
using ELFSharp.ELF.Sections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace SystemInsta
{
    public class SystemImageUploader : IDisposable
    {
        private readonly Uri _backendUrl;
        private readonly ILogger<SystemImageUploader> _logger;
        private readonly HttpClient _client;

        public SystemImageUploader(
            Uri backendUrl = null,
            HttpMessageHandler handler = null,
            ILogger<SystemImageUploader> logger = null)
        {
            // TODO: get rid of test url
            _backendUrl = backendUrl ?? new Uri("http://sentry.garcia.in/image");
                          //?? throw new ArgumentNullException(nameof(backendUrl));
            _logger = logger ?? NullLogger<SystemImageUploader>.Instance;
            _client = new HttpClient(handler ?? new HttpClientHandler());
        }

        public async Task Run(string path, bool searchSubDirectories)
        {
            if (!Directory.Exists(path))
            {
                _logger.LogWarning("Path {path} doesn't exist.", path);
                return;
            }

            if (searchSubDirectories)
            {
                foreach (var directory in Directory.GetDirectories(path, "*", SearchOption.AllDirectories))
                {
                    await Collect(directory);
                }
            }

            await Collect(path);
        }

        private async Task Collect(string path)
        {
            var files = Directory.GetFiles(path);
            _logger.LogInformation("Path {path} has {length} files to process", path, files.Length);

            foreach (var file in files)
            {
                _logger.LogInformation("Processing file: {file}.", file);
                IELF elf = null;
                try
                {
                    if (!ELFReader.TryLoad(file, out elf))
                    {
                        _logger.LogWarning("Couldn't load': {file} with ELF reader.", file);
                        continue;
                    }

                    var hasBuildId = elf.TryGetSection(".note.gnu.build-id", out var buildId);
                    if (!hasBuildId)
                    {
                        _logger.LogWarning("No Debug Id in {file}", file);
                        continue;
                    }

                    var hasUnwindingInfo = elf.TryGetSection(".eh_frame", out _);
                    var hasDwarfDebugInfo = elf.TryGetSection(".debug_frame", out _);

                    if (!hasUnwindingInfo && !hasDwarfDebugInfo)
                    {
                        _logger.LogWarning("No unwind nor DWARF debug info in {file}", file);
                        continue;
                    }

                    await ProcessFile(file, hasUnwindingInfo, hasDwarfDebugInfo, buildId, elf);
                }
                catch (Exception e)
                {
                    // You would expect TryLoad doesn't throw but that's not the case
                    _logger.LogError(e, "Failed processing file {file}.", file);
                }
                finally
                {
                    elf?.Dispose();
                }
            }
        }

        private async Task ProcessFile(
            string file,
            bool hasUnwindingInfo,
            bool hasDwarfDebugInfo,
            ISection buildId,
            IELF elf)
        {
            _logger.LogInformation("Contains unwinding info: {hasUnwindingInfo}", hasUnwindingInfo);
            _logger.LogInformation("Contains DWARF debug info: {hasDwarfDebugInfo}", hasDwarfDebugInfo);

            var builder = new StringBuilder();
            var bytes = buildId.GetContents().Skip(16);

            foreach (var @byte in bytes)
            {
                builder.Append(@byte.ToString("x2"));
            }

            var buildIdHex = builder.ToString();

            if (!(await _client.SendAsync(new HttpRequestMessage(HttpMethod.Head, _backendUrl)
                    {Headers = {{"debug-id", buildIdHex}}}))
                .IsSuccessStatusCode)
            {
                return;
            }

            // Better would be if `ELF` class would expose its buffer so we don't need to read the file twice.
            // Ideally ELF would read headers as a stream which we could reset to 0 after reading heads
            // and ensuring it's what we need.
            using (var fileStream = File.OpenRead(file))
            {
                var postResult = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Post, _backendUrl)
                {
                    Headers = {{"debug-id", buildIdHex}},
                    Content = new MultipartFormDataContent(
                        // TODO: add a proper boundary
                        $"Upload----WebKitFormBoundary7MA4YWxkTrZu0gW--")
                    {
                        {new StreamContent(fileStream), file}
                    }
                });

                if (!postResult.IsSuccessStatusCode)
                {
                    _logger.LogError("{statusCode} for file {file}", postResult.StatusCode, file);
                    if (postResult.Headers.TryGetValues("X-Error-Code", out var code))
                    {
                        _logger.LogError("Code: {code}");
                    }
                }
                else
                {
                    _logger.LogInformation("Sent file: {file}");
                }
            }

#if DEBUG
            _logger.LogInformation("Build Id: {buildIdHex}", buildIdHex);
            _logger.LogInformation("Class: {class}.", elf.Class);
            _logger.LogInformation("HasSectionsStringTable: {hasSectionsStringTable}.", elf.HasSectionsStringTable);
            _logger.LogInformation("HasSectionHeader: {hasSectionHeader}.", elf.HasSectionHeader);
            foreach (var section in elf.Sections)
            {
                _logger.LogInformation("Section: {section}.", section);
            }

            _logger.LogInformation("HasSegmentHeader: {hasSegmentHeader}.", elf.HasSegmentHeader);
            foreach (var segment in elf.Segments)
            {
                _logger.LogInformation("Segment: {segment}.", segment);
            }

            _logger.LogInformation("Endianness: {Eedianness}.", elf.Endianess);
            _logger.LogInformation("Machine: {machine}.", elf.Machine);
            _logger.LogInformation("Type: {type}.", elf.Type);
#endif
        }

        public void Dispose()
        {
            _logger.LogInformation("Disposing client.");
            _client?.Dispose();
        }
    }
}