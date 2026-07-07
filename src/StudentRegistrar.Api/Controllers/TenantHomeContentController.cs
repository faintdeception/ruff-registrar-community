using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services;

namespace StudentRegistrar.Api.Controllers;

[ApiController]
[Route("api/tenant-home-content")]
[Authorize]
public class TenantHomeContentController : ControllerBase
{
    private readonly ITenantHomeContentService _tenantHomeContentService;

    public TenantHomeContentController(ITenantHomeContentService tenantHomeContentService)
    {
        _tenantHomeContentService = tenantHomeContentService;
    }

    [HttpGet]
    public async Task<ActionResult<TenantHomeContentDto>> Get(CancellationToken cancellationToken)
    {
        var result = await _tenantHomeContentService.GetHomeContentAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPut]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<TenantHomeContentDto>> Update(
        [FromBody] UpdateTenantHomeContentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _tenantHomeContentService.UpdateHomeContentAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
