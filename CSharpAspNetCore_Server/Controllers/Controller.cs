using Microsoft.AspNetCore.Mvc;

namespace CSharpAspNetCore_Server.Controllers;

[Route("api/[controller]")]
public abstract class Controller : ControllerBase
{
    protected readonly ILogger<Controller> Logger;
    
    protected Controller(ILogger<Controller> logger)
    {
        Logger = logger;
    }
}