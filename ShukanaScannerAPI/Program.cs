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
        return Results.NotFound("No scanning devices found.");
    }
})
.WithName("GetScanDevices")
.WithOpenApi();


app.MapGet("/initiateScan", async (HttpContext httpContext, string scannerId) =>
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

    var options = new ScanOptions
    {
        Device = targetDevice,
        PaperSource = PaperSource.Flatbed,
        PageSize = PageSize.A4,
        Dpi = 300
    };

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