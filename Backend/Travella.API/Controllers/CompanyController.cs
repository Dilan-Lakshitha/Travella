using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Travella.Application.Services;

namespace Travella.API.Controllers
{
    [ApiController]
    [Route("api/company")]
    [Authorize(Roles = "ADMIN")]
    public class CompanyController : ControllerBase
    {
        private readonly ICompanyService _companyService;

        public CompanyController(ICompanyService companyService)
        {
            _companyService = companyService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCompanyRequest request)
        {
            var createdByClaim = User.FindFirst("userId")?.Value;
            var createdBy = int.TryParse(createdByClaim, out var value) ? value : 0;

            var companyId = await _companyService.CreateCompanyAsync(
                request.Name,
                request.Email,
                request.Phone,
                createdBy
            );

            return Ok(new { companyId });
        }
    }

    public class CreateCompanyRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
    }
}
