// See https://aka.ms/new-console-template for more information
using NAPS2.Scan;
using NAPS2.Images.Gdi;

using var scanningContext = new ScanningContext(new GdiImageContext());


var controller = new ScanController(scanningContext);
scanningContext.SetUpWin32Worker();
// Query for available scanning devices
var devices = await controller.GetDeviceList(Driver.Twain);

// Check if there are any devices available
if (devices != null && devices.Any())
{
    Console.WriteLine("Available Scanning Devices:");
    foreach (var device in devices)
    {
        Console.WriteLine($"Device Name: {device.Name}");
        Console.WriteLine($"Device ID: {device.ID}");
        // Add any other relevant details you want to print
    }
}
else
{
    Console.WriteLine("No scanning devices found.");
}

