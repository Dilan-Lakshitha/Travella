using System;
using System.Collections.Generic;
using System.Text;

namespace Travella.Application.DTOs
{
    public class CreateCompanyResponse
    {
        public int CompanyId { get; set; }

        public int AdminUserId { get; set; }

        public string CompanyName { get; set; } = string.Empty;

        public string Slug { get; set; } = string.Empty;

        public string CompanyUrl { get; set; } = string.Empty;

        public string AdminEmail { get; set; } = string.Empty;

        public bool WelcomeEmailSent { get; set; }
    }
}
