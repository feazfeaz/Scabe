using NAPS2.Images.Gdi;
using NAPS2.Pdf;
using NAPS2.Scan;
using System.IO;


using NAPS2.Images;

var builder = WebApplication.CreateBuilder(args);

var  MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<ScanningService>();
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          policy.WithOrigins("http://localhost:3000",
                                              "http://www.contoso.com");
                      });
});

var app = builder.Build();
app.UseCors(MyAllowSpecificOrigins);
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();
// scan
app.MapGet("/scanDevices", async (ScanningService scanningService) =>
{
 
    var devices = await scanningService.GetDevicesAsync();

    if (devices != null && devices.Any())
    {
        return Results.Ok(devices.Select(device => new { device.Name, device.ID }));
    }
    else
    {
        return Results.Ok(Array.Empty<object>());
    }
})
.WithName("GetScanDevices")
.WithOpenApi();


app.MapGet("/initiateScan",  async (HttpContext httpContext, string scannerId, string? paperSource, string? pageSize, string? dpi) =>
{
    string targetDeviceId = "TWAIN2 FreeImage Software Scanner";
    using var scanningContext = new ScanningContext(new GdiImageContext());
    scanningContext.SetUpWin32Worker();
    var controller = new ScanController(scanningContext);

    var devices = await controller.GetDeviceList(Driver.Twain);
    targetDeviceId = scannerId;
    var targetDevice = devices.FirstOrDefault(d => d.ID == targetDeviceId);

    if (targetDevice == null)
    {
        return Results.NotFound($"Device with ID {targetDeviceId} not found.");
    }

   // Initialize defaults
    PaperSource parsedPaperSource = PaperSource.Flatbed;
    PageSize parsedPageSize = PageSize.A4;
    int parsedDpi = 300; // Default DPI

    // Check and parse `paperSource`
    if (!string.IsNullOrEmpty(paperSource) && Enum.IsDefined(typeof(PaperSource), paperSource))
    {
        parsedPaperSource = (PaperSource)Enum.Parse(typeof(PaperSource), paperSource, true);
    }

    // Check and parse `pageSize`
    if (!string.IsNullOrEmpty(pageSize) && Enum.IsDefined(typeof(PageSize), pageSize))
    {
        parsedPageSize = (PageSize)Enum.Parse(typeof(PageSize), pageSize, true);
    }

    // Parse `dpi`
    if (!string.IsNullOrEmpty(dpi) && int.TryParse(dpi, out int dpiValue))
    {
        parsedDpi = dpiValue;
    }

    var options = new ScanOptions
    {
        Device = targetDevice,
        PaperSource = parsedPaperSource,
        PageSize = parsedPageSize,
        Dpi = parsedDpi // Default to 300 DPI if not provided
    };
    Console.WriteLine($"DPI: {dpi}");
    var images = await controller.Scan(options).ToListAsync();
    if (images.Count == 0)
    {
        return Results.NotFound("No pages were scanned.");
    }

    var pdfExporter = new PdfExporter(scanningContext);
    string userDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    string folderPath = Path.Combine(userDocuments, "ShukanaScan", DateTime.Now.ToString("yyyyMMddHHmmss"));
    Directory.CreateDirectory(folderPath);
    string pdfPath = Path.Combine(folderPath, "scanned_document.pdf");

    await pdfExporter.Export(pdfPath, images);

    var fileData = new FilePathResponse { FilePath = pdfPath };
var response = new GeneralApiResponse<FilePathResponse>("File saved successfully", fileData);
return Results.Ok(response);
})
.WithName("InitiateScan")
.WithOpenApi();
app.MapGet("/ping", () => {
    var response = new GeneralApiResponse<string>("pong", null);
    return Results.Ok(response);
}).WithName("Ping")
.WithOpenApi();


app.MapGet("/scanDevicesAll", async (HttpContext httpContext, string? driverType) =>
{
using var scanningContext = new ScanningContext(new GdiImageContext());
    scanningContext.SetUpWin32Worker();
    var controller = new ScanController(scanningContext);

    // Defining the functions to get devices by driver types
    async Task<IEnumerable<ScanDevice>> GetWiaDevices() => await controller.GetDeviceList(Driver.Wia);
    async Task<IEnumerable<ScanDevice>> GetTwainDevices() => await controller.GetDeviceList(Driver.Twain);
    async Task<IEnumerable<ScanDevice>> GetEsclDevices() => await controller.GetDeviceList(Driver.Escl);

    var allDriverTypes = new Dictionary<string, Func<Task<IEnumerable<ScanDevice>>>>()
    {
        {"wia", GetWiaDevices},
        {"twain", GetTwainDevices},
        {"escl", GetEsclDevices},
    };

    if (string.IsNullOrEmpty(driverType))
    {
        var groupedDeviceList = new Dictionary<string, IEnumerable<object>>();
foreach (var pair in allDriverTypes)
{
    var devices = await pair.Value();
    groupedDeviceList[pair.Key] = devices.Select(device => new { device.Name, device.ID } as object).ToList();
}

        return Results.Ok(groupedDeviceList);
    }
    else
    {
        driverType = driverType.ToLower();
        if (allDriverTypes.TryGetValue(driverType, out var getDevices))
        {
            var devices = await getDevices();
            return Results.Ok(new { DriverType = driverType, Devices = devices.Select(device => new { device.Name, device.ID }) });
        }
        else
        {
            return Results.BadRequest($"Invalid driver type specified: {driverType}");
        }
    }
})
.WithName("GetScanDevicesAll")
.WithOpenApi();
app.Run();
record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
public class GeneralApiResponse<T>
{
    public string Message { get; set; }
    public T Data { get; set; }

    public GeneralApiResponse(string message, T data)
    {
        Message = message;
        Data = data;
    }
}
public class FilePathResponse
{
    public string FilePath { get; set; }
}
public static class EnumParser
{
    public static TEnum ParseOrDefault<TEnum>(string value, TEnum defaultValue) where TEnum : struct
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return Enum.TryParse<TEnum>(value, true, out TEnum result) ? result : defaultValue;
    }
}
