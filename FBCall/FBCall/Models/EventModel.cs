using Antlr.Runtime.Misc;
using Syncfusion.JavaScript;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using static System.Net.Mime.MediaTypeNames;

namespace FBCall.Models
{
    public class EventModel
    {
        public string CustomerID { get; set; }
        public string SubmittedBy { get; set; }
        public int CallReason { get; set; }
        public string EquipmentLocation { get; set; }
        public string CallerName { get; set; }
        public string WorkorderContactPhone { get; set; }
        public int PriorityCode { get; set; }
        //public string myCaptcha_ValidText { get; set; }
        //public string myCaptcha { get; set; }
        //public int WorkOrderID { get; set; }

        public string CustomerZipCode { get; set; }
        public bool IsSpecificTechnician { get; set; }
        public string PreferredProvider { get; set; }
        public string Notes { get; set; }
    }
}