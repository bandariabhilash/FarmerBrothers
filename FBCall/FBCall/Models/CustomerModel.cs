using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Configuration;

namespace FBCall.Models
{
    public class CustomerModel
    {
        public CustomerModel()
        {            
        }
        [Range(1, 999999999, ErrorMessage = "Account Number between 1 to 999999999")]      
        public string CustomerId { get; set; }
        [MaxLength(100, ErrorMessage = "Customer Name cannot be longer than 100 characters.")]
        public string CustomerName { get; set; }
        [MaxLength(150, ErrorMessage = "Address1 cannot be longer than 150 characters.")]
        public string Address { get; set; }
        [MaxLength(100, ErrorMessage = "Address2 cannot be longer than 100 characters.")]
        public string Address2 { get; set; }
        [MaxLength(50, ErrorMessage = "City cannot be longer than 50 characters.")]
        public string City { get; set; }
        public string State { get; set; }
        [MaxLength(12, ErrorMessage = "Zip Code cannot be longer than 12 characters.")]
        public string ZipCode { get; set; }
        [MaxLength(50, ErrorMessage = "Main Contact Name cannot be longer than 50 characters.")]
        public string MainContactName { get; set; }
        [MaxLength(100, ErrorMessage = "Name cannot be longer than 100 characters.")]
        public string SubmittedBy { get; set; }

        public string AreaCode { get; set; }

        [MaxLength(30, ErrorMessage = "Phone Number cannot be longer than 30 characters.")]
        //[DataType(DataType.PhoneNumber)]
        //[RegularExpression(@"^\(?([0-9]{3})\)?[-. ]?([0-9]{3})[-. ]?([0-9]{4})$", ErrorMessage = "Not a valid Phone number")]
        public string PhoneNumber { get; set; }
        public string PhoneExtn { get; set; }
        [MaxLength(150, ErrorMessage = "Email Address cannot be longer than 150 characters.")]
        public string MainEmailAddress { get; set; }
        public string CustomerPreference { get; set; }
        public string RSM { get; set; }
        public string TSM { get; set; }
        public string TSMPhone { get; set; }
        public string TSMEmailAddress { get; set; }
        public string MarketSegment { get; set; }
        public string ProgramName { get; set; }
        public string DistributorName { get; set; }
        public string ServiceTier { get; set; }
        public string ServiceLevel { get; set; }

        public bool Billable { get; set; }
        public string CustomerType { get; set; }

        public string CoverageZone { get; set; }
        public string TechTeamLead { get; set; }
        public int? TechTeamLeadId { get; set; }
        public string TechType { get; set; }
        public int? ResponsibleTechId { get; set; }
        public string ResponsibleTechName { get; set; }
        public string ResponsibleTechPhone { get; set; }
        public string ResponsibleTechBranch { get; set; }
        public int? ResponsibleTechBranchId { get; set; }
        public string SecondaryTechName { get; set; }
        public string SecondaryTechPhone { get; set; }
        public int? SecondaryTechId { get; set; }
        public int? SecondaryTechBranchId { get; set; }
        public string SecondaryTechBrach { get; set; }
        public string FSMName { get; set; }
        public int? FSMId { get; set; }
        public string CustomerSpecialInstructions { get; set; }
        public string CustomerTimeZone { get; set; }
        public string CurrentTime { get; set; }

        public int FBProviderID { get; set; }
        public string PreferredProvider { get; set; }
        public string ManagerName { get; set; }
        public string DSMName { get; set; }
        public string ESMName { get; set; }
        public string RSMName { get; set; }
        public string Branch { get; set; }
        public string PricingParent { get; set; }
        public string SrviceTier { get; set; }
        public string ProviderPhone { get; set; }
        public string ManagerPhone { get; set; }
        public string DSMPhone { get; set; }
        public string ESMphone { get; set; }
        public string RSMphone { get; set; }
        public string Region { get; set; }
        public string Route { get; set; }

        public string WorkOrderId { get; set; }
        public string ErfId { get; set; }

        public string LastSaleDate { get; set; }


        public static string GetServiceTier(string serviceLevelCode)
        {
            string StrTierDesc = string.Empty;
            switch (serviceLevelCode)
            {
                case "001":
                    StrTierDesc = "Tier:001  ";
                    break;
                case "002":
                    StrTierDesc = "Tier:002  ";
                    break;
                case "003":
                    StrTierDesc = "Tier:003  ";
                    break;
                case "004":
                    StrTierDesc = "Tier:004  ";
                    break;
                case "005":
                case "0S5":
                case "NA1":
                case "NA2":
                case "NA3":
                case "NA4":
                case "NA5":
                case "NA6":
                case "NS5":
                case "NSW":
                    StrTierDesc = "Tier:005  ";

                    break;
                default:
                    StrTierDesc = "Tier:003  ";
                    break;
            }
            return StrTierDesc;
        }

        public CustomerModel(Contact customer, FBWWOCallEntities MarsServiceEntitites)
        {

         
            this.CustomerId = customer.ContactID.ToString();
            this.CustomerName = customer.CompanyName;
            this.Address = customer.Address1;
            this.Address2 = customer.Address2;
            this.City = customer.City;
            this.State = customer.State;
            this.ZipCode = customer.PostalCode;
            this.MainContactName = customer.FirstName + ' ' + customer.LastName;
            this.AreaCode = customer.AreaCode;
            
            
        }

    }
}