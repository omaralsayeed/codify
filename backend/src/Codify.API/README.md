# Codify.API

> **Entry point.** Controllers, middleware, startup configuration.

## What Goes Here

- `Controllers/` — One controller per module (AuthController, ProblemController, etc.)
- `Middleware/` — JWT validation, global error handling, rate limiting
- `Program.cs` — DI registration, middleware pipeline
- `appsettings.json` — Non-secret configuration defaults

## Rules

- Controllers must only: receive input, validate request shape, call a service, return a response
- No business logic in controllers
- All dependencies injected via constructor

## Controller Template

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProblemsController : ControllerBase
{
    private readonly IProblemService _problems;

    public ProblemsController(IProblemService problems)
    {
        _problems = problems;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll([FromQuery] ProblemFilterRequest filter)
    {
        var result = await _problems.GetAllAsync(filter);
        return Ok(ApiResponse.Success(result));
    }
}
```
