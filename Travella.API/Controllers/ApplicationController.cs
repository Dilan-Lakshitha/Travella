using Microsoft.AspNetCore.Mvc;
using Travella.Application.Services;

namespace Travella.API.Controllers
{
    [ApiController]
    [Route("api/application")]
    public class ApplicationController : ControllerBase
    {
        private readonly IApplicationService _applicationService;

        public ApplicationController(IApplicationService applicationService)
        {
            _applicationService = applicationService;
        }

        [HttpPost]
        public async Task<IActionResult> Submit([FromBody] SubmitCompanyApplicationRequest request)
        {
            var id = await _applicationService.SubmitCompanyApplicationAsync(
                request.CompanyName,
                request.Email,
                request.Phone
            );

            return Ok(new { id });
        }
    }

    public class SubmitCompanyApplicationRequest
    {
        public string CompanyName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
    }
}
