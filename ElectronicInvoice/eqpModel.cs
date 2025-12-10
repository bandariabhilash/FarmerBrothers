using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectronicInvoice
{
    class eqpModel
    {
        public int Assetid { get; set; }
        public string WorkOrderType { get; set; }
        public string Temperature { get; set; }
        public string WorkPerformedCounter { get; set; }
        public string WorkDescription { get; set; }
        public string Category { get; set; }
        public string Manufacturer { get; set; }
        public string Model { get; set; }
        public string Location { get; set; }
        public string SerialNumber { get; set; }
        public bool? QualityIssue { get; set; }
        public string Email { get; set; }
        public string SymptomDesc { get; set; }
        public string SolutionDesc { get; set; }
        public string Weight { get; set; }
        public string Ratio { get; set; }

        public IList<WOParts> Parts;
    }

    class WOParts
    {
        public int? PartsIssueid { get; set; }
        public int? Quantity { get; set; }
        public string Manufacturer { get; set; }
        public string Sku { get; set; }
        public string Description { get; set; }
        public bool? Issue { get; set; }
        public decimal skuCost { get; set; }
        public decimal partsTotal { get; set; }

        public WOParts(WorkorderPart workOrderPart)
        {
            PartsIssueid = workOrderPart.PartsIssueid;
            Quantity = workOrderPart.Quantity;
            if (!string.IsNullOrWhiteSpace(workOrderPart.Manufacturer))
            {
                Manufacturer = workOrderPart.Manufacturer.ToUpper().Trim();
            }
            if (!string.IsNullOrWhiteSpace(workOrderPart.Sku))
            {
                Sku = workOrderPart.Sku.ToUpper().Trim();
            }
            if (!string.IsNullOrWhiteSpace(workOrderPart.Description))
            {
                Description = workOrderPart.Description;
            }
            Issue = workOrderPart.NonSerializedIssue;
        }
    }

   
    
}
