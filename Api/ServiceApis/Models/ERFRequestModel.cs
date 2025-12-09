using System.ComponentModel.DataAnnotations;

namespace ServiceApis.Models
{
    public class ERFRequestModel
    {
        public int AccountNumber { get; set; }
        public int ErfId { get; set; }
        [MaxLength(150, ErrorMessage = "Customer Name cannot be longer than 150 characters.")]
        public string? CustomerName { get; set; }
        public string? Address1 { get; set; }
        public string? Address2 { get; set; }
        public string? City { get; set; }
        [MaxLength(2, ErrorMessage = "State/Province should be 2 character Code.")]
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        public string? MainContactNum { get; set; }
        public string? MainContactName { get; set; }
        public string? ErfNotes { get; set; }
        public bool? CreateWorkorder { get; set; }
        public string? OrderType { get; set; }
        public string? ShipToBranch { get; set; }
        public DateTime? FormDate { get; set; }
        public DateTime? ERFReceivedDate { get; set; }
        public DateTime? ERFProcessedDate { get; set; }
        public string? InstallDate { get; set; }        
        public string? HoursofOperation { get; set; }
        public string? InstallLocation { get; set; }
        public string? SiteReady { get; set; }
        public decimal? AdditionalNSV { get; set; }
        //public decimal? CurrentNSV { get; set; }
        //public decimal? ContributionMargin { get; set; }
        //public decimal? CurrentEquipment { get; set; }
        //public decimal? AdditionalEquipment { get; set; }
        public string? ApprovalStatus { get; set; }


        public IList<ERFAccessoryModel> EquipmentData { get; set; } = new List<ERFAccessoryModel>();
        public IList<ERFAccessoryModel> ExpendableData { get; set; } = new List<ERFAccessoryModel>();
    }


    public class ERFAccessoryModel
    {
        public string Category { get; set; }
        public string Brand { get; set; }
        public int Quantity { get; set; }
        public string UsingBranch { get; set; }
        public string SubstitutionPossible { get; set; }
        public string TransType { get; set; }
        public string EqpType { get; set; }
        //public decimal LaidInCost { get; set; }
    }
    //public class ERFEquipmentModel
    //{
    //    public string EqpCategory { get; set; }
    //    public string EqpBrand { get; set; }
    //    public int EqpQuantity { get; set; }
    //    public string EqpUsingBranch { get; set; }
    //    public string EqpSubstitutionPossible { get; set; }
    //    public string EqpTransType { get; set; }
    //    public string EqpType { get; set; }        
    //    public decimal LaidInCost { get; set; }
    //}
    //public class ERFExpendableModel
    //{
    //    public string ExpCategory { get; set; }
    //    public string ExpBrand { get; set; }
    //    public int ExpQuantity { get; set; }
    //    public string ExpUsingBranch { get; set; }
    //    public string ExpSubstitutionPossible { get; set; }
    //    public string ExpTransType { get; set; }
    //    public string ExpType { get; set; }
    //}

    public class ERFStatusChangeRequestModel
    {
        public int ERFId { get; set; }
        public string Status { get; set; }
    }

    public class ERFMaintenanceDataModel
    {
       public List<ERFBrand>? BrandList { get; set; }
        public List<ERFCategory>? CategoryList { get; set; }
    }

    public class ERFBrand
    {       

       // public int BrandId { get; set; }
        public string CategoryName { get; set; }
        public string BrandName { get; set; }
        public bool IsActive { get; set; }
        public decimal? LaidInCost { get; set; }
        public decimal? CashSale { get; set; }
        public decimal? RentalCost { get; set; }
    }

    public class ERFCategory
    {
        //public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string CategoryType { get; set; }
        public bool IsActive { get; set; }
    }
}
