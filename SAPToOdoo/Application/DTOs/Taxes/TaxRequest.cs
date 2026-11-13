using System.ComponentModel.DataAnnotations;

namespace SAPToOdoo.Application.DTOs.Taxes
{
    public class TaxRequest
    {
        [Required]
        [MaxLength(8)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public decimal Rate { get; set; }

        public string? Freight { get; set; }

        public string? ValidForAR { get; set; }

        public string? ValidForAP { get; set; }

        public string? Lock { get; set; }
    }
}
