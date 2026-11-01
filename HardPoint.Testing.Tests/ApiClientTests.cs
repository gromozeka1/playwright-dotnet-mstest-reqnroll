using System.Net;
using System.Net.Sockets;
using System.Text;
using HardPoint.Testing.Api;
using HardPoint.Testing.Config;
using HardPoint.Testing.Driver;
using HardPoint.Testing.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HardPoint.Testing.Tests;

// The client talks through a real Playwright API context (no browser needed) against a loopback
// stub, and reads the process-wide ConfigReader settings.
[TestClass]
[DoNotParallelize]
public sealed class ApiClientTests
{
    private string _outputDirectory = string.Empty;
    private StubServer? _server;

    [TestInitialize]
    public void SetUp()
    {
        _outputDirectory = Path.Combine(Path.GetTempPath(), "api-client-tests", Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(_outputDirectory);
        ExtentReportManager.ResetForTests();
        ConfigReader.Settings = new TestSettings
        {
            BrowserType = "chromium",
            ApplicationUrl = "http://localhost:5168",
            OutputDirectory = _outputDirectory,
        };
    }

    [TestCleanup]
    public async Task TearDown()
    {
        if (_server is not null)
        {
            await _server.DisposeAsync();
        }

        ConfigReader.Reload();

        if (Directory.Exists(_outputDirectory))
        {
            Directory.Delete(_outputDirectory, recursive: true);
        }
    }

    [TestMethod]
    public async Task GetJsonAsyncDeserialisesTheResponseBody()
    {
        _server = StubServer.Start("""{"id":7,"name":"widget","type":2}""");
        using var reports = new ExtentReportManager();
        await using var driver = new PlaywrightDriver();

        var product = await new ApiClient(driver, reports).GetJsonAsync<Product>(_server.Url("/api/products/7"));

        Assert.AreEqual(7, product.Id);
        Assert.AreEqual("widget", product.Name);
        Assert.AreEqual(2, product.Type);
    }

    [TestMethod]
    public async Task GetJsonAsyncThrowsWithTheBodyOnAnErrorStatus()
    {
        _server = StubServer.Start("""{"error":"not found"}""", status: 404);
        using var reports = new ExtentReportManager();
        await using var driver = new PlaywrightDriver();

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => new ApiClient(driver, reports).GetJsonAsync<Product>(_server.Url("/api/products/7")));

        StringAssert.Contains(exception.Message, "404");
        StringAssert.Contains(exception.Message, "not found");
    }

    [TestMethod]
    public async Task PostJsonAsyncReturnsTheResponseWithoutThrowing()
    {
        _server = StubServer.Start("""{"id":8}""", status: 201);
        using var reports = new ExtentReportManager();
        await using var driver = new PlaywrightDriver();

        var response = await new ApiClient(driver, reports)
            .PostJsonAsync(_server.Url("/api/products"), new { name = "new" });

        Assert.AreEqual(201, response.Status);
    }

    private sealed class Product
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int Type { get; set; }
    }

    // A minimal HTTP/1.1 responder: the Playwright API context needs a real endpoint, and a bare
    // TcpListener avoids HttpListener's Windows URL-ACL requirements.
    private sealed class StubServer : IAsyncDisposable
    {
        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource _stop = new();
        private readonly string _body;
        private readonly int _status;
        private Task? _acceptLoop;

        private StubServer(string body, int status)
        {
            _body = body;
            _status = status;
        }

        public static StubServer Start(string body, int status = 200)
        {
            var server = new StubServer(body, status);
            server._listener.Start();
            server._acceptLoop = Task.Run(() => server.AcceptAsync());

            return server;
        }

        public string Url(string path)
            => $"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}{path}";

        public async ValueTask DisposeAsync()
        {
            _stop.Cancel();
            _listener.Stop();

            if (_acceptLoop is not null)
            {
                try
                {
                    await _acceptLoop;
                }
                catch (Exception)
                {
                    // The loop ends by cancellation; nothing to report.
                }
            }

            _stop.Dispose();
        }

        private async Task AcceptAsync()
        {
            while (!_stop.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await _listener.AcceptTcpClientAsync(_stop.Token);
                }
                catch (Exception)
                {
                    return;
                }

                _ = Task.Run(() => RespondAsync(client));
            }
        }

        private async Task RespondAsync(TcpClient client)
        {
            using (client)
            using (var stream = client.GetStream())
            {
                var buffer = new byte[8192];
                var read = await stream.ReadAsync(buffer, _stop.Token);
                var request = Encoding.ASCII.GetString(buffer, 0, read);

                // Drain a request body so the client is not left writing into a closed socket.
                var headerEnd = request.IndexOf("\r\n\r\n", StringComparison.Ordinal);
                var contentLength = 0;
                if (headerEnd >= 0)
                {
                    foreach (var line in request[..headerEnd].Split("\r\n"))
                    {
                        if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                        {
                            contentLength = int.Parse(line["Content-Length:".Length..].Trim());
                        }
                    }
                }

                var remaining = contentLength - (read - (headerEnd + 4));
                while (remaining > 0)
                {
                    var drained = await stream.ReadAsync(buffer.AsMemory(0, Math.Min(buffer.Length, remaining)), _stop.Token);
                    if (drained <= 0)
                    {
                        break;
                    }

                    remaining -= drained;
                }

                var payload = Encoding.UTF8.GetBytes(_body);
                var header = $"HTTP/1.1 {_status} {(IsSuccess(_status) ? "OK" : "Error")}\r\n" +
                             "Content-Type: application/json\r\n" +
                             $"Content-Length: {payload.Length}\r\n" +
                             "Connection: close\r\n\r\n";

                await stream.WriteAsync(Encoding.ASCII.GetBytes(header), _stop.Token);
                await stream.WriteAsync(payload, _stop.Token);
            }
        }

        private static bool IsSuccess(int status) => status is >= 200 and < 300;
    }
}
