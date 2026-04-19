using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NymrSignIn.Application.Register.Admin;
using NymrSignIn.Application.Register.Admin.Dtos;

namespace NymrSignIn.Api.Admin;

[ApiController]
[Route("api/v1/admin/register")]
[Authorize(Policy = "AdminGroup")]
public sealed class AdminRegisterController : ControllerBase
{
    private readonly AdminRegisterService _adminRegisterService;

    public AdminRegisterController(AdminRegisterService adminRegisterService)
    {
        _adminRegisterService = adminRegisterService;
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(PagedResult<RegisterEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SearchAsync(
        [FromQuery] RegisterSearchCriteria criteria,
        [FromServices] IValidator<RegisterSearchCriteria> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(criteria, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(
                new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var result = await _adminRegisterService.SearchAsync(criteria, cancellationToken);
        return Ok(result);
    }
}
