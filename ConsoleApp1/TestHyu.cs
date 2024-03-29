using NAPS2.Images;
using NAPS2.Images.Gdi;
using NAPS2.Pdf;
using NAPS2.Scan;

public class TestHyu
{
    // Function without parameters and return value
    public void SayHello()
    {
        Console.WriteLine("Hello, world!");
    }

    // Function with parameters and return value
    public int Add(int a, int b)
    {
        return a + b;
    }

    public async Task Run()
    {
        // This is the absolute bare bones example of scanning.
        // See the other samples for more description and functionality.

        using var scanningContext = new ScanningContext(new GdiImageContext());
        var controller = new ScanController(scanningContext);
        scanningContext.SetUpWin32Worker();
        // Query for available scanning devices
        var devices = await controller.GetDeviceList(Driver.Twain);
        ScanDevice device = devices.First();
        var options = new ScanOptions { Device = device };
        await foreach (var image in controller.Scan(options))
        {
            Console.WriteLine("Scanned a page!");
        }
    }

    public async Task ScanAndSave()
    {
        using var scanningContext = new ScanningContext(new GdiImageContext());
        var controller = new ScanController(scanningContext);
        scanningContext.SetUpWin32Worker();
        // Query for available scanning devices
        var devices = await controller.GetDeviceList(Driver.Twain);

        // Set scanning options
        var options = new ScanOptions
        {
            Device = devices.First(),
            PaperSource = PaperSource.Feeder,
            PageSize = PageSize.A4,
            Dpi = 300
        };

        // Scan and save images
        int i = 1;
        await foreach (var image in controller.Scan(options))
        {
            Console.WriteLine("Scan and save!");
            image.Save($"page{i++}.jpg");
        }

        // Scan and save PDF
        // var images = await controller.Scan(options).ToListAsync();
        // var pdfExporter = new PdfExporter(scanningContext);
        // await pdfExporter.Export("doc.pdf", images);


         Console.WriteLine("Scan and save!");
    }
}