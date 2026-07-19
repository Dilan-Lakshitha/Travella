using System;
using System.Collections.Generic;
using System.Text;

namespace Travella.Application.DTOs
{
    public class CreateCompanyApplicationRequest
    {
        public string CompanyName { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? CompanyDescription { get; set; }
    }
}
