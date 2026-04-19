using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NymrSignIn.Application.Register;
using NymrSignIn.Application.Register.Dtos;

namespace NymrSignIn.Api.Register;

[ApiController]
[Route("api/v1/register")]
[AllowAnonymous]
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
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var result = await _registerService.SignInAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPost("{id:guid}/confirm-fit")]
    [ProducesResponseType(typeof(ConfirmFitResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ConfirmFitAsync(
        Guid id,
        [FromBody] ConfirmFitRequest request,
        [FromServices] IValidator<ConfirmFitRequest> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        try
        {
            var result = await _registerService.ConfirmFitAsync(id, request, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Cannot declare fit",
                detail: ex.Message);
        }
    }

    [HttpPost("{id:guid}/declare-not-fit")]
    [ProducesResponseType(typeof(DeclareNotFitResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeclareNotFitAsync(
        Guid id,
        [FromBody] DeclareNotFitRequest request,
        [FromServices] IValidator<DeclareNotFitRequest> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        try
        {
            var result = await _registerService.DeclareNotFitAsync(id, request, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Cannot declare not fit",
                detail: ex.Message);
        }
    }

    [HttpPost("{id:guid}/submit-site-code")]
    [ProducesResponseType(typeof(ConfirmFitResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SubmitSiteCodeAsync(
        Guid id,
        [FromBody] SubmitSiteCodeRequest request,
        [FromServices] IValidator<SubmitSiteCodeRequest> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        try
        {
            var result = await _registerService.SubmitSiteCodeAsync(id, request, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidSiteCodeException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Invalid site code",
                detail: ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Cannot submit site code",
                detail: ex.Message);
        }
    }

    [HttpPost("{id:guid}/signout")]
    [ProducesResponseType(typeof(SignOutResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SignOutAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _registerService.SignOutAsync(id, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
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
