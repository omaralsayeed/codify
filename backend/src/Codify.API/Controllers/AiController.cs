// AiController is intentionally empty — all AI routes are handled by dedicated controllers:
//
//   POST /api/hints          → HintsController.RequestHint  (rate-limited: ai-hints policy)
//   GET  /api/hints/history  → HintsController.GetHistory
//
// This file is kept as a placeholder so future AI-related routes that do not fit
// the hints domain (e.g. code explanations, code reviews) have a home here.

using Microsoft.AspNetCore.Mvc;

namespace Codify.API.Controllers;

[ApiController]
[Route("api/ai")]
public class AiController : ControllerBase
{
    // Reserved for future AI endpoints (code explanations, code reviews, etc.)
    // All hint endpoints live in HintsController at /api/hints.
}
