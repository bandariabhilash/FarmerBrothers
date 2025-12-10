using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ReviveCall.Models
{
    public class CustomerServiceModel
    {
        public string CallReason { get; set; }
        public string CustomerName { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string MainContactName { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Comments { get; set; }

        public List<FBCallReason> CallReasonList { get; set; }
        public List<State> StateList { get; set; }

        public int WorkorderId { get; set; }
    }
}