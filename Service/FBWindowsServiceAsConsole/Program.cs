using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using WOScheduleEventService;

namespace FBWindowsServiceAsConsole
{
    public enum MailType
    {
        INFO,
        DISPATCH,
        REDIRECTED,
        SPAWN,
        SALESNOTIFICATION
    }
    public class Program
    {
        public FarmerBrothersEntities FarmerBrothersEntitites = new FarmerBrothersEntities();
        static void Main(string[] args)
        {
       //     Error.WriteLogIntoDB(string.Format("Service started :  {0}", DateTime.UtcNow));
            //Console.WriteLine("Start");
            Program service = new Program();
            service.ProcessWorkOrderScheduleEvents();
            //Console.WriteLine("Stop");
      //      Error.WriteLogIntoDB(string.Format("Service stoped: {0}", DateTime.UtcNow));
            // //Console.ReadLine();

        }

        public void ProcessWorkOrderScheduleEvents()
        {
            try
            {
                //Console.WriteLine("Service Started");
                var workOrdres = (from w in FarmerBrothersEntitites.WorkOrders
                                  join ws in FarmerBrothersEntitites.WorkorderSchedules
                                  on w.WorkorderID equals ws.WorkorderID
                                  where w.WorkorderCallstatus == "Scheduled" &&
                                  ws.AssignedStatus == "Scheduled" &&
                            DbFunctions.TruncateTime(ws.EventScheduleDate) == DbFunctions.TruncateTime(DateTime.Now)
                                  select w).ToList();

                //Console.WriteLine("WO Events count " + workOrdres.Count);
                //Console.ReadLine();
                int i = 1;
                string failedWorkOrders = string.Empty;
                foreach (var workorder in workOrdres)
                {
                    WorkOrder workOrder = FarmerBrothersEntitites.WorkOrders.FirstOrDefault(w => w.WorkorderID == workorder.WorkorderID);
                    WorkorderSchedule workOrderSchedule = workOrder.WorkorderSchedules.Where(ws => ws.AssignedStatus == "Scheduled").FirstOrDefault();
                    //string value = Convert.ToDateTime(workOrderSchedule.EventScheduleDate).ToString("HHmm");
                    string hour = Convert.ToDateTime(workOrderSchedule.EventScheduleDate).ToString("HH");
                    string Minute = Convert.ToDateTime(workOrderSchedule.EventScheduleDate).ToString("mm");
                    //Console.WriteLine(DateTime.Now.Hour +" + <= +"+ Convert.ToInt32(hour)+" ----"+ DateTime.Now.Minute + " + <= +" + Convert.ToInt32(Minute));
                    //Console.ReadLine();
                    if (DateTime.Now.Hour >= Convert.ToInt32(hour))
                    {
                        //Console.WriteLine("Events " + i+" started");


                        StringBuilder subject = new StringBuilder();

                        subject.Append("Work Order ID#: ");
                        subject.Append(workOrder.WorkorderID);
                        subject.Append(" Customer: ");
                        subject.Append(workOrder.CustomerName);
                        subject.Append(" ST: ");
                        subject.Append(workOrder.CustomerState);
                        subject.Append(" Call Type: ");
                        subject.Append(workOrder.WorkorderCalltypeDesc);


                        string emailAddress = string.Empty;
                        //WorkorderSchedule workOrderSchedule = workorder.WorkorderSchedules.Where(ws => ws.AssignedStatus == "Scheduled").FirstOrDefault();
                        TECH_HIERARCHY techView = GetTechById(workOrderSchedule.Techid);
                        if (Convert.ToBoolean(ConfigurationManager.AppSettings["UseTestMails"]))
                        {
                            emailAddress = ConfigurationManager.AppSettings["TestEmail"];
                        }
                        else
                        {
                            if (techView != null)
                            {
                                if (!string.IsNullOrEmpty(techView.RimEmail))
                                {
                                    emailAddress = techView.RimEmail;
                                }

                                if (!string.IsNullOrEmpty(techView.EmailCC))
                                {
                                    emailAddress += "#" + techView.EmailCC;
                                }
                            }
                        }
                        //Console.WriteLine("Events " + i + " email process started WO ID"+workOrder.WorkorderID);
                       //--- bool isEventDispatched = SendWorkOrderMail(workOrder, subject.ToString(), emailAddress, ConfigurationManager.AppSettings["DispatchMailFromAddress"], workOrderSchedule.Techid, MailType.DISPATCH, true, null);
                        //Console.WriteLine("Events " + i + " email process ended WO ID" + workOrder.WorkorderID + " isEventDispatched ="+ isEventDispatched);
                        //Console.ReadLine();
                        if (workOrder != null)// && isEventDispatched)
                        {
                            DateTime currentTime = Utility.GetCurrentTime(workOrder.CustomerZipCode, FarmerBrothersEntitites);
                            workOrder.WorkorderCallstatus = "Pending Acceptance";
                            workOrder.ModifiedUserName = "SCHEDULE EVENTS";
                            workOrder.WorkorderModifiedDate = currentTime;
                            workOrderSchedule.AssignedStatus = "Sent";

                            TechHierarchyView techHierarchyView = Utility.GetTechDataByResponsibleTechId(FarmerBrothersEntitites, workOrderSchedule.Techid);

                            NotesHistory notesHistory = new NotesHistory()
                            {
                                AutomaticNotes = 1,
                                EntryDate = currentTime,
                                Notes = "Work order sent to " + techHierarchyView.PreferredProvider,
                                UserName = "SCHEDULE EVENTS"
                            };
                            workOrder.NotesHistories.Add(notesHistory);
                            bool isEventDispatched = SendWorkOrderMail(workOrder, subject.ToString(), emailAddress, ConfigurationManager.AppSettings["DispatchMailFromAddress"], workOrderSchedule.Techid, MailType.DISPATCH, true, null);
                            FarmerBrothersEntitites.SaveChanges();
                        }
                        else
                        {
                            failedWorkOrders += workorder.WorkorderID + Environment.NewLine;
                        }
                    }
                    else
                    {
                        //Console.WriteLine("Event time is not matched");
                    }
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex.Message);
                //Console.ReadLine();
                Error.LogError(ex);

            }

        }

        public bool SendWorkOrderMail_old(WorkOrder workOrder, string subject, string toAddress, string fromAddress, int? techId, MailType mailType, bool isResponsible, string additionalMessage, string mailFrom = "", bool isFromEmailCloserLink = false, string SalesEmailAddress = "", string esmEmailAddress = "")
        {
            bool result = true;
            try
            {
                Contact customer = FarmerBrothersEntitites.Contacts.Where(c => c.ContactID == workOrder.CustomerID).FirstOrDefault();
                int TotalCallsCount = CustomerModel.GetCallsTotalCount(FarmerBrothersEntitites, workOrder.CustomerID.ToString());

                List<CustomerNotesModel> CustomerNotesResults = new List<CustomerNotesModel>();
                int? custId = Convert.ToInt32(workOrder.CustomerID);
                var custNotes = FarmerBrothersEntitites.FBCustomerNotes.Where(c => c.CustomerId == custId && c.IsActive == true).ToList();

                string IsBillable = "";
                string ServiceLevelDesc = "";
                if (!string.IsNullOrEmpty(customer.BillingCode))
                {
                    IsBillable = CustomerModel.IsBillableService(customer.BillingCode, TotalCallsCount);
                    ServiceLevelDesc = CustomerModel.GetServiceLevelDesc(FarmerBrothersEntitites, customer.BillingCode);
                }
                else
                {
                    IsBillable = " ";
                    ServiceLevelDesc = " - ";
                }

                StringBuilder salesEmailBody = new StringBuilder();

                salesEmailBody.Append(@"<img src='cid:logo' width='15%' height='15%'>");

                salesEmailBody.Append("<BR>");
                salesEmailBody.Append("<BR>");
                salesEmailBody.Append("<BR>");
                string url = ConfigurationManager.AppSettings["DispatchResponseUrl"];
                string Redircturl = ConfigurationManager.AppSettings["RedirectResponseUrl"];
                string Closureurl = ConfigurationManager.AppSettings["CallClosureUrl"];
                //string finalUrl = string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=@response&isResponsible=" + isResponsible.ToString()));
                if (isFromEmailCloserLink)
                {
                    salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
                    salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=3&isResponsible=" + isResponsible.ToString())) + "\">COMPLETED</a>");
                    salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
                    salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=7&isResponsible=" + isResponsible.ToString() + "&isBillable=" + (IsBillable == "True" ? "True" : "False"))) + "\">CLOSE WORK ORDER</a>");
                    salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
                }
                else
                {
                    if ((mailType == MailType.DISPATCH || mailType == MailType.SPAWN) && techId.HasValue)
                    {
                        if (string.Compare(workOrder.WorkorderCallstatus, "Closed", true) != 0)
                        {
                            TECH_HIERARCHY techView = FarmerBrothersEntitites.TECH_HIERARCHY.Where(x => x.DealerId == techId).FirstOrDefault();
                            if (mailType == MailType.DISPATCH)
                            {
                                //salesEmailBody.Append("<a href=\"" + url + "?workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=0&isResponsible=" + isResponsible + "\">ACCEPT</a>");                        
                                salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=0&isResponsible=" + isResponsible.ToString())) + "\">ACCEPT</a>");
                                salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
                            }
                            if (workOrder.WorkorderCallstatus == "Pending Acceptance" && techView.FamilyAff != "SPT")
                            {
                                string redirectFinalUrl = string.Format("{0}{1}&encrypt=yes", Redircturl, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=5&isResponsible=" + isResponsible.ToString()));
                                //salesEmailBody.Append("<a href=\"" + Redircturl + "?workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=5&isResponsible=" + isResponsible + "\">REDIRECT</a>");
                                salesEmailBody.Append("<a href=\"" + redirectFinalUrl + "\">REDIRECT</a>");
                                salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
                            }
                            salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=1&isResponsible=" + isResponsible.ToString())) + "\">REJECT</a>");
                            salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
                            salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=6&isResponsible=" + isResponsible.ToString())) + "\">START</a>");
                            salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
                            salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=2&isResponsible=" + isResponsible.ToString())) + "\">ARRIVAL</a>");
                            salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
                            salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=3&isResponsible=" + isResponsible.ToString())) + "\">COMPLETED</a>");
                            salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
                            salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=7&isResponsible=" + isResponsible.ToString() + "&isBillable=" + (IsBillable == "True" ? "True" : "False"))) + "\">CLOSE WORK ORDER</a>");
                            salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
                            salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=8&isResponsible=" + isResponsible.ToString())) + "\">SCHEDULE EVENT</a>");
                            salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");

                        }
                    }
                    else if (mailType == MailType.REDIRECTED)
                    {
                        //salesEmailBody.Append("<a href=\"" + url + "?workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=4&isResponsible=" + isResponsible + "\">DISREGARD</a>");
                        salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=4&isResponsible=" + isResponsible.ToString())) + "\">DISREGARD</a>");
                    }

                }

                salesEmailBody.Append("<BR>");
                salesEmailBody.Append("<BR>");

                TECH_HIERARCHY tchView = FarmerBrothersEntitites.TECH_HIERARCHY.Where(x => x.DealerId == techId).FirstOrDefault();
                if (tchView != null && tchView.FamilyAff.ToUpper() == "SPT")
                {
                    salesEmailBody.Append("<span style='color:#ff0000'><b>");
                    salesEmailBody.Append("Third Party Dispatch ");
                    salesEmailBody.Append("</b></span>");
                }
                salesEmailBody.Append("<BR>");
                salesEmailBody.Append("<BR>");

                if (!string.IsNullOrEmpty(additionalMessage) && mailFrom == "TRANSMIT")
                {
                    salesEmailBody.Append("<b>ADDITIONAL NOTES: </b>");
                    salesEmailBody.Append(Utility.GetStringWithNewLine(additionalMessage));
                    salesEmailBody.Append("<BR>");
                }

                if (custNotes != null && custNotes.Count > 0)
                {
                    salesEmailBody.Append("<b>CUSTOMER NOTES: </b>");
                    salesEmailBody.Append(Environment.NewLine);
                    foreach (var dbCustNotes in custNotes)
                    {
                        salesEmailBody.Append("[" + dbCustNotes.UserName + "] : " + dbCustNotes.Notes + Environment.NewLine);
                    }
                    salesEmailBody.Append("<BR>");
                }

                if (!string.IsNullOrEmpty(additionalMessage) && mailFrom == "ESCALATION")
                {
                    salesEmailBody.Append("<span style='color:#ff0000'><b>");
                    salesEmailBody.Append("ESCALATION NOTES: ");
                    salesEmailBody.Append(Utility.GetStringWithNewLine(additionalMessage));
                    salesEmailBody.Append("</b></span>");
                    salesEmailBody.Append("<BR>");
                }
                salesEmailBody.Append("CALL TIME: ");
                salesEmailBody.Append(workOrder.WorkorderEntryDate);
                salesEmailBody.Append("<BR>");

                salesEmailBody.Append("Work Order ID#: ");
                salesEmailBody.Append(workOrder.WorkorderID);
                /*salesEmailBody.Append("<BR>");
                salesEmailBody.Append("<BR>");
                salesEmailBody.Append("Service Level: ");
                salesEmailBody.Append(ServiceLevelDesc);
                salesEmailBody.Append("<BR>");
                salesEmailBody.Append("Billable: ");
                //salesEmailBody.Append(IsBillable);
                if (IsBillable == "True")
                    salesEmailBody.Append("Billable");
                else if (IsBillable == "False")
                    salesEmailBody.Append("Non-Billable");*/
                salesEmailBody.Append("<BR>");
                salesEmailBody.Append("<BR>");
                salesEmailBody.Append("CUSTOMER INFORMATION: ");
                salesEmailBody.Append("<BR>");
                salesEmailBody.Append("CUSTOMER#: ");
                salesEmailBody.Append(workOrder.CustomerID);
                salesEmailBody.Append("<BR>");
                salesEmailBody.Append(workOrder.CustomerName);
                salesEmailBody.Append("<BR>");
                salesEmailBody.Append(customer.Address1);
                salesEmailBody.Append(",");
                salesEmailBody.Append(customer.Address2);
                salesEmailBody.Append("<BR>");
                salesEmailBody.Append(workOrder.CustomerCity);
                salesEmailBody.Append(",");
                salesEmailBody.Append(workOrder.CustomerState);
                salesEmailBody.Append(" ");
                salesEmailBody.Append(workOrder.CustomerZipCode);
                salesEmailBody.Append("<BR>");
                salesEmailBody.Append(workOrder.WorkorderContactName);
                salesEmailBody.Append("<BR>");
                salesEmailBody.Append("PHONE: ");
                salesEmailBody.Append(workOrder.WorkorderContactPhone);
                salesEmailBody.Append("<BR>");

                salesEmailBody.Append("BRANCH: ");
                salesEmailBody.Append(customer.Branch);
                salesEmailBody.Append("<BR>");
                salesEmailBody.Append("ROUTE#: ");
                salesEmailBody.Append(customer.Route);
                salesEmailBody.Append("<BR>");
                if (workOrder.FollowupCallID == 601 || workOrder.FollowupCallID == 602)
                {
                    int? followupId = workOrder.FollowupCallID;
                    AllFBStatu status = FarmerBrothersEntitites.AllFBStatus.Where(s => s.FBStatusID == followupId).FirstOrDefault();
                    if (status != null && !string.IsNullOrEmpty(status.FBStatus))
                    {
                        //salesEmailBody.Append("Follow Up Reason: ");
                        //salesEmailBody.Append(status.FBStatus);
                        if (workOrder.FollowupCallID == 601)
                            salesEmailBody.Append("Customer requesting an ETA phone call within the hour");
                        else if (workOrder.FollowupCallID == 602)
                            salesEmailBody.Append("Contact Customer Within The Hour");
                        salesEmailBody.Append("<BR>");
                    }
                }
                salesEmailBody.Append("<span style='color:#ff0000'><b>");
                salesEmailBody.Append("LAST SALES DATE: ");
                salesEmailBody.Append(GetCustomerById(workOrder.CustomerID).LastSaleDate);
                salesEmailBody.Append("</b></span>");
                salesEmailBody.Append("<BR>");

                salesEmailBody.Append("HOURS OF OPERATION: ");
                salesEmailBody.Append(workOrder.HoursOfOperation);
                salesEmailBody.Append("<BR>");

                salesEmailBody.Append("<BR>");
                salesEmailBody.Append("CALL CODES: ");
                salesEmailBody.Append("<BR>");

                foreach (WorkorderEquipmentRequested equipment in workOrder.WorkorderEquipmentRequesteds)
                {
                    salesEmailBody.Append("EQUIPMENT TYPE: ");
                    salesEmailBody.Append(equipment.Category);
                    salesEmailBody.Append("<BR>");

                    WorkorderType callType = FarmerBrothersEntitites.WorkorderTypes.Where(w => w.CallTypeID == equipment.CallTypeid).FirstOrDefault();
                    if (callType != null)
                    {
                        salesEmailBody.Append("SERVICE CODE: ");
                        salesEmailBody.Append(callType.CallTypeID);
                        salesEmailBody.Append(" - ");
                        salesEmailBody.Append(callType.Description);
                        salesEmailBody.Append("<BR>");
                    }
                    Symptom symptom = FarmerBrothersEntitites.Symptoms.Where(s => s.SymptomID == equipment.Symptomid).FirstOrDefault();
                    if (symptom != null)
                    {
                        salesEmailBody.Append("SYMPTOM: ");
                        salesEmailBody.Append(symptom.SymptomID);
                        salesEmailBody.Append(" - ");
                        salesEmailBody.Append(symptom.Description);
                        salesEmailBody.Append("<BR>");
                    }
                    salesEmailBody.Append("LOCATION: ");
                    salesEmailBody.Append(equipment.Location);
                    salesEmailBody.Append("<BR>");
                    salesEmailBody.Append("SERIAL NUMBER: ");
                    salesEmailBody.Append(equipment.SerialNumber);

                    salesEmailBody.Append("<BR>");
                }

                salesEmailBody.Append("<BR>");
                salesEmailBody.Append("CALL NOTES: ");
                salesEmailBody.Append("<BR>");
                IEnumerable<NotesHistory> histories = workOrder.NotesHistories.OrderByDescending(n => n.EntryDate);

                foreach (NotesHistory history in histories)
                {
                    //Remove Redirected/Rejected notes for 3rd Party Tech
                    if (tchView != null && tchView.FamilyAff.ToUpper() == "SPT")
                    {
                        if (history.Notes.ToLower().Contains("redirected") || history.Notes.ToLower().Contains("rejected") || history.Notes.ToLower().Contains("declined"))
                        {
                            continue;
                        }
                    }

                    salesEmailBody.Append(history.UserName);
                    salesEmailBody.Append(" ");
                    salesEmailBody.Append(history.EntryDate);
                    salesEmailBody.Append(" ");
                    salesEmailBody.Append(history.Notes);
                    salesEmailBody.Append("<BR>");
                }

                salesEmailBody.Append("<BR>");
                salesEmailBody.Append("SERVICE HISTORY:");
                salesEmailBody.Append("<BR>");

                DateTime currentTime = Utility.GetCurrentTime(workOrder.CustomerZipCode, FarmerBrothersEntitites);

                /*IEnumerable<WorkOrder> previousWorkOrders = FarmerBrothersEntitites.WorkOrders.
                    Where(w => w.CustomerID == workOrder.CustomerID && (DbFunctions.DiffDays(w.WorkorderEntryDate, currentTime) < 90
                                  && DbFunctions.DiffDays(w.WorkorderEntryDate, currentTime) > -90));*/

                IEnumerable<WorkOrder> previousWorkOrders = FarmerBrothersEntitites.WorkOrders.
                   Where(w => w.CustomerID == workOrder.CustomerID).OrderByDescending(ed => ed.WorkorderEntryDate).Take(5);


                foreach (WorkOrder previousWorkOrder in previousWorkOrders)
                {
                    salesEmailBody.Append("Work Order ID#: ");
                    salesEmailBody.Append(previousWorkOrder.WorkorderID);
                    salesEmailBody.Append("<BR>");
                    salesEmailBody.Append("ENTRY DATE: ");
                    salesEmailBody.Append(previousWorkOrder.WorkorderEntryDate);
                    salesEmailBody.Append("<BR>");
                    salesEmailBody.Append("STATUS: ");
                    salesEmailBody.Append(previousWorkOrder.WorkorderCallstatus);
                    salesEmailBody.Append("<BR>");
                    salesEmailBody.Append("CALL CODES: ");
                    salesEmailBody.Append("<BR>");

                    foreach (WorkorderEquipment equipment in previousWorkOrder.WorkorderEquipments)
                    {
                        salesEmailBody.Append("MAKE: ");
                        salesEmailBody.Append(equipment.Manufacturer);
                        salesEmailBody.Append("<BR>");
                        salesEmailBody.Append("MODEL#: ");
                        salesEmailBody.Append(equipment.Model);
                        salesEmailBody.Append("<BR>");

                        WorkorderType callType = FarmerBrothersEntitites.WorkorderTypes.Where(w => w.CallTypeID == equipment.CallTypeid).FirstOrDefault();
                        if (callType != null)
                        {
                            salesEmailBody.Append("SERVICE CODE: ");
                            salesEmailBody.Append(callType.CallTypeID);
                            salesEmailBody.Append(" - ");
                            salesEmailBody.Append(callType.Description);
                            salesEmailBody.Append("<BR>");
                        }

                        Symptom symptom = FarmerBrothersEntitites.Symptoms.Where(s => s.SymptomID == equipment.Symptomid).FirstOrDefault();
                        if (symptom != null)
                        {
                            salesEmailBody.Append("SYMPTOM: ");
                            salesEmailBody.Append(symptom.SymptomID);
                            salesEmailBody.Append(" - ");
                            salesEmailBody.Append(symptom.Description);
                            salesEmailBody.Append("<BR>");
                        }

                        salesEmailBody.Append("Location: ");
                        salesEmailBody.Append(equipment.Location);
                        salesEmailBody.Append("<BR>");
                    }
                    salesEmailBody.Append("<BR>");
                }

                salesEmailBody.Append("<BR>");
                salesEmailBody.Append("<BR>");
                if (isFromEmailCloserLink)
                {
                    salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
                    salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=3&isResponsible=" + isResponsible.ToString())) + "\">COMPLETED</a>");
                    salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
                    salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=7&isResponsible=" + isResponsible.ToString() + "&isBillable=" + (IsBillable == "True" ? "True" : "False"))) + "\">CLOSE WORK ORDER</a>");
                    salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");

                }
                else
                {
                    if ((mailType == MailType.DISPATCH || mailType == MailType.SPAWN) && techId.HasValue)
                    {
                        if (string.Compare(workOrder.WorkorderCallstatus, "Closed", true) != 0)
                        {
                            TECH_HIERARCHY techView = FarmerBrothersEntitites.TECH_HIERARCHY.Where(x => x.DealerId == techId).FirstOrDefault();
                            if (mailType == MailType.DISPATCH)
                            {
                                //salesEmailBody.Append("<a href=\"" + url + "?workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=0&isResponsible=" + isResponsible + "\">ACCEPT</a>");
                                salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=0&isResponsible=" + isResponsible.ToString())) + "\">ACCEPT</a>");
                                salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
                            }
                            if (workOrder.WorkorderCallstatus == "Pending Acceptance" && techView.FamilyAff != "SPT")
                            {
                                // salesEmailBody.Append("<a href=\"" + Redircturl + "?workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=5&isResponsible=" + isResponsible + "\">REDIRECT</a>");
                                string redirectFinalUrl = string.Format("{0}{1}&encrypt=yes", Redircturl, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=5&isResponsible=" + isResponsible.ToString()));
                                salesEmailBody.Append("<a href=\"" + redirectFinalUrl + "\">REDIRECT</a>");
                                salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
                            }
                            salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=1&isResponsible=" + isResponsible.ToString())) + "\">REJECT</a>");
                            salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
                            salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=6&isResponsible=" + isResponsible.ToString())) + "\">START</a>");
                            salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
                            salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=2&isResponsible=" + isResponsible.ToString())) + "\">ARRIVAL</a>");
                            salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
                            salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=3&isResponsible=" + isResponsible.ToString())) + "\">COMPLETED</a>");
                            salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
                            salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=7&isResponsible=" + isResponsible.ToString() + "&isBillable=" + (IsBillable == "True" ? "True" : "False"))) + "\">CLOSE WORK ORDER</a>");
                            salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
                            salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=8&isResponsible=" + isResponsible.ToString())) + "\">SCHEDULE EVENT</a>");
                            salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");

                        }
                    }
                    else if (mailType == MailType.REDIRECTED)
                    {
                        //salesEmailBody.Append("<a href=\"" + url + "?workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=4&isResponsible=" + isResponsible + "\">DISREGARD</a>");
                        salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=4&isResponsible=" + isResponsible.ToString())) + "\">DISREGARD</a>");
                    }
                }


                string contentId = Guid.NewGuid().ToString();
                string logoPath = System.AppDomain.CurrentDomain.BaseDirectory.ToString() + "/img/main-logo.png";



                salesEmailBody = salesEmailBody.Replace("cid:logo", "cid:" + contentId);

                AlternateView avHtml = AlternateView.CreateAlternateViewFromString
                   (salesEmailBody.ToString(), null, MediaTypeNames.Text.Html);

                LinkedResource inline = new LinkedResource(logoPath, MediaTypeNames.Image.Jpeg);
                inline.ContentId = contentId;
                avHtml.LinkedResources.Add(inline);

                var message = new MailMessage();

                message.AlternateViews.Add(avHtml);

                message.IsBodyHtml = true;
                message.Body = salesEmailBody.Replace("cid:logo", "cid:" + inline.ContentId).ToString();


                string mailTo = toAddress;
                string mailCC = string.Empty;
                if (!string.IsNullOrWhiteSpace(mailTo))
                {
                    if (toAddress.Contains("#"))
                    {
                        string[] mailCCAddress = toAddress.Split('#');

                        if (mailCCAddress.Count() > 0)
                        {
                            string[] CCAddresses = mailCCAddress[1].Split(';');
                            foreach (string address in CCAddresses)
                            {
                                if (!string.IsNullOrWhiteSpace(address))
                                {
                                    message.CC.Add(new MailAddress(address));
                                }
                            }
                            string[] addresses = mailCCAddress[0].Split(';');
                            foreach (string address in addresses)
                            {
                                if (!string.IsNullOrWhiteSpace(address))
                                {
                                    message.To.Add(new MailAddress(address));
                                }
                            }
                        }
                    }
                    else
                    {
                        string[] addresses = mailTo.Split(';');
                        foreach (string address in addresses)
                        {
                            if (!string.IsNullOrWhiteSpace(address))
                            {
                                message.To.Add(new MailAddress(address));
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(SalesEmailAddress))
                    {
                        if (SalesEmailAddress.Contains(";"))
                        {
                            string[] addresses = SalesEmailAddress.Split(';');
                            foreach (string address in addresses)
                            {
                                if (!string.IsNullOrWhiteSpace(address))
                                {
                                    if (address.ToLower().Contains("@jmsmucker.com")) continue;

                                    message.CC.Add(new MailAddress(address));
                                }
                            }
                        }
                        else
                        {
                            message.CC.Add(SalesEmailAddress);
                        }
                    }
                    //if (!string.IsNullOrEmpty(esmEmailAddress) && !Convert.ToBoolean(ConfigurationManager.AppSettings["UseTestMails"]))
                    //{
                    //    if (esmEmailAddress.Contains(";"))
                    //    {
                    //        string[] addresses = esmEmailAddress.Split(';');
                    //        foreach (string address in addresses)
                    //        {
                    //            if (!string.IsNullOrWhiteSpace(address))
                    //            {
                    //                if (address.ToLower().Contains("@jmsmucker.com")) continue;

                    //                message.CC.Add(new MailAddress(address));
                    //            }
                    //        }
                    //    }
                    //    else
                    //    {
                    //        message.CC.Add(esmEmailAddress);
                    //    }
                    //}

                    NonFBCustomer nonFBCustomer = FarmerBrothersEntitites.NonFBCustomers.Where(n => n.NonFBCustomerId == customer.PricingParentID).FirstOrDefault();
                    if (nonFBCustomer != null)
                    {
                        message.CC.Clear();
                    }


                    message.From = new MailAddress(fromAddress);
                    message.Subject = subject;
                    message.IsBodyHtml = true;

                    using (var smtp = new SmtpClient())
                    {
                        smtp.Host = ConfigurationManager.AppSettings["MailServer"];
                        smtp.Port = 25;

                        try
                        {
                            //Console.WriteLine("email start");
                            smtp.Send(message);
                            //Console.WriteLine("email end" );
                        }
                        catch (Exception ex)
                        {
                            result = false;
                            //Console.WriteLine("Failed to process email " + ex.Message);
                            //Console.ReadLine();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine("Failed to process email "+ex.Message);
                //Console.ReadLine();
            }

            return result;
        }

        public bool SendWorkOrderMail(WorkOrder workOrder, string subject, string toAddress, string fromAddress, int? techId, MailType mailType, bool isResponsible, string additionalMessage, string mailFrom = "", bool isFromEmailCloserLink = false, string SalesEmailAddress = "", string esmEmailAddress = "")
        {
            Contact customer = FarmerBrothersEntitites.Contacts.Where(c => c.ContactID == workOrder.CustomerID).FirstOrDefault();
            int TotalCallsCount = GetCallsTotalCount(FarmerBrothersEntitites, workOrder.CustomerID.ToString());

            //List<CustomerNotesModel> CustomerNotesResults = new List<CustomerNotesModel>();
            int? custId = Convert.ToInt32(workOrder.CustomerID);
            var custNotes = FarmerBrothersEntitites.FBCustomerNotes.Where(c => c.CustomerId == custId && c.IsActive == true).ToList();


            string BccEmailAddress = fromAddress;
            /*ESMCCMRSMEscalation esmEscalation = FarmerBrothersEntitites.ESMCCMRSMEscalations.Where(e => e.ZIPCode == workOrder.CustomerZipCode).FirstOrDefault();
            if (esmEscalation != null)
            {
                fromAddress = esmEscalation.ESMEmail != null ? esmEscalation.ESMEmail : BccEmailAddress;
            }
            else
            {
                fromAddress = BccEmailAddress;
            }*/

            StringBuilder salesEmailBodywithLinks = GetEmailBodyWithLinks(workOrder, subject, toAddress, fromAddress, techId, mailType, isResponsible, additionalMessage, mailFrom, isFromEmailCloserLink, SalesEmailAddress, esmEmailAddress);
            StringBuilder salesEmailBodywithOutLinks = GetEmailBodyWithOutLinks(workOrder, subject, toAddress, fromAddress, techId, mailType, isResponsible, additionalMessage, mailFrom, isFromEmailCloserLink, SalesEmailAddress, esmEmailAddress);
            TECH_HIERARCHY tchView = FarmerBrothersEntitites.TECH_HIERARCHY.Where(x => x.DealerId == techId).FirstOrDefault();


            string IsBillable = "";
            string ServiceLevelDesc = "";
            if (!string.IsNullOrEmpty(customer.BillingCode))
            {
                IsBillable = IsBillableService(customer.BillingCode, TotalCallsCount);
                ServiceLevelDesc = GetServiceLevelDesc(FarmerBrothersEntitites, customer.BillingCode);
            }
            else
            {
                IsBillable = " ";
                ServiceLevelDesc = " - ";
            }


            bool result = true;

            // bool toResult = sendToListEmail(salesEmailBodywithLinks, fromAddress, toAddress, BccEmailAddress, subject, techId, customer);
            // bool ccResult = sendCCListEmail(salesEmailBodywithOutLinks, fromAddress, toAddress, BccEmailAddress, subject, techId, customer, SalesEmailAddress, esmEmailAddress);

            string toMailAddress = string.Empty;
            string ccMailAddress = string.Empty;
            if (!string.IsNullOrWhiteSpace(toAddress))
            {
                if (toAddress.Contains("#"))
                {
                    string[] mailToAddress = toAddress.Split('#');
                    if (mailToAddress.Count() > 0)
                    {
                        string[] addresses = mailToAddress[0].Split(';');
                        foreach (string address in addresses)
                        {
                            if (!string.IsNullOrWhiteSpace(address))
                            {
                                if (address.ToLower().Contains("@jmsmucker.com")) continue;

                                toMailAddress += address + ";";
                            }
                        }

                        string[] ccaddresses = mailToAddress[1].Split(';');
                        foreach (string address in ccaddresses)
                        {
                            if (!string.IsNullOrWhiteSpace(address))
                            {
                                if (address.ToLower().Contains("@jmsmucker.com")) continue;

                                ccMailAddress += address + ";";
                            }
                        }
                    }
                }
                else
                {
                    string[] addresses = toAddress.Split(';');
                    foreach (string address in addresses)
                    {
                        if (!string.IsNullOrWhiteSpace(address))
                        {
                            if (address.ToLower().Contains("@jmsmucker.com")) continue;

                            toMailAddress += address + ";";
                        }
                    }
                }
            }
            bool toResult = sendToListEmail(salesEmailBodywithLinks, fromAddress, toAddress, BccEmailAddress, subject, techId, customer);

            FBCustomerServiceDistribution fbcs = FarmerBrothersEntitites.FBCustomerServiceDistributions.Where(f => f.Route == customer.Route).FirstOrDefault();
            if (fbcs != null)
            {
                if (fbcs.SalesMmanagerEmail != null)
                {
                    ccMailAddress += fbcs.SalesMmanagerEmail + ";";
                }
                if (fbcs.RegionalsEmail != null)
                {
                    ccMailAddress += fbcs.RegionalsEmail + ";";
                }
                if (fbcs.RSREmail != null)
                {
                    ccMailAddress += fbcs.RSREmail + ";";
                }
            }

            //Included as per Email "Hardcode to Revive Parent #'s" received on Feb 24th, 2024
            if (customer.PricingParentID == "9001228")
            {
                ccMailAddress += "cfrancis@reviveservice.com";
            }
            if (customer.PricingParentID == "9001239")
            {
                ccMailAddress += "cfrancis@reviveservice.com";
            }

            bool ccResult = sendCCListEmail(salesEmailBodywithOutLinks, fromAddress, ccMailAddress, BccEmailAddress, subject, techId, customer, SalesEmailAddress, esmEmailAddress);

            //NotesHistory nh = new NotesHistory();
            //nh.WorkorderID = workOrder.WorkorderID;
            //nh.Notes = "Auto Dispatch E-mail sent to : " + toMailAddress + ", and Copied to : " + ccMailAddress;
            //nh.EntryDate = DateTime.Now;
            //nh.AutomaticNotes = 1;
            //FarmerBrothersEntitites.NotesHistories.Add(nh);

            result = toResult;

            return result;
        }

        public StringBuilder GetEmailBodyWithOutLinks(WorkOrder workOrder, string subject, string toAddress, string fromAddress, int? techId, MailType mailType, bool isResponsible, string additionalMessage, string mailFrom = "", bool isFromEmailCloserLink = false, string SalesEmailAddress = "", string esmEmailAddress = "")
        {
            StringBuilder salesEmailBody = new StringBuilder();

            Contact customer = FarmerBrothersEntitites.Contacts.Where(c => c.ContactID == workOrder.CustomerID).FirstOrDefault();
            int TotalCallsCount = GetCallsTotalCount(FarmerBrothersEntitites, workOrder.CustomerID.ToString());

            //List<CustomerNotesModel> CustomerNotesResults = new List<CustomerNotesModel>();
            int? custId = Convert.ToInt32(workOrder.CustomerID);
            var custNotes = FarmerBrothersEntitites.FBCustomerNotes.Where(c => c.CustomerId == custId && c.IsActive == true).ToList();


            string BccEmailAddress = fromAddress;
            ESMCCMRSMEscalation esmEscalation = FarmerBrothersEntitites.ESMCCMRSMEscalations.Where(e => e.ZIPCode == workOrder.CustomerZipCode).FirstOrDefault();
            if (esmEscalation != null)
            {
                fromAddress = esmEscalation.ESMEmail != null ? esmEscalation.ESMEmail : BccEmailAddress;
            }
            else
            {
                fromAddress = BccEmailAddress;
            }

            string IsBillable = "";
            string ServiceLevelDesc = "";
            if (!string.IsNullOrEmpty(customer.BillingCode))
            {
                IsBillable = IsBillableService(customer.BillingCode, TotalCallsCount);
                ServiceLevelDesc = GetServiceLevelDesc(FarmerBrothersEntitites, customer.BillingCode);
            }
            else
            {
                IsBillable = " ";
                ServiceLevelDesc = " - ";
            }

            salesEmailBody.Append(@"<img src='cid:logo' width='80' height='100'>");

            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("<BR>");

            TECH_HIERARCHY tchView = FarmerBrothersEntitites.TECH_HIERARCHY.Where(x => x.DealerId == techId).FirstOrDefault();
            if (tchView != null)
            {
                salesEmailBody.Append("<b>");
                salesEmailBody.Append("Dispatched To : ");
                salesEmailBody.Append("</b>");
                salesEmailBody.Append("<span style='color:#ff0000'><b>");
                salesEmailBody.Append(tchView.CompanyName);
                salesEmailBody.Append("</b></span>");

                if (tchView.FamilyAff.ToUpper() == "SPT")
                {
                    salesEmailBody.Append("<span style='color:#ff0000'><b>");
                    salesEmailBody.Append("Third Party Dispatch ");
                    salesEmailBody.Append("</b></span>");
                }
            }
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("<BR>");

            if (!string.IsNullOrEmpty(additionalMessage) && mailFrom == "TRANSMIT")
            {
                salesEmailBody.Append("<b>ADDITIONAL NOTES: </b>");
                salesEmailBody.Append(Environment.NewLine);
                //salesEmailBody.Append(Utility.GetStringWithNewLine(additionalMessage));
                salesEmailBody.Append(additionalMessage);
                salesEmailBody.Append("<BR>");
            }

            if (custNotes != null && custNotes.Count > 0)
            {
                salesEmailBody.Append("<b>CUSTOMER NOTES: </b>");
                salesEmailBody.Append(Environment.NewLine);
                foreach (var dbCustNotes in custNotes)
                {
                    salesEmailBody.Append("[" + dbCustNotes.UserName + "] : " + dbCustNotes.Notes + Environment.NewLine);
                }
                salesEmailBody.Append("<BR>");
            }

            if (!string.IsNullOrEmpty(additionalMessage) && mailFrom == "ESCALATION")
            {
                salesEmailBody.Append("<span style='color:#ff0000'><b>");
                salesEmailBody.Append("ESCALATION NOTES: ");
                //salesEmailBody.Append(Utility.GetStringWithNewLine(additionalMessage));
                salesEmailBody.Append(additionalMessage);
                salesEmailBody.Append("</b></span>");
                salesEmailBody.Append("<BR>");
            }
            salesEmailBody.Append("CALL TIME: ");
            salesEmailBody.Append(workOrder.WorkorderEntryDate);
            salesEmailBody.Append("<BR>");

            salesEmailBody.Append("Work Order ID#: ");
            salesEmailBody.Append(workOrder.WorkorderID);
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("ERF#: ");
            salesEmailBody.Append(workOrder.WorkorderErfid);
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("Appointment Date: ");
            salesEmailBody.Append(workOrder.AppointmentDate);
            salesEmailBody.Append("<BR>");

            WorkorderSchedule ws = FarmerBrothersEntitites.WorkorderSchedules.Where(w => w.WorkorderID == workOrder.WorkorderID && (w.AssignedStatus == "Accepted" || w.AssignedStatus == "Scheduled")).FirstOrDefault();
            string schedlDate = ws == null ? "" : ws.EventScheduleDate.ToString();

            if (workOrder.WorkorderCalltypeid == 1300)
            {
                Erf workorderERF = FarmerBrothersEntitites.Erfs.Where(ew => ew.ErfID == workOrder.WorkorderErfid).FirstOrDefault();
                schedlDate = workorderERF == null ? schedlDate : workorderERF.OriginalRequestedDate.ToString();
            }

            salesEmailBody.Append("Schedule Date: ");
            salesEmailBody.Append(schedlDate);

            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("Service Level: ");
            salesEmailBody.Append(ServiceLevelDesc);
            salesEmailBody.Append("<BR>");

            string ServiceTier = customer == null ? "" : string.IsNullOrEmpty(customer.ProfitabilityTier) ? " - " : customer.ProfitabilityTier;
            string paymentTerm = customer == null ? "" : (string.IsNullOrEmpty(customer.PaymentTerm) ? "" : customer.PaymentTerm);
            string PaymentTermDesc = "";
            if (!string.IsNullOrEmpty(paymentTerm))
            {
                JDEPaymentTerm paymentDesc = FarmerBrothersEntitites.JDEPaymentTerms.Where(c => c.PaymentTerm == paymentTerm).FirstOrDefault();
                PaymentTermDesc = paymentDesc == null ? "" : paymentDesc.Description;
            }
            else
            {
                PaymentTermDesc = "";
            }

            salesEmailBody.Append("Tier: ");
            salesEmailBody.Append(ServiceTier);
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("Payment Terms: ");
            salesEmailBody.Append(PaymentTermDesc);
            salesEmailBody.Append("<BR>");

            AllFBStatu priority = FarmerBrothersEntitites.AllFBStatus.Where(p => p.FBStatusID == workOrder.PriorityCode).FirstOrDefault();
            string priorityDesc = priority == null ? "" : priority.FBStatus;

            salesEmailBody.Append("Service Priority: ");
            salesEmailBody.Append(priorityDesc);
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("Parent: ");
            if (customer.PricingParentID != null)
            {
                NonFBCustomer nonfbcust = FarmerBrothersEntitites.NonFBCustomers.Where(c => c.NonFBCustomerId == customer.PricingParentID).FirstOrDefault();
                string parentNum = "", ParentName = "";
                if (nonfbcust != null)
                {
                    parentNum = nonfbcust.NonFBCustomerId;
                    ParentName = nonfbcust.NonFBCustomerName;
                }
                else
                {
                    parentNum = customer.PricingParentID;
                    ParentName = customer.PricingParentDesc == null ? "" : customer.PricingParentDesc;
                }
                salesEmailBody.Append(parentNum + " " + ParentName);
            }
            else
            {
                salesEmailBody.Append("");
            }
            salesEmailBody.Append("<BR>");

            salesEmailBody.Append("Billable: ");
            salesEmailBody.Append(IsBillable);
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("Customer PO: ");
            salesEmailBody.Append(workOrder.CustomerPO);

            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("CUSTOMER INFORMATION: ");
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("CUSTOMER#: ");
            salesEmailBody.Append(workOrder.CustomerID);
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append(workOrder.CustomerName);
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append(customer.Address1);
            salesEmailBody.Append(",");
            salesEmailBody.Append(customer.Address2);
            salesEmailBody.Append("<BR>");
            //salesEmailBody.Append(workOrder.CustomerCity);
            salesEmailBody.Append(customer.City);
            salesEmailBody.Append(",");
            //salesEmailBody.Append(workOrder.CustomerState);
            salesEmailBody.Append(customer.State);
            salesEmailBody.Append(" ");
            //salesEmailBody.Append(workOrder.CustomerZipCode);
            salesEmailBody.Append(customer.PostalCode);
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append(workOrder.WorkorderContactName);
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("PHONE: ");
            salesEmailBody.Append(workOrder.WorkorderContactPhone);
            salesEmailBody.Append("<BR>");

            salesEmailBody.Append("BRANCH: ");
            salesEmailBody.Append(customer.Branch);
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("ROUTE#: ");
            salesEmailBody.Append(customer.Route);
            salesEmailBody.Append("<BR>");
            if (workOrder.FollowupCallID == 601 || workOrder.FollowupCallID == 602)
            {
                int? followupId = workOrder.FollowupCallID;
                AllFBStatu status = FarmerBrothersEntitites.AllFBStatus.Where(s => s.FBStatusID == followupId).FirstOrDefault();
                if (status != null && !string.IsNullOrEmpty(status.FBStatus))
                {
                    //salesEmailBody.Append("Follow Up Reason: ");
                    //salesEmailBody.Append(status.FBStatus);
                    if (workOrder.FollowupCallID == 601)
                        salesEmailBody.Append("Customer requesting an ETA phone call within the hour");
                    else if (workOrder.FollowupCallID == 602)
                        salesEmailBody.Append("Contact Customer Within The Hour");
                    salesEmailBody.Append("<BR>");
                }
            }
            salesEmailBody.Append("<span style='color:#ff0000'><b>");
            salesEmailBody.Append("LAST SALES DATE: ");
            salesEmailBody.Append(GetCustomerById(workOrder.CustomerID).LastSaleDate);
            salesEmailBody.Append("</b></span>");
            salesEmailBody.Append("<BR>");

            salesEmailBody.Append("HOURS OF OPERATION: ");
            salesEmailBody.Append(workOrder.HoursOfOperation);
            salesEmailBody.Append("<BR>");

            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("CALL CODES: ");
            salesEmailBody.Append("<BR>");

            foreach (WorkorderEquipmentRequested equipment in workOrder.WorkorderEquipmentRequesteds)
            {
                salesEmailBody.Append("EQUIPMENT TYPE: ");
                salesEmailBody.Append(equipment.Category);
                salesEmailBody.Append("<BR>");

                WorkorderType callType = FarmerBrothersEntitites.WorkorderTypes.Where(w => w.CallTypeID == equipment.CallTypeid).FirstOrDefault();
                if (callType != null)
                {
                    salesEmailBody.Append("SERVICE CODE: ");
                    salesEmailBody.Append(callType.CallTypeID);
                    salesEmailBody.Append(" - ");
                    salesEmailBody.Append(callType.Description);
                    salesEmailBody.Append("<BR>");
                }
                Symptom symptom = FarmerBrothersEntitites.Symptoms.Where(s => s.SymptomID == equipment.Symptomid).FirstOrDefault();
                if (symptom != null)
                {
                    salesEmailBody.Append("SYMPTOM: ");
                    salesEmailBody.Append(symptom.SymptomID);
                    salesEmailBody.Append(" - ");
                    salesEmailBody.Append(symptom.Description);
                    salesEmailBody.Append("<BR>");
                }
                salesEmailBody.Append("LOCATION: ");
                salesEmailBody.Append(equipment.Location);
                salesEmailBody.Append("<BR>");
                salesEmailBody.Append("SERIAL NUMBER: ");
                salesEmailBody.Append(equipment.SerialNumber);

                salesEmailBody.Append("<BR>");
            }

            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("CALL NOTES: ");
            salesEmailBody.Append("<BR>");
            IEnumerable<NotesHistory> histories = workOrder.NotesHistories.Where(n => n.AutomaticNotes == 0).OrderByDescending(n => n.EntryDate);

            foreach (NotesHistory history in histories)
            {
                //Remove Redirected/Rejected notes for 3rd Party Tech
                if (tchView != null && tchView.FamilyAff.ToUpper() == "SPT")
                {
                    if (history.Notes != null && (history.Notes.ToLower().Contains("redirected") || history.Notes.ToLower().Contains("rejected") || history.Notes.ToLower().Contains("declined")))
                    {
                        continue;
                    }
                }

                salesEmailBody.Append(history.UserName);
                salesEmailBody.Append(" ");
                salesEmailBody.Append(history.EntryDate);
                salesEmailBody.Append(" ");
                //salesEmailBody.Append(history.Notes.Replace("\\n", " ").Replace("\\t", " ").Replace("\\r", " ").Replace("\n", " ").Replace("\t", " ").Replace("\r", " "));
                salesEmailBody.Append(history.Notes);
                salesEmailBody.Append("<BR>");
            }
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("<BR>");

            //*****************************************************
            if (!string.IsNullOrEmpty(workOrder.WorkorderErfid))
            {
                salesEmailBody.Append("<b>ERF EQUIPMENT: </b>");
                salesEmailBody.Append("<BR>");

                salesEmailBody.Append("<table cellpadding='5'>");
                salesEmailBody.Append("<tr>");
                salesEmailBody.Append("<th style='border: 1px solid;'>Quantity</th>");
                salesEmailBody.Append("<th style='border: 1px solid;'>Equipment Category</th>");
                salesEmailBody.Append("<th style='border: 1px solid;'>Brand - Equipment Model Number - Description</th>");
                salesEmailBody.Append("<th style='border: 1px solid;'>Using Branch Stock</th>");
                salesEmailBody.Append("<th style='border: 1px solid;'>Substitution Possible</th>");
                salesEmailBody.Append("<th style='border: 1px solid;'>Trans Type</th>");
                salesEmailBody.Append("<th style='border: 1px solid;'>Equipment Type</th>");
                salesEmailBody.Append("<th style='border: 1px solid;'>Laid-In-Cost</th>");
                salesEmailBody.Append("<th style='border: 1px solid;'>Rental/Sale Cost</th>");
                salesEmailBody.Append("<th style='border: 1px solid;'>Total</th>");
                salesEmailBody.Append("<th style='border: 1px solid;'>ST/ON #</th>");
                salesEmailBody.Append("<th style='border: 1px solid;'>OT #</th>");
                salesEmailBody.Append("</tr>");

                List<FBERFEquipment> eqpModelList = FarmerBrothersEntitites.FBERFEquipments.Where(eqp => eqp.ERFId == workOrder.WorkorderErfid).ToList();
                foreach (FBERFEquipment equipment in eqpModelList)
                {
                    ContingentDetail Brand = FarmerBrothersEntitites.ContingentDetails.Where(cat => cat.ID == equipment.ContingentCategoryTypeId).FirstOrDefault();
                    Contingent category = FarmerBrothersEntitites.Contingents.Where(c => c.ContingentID == equipment.ContingentCategoryId).FirstOrDefault();

                    salesEmailBody.Append("<tr>");
                    salesEmailBody.Append("<td style='border: 1px solid;'>" + equipment.Quantity + "</td>");
                    salesEmailBody.Append("<td style='border: 1px solid;'>" + (category != null ? category.ContingentName : "") + "</td>");
                    salesEmailBody.Append("<td style='border: 1px solid;'>" + (Brand != null ? Brand.Name : "") + "</td>");
                    salesEmailBody.Append("<td style='border: 1px solid;'>" + (string.IsNullOrEmpty(equipment.UsingBranch) ? "" : equipment.UsingBranch) + "</td>");
                    salesEmailBody.Append("<td style='border: 1px solid;'>" + (string.IsNullOrEmpty(equipment.Substitution) ? "" : equipment.Substitution) + "</td>");
                    salesEmailBody.Append("<td style='border: 1px solid;'>" + (string.IsNullOrEmpty(equipment.TransactionType) ? "" : equipment.TransactionType) + "</td>");
                    salesEmailBody.Append("<td style='border: 1px solid;'>" + (string.IsNullOrEmpty(equipment.EquipmentType) ? "" : equipment.EquipmentType) + "</td>");
                    salesEmailBody.Append("<td style='border: 1px solid;'>" + equipment.LaidInCost + "</td>");
                    salesEmailBody.Append("<td style='border: 1px solid;'>" + equipment.RentalCost + "</td>");
                    salesEmailBody.Append("<td style='border: 1px solid;'>" + equipment.TotalCost + "</td>");
                    salesEmailBody.Append("<td style='border: 1px solid;'>" + (string.IsNullOrEmpty(equipment.InternalOrderType) ? "" : equipment.InternalOrderType) + "</td>");
                    salesEmailBody.Append("<td style='border: 1px solid;'>" + (string.IsNullOrEmpty(equipment.VendorOrderType) ? "" : equipment.VendorOrderType) + "</td>");
                    salesEmailBody.Append("</tr>");
                }
                salesEmailBody.Append("</table>");

                salesEmailBody.Append("<BR>");
                salesEmailBody.Append("<BR>");


                salesEmailBody.Append("<b>ERF ACCESSORIES: </b>");
                salesEmailBody.Append("<BR>");



                salesEmailBody.Append("<table cellpadding='5'>");
                salesEmailBody.Append("<tr>");
                salesEmailBody.Append("<th style='border: 1px solid;'>Quantity</th>");
                salesEmailBody.Append("<th style='border: 1px solid;'>Equipment Category</th>");
                salesEmailBody.Append("<th style='border: 1px solid;'>Brand - Equipment Model Number - Description</th>");
                salesEmailBody.Append("<th style='border: 1px solid;'>Using Branch Stock</th>");
                salesEmailBody.Append("<th style='border: 1px solid;'>Substitution Possible</th>");
                salesEmailBody.Append("<th style='border: 1px solid;'>Trans Type</th>");
                salesEmailBody.Append("<th style='border: 1px solid;'>Equipment Type</th>");
                salesEmailBody.Append("<th style='border: 1px solid;'>Laid-In-Cost</th>");
                salesEmailBody.Append("<th style='border: 1px solid;'>Rental Cost</th>");
                salesEmailBody.Append("<th style='border: 1px solid;'>Total</th>");
                salesEmailBody.Append("<th style='border: 1px solid;'>ST/ON #</th>");
                salesEmailBody.Append("<th style='border: 1px solid;'>OT #</th>");
                salesEmailBody.Append("</tr>");

                List<FBERFExpendable> expModelList = FarmerBrothersEntitites.FBERFExpendables.Where(eqp => eqp.ERFId == workOrder.WorkorderErfid).ToList();
                foreach (FBERFExpendable expendible in expModelList)
                {
                    ContingentDetail Brand = FarmerBrothersEntitites.ContingentDetails.Where(cat => cat.ID == expendible.ContingentCategoryTypeId).FirstOrDefault();
                    Contingent category = FarmerBrothersEntitites.Contingents.Where(c => c.ContingentID == expendible.ContingentCategoryId).FirstOrDefault();

                    salesEmailBody.Append("<tr>");
                    salesEmailBody.Append("<td style='border: 1px solid;'>" + expendible.Quantity + "</td>");
                    salesEmailBody.Append("<td style='border: 1px solid;'>" + (category != null ? category.ContingentName : "") + "</td>");
                    salesEmailBody.Append("<td style='border: 1px solid;'>" + (Brand != null ? Brand.Name : "") + "</td>");
                    salesEmailBody.Append("<td style='border: 1px solid;'>" + (string.IsNullOrEmpty(expendible.UsingBranch) ? "" : expendible.UsingBranch) + "</td>");
                    salesEmailBody.Append("<td style='border: 1px solid;'>" + (string.IsNullOrEmpty(expendible.Substitution) ? "" : expendible.Substitution) + "</td>");
                    salesEmailBody.Append("<td style='border: 1px solid;'>" + (string.IsNullOrEmpty(expendible.TransactionType) ? "" : expendible.TransactionType) + "</td>");
                    salesEmailBody.Append("<td style='border: 1px solid;'>" + (string.IsNullOrEmpty(expendible.EquipmentType) ? "" : expendible.EquipmentType) + "</td>");
                    salesEmailBody.Append("<td style='border: 1px solid;'>" + expendible.LaidInCost + "</td>");
                    salesEmailBody.Append("<td style='border: 1px solid;'>" + expendible.RentalCost + "</td>");
                    salesEmailBody.Append("<td style='border: 1px solid;'>" + expendible.TotalCost + "</td>");
                    salesEmailBody.Append("<td style='border: 1px solid;'>" + (string.IsNullOrEmpty(expendible.InternalOrderType) ? "" : expendible.InternalOrderType) + "</td>");
                    salesEmailBody.Append("<td style='border: 1px solid;'>" + (string.IsNullOrEmpty(expendible.VendorOrderType) ? "" : expendible.VendorOrderType) + "</td>");
                    salesEmailBody.Append("</tr>");
                }
                salesEmailBody.Append("</table>");

                salesEmailBody.Append("<BR>");
                salesEmailBody.Append("<BR>");
            }
            //*****************************************************
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("SERVICE HISTORY:");
            salesEmailBody.Append("<BR>");

            DateTime currentTime = Utility.GetCurrentTime(workOrder.CustomerZipCode, FarmerBrothersEntitites);

            /*IEnumerable<WorkOrder> previousWorkOrders = FarmerBrothersEntitites.WorkOrders.
                Where(w => w.CustomerID == workOrder.CustomerID && (DbFunctions.DiffDays(w.WorkorderEntryDate, currentTime) < 90
                              && DbFunctions.DiffDays(w.WorkorderEntryDate, currentTime) > -90));*/

            IEnumerable<WorkOrder> previousWorkOrders = FarmerBrothersEntitites.WorkOrders.
                Where(w => w.CustomerID == workOrder.CustomerID).OrderByDescending(ed => ed.WorkorderEntryDate).Take(3);

            foreach (WorkOrder previousWorkOrder in previousWorkOrders)
            {
                salesEmailBody.Append("Work Order ID#: ");
                salesEmailBody.Append(previousWorkOrder.WorkorderID);
                salesEmailBody.Append("<BR>");
                salesEmailBody.Append("ENTRY DATE: ");
                salesEmailBody.Append(previousWorkOrder.WorkorderEntryDate);
                salesEmailBody.Append("<BR>");
                salesEmailBody.Append("STATUS: ");
                salesEmailBody.Append(previousWorkOrder.WorkorderCallstatus);
                salesEmailBody.Append("<BR>");
                salesEmailBody.Append("CALL CODES: ");
                salesEmailBody.Append("<BR>");

                foreach (WorkorderEquipment equipment in previousWorkOrder.WorkorderEquipments)
                {
                    salesEmailBody.Append("MAKE: ");
                    salesEmailBody.Append(equipment.Manufacturer);
                    salesEmailBody.Append("<BR>");
                    salesEmailBody.Append("MODEL#: ");
                    salesEmailBody.Append(equipment.Model);
                    salesEmailBody.Append("<BR>");

                    WorkorderType callType = FarmerBrothersEntitites.WorkorderTypes.Where(w => w.CallTypeID == equipment.CallTypeid).FirstOrDefault();
                    if (callType != null)
                    {
                        salesEmailBody.Append("SERVICE CODE: ");
                        salesEmailBody.Append(callType.CallTypeID);
                        salesEmailBody.Append(" - ");
                        salesEmailBody.Append(callType.Description);
                        salesEmailBody.Append("<BR>");
                    }

                    Symptom symptom = FarmerBrothersEntitites.Symptoms.Where(s => s.SymptomID == equipment.Symptomid).FirstOrDefault();
                    if (symptom != null)
                    {
                        salesEmailBody.Append("SYMPTOM: ");
                        salesEmailBody.Append(symptom.SymptomID);
                        salesEmailBody.Append(" - ");
                        salesEmailBody.Append(symptom.Description);
                        salesEmailBody.Append("<BR>");
                    }

                    salesEmailBody.Append("Location: ");
                    salesEmailBody.Append(equipment.Location);
                    salesEmailBody.Append("<BR>");
                }
                salesEmailBody.Append("<BR>");
            }

            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("<BR>");

            salesEmailBody.Append("<a href=&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;></a>");

            return salesEmailBody;
        }

        public StringBuilder GetEmailBodyWithLinks(WorkOrder workOrder, string subject, string toAddress, string fromAddress, int? techId, MailType mailType, bool isResponsible, string additionalMessage, string mailFrom = "", bool isFromEmailCloserLink = false, string SalesEmailAddress = "", string esmEmailAddress = "")
        {
            StringBuilder salesEmailBody = new StringBuilder();

            Contact customer = FarmerBrothersEntitites.Contacts.Where(c => c.ContactID == workOrder.CustomerID).FirstOrDefault();
            int TotalCallsCount = GetCallsTotalCount(FarmerBrothersEntitites, workOrder.CustomerID.ToString());

            //List<CustomerNotesModel> CustomerNotesResults = new List<CustomerNotesModel>();
            int? custId = Convert.ToInt32(workOrder.CustomerID);
            var custNotes = FarmerBrothersEntitites.FBCustomerNotes.Where(c => c.CustomerId == custId && c.IsActive == true).ToList();


            string BccEmailAddress = fromAddress;
            ESMCCMRSMEscalation esmEscalation = FarmerBrothersEntitites.ESMCCMRSMEscalations.Where(e => e.ZIPCode == workOrder.CustomerZipCode).FirstOrDefault();
            if (esmEscalation != null)
            {
                fromAddress = esmEscalation.ESMEmail != null ? esmEscalation.ESMEmail : BccEmailAddress;
            }
            else
            {
                fromAddress = BccEmailAddress;
            }

            string IsBillable = "";
            string ServiceLevelDesc = "";
            if (!string.IsNullOrEmpty(customer.BillingCode))
            {
                IsBillable = IsBillableService(customer.BillingCode, TotalCallsCount);
                ServiceLevelDesc = GetServiceLevelDesc(FarmerBrothersEntitites, customer.BillingCode);
            }
            else
            {
                IsBillable = " ";
                ServiceLevelDesc = " - ";
            }

            salesEmailBody.Append(@"<img src='cid:logo' width='80' height='100'>");

            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("<BR>");
            string url = ConfigurationManager.AppSettings["DispatchResponseUrl"];
            string Redircturl = ConfigurationManager.AppSettings["RedirectResponseUrl"];
            string Closureurl = ConfigurationManager.AppSettings["CallClosureUrl"];
            string processCardurl = ConfigurationManager.AppSettings["ProcessCardUrl"];
            //string finalUrl = string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=@response&isResponsible=" + isResponsible.ToString()));

            salesEmailBody.Append("<a href=&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;></a>");
            if (isFromEmailCloserLink)
            {
                salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
                salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=3&isResponsible=" + isResponsible.ToString())) + "\">COMPLETED</a>");
                salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
                salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=7&isResponsible=" + isResponsible.ToString() + "&isBillable=" + (IsBillable == "True" ? "True" : "False"))) + "\">CLOSE WORK ORDER</a>");
                salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
            }
            else
            {
                if ((mailType == MailType.DISPATCH || mailType == MailType.SPAWN) && techId.HasValue)
                {
                    if (string.Compare(workOrder.WorkorderCallstatus, "Closed", true) != 0)
                    {
                        TECH_HIERARCHY techView = FarmerBrothersEntitites.TECH_HIERARCHY.Where(x => x.DealerId == techId).FirstOrDefault();
                        //if (techView.FamilyAff == "SPT")
                        //{
                        //    salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=0&isResponsible=" + isResponsible.ToString())) + "\">ACCEPT</a>");
                        //    salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
                        //    salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=1&isResponsible=" + isResponsible.ToString())) + "\">REJECT</a>");
                        //    salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
                        //}
                        //else
                        //{

                        salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=8&isResponsible=" + isResponsible.ToString())) + "\">SCHEDULE EVENT</a>");
                        salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");

                        /*salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=9&isResponsible=" + isResponsible.ToString())) + "\">ESM ESCALATION</a>");
                        salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");*/
                        if (mailType == MailType.DISPATCH)
                        {
                            salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=0&isResponsible=" + isResponsible.ToString())) + "\">ACCEPT</a>");
                            salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
                        }
                        if (workOrder.WorkorderCallstatus == "Pending Acceptance" && techView.FamilyAff != "SPT")
                        {
                            /*string redirectFinalUrl = string.Format("{0}{1}&encrypt=yes", Redircturl, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=5&isResponsible=" + isResponsible.ToString()));                            
                            salesEmailBody.Append("<a href=\"" + redirectFinalUrl + "\">REDIRECT</a>");
                            salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");*/
                        }


                        /*salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=1&isResponsible=" + isResponsible.ToString())) + "\">REJECT</a>");
                        salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");*/
                        salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=6&isResponsible=" + isResponsible.ToString())) + "\">START</a>");
                        salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
                        salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=2&isResponsible=" + isResponsible.ToString())) + "\">ARRIVAL</a>");
                        salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
                        salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=3&isResponsible=" + isResponsible.ToString())) + "\">COMPLETED</a>");
                        salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
                        salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=7&isResponsible=" + isResponsible.ToString() + "&isBillable=" + (IsBillable == "True" ? "True" : "False"))) + "\">CLOSE WORK ORDER</a>");
                        salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
                        salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", processCardurl, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=10&isResponsible=" + isResponsible.ToString())) + "\">PROCESS CARD</a>");
                        salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");

                        //}
                    }
                }
                else if (mailType == MailType.REDIRECTED)
                {
                    //salesEmailBody.Append("<a href=\"" + url + "?workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=4&isResponsible=" + isResponsible + "\">DISREGARD</a>");
                    salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=4&isResponsible=" + isResponsible.ToString())) + "\">DISREGARD</a>");
                }

            }

            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("<BR>");

            TECH_HIERARCHY tchView = FarmerBrothersEntitites.TECH_HIERARCHY.Where(x => x.DealerId == techId).FirstOrDefault();
            if (tchView != null && tchView.FamilyAff.ToUpper() == "SPT")
            {
                salesEmailBody.Append("<span style='color:#ff0000'><b>");
                salesEmailBody.Append("Third Party Dispatch ");
                salesEmailBody.Append("</b></span>");
            }
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("<BR>");

            if (!string.IsNullOrEmpty(additionalMessage) && mailFrom == "TRANSMIT")
            {
                salesEmailBody.Append("<b>ADDITIONAL NOTES: </b>");
                salesEmailBody.Append(Environment.NewLine);
                //salesEmailBody.Append(Utility.GetStringWithNewLine(additionalMessage));
                salesEmailBody.Append(additionalMessage);
                salesEmailBody.Append("<BR>");
            }

            if (custNotes != null && custNotes.Count > 0)
            {
                salesEmailBody.Append("<b>CUSTOMER NOTES: </b>");
                salesEmailBody.Append(Environment.NewLine);
                foreach (var dbCustNotes in custNotes)
                {
                    salesEmailBody.Append("[" + dbCustNotes.UserName + "] : " + dbCustNotes.Notes + Environment.NewLine);
                }
                salesEmailBody.Append("<BR>");
            }

            if (!string.IsNullOrEmpty(additionalMessage) && mailFrom == "ESCALATION")
            {
                salesEmailBody.Append("<span style='color:#ff0000'><b>");
                salesEmailBody.Append("ESCALATION NOTES: ");
                //salesEmailBody.Append(Utility.GetStringWithNewLine(additionalMessage));
                salesEmailBody.Append(additionalMessage);
                salesEmailBody.Append("</b></span>");
                salesEmailBody.Append("<BR>");
            }
            salesEmailBody.Append("CALL TIME: ");
            salesEmailBody.Append(workOrder.WorkorderEntryDate);
            salesEmailBody.Append("<BR>");

            salesEmailBody.Append("Work Order ID#: ");
            salesEmailBody.Append(workOrder.WorkorderID);
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("ERF#: ");
            salesEmailBody.Append(workOrder.WorkorderErfid);
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("Appointment Date: ");
            salesEmailBody.Append(workOrder.AppointmentDate);
            salesEmailBody.Append("<BR>");

            WorkorderSchedule ws = FarmerBrothersEntitites.WorkorderSchedules.Where(w => w.WorkorderID == workOrder.WorkorderID && (w.AssignedStatus == "Accepted" || w.AssignedStatus == "Scheduled")).FirstOrDefault();
            string schedlDate = ws == null ? "" : ws.EventScheduleDate.ToString();

            if (workOrder.WorkorderCalltypeid == 1300)
            {
                Erf workorderERF = FarmerBrothersEntitites.Erfs.Where(ew => ew.ErfID == workOrder.WorkorderErfid).FirstOrDefault();
                schedlDate = workorderERF == null ? schedlDate : workorderERF.OriginalRequestedDate.ToString();
            }

            salesEmailBody.Append("Schedule Date: ");
            salesEmailBody.Append(schedlDate);

            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("Service Level: ");
            salesEmailBody.Append(ServiceLevelDesc);
            salesEmailBody.Append("<BR>");

            string ServiceTier = customer == null ? "" : string.IsNullOrEmpty(customer.ProfitabilityTier) ? " - " : customer.ProfitabilityTier;
            string paymentTerm = customer == null ? "" : (string.IsNullOrEmpty(customer.PaymentTerm) ? "" : customer.PaymentTerm);
            string PaymentTermDesc = "";
            if (!string.IsNullOrEmpty(paymentTerm))
            {
                JDEPaymentTerm paymentDesc = FarmerBrothersEntitites.JDEPaymentTerms.Where(c => c.PaymentTerm == paymentTerm).FirstOrDefault();
                PaymentTermDesc = paymentDesc == null ? "" : paymentDesc.Description;
            }
            else
            {
                PaymentTermDesc = "";
            }

            salesEmailBody.Append("Tier: ");
            salesEmailBody.Append(ServiceTier);
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("Payment Terms: ");
            salesEmailBody.Append(PaymentTermDesc);
            salesEmailBody.Append("<BR>");

            AllFBStatu priority = FarmerBrothersEntitites.AllFBStatus.Where(p => p.FBStatusID == workOrder.PriorityCode).FirstOrDefault();
            string priorityDesc = priority == null ? "" : priority.FBStatus;

            salesEmailBody.Append("Service Priority: ");
            salesEmailBody.Append(priorityDesc);
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("Parent: ");
            if (customer.PricingParentID != null)
            {
                NonFBCustomer nonfbcust = FarmerBrothersEntitites.NonFBCustomers.Where(c => c.NonFBCustomerId == customer.PricingParentID).FirstOrDefault();
                string parentNum = "", ParentName = "";
                if (nonfbcust != null)
                {
                    parentNum = nonfbcust.NonFBCustomerId;
                    ParentName = nonfbcust.NonFBCustomerName;
                }
                else
                {
                    parentNum = customer.PricingParentID;
                    ParentName = customer.PricingParentDesc == null ? "" : customer.PricingParentDesc;
                }
                salesEmailBody.Append(parentNum + " " + ParentName);
            }
            else
            {
                salesEmailBody.Append("");
            }
            salesEmailBody.Append("<BR>");

            salesEmailBody.Append("Billable: ");
            salesEmailBody.Append(IsBillable);
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("Customer PO: ");
            salesEmailBody.Append(workOrder.CustomerPO);

            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("CUSTOMER INFORMATION: ");
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("CUSTOMER#: ");
            salesEmailBody.Append(workOrder.CustomerID);
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append(workOrder.CustomerName);
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append(customer.Address1);
            salesEmailBody.Append(",");
            salesEmailBody.Append(customer.Address2);
            salesEmailBody.Append("<BR>");
            //salesEmailBody.Append(workOrder.CustomerCity);
            salesEmailBody.Append(customer.City);
            salesEmailBody.Append(",");
            //salesEmailBody.Append(workOrder.CustomerState);
            salesEmailBody.Append(customer.State);
            salesEmailBody.Append(" ");
            //salesEmailBody.Append(workOrder.CustomerZipCode);
            salesEmailBody.Append(customer.PostalCode);
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append(workOrder.WorkorderContactName);
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("PHONE: ");
            salesEmailBody.Append(workOrder.WorkorderContactPhone);
            salesEmailBody.Append("<BR>");

            salesEmailBody.Append("BRANCH: ");
            salesEmailBody.Append(customer.Branch);
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("ROUTE#: ");
            salesEmailBody.Append(customer.Route);
            salesEmailBody.Append("<BR>");
            if (workOrder.FollowupCallID == 601 || workOrder.FollowupCallID == 602)
            {
                int? followupId = workOrder.FollowupCallID;
                AllFBStatu status = FarmerBrothersEntitites.AllFBStatus.Where(s => s.FBStatusID == followupId).FirstOrDefault();
                if (status != null && !string.IsNullOrEmpty(status.FBStatus))
                {
                    //salesEmailBody.Append("Follow Up Reason: ");
                    //salesEmailBody.Append(status.FBStatus);
                    if (workOrder.FollowupCallID == 601)
                        salesEmailBody.Append("Customer requesting an ETA phone call within the hour");
                    else if (workOrder.FollowupCallID == 602)
                        salesEmailBody.Append("Contact Customer Within The Hour");
                    salesEmailBody.Append("<BR>");
                }
            }
            salesEmailBody.Append("<span style='color:#ff0000'><b>");
            salesEmailBody.Append("LAST SALES DATE: ");
            salesEmailBody.Append(GetCustomerById(workOrder.CustomerID).LastSaleDate);
            salesEmailBody.Append("</b></span>");
            salesEmailBody.Append("<BR>");

            salesEmailBody.Append("HOURS OF OPERATION: ");
            salesEmailBody.Append(workOrder.HoursOfOperation);
            salesEmailBody.Append("<BR>");

            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("CALL CODES: ");
            salesEmailBody.Append("<BR>");

            foreach (WorkorderEquipmentRequested equipment in workOrder.WorkorderEquipmentRequesteds)
            {
                salesEmailBody.Append("EQUIPMENT TYPE: ");
                salesEmailBody.Append(equipment.Category);
                salesEmailBody.Append("<BR>");

                WorkorderType callType = FarmerBrothersEntitites.WorkorderTypes.Where(w => w.CallTypeID == equipment.CallTypeid).FirstOrDefault();
                if (callType != null)
                {
                    salesEmailBody.Append("SERVICE CODE: ");
                    salesEmailBody.Append(callType.CallTypeID);
                    salesEmailBody.Append(" - ");
                    salesEmailBody.Append(callType.Description);
                    salesEmailBody.Append("<BR>");
                }
                Symptom symptom = FarmerBrothersEntitites.Symptoms.Where(s => s.SymptomID == equipment.Symptomid).FirstOrDefault();
                if (symptom != null)
                {
                    salesEmailBody.Append("SYMPTOM: ");
                    salesEmailBody.Append(symptom.SymptomID);
                    salesEmailBody.Append(" - ");
                    salesEmailBody.Append(symptom.Description);
                    salesEmailBody.Append("<BR>");
                }
                salesEmailBody.Append("LOCATION: ");
                salesEmailBody.Append(equipment.Location);
                salesEmailBody.Append("<BR>");
                salesEmailBody.Append("SERIAL NUMBER: ");
                salesEmailBody.Append(equipment.SerialNumber);

                salesEmailBody.Append("<BR>");
            }

            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("CALL NOTES: ");
            salesEmailBody.Append("<BR>");
            IEnumerable<NotesHistory> histories = workOrder.NotesHistories.Where(n => n.AutomaticNotes == 0).OrderByDescending(n => n.EntryDate);

            foreach (NotesHistory history in histories)
            {
                //Remove Redirected/Rejected notes for 3rd Party Tech
                if (tchView != null && tchView.FamilyAff.ToUpper() == "SPT")
                {
                    if (history.Notes != null && (history.Notes.ToLower().Contains("redirected") || history.Notes.ToLower().Contains("rejected") || history.Notes.ToLower().Contains("declined")))
                    {
                        continue;
                    }
                }

                salesEmailBody.Append(history.UserName);
                salesEmailBody.Append(" ");
                salesEmailBody.Append(history.EntryDate);
                salesEmailBody.Append(" ");
                //salesEmailBody.Append(history.Notes.Replace("\\n", " ").Replace("\\t", " ").Replace("\\r", " ").Replace("\n", " ").Replace("\t", " ").Replace("\r", " "));
                salesEmailBody.Append(history.Notes);
                salesEmailBody.Append("<BR>");
            }
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("<BR>");

            //*****************************************************
            if (!string.IsNullOrEmpty(workOrder.WorkorderErfid))
            {
                salesEmailBody.Append("<b>ERF EQUIPMENT: </b>");
                salesEmailBody.Append("<BR>");

                salesEmailBody.Append("<table cellpadding='5'>");
                salesEmailBody.Append("<tr>");
                salesEmailBody.Append("<th style='border: 1px solid;'>Quantity</th>");
                salesEmailBody.Append("<th style='border: 1px solid;'>Equipment Category</th>");
                salesEmailBody.Append("<th style='border: 1px solid;'>Brand - Equipment Model Number - Description</th>");
                salesEmailBody.Append("<th style='border: 1px solid;'>Using Branch Stock</th>");
                salesEmailBody.Append("<th style='border: 1px solid;'>Substitution Possible</th>");
                salesEmailBody.Append("<th style='border: 1px solid;'>Trans Type</th>");
                salesEmailBody.Append("<th style='border: 1px solid;'>Equipment Type</th>");
                salesEmailBody.Append("<th style='border: 1px solid;'>Laid-In-Cost</th>");
                salesEmailBody.Append("<th style='border: 1px solid;'>Rental/Sale Cost</th>");
                salesEmailBody.Append("<th style='border: 1px solid;'>Total</th>");
                salesEmailBody.Append("<th style='border: 1px solid;'>ST/ON #</th>");
                salesEmailBody.Append("<th style='border: 1px solid;'>OT #</th>");
                salesEmailBody.Append("</tr>");

                List<FBERFEquipment> eqpModelList = FarmerBrothersEntitites.FBERFEquipments.Where(eqp => eqp.ERFId == workOrder.WorkorderErfid).ToList();
                foreach (FBERFEquipment equipment in eqpModelList)
                {
                    ContingentDetail Brand = FarmerBrothersEntitites.ContingentDetails.Where(cat => cat.ID == equipment.ContingentCategoryTypeId).FirstOrDefault();
                    Contingent category = FarmerBrothersEntitites.Contingents.Where(c => c.ContingentID == equipment.ContingentCategoryId).FirstOrDefault();

                    salesEmailBody.Append("<tr>");
                    salesEmailBody.Append("<td style='border: 1px solid;'>" + equipment.Quantity + "</td>");
                    salesEmailBody.Append("<td style='border: 1px solid;'>" + (category != null ? category.ContingentName : "") + "</td>");
                    salesEmailBody.Append("<td style='border: 1px solid;'>" + (Brand != null ? Brand.Name : "") + "</td>");
                    salesEmailBody.Append("<td style='border: 1px solid;'>" + (string.IsNullOrEmpty(equipment.UsingBranch) ? "" : equipment.UsingBranch) + "</td>");
                    salesEmailBody.Append("<td style='border: 1px solid;'>" + (string.IsNullOrEmpty(equipment.Substitution) ? "" : equipment.Substitution) + "</td>");
                    salesEmailBody.Append("<td style='border: 1px solid;'>" + (string.IsNullOrEmpty(equipment.TransactionType) ? "" : equipment.TransactionType) + "</td>");
                    salesEmailBody.Append("<td style='border: 1px solid;'>" + (string.IsNullOrEmpty(equipment.EquipmentType) ? "" : equipment.EquipmentType) + "</td>");
                    salesEmailBody.Append("<td style='border: 1px solid;'>" + equipment.LaidInCost + "</td>");
                    salesEmailBody.Append("<td style='border: 1px solid;'>" + equipment.RentalCost + "</td>");
                    salesEmailBody.Append("<td style='border: 1px solid;'>" + equipment.TotalCost + "</td>");
                    salesEmailBody.Append("<td style='border: 1px solid;'>" + (string.IsNullOrEmpty(equipment.InternalOrderType) ? "" : equipment.InternalOrderType) + "</td>");
                    salesEmailBody.Append("<td style='border: 1px solid;'>" + (string.IsNullOrEmpty(equipment.VendorOrderType) ? "" : equipment.VendorOrderType) + "</td>");
                    salesEmailBody.Append("</tr>");
                }
                salesEmailBody.Append("</table>");

                salesEmailBody.Append("<BR>");
                salesEmailBody.Append("<BR>");


                salesEmailBody.Append("<b>ERF ACCESSORIES: </b>");
                salesEmailBody.Append("<BR>");



                salesEmailBody.Append("<table cellpadding='5'>");
                salesEmailBody.Append("<tr>");
                salesEmailBody.Append("<th style='border: 1px solid;'>Quantity</th>");
                salesEmailBody.Append("<th style='border: 1px solid;'>Equipment Category</th>");
                salesEmailBody.Append("<th style='border: 1px solid;'>Brand - Equipment Model Number - Description</th>");
                salesEmailBody.Append("<th style='border: 1px solid;'>Using Branch Stock</th>");
                salesEmailBody.Append("<th style='border: 1px solid;'>Substitution Possible</th>");
                salesEmailBody.Append("<th style='border: 1px solid;'>Trans Type</th>");
                salesEmailBody.Append("<th style='border: 1px solid;'>Equipment Type</th>");
                salesEmailBody.Append("<th style='border: 1px solid;'>Laid-In-Cost</th>");
                salesEmailBody.Append("<th style='border: 1px solid;'>Rental Cost</th>");
                salesEmailBody.Append("<th style='border: 1px solid;'>Total</th>");
                salesEmailBody.Append("<th style='border: 1px solid;'>ST/ON #</th>");
                salesEmailBody.Append("<th style='border: 1px solid;'>OT #</th>");
                salesEmailBody.Append("</tr>");

                List<FBERFExpendable> expModelList = FarmerBrothersEntitites.FBERFExpendables.Where(eqp => eqp.ERFId == workOrder.WorkorderErfid).ToList();
                foreach (FBERFExpendable expendible in expModelList)
                {
                    ContingentDetail Brand = FarmerBrothersEntitites.ContingentDetails.Where(cat => cat.ID == expendible.ContingentCategoryTypeId).FirstOrDefault();
                    Contingent category = FarmerBrothersEntitites.Contingents.Where(c => c.ContingentID == expendible.ContingentCategoryId).FirstOrDefault();

                    salesEmailBody.Append("<tr>");
                    salesEmailBody.Append("<td style='border: 1px solid;'>" + expendible.Quantity + "</td>");
                    salesEmailBody.Append("<td style='border: 1px solid;'>" + (category != null ? category.ContingentName : "") + "</td>");
                    salesEmailBody.Append("<td style='border: 1px solid;'>" + (Brand != null ? Brand.Name : "") + "</td>");
                    salesEmailBody.Append("<td style='border: 1px solid;'>" + (string.IsNullOrEmpty(expendible.UsingBranch) ? "" : expendible.UsingBranch) + "</td>");
                    salesEmailBody.Append("<td style='border: 1px solid;'>" + (string.IsNullOrEmpty(expendible.Substitution) ? "" : expendible.Substitution) + "</td>");
                    salesEmailBody.Append("<td style='border: 1px solid;'>" + (string.IsNullOrEmpty(expendible.TransactionType) ? "" : expendible.TransactionType) + "</td>");
                    salesEmailBody.Append("<td style='border: 1px solid;'>" + (string.IsNullOrEmpty(expendible.EquipmentType) ? "" : expendible.EquipmentType) + "</td>");
                    salesEmailBody.Append("<td style='border: 1px solid;'>" + expendible.LaidInCost + "</td>");
                    salesEmailBody.Append("<td style='border: 1px solid;'>" + expendible.RentalCost + "</td>");
                    salesEmailBody.Append("<td style='border: 1px solid;'>" + expendible.TotalCost + "</td>");
                    salesEmailBody.Append("<td style='border: 1px solid;'>" + (string.IsNullOrEmpty(expendible.InternalOrderType) ? "" : expendible.InternalOrderType) + "</td>");
                    salesEmailBody.Append("<td style='border: 1px solid;'>" + (string.IsNullOrEmpty(expendible.VendorOrderType) ? "" : expendible.VendorOrderType) + "</td>");
                    salesEmailBody.Append("</tr>");
                }
                salesEmailBody.Append("</table>");

                salesEmailBody.Append("<BR>");
                salesEmailBody.Append("<BR>");
            }
            //*****************************************************
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("SERVICE HISTORY:");
            salesEmailBody.Append("<BR>");

            DateTime currentTime = Utility.GetCurrentTime(workOrder.CustomerZipCode, FarmerBrothersEntitites);

            /*IEnumerable<WorkOrder> previousWorkOrders = FarmerBrothersEntitites.WorkOrders.
                Where(w => w.CustomerID == workOrder.CustomerID && (DbFunctions.DiffDays(w.WorkorderEntryDate, currentTime) < 90
                              && DbFunctions.DiffDays(w.WorkorderEntryDate, currentTime) > -90));*/

            IEnumerable<WorkOrder> previousWorkOrders = FarmerBrothersEntitites.WorkOrders.
                Where(w => w.CustomerID == workOrder.CustomerID).OrderByDescending(ed => ed.WorkorderEntryDate).Take(3);

            foreach (WorkOrder previousWorkOrder in previousWorkOrders)
            {
                salesEmailBody.Append("Work Order ID#: ");
                salesEmailBody.Append(previousWorkOrder.WorkorderID);
                salesEmailBody.Append("<BR>");
                salesEmailBody.Append("ENTRY DATE: ");
                salesEmailBody.Append(previousWorkOrder.WorkorderEntryDate);
                salesEmailBody.Append("<BR>");
                salesEmailBody.Append("STATUS: ");
                salesEmailBody.Append(previousWorkOrder.WorkorderCallstatus);
                salesEmailBody.Append("<BR>");
                salesEmailBody.Append("CALL CODES: ");
                salesEmailBody.Append("<BR>");

                foreach (WorkorderEquipment equipment in previousWorkOrder.WorkorderEquipments)
                {
                    salesEmailBody.Append("MAKE: ");
                    salesEmailBody.Append(equipment.Manufacturer);
                    salesEmailBody.Append("<BR>");
                    salesEmailBody.Append("MODEL#: ");
                    salesEmailBody.Append(equipment.Model);
                    salesEmailBody.Append("<BR>");

                    WorkorderType callType = FarmerBrothersEntitites.WorkorderTypes.Where(w => w.CallTypeID == equipment.CallTypeid).FirstOrDefault();
                    if (callType != null)
                    {
                        salesEmailBody.Append("SERVICE CODE: ");
                        salesEmailBody.Append(callType.CallTypeID);
                        salesEmailBody.Append(" - ");
                        salesEmailBody.Append(callType.Description);
                        salesEmailBody.Append("<BR>");
                    }

                    Symptom symptom = FarmerBrothersEntitites.Symptoms.Where(s => s.SymptomID == equipment.Symptomid).FirstOrDefault();
                    if (symptom != null)
                    {
                        salesEmailBody.Append("SYMPTOM: ");
                        salesEmailBody.Append(symptom.SymptomID);
                        salesEmailBody.Append(" - ");
                        salesEmailBody.Append(symptom.Description);
                        salesEmailBody.Append("<BR>");
                    }

                    salesEmailBody.Append("Location: ");
                    salesEmailBody.Append(equipment.Location);
                    salesEmailBody.Append("<BR>");
                }
                salesEmailBody.Append("<BR>");
            }

            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("<BR>");

            salesEmailBody.Append("<a href=&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;></a>");
            if (isFromEmailCloserLink)
            {
                salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
                salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=3&isResponsible=" + isResponsible.ToString())) + "\">COMPLETED</a>");
                salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
                salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=7&isResponsible=" + isResponsible.ToString() + "&isBillable=" + (IsBillable == "True" ? "True" : "False"))) + "\">CLOSE WORK ORDER</a>");
                salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");

            }
            else
            {
                if ((mailType == MailType.DISPATCH || mailType == MailType.SPAWN) && techId.HasValue)
                {
                    if (string.Compare(workOrder.WorkorderCallstatus, "Closed", true) != 0)
                    {
                        TECH_HIERARCHY techView = FarmerBrothersEntitites.TECH_HIERARCHY.Where(x => x.DealerId == techId).FirstOrDefault();


                        salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=9&isResponsible=" + isResponsible.ToString())) + "\">ESM ESCALATION</a>");
                        salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
                        if (mailType == MailType.DISPATCH)
                        {
                            /*salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=0&isResponsible=" + isResponsible.ToString())) + "\">ACCEPT</a>");
                            salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");*/
                        }
                        if (workOrder.WorkorderCallstatus == "Pending Acceptance" && techView.FamilyAff != "SPT")
                        {
                            string redirectFinalUrl = string.Format("{0}{1}&encrypt=yes", Redircturl, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=5&isResponsible=" + isResponsible.ToString()));
                            salesEmailBody.Append("<a href=\"" + redirectFinalUrl + "\">REDIRECT</a>");
                            salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
                        }
                        salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=1&isResponsible=" + isResponsible.ToString())) + "\">REJECT</a>");
                        salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");

                    }
                }
                else if (mailType == MailType.REDIRECTED)
                {
                    salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=4&isResponsible=" + isResponsible.ToString())) + "\">DISREGARD</a>");
                }
            }
            return salesEmailBody;
        }

        public bool sendToListEmail(StringBuilder salesEmailBodywithLinks, string fromAddress, string toAddress, string BccEmailAddress, string subject, int? techId, Contact customer)
        {
            StringBuilder salesEmailBody = new StringBuilder();
            TECH_HIERARCHY tchView = FarmerBrothersEntitites.TECH_HIERARCHY.Where(x => x.DealerId == techId).FirstOrDefault();
            //string contentId = Guid.NewGuid().ToString();
            //string logoPath = System.AppDomain.CurrentDomain.BaseDirectory.ToString() + "img\\mainlogo.jpg";


            salesEmailBody = salesEmailBodywithLinks;
            //salesEmailBody = salesEmailBody.Replace("cid:logo", "cid:" + contentId);

            //AlternateView avHtml = AlternateView.CreateAlternateViewFromString
            //   (salesEmailBody.ToString(), null, MediaTypeNames.Text.Html);

            //LinkedResource inline = new LinkedResource(logoPath, MediaTypeNames.Image.Jpeg);
            //inline.ContentId = contentId;
            //avHtml.LinkedResources.Add(inline);

            //var message = new MailMessage();

            //message.AlternateViews.Add(avHtml);

            string ToAddr = string.Empty;
            string CcAddr = string.Empty;
            bool result = true;
            if (!string.IsNullOrEmpty(toAddress))
            {

                if (Convert.ToBoolean(ConfigurationManager.AppSettings["UseTestMails"]))
                {
                    toAddress = ConfigurationManager.AppSettings["TestEmail"];
                }

                if (toAddress.Contains("#"))
                {
                    string[] mailCCAddress = toAddress.Split('#');

                    if (mailCCAddress.Count() > 0)
                    {
                        ToAddr = mailCCAddress[0];
                    }
                }
                else
                {
                    ToAddr = toAddress;
                }

                //message.From = new MailAddress(fromAddress);
                //message.Subject = subject;
                //message.IsBodyHtml = true;

                //if (tchView != null && tchView.FamilyAff != "SP")
                //{
                //    message.Priority = MailPriority.High;
                //}

                EmailUtility eu = new EmailUtility();
                eu.SendEmail(fromAddress, ToAddr, CcAddr, subject, salesEmailBody.ToString());

                //using (var smtp = new SmtpClient())
                //{
                //    smtp.Host = ConfigurationManager.AppSettings["MailServer"];
                //    smtp.Port = 25;

                //    try
                //    {
                //        smtp.Send(message);
                //    }
                //    catch (Exception ex)
                //    {
                //        result = false;
                //    }
                //}
            }
            return result;
        }

        public bool sendCCListEmail(StringBuilder salesEmailBodywithoutLinks, string fromAddress, string ccMailAddress, string BccEmailAddress, string subject, int? techId, Contact customer, string SalesEmailAddress, string esmEmailAddress)
        {
            StringBuilder salesEmailBody = new StringBuilder();
            TECH_HIERARCHY tchView = FarmerBrothersEntitites.TECH_HIERARCHY.Where(x => x.DealerId == techId).FirstOrDefault();
            //string contentId = Guid.NewGuid().ToString();
            //string logoPath = System.AppDomain.CurrentDomain.BaseDirectory.ToString() + "img\\mainlogo.jpg";


            salesEmailBody = salesEmailBodywithoutLinks;
            //salesEmailBody = salesEmailBody.Replace("cid:logo", "cid:" + contentId);

            //AlternateView avHtml = AlternateView.CreateAlternateViewFromString
            //   (salesEmailBody.ToString(), null, MediaTypeNames.Text.Html);

            //LinkedResource inline = new LinkedResource(logoPath, MediaTypeNames.Image.Jpeg);
            //inline.ContentId = contentId;
            //avHtml.LinkedResources.Add(inline);

            //var message = new MailMessage();

            //message.AlternateViews.Add(avHtml);

            string ToAddr = string.Empty;
            string CcAddr = string.Empty;
            bool result = true;
            if (!string.IsNullOrEmpty(ccMailAddress))
            {
                if (Convert.ToBoolean(ConfigurationManager.AppSettings["UseTestMails"]))
                {
                    ccMailAddress = ConfigurationManager.AppSettings["TestEmail"];
                }
                if (ccMailAddress.Contains("#"))
                {
                    string[] mailCCAddress = ccMailAddress.Split('#');

                    if (mailCCAddress.Count() > 0)
                    {
                        CcAddr = mailCCAddress[1];
                    }
                }
                else
                {
                    CcAddr = ccMailAddress;
                }

                //if (ccMailAddress.Contains(";"))
                //{
                //    string[] addresses = ccMailAddress.Split(';');
                //    foreach (string address in addresses)
                //    {
                //        if (!string.IsNullOrWhiteSpace(address))
                //        {
                //            if (address.ToLower().Contains("@jmsmucker.com")) continue;

                //            message.CC.Add(new MailAddress(address));
                //        }
                //    }
                //}

                //message.From = new MailAddress(fromAddress);
                //message.Subject = subject;
                //message.IsBodyHtml = true;

                //if (tchView != null && tchView.FamilyAff != "SP")
                //{
                //    message.Priority = MailPriority.High;
                //}

                if (!string.IsNullOrEmpty(SalesEmailAddress))
                {
                    CcAddr += ";" + SalesEmailAddress;
                }

                EmailUtility eu = new EmailUtility();
                eu.SendEmail(fromAddress, ToAddr, CcAddr, subject, salesEmailBody.ToString());


            }
            return result;
        }

        public static int GetCallsTotalCount(FarmerBrothersEntities FBE, string CustomerID)
        {
            DateTime Last12Months = DateTime.Now.AddMonths(-12);
            return FBE.Set<WorkOrder>().Where(w => w.CustomerID.ToString() == CustomerID && w.WorkorderCalltypeid == 1200 && w.WorkorderEntryDate >= Last12Months).Count();
        }

        public static string IsBillableService(string BillingCode, int TotalCallCount)
        {
            string flag = "False";

            switch (BillingCode)
            {
                case "S00":
                    flag = "False";
                    break;
                case "S01":
                    if (TotalCallCount > 2)
                        flag = "True";
                    else
                        flag = "False";
                    break;
                case "S02":
                    if (TotalCallCount > 3)
                        flag = "True";
                    else
                        flag = "False";
                    break;
                case "S03":
                    if (TotalCallCount > 4)
                        flag = "True";
                    else
                        flag = "False";
                    break;
                case "S04":
                    flag = "False";
                    break;
                case "S05":
                    flag = "False";
                    break;
                case "S06":
                    flag = "False";
                    break;
                case "S07":
                    flag = "False";
                    break;
                case "S08":
                    flag = "True";
                    break;
                default:
                    flag = "False";
                    break;
            }

            return flag;
        }

        public static string GetServiceLevelDesc(FarmerBrothersEntities FBE, string BillingCode)
        {
            string Description = "";
            FBBillableFeed fbFeed = FBE.FBBillableFeeds.Where(b => b.Code == BillingCode).FirstOrDefault();
            if (fbFeed != null)
                Description = fbFeed.Description;

            return BillingCode + "  -  " + Description;
        }

        protected Contact GetCustomerById(int? customerId)
        {
            Contact customer = FarmerBrothersEntitites.Contacts.Where(x => x.ContactID == customerId).FirstOrDefault();
            return customer;
        }

        protected TECH_HIERARCHY GetTechById(int? techId)
        {
            TECH_HIERARCHY techView = FarmerBrothersEntitites.TECH_HIERARCHY.Where(x => x.DealerId == techId).FirstOrDefault();
            return techView;
        }

        public static void WriteToFile(string text)
        {
            string path = "D:\\ServiceLog.txt";
            using (StreamWriter writer = new StreamWriter(path, true))
            {
                writer.WriteLine(string.Format(text, DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt")));
                writer.Close();
            }
        }
    }
}
