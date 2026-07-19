using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Travella.Application.DTOs;
using Travella.Application.Services;

namespace Travella.API.Controllers
{
    [ApiController]
    [Route("api/company")]
    public class CompanyController : ControllerBase
    {
        private readonly ICompanyService _companyService;

        public CompanyController(ICompanyService companyService)
        {
            _companyService = companyService;
        }

        [HttpPost("Creation")]
        [AllowAnonymous]
        public async Task<IActionResult> Create(
            [FromBody] CreateCompanyRequest request)
        {
            var result = await _companyService.CreateCompanyAsync(request,null);

            return Ok(result);
        }

        [HttpPost("applications")]
        [AllowAnonymous]
        public async Task<IActionResult> Submit([FromBody] CreateCompanyApplicationRequest request)
        {
            var applicationId = await _companyService.SubmitApplicationAsync(request);

            return Ok(new
            {
                applicationId, message = "Company application submitted successfully."
            });
        }
    }
}
