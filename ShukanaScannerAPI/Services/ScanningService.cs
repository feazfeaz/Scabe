using NAPS2.Images.Gdi;
using NAPS2.Pdf;
using NAPS2.Scan;
public class ScanningService
{
    public async Task<IEnumerable<ScanDevice>> GetDevicesAsync()
    {
        using var scanningContext = new ScanningContext(new GdiImageContext());
        var controller = new ScanController(scanningContext);
        scanningContext.SetUpWin32Worker();
        return await controller.GetDeviceList(Driver.Twain);
    }

    // Other common methods...
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Register your scanning service
        services.AddScoped<ScanningService>();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // Existing configuration...
    }
}

