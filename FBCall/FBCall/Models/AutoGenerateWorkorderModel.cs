using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace FBCall.Models
{
    public enum WorkOrderManagementSubmitType
    {
        NONE = 0,
        SAVE = 1,
        NOTIFYSALES = 2,
        OVERTIMEREQUEST = 3,
        PUTONHOLD = 4,
        UPDATEAPPOINTMENT = 5,
        COMPLETE = 6,
        CREATEWORKORDER = 7,
        CREATEFEASTMOVEMENT = 8
    }

    public class AutoGenerateWorkorderModel
    {

        public AutoGenerateWorkorderModel()
        {

        }
        public AutoGenerateWorkorderModel(WorkOrder autoGenerateWorkorder)
        {

            this.WorkOrderID = autoGenerateWorkorder.WorkorderID;
            this.CustomerID = autoGenerateWorkorder.CustomerID;
            this.callReason = autoGenerateWorkorder.WorkorderCalltypeDesc;
            this.callReasonId = autoGenerateWorkorder.WorkorderCalltypeid;
            this.UserName = "WEB";
            this.CreatedDate = autoGenerateWorkorder.WorkorderEntryDate;
        }
        public NotesModel Notes { get; set; }
        public CustomerModel Customer { get; set; }
        public List<WorkorderType> WorkorderTypes { get; set; }
        public WorkOrderManagementSubmitType Operation { get; set; }
        public string CreatedBy { get; set; }
        public IList<NewNotesModel> NewNotes;
        [Required]
        public string callReason { get; set; }
        public int? callReasonId { get; set; }
        [Required]
        [MaxLength(50, ErrorMessage = "Equipment Location cannot be longer than 50 characters.")]
        public string EquipmentLocation { get; set; }
        [Required]
        [MaxLength(60, ErrorMessage = "Caller Name cannot be longer than 60 characters.")]
        public string CallerName { get; set; }
        [Required]
        [MaxLength(30, ErrorMessage = "Contact Phone Number cannot be longer than 30 characters.")]
        public string WorkorderContactPhone { get; set; }
        [Required]
        public Nullable<int> PriorityCode { get; set; }        

        public string PriorityName { get; set; }
        public IList<AllFBStatu> PriorityList;
        public int WorkOrderID { get; set; }
        public Nullable<int> CustomerID { get; set; }
        public string UserName { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
    }
}