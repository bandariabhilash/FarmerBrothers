using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ServiceApis.Models
{
    public class WorkorderRequestModel
    {
       
        public int? AccountNumber { get; set; }
       
        public int? ERFId { get; set; }
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
       
        public string? Comments { get; set; }
        public string? HoursOfOperation { get; set; }

        //ERF Related Data
        public string? SubmissionDate { get; set; }
        public string? InstallDate { get; set; }
        public string? InstallLocation { get; set; }
        public string? SiteReady { get; set; }
    }
}
