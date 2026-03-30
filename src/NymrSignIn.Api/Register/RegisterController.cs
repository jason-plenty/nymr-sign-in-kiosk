using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NymrSignIn.Application.Register;
using NymrSignIn.Application.Register.Dtos;

namespace NymrSignIn.Api.Register;

[ApiController]
[Route("api/v1/register")]
public sealed class RegisterController : ControllerBase
{
    private readonly RegisterService _registerService;

    public RegisterController(RegisterService registerService)
    {
        _registerService = registerService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTodaysRegisterAsync(CancellationToken cancellationToken)
    {
        var result = await _registerService.GetTodaysRegisterAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost("signin")]
    [ProducesResponseType(typeof(SignInResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SignInAsync(
        [FromBody] SignInRequest request,
        [FromServices] IValidator<SignInRequest> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(
                new ValidationProblemDetails(
                    validationResult.ToDictionary()));
        }

        var result = await _registerService.SignInAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetTodaysRegisterAsync), result);
    }

    [HttpPost("{id:guid}/signout")]
    [ProducesResponseType(typeof(SignOutResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SignOutAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _registerService.SignOutAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpGet("export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportCsvAsync(CancellationToken cancellationToken)
    {
        var csvBytes = await _registerService.ExportCsvAsync(cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var fileName = $"site-register-{today:yyyy-MM-dd}.csv";
        return File(csvBytes, "text/csv", fileName);
    }

    [HttpPost("email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SendDailyEmailAsync(CancellationToken cancellationToken)
    {
        await _registerService.SendDailyEmailAsync(cancellationToken);
        return Ok(new { ok = true });
    }
}
