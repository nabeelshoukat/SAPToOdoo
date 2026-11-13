using System.ComponentModel.DataAnnotations;

namespace SAPToOdoo.Application.DTOs.Items
{
    public sealed class GetRetailItemsRequest
    {
        /// <summary>Optional exact item code, e.g. T017827.</summary>
        [MaxLength(50)]
        public string? ItemCode { get; set; }

        /// <summary>Optional. Repeat or comma-separate: ?warehouseCode=01&warehouseCode=07</summary>
        public List<string> WarehouseCode { get; set; } = new();

        /// <summary>Price list number. Default 2, as in your query.</summary>
        [Range(1, int.MaxValue)]
        public int? PriceList { get; set; }

        [Range(1, 100)]
        public int Limit { get; set; } = 20;

        [Range(0, int.MaxValue)]
        public int Offset { get; set; } = 0;
    }

    public sealed class RetailItemResponse
    {
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string? VatGroup { get; set; }          // OITM.VatGourpSa
        public string? ItemGroupName { get; set; }     // OITB.ItmsGrpNam
        public string? Barcode { get; set; }           // OITM.CodeBars
        public string? HsCode { get; set; }            // OITM.U_HSCode
        public List<PriceResponse> Prices { get; set; } = new();
        public List<WarehouseStockResponse> Warehouses { get; set; } = new();
    }


    public sealed class PriceResponse
    {
        public int PriceList { get; set; }          // ITM1.PriceList / OPLN.ListNum
        public string? PriceListName { get; set; }  // OPLN.ListName
        public decimal Price { get; set; }          // ITM1.Price
    }
}
