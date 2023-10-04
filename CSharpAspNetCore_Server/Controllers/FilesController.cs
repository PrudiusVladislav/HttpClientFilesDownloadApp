using Microsoft.AspNetCore.Mvc;

namespace CSharpAspNetCore_Server.Controllers;

public class FilesController: Controller
{
    private readonly string _sourceDirPath = @"FilesSourceDirectory";
    
    public FilesController(ILogger<FilesController> logger)
        : base(logger)
    {
    }

    [HttpGet("{fileName}")]
    public IActionResult DownloadFile(string fileName)
    {
        var filePath = Path.Combine(_sourceDirPath, fileName);
        Console.WriteLine($"Current directory: {Directory.GetCurrentDirectory()}");
        Console.WriteLine(filePath);
        if (!System.IO.File.Exists(filePath))
            return NotFound();

        if (Request.Method.Equals("HEAD"))
        {
            var fileInfo = new FileInfo(filePath);
            var contentLength = fileInfo.Length;
            Response.ContentLength = contentLength;
            return NoContent();
        }
        
        var stream = System.IO.File.OpenRead(filePath);
        return File(stream, "application/octet-stream", fileName);
    }
}