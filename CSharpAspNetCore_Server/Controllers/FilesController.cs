using Microsoft.AspNetCore.Mvc;

namespace CSharpAspNetCore_Server.Controllers;

public class FilesController: Controller
{
    private readonly string _sourceDirPath = @"FilesSourceDirectory";
    
    public FilesController(ILogger<FilesController> logger)
        : base(logger)
    {
    }
    
    //[AcceptVerbs(new[] {"GET", "HEAD"})]
    [HttpGet("{fileName}")]
    [HttpHead("{fileName}")]
    public IActionResult DownloadFile(string fileName)
    {
        var filePath = Path.Combine(_sourceDirPath, fileName);
        Console.WriteLine($"Current directory: {Directory.GetCurrentDirectory()}");
        Console.WriteLine(filePath);
        if (!System.IO.File.Exists(filePath))
            return NotFound();
        
        var stream = System.IO.File.OpenRead(filePath);
        return File(stream, "application/octet-stream", fileName);
    }
}