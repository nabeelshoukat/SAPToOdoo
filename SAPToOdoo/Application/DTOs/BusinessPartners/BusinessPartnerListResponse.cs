namespace SAPToOdoo.Application.DTOs.BusinessPartners
{
    public class BusinessPartnerListResponse
    {
        public string? CardCode { get; set; }
        public string? CardName { get; set; }
        public string? CardType { get; set; }
        public string? CmpPrivate { get; set; }
        public string? GroupCode { get; set; }
        public string? CntctPrsn { get; set; }
        public string? BillToDef { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? ZipCode { get; set; }
        public string? Country { get; set; }
        public string? MailCountr { get; set; }
        public string? Phone1 { get; set; }
        public string? Cellular { get; set; }
        public string? Phone2 { get; set; }
        public string? E_Mail { get; set; }
        public string? IntrntSite { get; set; }
        public string? Notes { get; set; }
        public string? Free_Text { get; set; }
        public string? VatIdUnCmp { get; set; }
        public string? NINum { get; set; }
        public string? LicTradNum { get; set; }
        public decimal? Balance { get; set; }
        public decimal? BalanceSys { get; set; }
        public decimal? CreditLine { get; set; }
        public decimal? DebtLine { get; set; }
        public string? DebPayAcct { get; set; }
        public string? HouseBank { get; set; }
        public string? HousBnkAct { get; set; }
        public string? HousBnkCry { get; set; }
        public string? ValidFor { get; set; }
        public string? FrozenFor { get; set; }
        public string? ShipToDef { get; set; }
        public DateTime? CreateDate { get; set; }
        public DateTime? UpdateDate { get; set; }
    }
}
