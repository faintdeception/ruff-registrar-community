using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services.Infrastructure;

namespace StudentRegistrar.Api.Controllers;

[ApiController]
[Route("api/settings/branding")]
[Authorize(Roles = "Administrator")]
public class BrandingSettingsController : ControllerBase
{
    private readonly IBrandingSettingsService _brandingSettingsService;

    public BrandingSettingsController(IBrandingSettingsService brandingSettingsService)
    {
        _brandingSettingsService = brandingSettingsService;
    }

    [HttpGet]
    public async Task<ActionResult<BrandingSettingsDto>> Get(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _brandingSettingsService.GetSettingsAsync(cancellationToken));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPut]
    public async Task<ActionResult<BrandingSettingsDto>> Put(UpdateBrandingSettingsDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            return Ok(await _brandingSettingsService.UpdateSettingsAsync(request, cancellationToken));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}