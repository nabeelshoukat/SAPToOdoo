namespace SAPToOdoo.Application.DTOs.Taxes
{
    public class TaxResponse
    {
        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public decimal Rate { get; set; }

        public string? Freight { get; set; }

        public int? UserSign { get; set; }

        public string? ValidForAR { get; set; }

        public string? ValidForAP { get; set; }

        public int? TfcId { get; set; }

        public string? Lock { get; set; }

        public string? TaxIcms { get; set; }

        public string? IsItmLevel { get; set; }

        public string? CfopIn { get; set; }

        public string? CfopOut { get; set; }

        public int? LogInstanc { get; set; }

        public int? UserSign2 { get; set; }

        public DateTime? UpdateDate { get; set; }

        public string? FADebit { get; set; }
    }
}
