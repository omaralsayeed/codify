using Codify.API.Common;
using Codify.API.Controllers;
using Codify.Application.DTOs.AI;
using Codify.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using System.Security.Claims;

namespace Codify.Tests.API;

public class HintsControllerTests
{
    private readonly IAiHintService _hintService = Substitute.For<IAiHintService>();
    private readonly HintsController _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public HintsControllerTests()
    {
        _sut = new HintsController(_hintService);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, _userId.ToString()),
            new Claim(ClaimTypes.Role, "Student")
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    [Fact]
    public async Task RequestHint_ShouldReturnOk_WithHintResponse()
    {
        var problemId = Guid.NewGuid();
        var request = new HintRequest { ProblemId = problemId, StudentCode = "def test(): pass" };
        var expectedResponse = new HintResponse
        {
            HintText = "Think about using a dictionary",
            HintLevel = 1,
            FollowUpQuestion = "What key would you use?",
            HasMoreHints = true
        };

        _hintService.GetHintAsync(request, _userId, Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        var actionResult = await _sut.RequestHint(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var envelope = Assert.IsType<ApiResponse>(okResult.Value);
        Assert.True(envelope.Success);
        var data = Assert.IsType<HintResponse>(envelope.Data);
        Assert.Equal(expectedResponse.HintText, data.HintText);
        Assert.Equal(1, data.HintLevel);
    }

    [Fact]
    public async Task GetHistory_ShouldReturnOk_WithHintHistoryResponse()
    {
        var problemId = Guid.NewGuid();
        var expectedHistory = new HintHistoryResponse
        {
            ProblemId = problemId,
            TotalHintsUsed = 2,
            CanRequestMore = true,
            Hints = new List<HintHistoryItem>
            {
                new() { HintLevel = 1, HintText = "Hint 1", CreatedAt = DateTime.UtcNow },
                new() { HintLevel = 2, HintText = "Hint 2", CreatedAt = DateTime.UtcNow }
            }
        };

        _hintService.GetHintHistoryAsync(problemId, _userId, Arg.Any<CancellationToken>())
            .Returns(expectedHistory);

        var actionResult = await _sut.GetHistory(problemId, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var envelope = Assert.IsType<ApiResponse>(okResult.Value);
        Assert.True(envelope.Success);
        var data = Assert.IsType<HintHistoryResponse>(envelope.Data);
        Assert.Equal(2, data.TotalHintsUsed);
        Assert.True(data.CanRequestMore);
    }
}
