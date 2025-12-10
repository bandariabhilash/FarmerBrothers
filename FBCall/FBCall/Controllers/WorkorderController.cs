using FBCall.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Web;
using System.Web.Configuration;
using System.Web.Http.Cors;
using System.Web.Mvc;
using ActionNameAttribute = System.Web.Mvc.ActionNameAttribute;
using HttpPostAttribute = System.Web.Mvc.HttpPostAttribute;
using ModelBinderAttribute = System.Web.Mvc.ModelBinderAttribute;

namespace FBCall.Controllers
{
    public enum MailType
    {
        INFO,
        DISPATCH,
        REDIRECTED,
        SPAWN,
        SALESNOTIFICATION
    }

    public class WorkorderController : Controller
    {
        int defaultFollowUpCall;
        string SubmittedBy = "";
        public WorkorderController()
        {
            using (FBWWOCallEntities s = new FBWWOCallEntities())
            {
                AllFBStatu FarmarBortherStatus = s.AllFBStatus.Where(a => a.FBStatus == "None" && a.StatusFor == "Follow Up Call").FirstOrDefault();
                if (FarmarBortherStatus != null)
                {
                    defaultFollowUpCall = FarmarBortherStatus.FBStatusID;
                }
            }

        }

        [HttpPost]
        [EnableCors(origins: "*", methods: "*", headers: "*")]
        //[MultipleButton(Name = "action", Argument = "EventSave")]
        //[Microsoft.AspNetCore.Mvc.ActionName("SaveEvent")]
        public JsonResult SaveEvent([FromBody]EventModel eventModel)
        {
            int returnValue = -1;
            WorkOrder workorder = null;
            string message = string.Empty;

            //JsonResult jsonResult1 = new JsonResult();
            //jsonResult1.Data = new { success = true, serverError = ErrorCode.SUCCESS, data = eventModel };
            //jsonResult1.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            //return jsonResult1;

            FBWWOCallEntities MarsServiceEntitites = new FBWWOCallEntities();

            int custId = Convert.ToInt32(eventModel.CustomerID);
            Contact customer = MarsServiceEntitites.Contacts.Where(x => x.ContactID == custId).FirstOrDefault();
            CustomerModel customerModel = new CustomerModel();
            if (customer != null)
            {
                customerModel = new CustomerModel(customer, MarsServiceEntitites);
                customerModel.PhoneNumber = Utility.FormatPhoneNumber(customer.PhoneWithAreaCode);
            }

            WorkorderManagementModel workorderManagement = new WorkorderManagementModel();
            if (customerModel != null)
            {
                workorderManagement.Customer = customerModel;
                workorderManagement.Customer.CustomerId = customerModel.CustomerId;
            }
            else
            {
                workorderManagement.Customer = new CustomerModel();
                workorderManagement.Customer.CustomerId = eventModel.CustomerID;
                workorderManagement.Customer.ZipCode = eventModel.CustomerZipCode;
            }            

            workorderManagement.WorkOrder = new WorkOrder();
            workorderManagement.WorkOrder.PriorityCode = eventModel.PriorityCode;


            WorkorderType calltype = MarsServiceEntitites.WorkorderTypes.Where(c => c.CallTypeID == eventModel.CallReason).FirstOrDefault();
            workorderManagement.WorkOrder.WorkorderCalltypeid = eventModel.CallReason;
            if (calltype != null)
            {
                workorderManagement.WorkOrder.WorkorderCalltypeDesc = calltype.Description;
            }
            
            workorderManagement.NewNotes = new List<NewNotesModel>();
            NewNotesModel notes = new NewNotesModel();
            notes.Text = eventModel.Notes;
            workorderManagement.NewNotes.Add(notes);

            workorderManagement.Notes = new NotesModel();
            workorderManagement.Notes.TechID = eventModel.PreferredProvider;
            workorderManagement.Notes.IsSpecificTechnician = eventModel.IsSpecificTechnician;

            workorderManagement.WorkOrderEquipmentsRequested = new List<WorkOrderManagementEquipmentModel>();

            workorderManagement.Closure = new WorkOrderClosureModel();

            workorderManagement.WorkOrder.ClosedUserName = eventModel.EquipmentLocation; //Location is assigned to ClosedUserName for reference
            workorderManagement.WorkOrder.CallerName = eventModel.CallerName;
            workorderManagement.WorkOrder.WorkorderContactName = eventModel.CallerName;
            workorderManagement.WorkOrder.WorkorderContactPhone = eventModel.WorkorderContactPhone;

            workorderManagement.WorkOrder.EntryUserName = "WEB - " + eventModel.SubmittedBy;
            workorderManagement.SubmittedBy = "WEB - " + eventModel.SubmittedBy;

            workorderManagement.WorkOrder.HoursOfOperation = "N/A";
            workorderManagement.WorkOrder.WorkOrderBrands = new List<WorkOrderBrand>();
            WorkOrderBrand brand = new WorkOrderBrand();
            brand.BrandID = 997;
            workorderManagement.WorkOrder.WorkOrderBrands.Add(brand);

            SubmittedBy = workorderManagement.SubmittedBy;
            returnValue = WorkOrderSave(workorderManagement, WorkOrderManagementSubmitType.CREATEWORKORDER, MarsServiceEntitites, out workorder, out message, true);


            if (returnValue == 0)
            {
                message = "No Updates to Save!";
            }

            var redirectUrl = string.Empty;
            string WOConfirmationCode = string.Empty;

            JsonResult jsonResult = new JsonResult();
            jsonResult.Data = new { success = true, serverError = ErrorCode.SUCCESS, Url = redirectUrl, WorkOrderId = workorder.WorkorderID, returnValue = returnValue, WorkorderCallstatus = workorder.WorkorderCallstatus, message = message, WOConfirmationCode = WOConfirmationCode };
            jsonResult.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            return jsonResult;
        }

        [HttpPost]
        [MultipleButton(Name = "action", Argument = "WorkorderSave")]
        [ActionName("SaveWorkOrder")]
        public JsonResult SaveWorkOrder([ModelBinder(typeof(WorkorderManagementModelBinder))] WorkorderManagementModel workorderManagement, HttpPostedFileBase fileToUpload, string foo, FBWWOCallEntities MarsServiceEntitites, out WorkOrder workorder, bool isAutoGenWO = false)
        {
            int returnValue = -1;
            workorder = null;
            string message = string.Empty;

            switch (workorderManagement.Operation)
            {

                case WorkOrderManagementSubmitType.CREATEWORKORDER:
                    {
                        SubmittedBy = workorderManagement.SubmittedBy;
                        returnValue = WorkOrderSave(workorderManagement, workorderManagement.Operation, MarsServiceEntitites, out workorder, out message, true);                        
                    }
                    break;

            }

            if (returnValue == 0)
            {
                message = "No Updates to Save!";
            }

            var redirectUrl = string.Empty;
            string WOConfirmationCode = string.Empty;

            JsonResult jsonResult = new JsonResult();
            jsonResult.Data = new { success = true, serverError = ErrorCode.SUCCESS, Url = redirectUrl, WorkOrderId = workorder.WorkorderID, returnValue = returnValue, WorkorderCallstatus = workorder.WorkorderCallstatus, message = message, WOConfirmationCode = WOConfirmationCode };
            jsonResult.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            return jsonResult;
        }
        public int WorkOrderSave(WorkorderManagementModel workorderManagement, WorkOrderManagementSubmitType operation, FBWWOCallEntities MarsServiceEntitites, out WorkOrder workOrder, out string message, bool isAutoGenWO = false)
        {
            int returnValue = 0;
            message = string.Empty;
            workOrder = null;

            if (operation == WorkOrderManagementSubmitType.CREATEWORKORDER)
            {
                var CustomerId = int.Parse(workorderManagement.Customer.CustomerId);
                Contact serviceCustomer = MarsServiceEntitites.Contacts.Where(x => x.ContactID == CustomerId).FirstOrDefault();

                workOrder = workorderManagement.FillCustomerData(new WorkOrder(), true, MarsServiceEntitites, serviceCustomer);


                IndexCounter counter = Utility.GetIndexCounter("WorkorderID", MarsServiceEntitites);
                counter.IndexValue++;
                MarsServiceEntitites.Entry(counter).State = System.Data.Entity.EntityState.Modified;

                workOrder.WorkorderID = counter.IndexValue.Value;
                workOrder.WorkorderCalltypeid = workorderManagement.WorkOrder.WorkorderCalltypeid;
                workOrder.WorkorderCalltypeDesc = workorderManagement.WorkOrder.WorkorderCalltypeDesc;
                workOrder.WorkorderErfid = workorderManagement.WorkOrder.WorkorderErfid;
                workOrder.WorkorderEquipCount = Convert.ToInt16(workorderManagement.WorkOrderEquipmentsRequested.Count());
                workOrder.PriorityCode = workorderManagement.WorkOrder.PriorityCode == null ?  workorderManagement.PriorityList[0].FBStatusID : workorderManagement.WorkOrder.PriorityCode;

                workOrder.FollowupCallID = defaultFollowUpCall;

                //TimeZoneInfo newTimeZoneInfo = null;
                //Utility.GetCustomerTimeZone(workorderManagement.Customer.ZipCode, MarsServiceEntitites);
                DateTime CurrentTime = Utility.GetCurrentTime(workorderManagement.Customer.ZipCode, MarsServiceEntitites);
                workOrder.WorkorderEntryDate = CurrentTime;
                workOrder.WorkorderModifiedDate = workOrder.WorkorderEntryDate;
                workOrder.ModifiedUserName = SubmittedBy;//"WEB";
                workOrder.IsAutoGenerated = true;
                workOrder.EntryUserName = SubmittedBy;//"WEB";

                workOrder.WorkorderModifiedDate = workOrder.WorkorderEntryDate;
                workOrder.WorkorderCallstatus = "Open";

                {

                    //DateTime currentTime = Utility.GetCurrentTime(workorderManagement.Customer.ZipCode, MarsServiceEntitites);

                    NotesHistory notesHistory = new NotesHistory()
                    {
                        AutomaticNotes = 1,
                        EntryDate = CurrentTime,
                        Notes = isAutoGenWO ? @"Work Order created from MARS WO#: " + workOrder.WorkorderID + @" in “MARS”!" : @"Work Order created from ERF WO#: " + workOrder.WorkorderID + @" in “MARS”!",
                        Userid = 99999,
                        UserName = SubmittedBy,//"WEB"
                        isDispatchNotes = 1
                    };
                    notesHistory.WorkorderID = workOrder.WorkorderID;
                    workOrder.NotesHistories.Add(notesHistory);


                    foreach (NewNotesModel newNotesModel in workorderManagement.NewNotes)
                    {
                        NotesHistory newnotesHistory = new NotesHistory()
                        {
                            AutomaticNotes = 0,
                            EntryDate = CurrentTime,
                            Notes = newNotesModel.Text,
                            Userid = 99999,
                            UserName = SubmittedBy,//"WEB",
                            WorkorderID = workOrder.WorkorderID,
                            isDispatchNotes = 0
                        };
                        MarsServiceEntitites.NotesHistories.Add(newnotesHistory);
                    }
                    if (workorderManagement.Notes.TechID != null && workorderManagement.Notes.TechID != "-1")
                    {
                        workOrder.SpecificTechnician = workorderManagement.Notes.TechID;
                    }

                    workOrder.IsSpecificTechnician = workorderManagement.Notes.IsSpecificTechnician;
                    workOrder.IsAutoDispatched = workorderManagement.Notes.IsAutoDispatched;

                    foreach (WorkOrderBrand brand in workorderManagement.WorkOrder.WorkOrderBrands)
                    {
                        WorkOrderBrand newBrand = new WorkOrderBrand();
                        foreach (var property in brand.GetType().GetProperties())
                        {
                            if (property.GetValue(brand) != null && property.GetValue(brand).GetType() != null && (property.GetValue(brand).GetType().IsValueType || property.GetValue(brand).GetType() == typeof(string)))
                            {
                                property.SetValue(newBrand, property.GetValue(brand));
                            }
                        }
                        newBrand.WorkorderID = workOrder.WorkorderID;
                        workOrder.WorkOrderBrands.Add(newBrand);
                    }

                    IndexCounter assetCounter = Utility.GetIndexCounter("AssetID", MarsServiceEntitites);
                    assetCounter.IndexValue++;
                    MarsServiceEntitites.Entry(assetCounter).State = System.Data.Entity.EntityState.Modified;

                    WorkorderEquipment equipment = new WorkorderEquipment()
                    {
                        Assetid = assetCounter.IndexValue.Value,
                        CallTypeid = isAutoGenWO ? workorderManagement.WorkOrder.WorkorderCalltypeid : 1720,
                        Category = ".11 - No Info – Only OTHER",
                        Location = isAutoGenWO ? workorderManagement.WorkOrder.ClosedUserName : "OTH"
                    };
                    workOrder.WorkorderEquipments.Add(equipment);

                    WorkorderEquipmentRequested equipmentReq = new WorkorderEquipmentRequested()
                    {
                        Assetid = assetCounter.IndexValue.Value,
                        CallTypeid = isAutoGenWO ? workorderManagement.WorkOrder.WorkorderCalltypeid : 1300,
                        Category = ".11 - No Info – Only OTHER",
                        Location = isAutoGenWO ? workorderManagement.WorkOrder.ClosedUserName : "OTH"
                    };
                    workOrder.WorkorderEquipmentRequesteds.Add(equipmentReq);
                    if (isAutoGenWO)
                    {
                        workorderManagement.WorkOrder.ClosedUserName = null;
                        workOrder.ClosedUserName = null;
                    }
                    notesHistory = new NotesHistory()
                    {
                        AutomaticNotes = 1,
                        EntryDate = workOrder.WorkorderEntryDate,
                        Notes = isAutoGenWO ? workOrder.WorkorderCalltypeDesc + " Work Order # " + workOrder.WorkorderID + " in MARS!" : @"Install Work Order # " + workOrder.WorkorderID + " in MARS!",
                        Userid = 99999,
                        UserName = SubmittedBy,//"WEB"
                        isDispatchNotes = 0
                    };
                    workOrder.NotesHistories.Add(notesHistory);
                    if (workorderManagement.Erf != null)
                    {
                        workOrder.WorkorderErfid = workorderManagement.Erf.ErfID;
                    }

                }


                if (workorderManagement.RemovalCount > 5)
                {
                    workOrder.WorkorderCallstatus = "Open";
                }

                MarsServiceEntitites.WorkOrders.Add(workOrder);
                SaveRemovalDetails(workorderManagement, workOrder, MarsServiceEntitites);


                WorkorderDetail workOrderDetail = new WorkorderDetail()
                {
                    StartDateTime = workorderManagement.Closure.StartDateTime,
                    InvoiceNo = workorderManagement.Closure.InvoiceNo,
                    ArrivalDateTime = workorderManagement.Closure.ArrivalDateTime,
                    CompletionDateTime = workorderManagement.Closure.CompletionDateTime,
                    ResponsibleTechName = workorderManagement.Closure.ResponsibleTechName,
                    Mileage = workorderManagement.Closure.Mileage,
                    CustomerName = workorderManagement.Closure.CustomerName,
                    CustomerEmail = workorderManagement.Closure.CustomerEmail,
                    CustomerSignatureDetails = workorderManagement.Closure.CustomerSignatureDetails,
                    WorkorderID = workOrder.WorkorderID,
                    EntryDate = workOrder.WorkorderEntryDate,
                    ModifiedDate = workOrder.WorkorderEntryDate,
                    SpecialClosure = null,
                    TravelTime = workorderManagement.Closure.TravelHours + ":" + workorderManagement.Closure.TravelMinutes
                };

                if (workorderManagement.Closure.PhoneSolveid > 0)
                {
                    workOrderDetail.PhoneSolveid = workorderManagement.Closure.PhoneSolveid;
                }
                if (workOrderDetail.CustomerSignatureDetails != null)
                {
                    //890 is for empty signature box
                    if (workOrderDetail.CustomerSignatureDetails.Length == 890)
                    {
                        workOrderDetail.CustomerSignatureDetails = MarsServiceEntitites.WorkorderDetails.Where(w => w.WorkorderID == workorderManagement.WorkOrder.WorkorderID).
                            Select(s => s.CustomerSignatureDetails).FirstOrDefault();
                        if (workOrderDetail.CustomerSignatureDetails != null)
                        {
                            if (workOrderDetail.CustomerSignatureDetails.Length == 890)
                            {
                                workOrderDetail.CustomerSignatureDetails = string.Empty;
                            }
                        }
                        else
                        {
                            workOrderDetail.CustomerSignatureDetails = string.Empty;
                        }
                    }
                }

                MarsServiceEntitites.WorkorderDetails.Add(workOrderDetail);
                workOrder.CurrentUserName = SubmittedBy;// "WEB";

                int effectedRecords = MarsServiceEntitites.SaveChanges();
                returnValue = effectedRecords > 0 ? 1 : 0;


            }

            return returnValue;
        }

        public int IsValidWorkOrderToStartAutoDispatch(WorkOrder workOrderModel, FBWWOCallEntities FarmerBrothersEntitites)
        {
            int resultFlag = 0;
            bool success = false;
            SqlHelper helper = new SqlHelper();
            DateTime currentTime = Utility.GetCurrentTime(workOrderModel.CustomerZipCode, FarmerBrothersEntitites);
            string customerSearchType = FarmerBrothersEntitites.Contacts.Where(c => c.ContactID == workOrderModel.CustomerID).Select(c => c.SearchType).FirstOrDefault();
            if (workOrderModel.IsSpecificTechnician == false && !string.IsNullOrEmpty(customerSearchType) && customerSearchType.Trim() != "CCP" && customerSearchType.Trim() != "LEGACY")
            {
                if (workOrderModel.WorkorderCalltypeid == 1200 || workOrderModel.WorkorderCalltypeid == 1100 ||
                workOrderModel.WorkorderCalltypeid == 1110 || workOrderModel.WorkorderCalltypeid == 1120 ||
                workOrderModel.WorkorderCalltypeid == 1130 || workOrderModel.WorkorderCalltypeid == 1400 ||
                workOrderModel.WorkorderCalltypeid == 1410 || workOrderModel.WorkorderCalltypeid == 1900 ||
                workOrderModel.WorkorderCalltypeid == 1800 || workOrderModel.WorkorderCalltypeid == 1810 ||
                workOrderModel.WorkorderCalltypeid == 1300 || workOrderModel.WorkorderCalltypeid == 1600 || workOrderModel.WorkorderCalltypeid == 1700 ||
                workOrderModel.WorkorderCalltypeid == 1710 || workOrderModel.WorkorderCalltypeid == 1820 || workOrderModel.WorkorderCalltypeid == 1830 ||
                workOrderModel.WorkorderCalltypeid == 1900)
                {
                    if (string.IsNullOrEmpty(workOrderModel.OverrideAutoEmail.ToString()))
                    {
                        string NoAutoEmailZipCodesQuery = "Select * from NoAutoEmailZipCodes where PostalCode = '" + workOrderModel.CustomerZipCode + "'";
                        DataTable NoAutoEmailZipCodesdt = helper.GetDatatable(NoAutoEmailZipCodesQuery);

                        if (NoAutoEmailZipCodesdt.Rows.Count == 0)
                        {
                            string AvailableEmailStartTime;
                            string AvailableEmailEndTime;
                            //DateTime dateTime = DateTime.UtcNow.Date;
                            DateTime dateTime;
                            dateTime = Utility.GetCurrentTime(workOrderModel.CustomerZipCode, FarmerBrothersEntitites);
                            AvailableEmailStartTime = (dateTime.ToString("MM/dd/yyyy") + " " + ConfigurationManager.AppSettings["AutoDispatchAvailableEmailStartTime"]);
                            AvailableEmailEndTime = (dateTime.ToString("MM/dd/yyyy") + " " + ConfigurationManager.AppSettings["AutoDispatchAvailableEmailEndTime"]);

                            if (((customerSearchType.Trim() == "CBI") && (customerSearchType.Trim() == "PFS")))
                            {
                                AvailableEmailStartTime = (dateTime.ToString("MM/dd/yyyy") + " " + ConfigurationManager.AppSettings["CBIAndPFSAvailableEmailStartTime"]);
                                AvailableEmailEndTime = (dateTime.ToString("MM/dd/yyyy") + " " + ConfigurationManager.AppSettings["CBIAndPFSAvailableEmailEndTime"]);
                            }


                            if (Convert.ToDateTime(dateTime).DayOfWeek == DayOfWeek.Saturday || Convert.ToDateTime(dateTime).DayOfWeek == DayOfWeek.Sunday
                                || IsHoliday(dateTime))
                            {
                                resultFlag = 2;
                            }
                            else if(((DateTime.Parse(AvailableEmailStartTime) <= DateTime.Parse(workOrderModel.WorkorderEntryDate.ToString()))
                                  && (DateTime.Parse(workOrderModel.WorkorderEntryDate.ToString()) <= DateTime.Parse(AvailableEmailEndTime))))
                            {
                                //success = true;
                                resultFlag = 1;
                            }
                            else
                            {
                                string updateAutoEmailQuery = "Update WorkOrder Set AutoEmailSent=0,HighlightDispatchBoard=2 where WorkOrderID = " + workOrderModel.WorkorderID;
                                helper.UpdateCommand(updateAutoEmailQuery);
                                resultFlag = 2;
                            }
                            /*if (!IsHoliday(dateTime))
                            {
                                if (Convert.ToDateTime(dateTime).DayOfWeek == DayOfWeek.Saturday || Convert.ToDateTime(dateTime).DayOfWeek == DayOfWeek.Sunday)
                                {
                                    string updateAutoEmailQuery = "Update WorkOrder Set AutoEmailSent=0,HighlightDispatchBoard=2 where WorkOrderID = " + workOrderModel.WorkorderID;
                                    helper.UpdateCommand(updateAutoEmailQuery);
                                }
                                else if (((DateTime.Parse(AvailableEmailStartTime) <= DateTime.Parse(workOrderModel.WorkorderEntryDate.ToString()))
                                   && (DateTime.Parse(workOrderModel.WorkorderEntryDate.ToString()) <= DateTime.Parse(AvailableEmailEndTime))))
                                {
                                    success = true;
                                }
                                else
                                {
                                    string updateAutoEmailQuery = "Update WorkOrder Set AutoEmailSent=0,HighlightDispatchBoard=2 where WorkOrderID = " + workOrderModel.WorkorderID;
                                    helper.UpdateCommand(updateAutoEmailQuery);
                                }
                            }
                            else
                            {
                                string updateAutoEmailQuery = "Update WorkOrder Set AutoEmailSent=0,HighlightDispatchBoard=2 where WorkOrderID = " + workOrderModel.WorkorderID;
                                helper.UpdateCommand(updateAutoEmailQuery);
                            }*/

                        }
                        else
                        {
                            string updateAutoEmailQuery = "Update WorkOrder Set AutoEmailSent=0,HighlightDispatchBoard=2 where WorkOrderID = " + workOrderModel.WorkorderID;
                            helper.UpdateCommand(updateAutoEmailQuery);
                        }
                    }
                    else
                    {
                        string updateAutoEmailQuery = "Update WorkOrder Set AutoEmailSent=0,HighlightDispatchBoard=3 where WorkOrderID = " + workOrderModel.WorkorderID;
                        helper.UpdateCommand(updateAutoEmailQuery);


                        NotesHistory notesHistory = new NotesHistory()
                        {
                            AutomaticNotes = 1,
                            EntryDate = currentTime,
                            Notes = "FB Employee is calling. Auto email functionality is over rided",
                            Userid = 99999,
                            UserName = SubmittedBy,//"WEB"
                            isDispatchNotes = 1

                        };
                        WorkOrder wr = new WorkOrder();
                        wr.NotesHistories.Add(notesHistory);
                        FarmerBrothersEntitites.SaveChanges();
                    }
                }
            }
            //return success;
            return resultFlag;
        }
                
        public void StartAutoDispatchProcess1(WorkOrder workOrderModel)
        {
            #region properties initialization
            FBWWOCallEntities FarmerBrothersEntitites = new FBWWOCallEntities();
            SqlHelper helper = new SqlHelper();
            DateTime currentTime = Utility.GetCurrentTime(workOrderModel.CustomerZipCode, FarmerBrothersEntitites);
            DataTable WorkOrderdt;
            DataTable rsAssetList;
            DataTable Contactdt;

            int TechID = -1;
            DataTable rsDealerEmail;
            int ContactId;

            #endregion

            //if (IsValidWorkOrderToStartAutoDispatch(workOrderModel, FarmerBrothersEntitites))
            {
                string WorkOrderQuery = "Select * from WorkOrder where WorkorderID = " + workOrderModel.WorkorderID;
                WorkOrderdt = helper.GetDatatable(WorkOrderQuery);

                ContactId = WorkOrderdt.Rows.Count > 0 ? Convert.ToInt32(WorkOrderdt.Rows[0]["CustomerID"]) : 0;
                string ContactQuery = "Select * from v_Contact where ContactID = " + ContactId;
                Contactdt = helper.GetDatatable(ContactQuery);


                //string WorkOrderHistoryQuery = "Select * from v_ContactServiceHistory where WorkorderID = " + WorkOrderdt.Rows[0]["WorkorderID"];
                //rsAssetList = helper.GetDatatable(WorkOrderHistoryQuery);

                //if (rsAssetList.Rows.Count <= 0)
                //{
                //    using (FBWWOCallEntities entity = new FBWWOCallEntities())
                //    {
                //        FBActivityLog log = new FBActivityLog();
                //        log.LogDate = DateTime.UtcNow;
                //        log.UserId = 99999;
                //        log.ErrorDetails = "Auto Dispatch - Unable to get Asset information";
                //        entity.FBActivityLogs.Add(log);
                //        entity.SaveChanges();
                //    }
                //    return;
                //}
                //else
                //{

                //}

                int replaceTechId = 0;
                if (string.IsNullOrEmpty(Convert.ToString(Contactdt.Rows[0]["FBProviderID"])))
                {
                    string postCode = Convert.ToString(Contactdt.Rows[0]["PostalCode"]);
                    DataTable rsReferralList = null;
                    FindAvailableDealers(postCode, false, out rsReferralList);

                    foreach (DataRow dr in rsReferralList.Rows)
                    {
                        int techId = Convert.ToInt32(dr["DealerID"]);
                        bool IsUnavailable = IsTechUnAvailable(techId, currentTime, out replaceTechId);

                        if (!IsUnavailable)
                        {
                            if (replaceTechId != 0)
                            {
                                TECH_HIERARCHY THV = FarmerBrothersEntitites.TECH_HIERARCHY.Where(x => x.SearchType == "SP" && x.DealerId == replaceTechId).FirstOrDefault();

                                if (THV != null)
                                {
                                    TechID = replaceTechId;
                                }
                            }
                            else
                            {
                                TECH_HIERARCHY THV = FarmerBrothersEntitites.TECH_HIERARCHY.Where(x => x.SearchType == "SP" && x.DealerId == techId).FirstOrDefault();

                                if (THV != null)
                                {
                                    TechID = techId;
                                }
                            }
                            break;
                        }
                    }
                }
                else
                {
                    string TechnicianQuery = ("Select * from TECH_HIERARCHY where DealerID = " + Contactdt.Rows[0]["FBProviderID"] + " and SearchType='SP'  ");
                    rsDealerEmail = helper.GetDatatable(TechnicianQuery);

                    if (rsDealerEmail.Rows.Count > 0)
                    {
                        int techId = Convert.ToInt32(rsDealerEmail.Rows[0]["DealerID"]);
                        bool IsUnavailable = IsTechUnAvailable(techId, currentTime, out replaceTechId);
                        if (!IsUnavailable)
                        {
                            if (replaceTechId != 0)
                            {
                                TECH_HIERARCHY THV = FarmerBrothersEntitites.TECH_HIERARCHY.Where(x => x.SearchType == "SP" && x.DealerId == replaceTechId).FirstOrDefault();

                                if (THV != null)
                                {
                                    TechID = replaceTechId;
                                }
                            }
                            else
                            {
                                TECH_HIERARCHY THV = FarmerBrothersEntitites.TECH_HIERARCHY.Where(x => x.SearchType == "SP" && x.DealerId == techId).FirstOrDefault();

                                if (THV != null)
                                {
                                    TechID = techId;
                                }
                            }
                        }
                        else
                        {
                            string postCode = Convert.ToString(Contactdt.Rows[0]["PostalCode"]);
                            DataTable rsReferralList = null;
                            FindAvailableDealers(postCode, false, out rsReferralList);

                            foreach (DataRow dr in rsReferralList.Rows)
                            {
                                int nearestTechId = Convert.ToInt32(dr["DealerID"]);
                                bool IsTechUnavailable = IsTechUnAvailable(nearestTechId, currentTime, out replaceTechId);

                                if (!IsTechUnavailable)
                                {
                                    if (replaceTechId != 0)
                                    {
                                        TECH_HIERARCHY THV = FarmerBrothersEntitites.TECH_HIERARCHY.Where(x => x.SearchType == "SP" && x.DealerId == replaceTechId).FirstOrDefault();

                                        if (THV != null)
                                        {
                                            TechID = replaceTechId;
                                        }
                                    }
                                    else
                                    {
                                        TECH_HIERARCHY THV = FarmerBrothersEntitites.TECH_HIERARCHY.Where(x => x.SearchType == "SP" && x.DealerId == nearestTechId).FirstOrDefault();

                                        if (THV != null)
                                        {
                                            TechID = nearestTechId;
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        string postCode = Convert.ToString(Contactdt.Rows[0]["PostalCode"]);
                        DataTable rsReferralList = null;
                        FindAvailableDealers(postCode, false, out rsReferralList);

                        foreach (DataRow dr in rsReferralList.Rows)
                        {
                            int nearestTechId = Convert.ToInt32(dr["DealerID"]);
                            bool IsTechUnavailable = IsTechUnAvailable(nearestTechId, currentTime, out replaceTechId);

                            if (!IsTechUnavailable)
                            {
                                if (replaceTechId != 0)
                                {
                                    TECH_HIERARCHY THV = FarmerBrothersEntitites.TECH_HIERARCHY.Where(x => x.SearchType == "SP" && x.DealerId == replaceTechId).FirstOrDefault();

                                    if (THV != null)
                                    {
                                        TechID = replaceTechId;
                                    }
                                }
                                else
                                {
                                    TECH_HIERARCHY THV = FarmerBrothersEntitites.TECH_HIERARCHY.Where(x => x.SearchType == "SP" && x.DealerId == nearestTechId).FirstOrDefault();

                                    if (THV != null)
                                    {
                                        TechID = nearestTechId;
                                    }
                                }
                                break;
                            }
                        }
                    }

                }

                if (TechID != -1)
                {
                    DispatchMail(workOrderModel.WorkorderID, TechID, true, new List<string>(), false, FarmerBrothersEntitites, false);
                    TECH_HIERARCHY techView = GetTechById(TechID);
                    NotesHistory notesHistory = new NotesHistory()
                    {
                        AutomaticNotes = 1,
                        EntryDate = currentTime,
                        Notes = "Auto Dispatch E-mail  Sent to " + techView.RimEmail + " " + techView.EmailCC,
                        Userid = 99999,
                        UserName = SubmittedBy,//"WEB",
                        WorkorderID = workOrderModel.WorkorderID,
                        isDispatchNotes = 1
                    };
                    FarmerBrothersEntitites.NotesHistories.Add(notesHistory);
                }


                FarmerBrothersEntitites.SaveChanges();
                //if (Contactdt.Rows.Count <= 0)
                //{
                //    using (FBWWOCallEntities entity = new FBWWOCallEntities())
                //    {
                //        FBActivityLog log = new FBActivityLog();
                //        log.LogDate = DateTime.UtcNow;
                //        log.UserId = 99999;
                //        log.ErrorDetails = "Auto Dispatch - Unable to get contact information";
                //        entity.FBActivityLogs.Add(log);
                //        entity.SaveChanges();
                //    }
                //    return;
                //}
                //else
                //{


                //}
            }

        }

        public void StartAutoDispatchProcess(WorkOrder workOrderModel)
        {
            #region properties initialization
            FBWWOCallEntities FarmerBrothersEntitites = new FBWWOCallEntities();
            SqlHelper helper = new SqlHelper();
            DateTime currentTime = Utility.GetCurrentTime(workOrderModel.CustomerZipCode, FarmerBrothersEntitites);
            DataTable WorkOrderdt;
            DataTable rsAssetList;
            DataTable Contactdt;

            int TechID = -1;
            DataTable rsDealerEmail;
            int ContactId;

            #endregion

            int resultFlag = IsValidWorkOrderToStartAutoDispatch(workOrderModel, FarmerBrothersEntitites);

            if (resultFlag != 0)
            {
                string WorkOrderQuery = "Select * from WorkOrder where WorkorderID = " + workOrderModel.WorkorderID;
                WorkOrderdt = helper.GetDatatable(WorkOrderQuery);

                ContactId = WorkOrderdt.Rows.Count > 0 ? Convert.ToInt32(WorkOrderdt.Rows[0]["CustomerID"]) : 0;
                string ContactQuery = "Select * from v_Contact where ContactID = " + ContactId;
                Contactdt = helper.GetDatatable(ContactQuery);

                if (resultFlag == 1)
                {
                    int replaceTechId = 0;
                    if (string.IsNullOrEmpty(Convert.ToString(Contactdt.Rows[0]["FBProviderID"])))
                    {
                        string postCode = Convert.ToString(Contactdt.Rows[0]["PostalCode"]);
                        TechID = getAvailableTechId(postCode, currentTime, FarmerBrothersEntitites);
                    }
                    else
                    {
                        string TechnicianQuery = ("Select * from TECH_HIERARCHY where DealerID = " + Contactdt.Rows[0]["FBProviderID"] + " and SearchType='SP'  ");
                        rsDealerEmail = helper.GetDatatable(TechnicianQuery);

                        if (rsDealerEmail.Rows.Count > 0)
                        {
                            int techId = Convert.ToInt32(rsDealerEmail.Rows[0]["DealerID"]);
                            bool IsUnavailable = IsTechUnAvailable(techId, currentTime, out replaceTechId);
                            if (!IsUnavailable)
                            {
                                if (replaceTechId != 0)
                                {
                                    TECH_HIERARCHY THV = FarmerBrothersEntitites.TECH_HIERARCHY.Where(x => x.SearchType == "SP" && x.DealerId == replaceTechId).FirstOrDefault();

                                    if (THV != null)
                                    {
                                        TechID = replaceTechId;
                                    }
                                }
                                else
                                {
                                    TECH_HIERARCHY THV = FarmerBrothersEntitites.TECH_HIERARCHY.Where(x => x.SearchType == "SP" && x.DealerId == techId).FirstOrDefault();

                                    if (THV != null)
                                    {
                                        TechID = techId;
                                    }
                                }
                            }
                            else
                            {
                                string postCode = Convert.ToString(Contactdt.Rows[0]["PostalCode"]);
                                TechID = getAvailableTechId(postCode, currentTime, FarmerBrothersEntitites);
                            }
                        }
                        else
                        {
                            string postCode = Convert.ToString(Contactdt.Rows[0]["PostalCode"]);
                            TechID = getAvailableTechId(postCode, currentTime, FarmerBrothersEntitites);

                        }
                    }
                }
                else if (resultFlag == 2)
                {
                    string postCode = Convert.ToString(Contactdt.Rows[0]["PostalCode"]);
                    TechID = getAvailableOnCallTechId(postCode, currentTime, workOrderModel.WorkorderID, FarmerBrothersEntitites);
                }
                if (TechID != -1 && TechID != 0)
                {
                    TECH_HIERARCHY THV = FarmerBrothersEntitites.TECH_HIERARCHY.Where(x => x.SearchType == "SP" && x.DealerId == TechID).FirstOrDefault();
                    if (THV.FamilyAff == "SPT")
                    {
                        if (workOrderModel.WorkorderCalltypeid == 1300)
                        {
                            TechID = 909360; // If the 3rd party call type is 1300 installation, those events will be sent to Christina Ware – SP 909360(Email from Mike on July 15, 2020)
                        }
                    }

                    DispatchMail(workOrderModel.WorkorderID, TechID, true, new List<string>(), false, FarmerBrothersEntitites, false);
                    TECH_HIERARCHY techView = GetTechById(TechID);
                    NotesHistory notesHistory = new NotesHistory()
                    {
                        AutomaticNotes = 1,
                        EntryDate = currentTime,
                        Notes = "Auto Dispatch E-mail  Sent to " + techView.RimEmail + " " + techView.EmailCC,
                        Userid = 99999,
                        UserName = SubmittedBy,//"WEB",
                        WorkorderID = workOrderModel.WorkorderID,
                        isDispatchNotes = 1
                    };
                    FarmerBrothersEntitites.NotesHistories.Add(notesHistory);
                }


                FarmerBrothersEntitites.SaveChanges();
            }

        }

        private int getAvailableTechId(string PostalCode, DateTime currentTime, FBWWOCallEntities FarmerBrothersEntitites)
        {
            int availableTechId = 0;
            int replaceTechId = 0;

            DataTable rsReferralList = null;
            FindAvailableDealers(PostalCode, false, out rsReferralList);

            //Check for Internal Techs
            foreach (DataRow dr in rsReferralList.Rows)
            {
                string techType = dr["TechType"].ToString();
                if (techType.ToUpper() != "FB") continue;

                int techId = Convert.ToInt32(dr["DealerID"]);
                bool IsUnavailable = IsTechUnAvailable(techId, currentTime, out replaceTechId);

                if (!IsUnavailable)
                {
                    if (replaceTechId != 0)
                    {
                        TECH_HIERARCHY THV = FarmerBrothersEntitites.TECH_HIERARCHY.Where(x => x.SearchType == "SP" && x.DealerId == replaceTechId).FirstOrDefault();

                        if (THV != null)
                        {
                            availableTechId = replaceTechId;
                            break;
                        }
                    }
                    else
                    {
                        TECH_HIERARCHY THV = FarmerBrothersEntitites.TECH_HIERARCHY.Where(x => x.SearchType == "SP" && x.DealerId == techId).FirstOrDefault();

                        if (THV != null)
                        {
                            availableTechId = techId;
                            break;
                        }
                    }
                }
            }

            if (availableTechId == 0)
            {
                //Check for thirdParty Techs
                foreach (DataRow dr in rsReferralList.Rows)
                {
                    string techType = dr["TechType"].ToString();
                    if (techType.ToUpper() == "FB") continue;

                    int techId = Convert.ToInt32(dr["DealerID"]);
                    bool IsUnavailable = IsTechUnAvailable(techId, currentTime, out replaceTechId);

                    if (!IsUnavailable)
                    {
                        if (replaceTechId != 0)
                        {
                            TECH_HIERARCHY THV = FarmerBrothersEntitites.TECH_HIERARCHY.Where(x => x.SearchType == "SP" && x.DealerId == replaceTechId).FirstOrDefault();

                            if (THV != null)
                            {
                                availableTechId = replaceTechId;
                                break;
                            }
                        }
                        else
                        {
                            TECH_HIERARCHY THV = FarmerBrothersEntitites.TECH_HIERARCHY.Where(x => x.SearchType == "SP" && x.DealerId == techId).FirstOrDefault();

                            if (THV != null)
                            {
                                availableTechId = techId;
                                break;
                            }
                        }

                    }
                }
            }

            return availableTechId;
        }

        private int getAvailableOnCallTechId(string PostalCode, DateTime currentTime, int WorkorderId, FBWWOCallEntities FarmerBrothersEntitites)
        {
            int availableTechId = -1;
            int replaceTechId = 0;

            // DataTable rsReferralList =
            SqlHelper helper = new SqlHelper();
            DataTable rsReferralList = helper.GetAfterHoursOnCallTechDetails(PostalCode, WorkorderId);

            //Check for Internal Techs
            foreach (DataRow dr in rsReferralList.Rows)
            {
                string techType = dr["TechType"].ToString();
                if (techType.ToUpper() != "FB") continue;

                int techId = Convert.ToInt32(dr["ServiceCenterId"]);
                bool IsUnavailable = IsTechUnAvailable(techId, currentTime, out replaceTechId);

                if (!IsUnavailable)
                {
                    if (replaceTechId != 0)
                    {
                        TECH_HIERARCHY THV = FarmerBrothersEntitites.TECH_HIERARCHY.Where(x => x.SearchType == "SP" && x.DealerId == replaceTechId).FirstOrDefault();

                        if (THV != null)
                        {
                            availableTechId = replaceTechId;
                            break;
                        }
                    }
                    else
                    {
                        TECH_HIERARCHY THV = FarmerBrothersEntitites.TECH_HIERARCHY.Where(x => x.SearchType == "SP" && x.DealerId == techId).FirstOrDefault();

                        if (THV != null)
                        {
                            availableTechId = techId;
                            break;
                        }
                    }
                }
            }

            if (availableTechId == -1)
            {
                //Check for thirdParty Techs
                foreach (DataRow dr in rsReferralList.Rows)
                {
                    string techType = dr["TechType"].ToString();
                    if (techType.ToUpper() == "FB") continue;

                    int techId = Convert.ToInt32(dr["ServiceCenterId"]);
                    bool IsUnavailable = IsTechUnAvailable(techId, currentTime, out replaceTechId);

                    if (!IsUnavailable)
                    {
                        if (replaceTechId != 0)
                        {
                            TECH_HIERARCHY THV = FarmerBrothersEntitites.TECH_HIERARCHY.Where(x => x.SearchType == "SP" && x.DealerId == replaceTechId).FirstOrDefault();

                            if (THV != null)
                            {
                                availableTechId = replaceTechId;
                                break;
                            }
                        }
                        else
                        {
                            TECH_HIERARCHY THV = FarmerBrothersEntitites.TECH_HIERARCHY.Where(x => x.SearchType == "SP" && x.DealerId == techId).FirstOrDefault();

                            if (THV != null)
                            {
                                availableTechId = techId;
                                break;
                            }
                        }

                    }
                }
            }

            return availableTechId;
        }

        [ValidateInput(false)]
        [HttpPost]
        public JsonResult DispatchMail(int workOrderId, int techId, bool isResponsible, List<String> notes, bool IsAutoDispatched, FBWWOCallEntities FarmerBrothersEntitites, bool isFromAutoDispatch = true)
        {
            int returnValue = -1;
            TechHierarchyView techHierarchyView = Utility.GetTechDataByResponsibleTechId(FarmerBrothersEntitites, techId);
            StringBuilder salesEmailBody = new StringBuilder();
            WorkOrder workOrder = FarmerBrothersEntitites.WorkOrders.Where(w => w.WorkorderID == workOrderId).FirstOrDefault();
            string workOrderStatus = "";
            string addtionalNotes = string.Empty;

            DateTime currentTime = Utility.GetCurrentTime(techHierarchyView.TechZip, FarmerBrothersEntitites);

            string message = string.Empty;
            string redirectUrl = string.Empty;

            if (workOrder != null)
            {
                if (string.Compare(workOrder.WorkorderCallstatus, "Closed", true) != 0
                && string.Compare(workOrder.WorkorderCallstatus, "Invoiced", true) != 0
                && string.Compare(workOrder.WorkorderCallstatus, "Completed", true) != 0
                && string.Compare(workOrder.WorkorderCallstatus, "Attempting", true) != 0)
                {
                    if (isResponsible == true)
                    {
                        UpdateTechAssignedStatus(techId, workOrder, "Sent", FarmerBrothersEntitites, 0, -1);
                    }
                    else
                    {
                        UpdateTechAssignedStatus(techId, workOrder, "Sent", FarmerBrothersEntitites, -1, 0);
                    }

                    StringBuilder subject = new StringBuilder();
                    if (workOrder.PriorityCode == 1 || workOrder.PriorityCode == 2 || workOrder.PriorityCode == 3 || workOrder.PriorityCode == 4)
                    {
                        subject.Append("CRITICAL WO: ");
                    }
                    else
                    {
                        subject.Append("WO: ");
                    }

                    subject.Append(workOrder.WorkorderID);
                    subject.Append(" ST: ");
                    subject.Append(workOrder.CustomerState);
                    subject.Append(" Call Type: ");
                    subject.Append(workOrder.WorkorderCalltypeDesc);

                    string emailAddress = string.Empty;
                    int userId = 99999;
                    TECH_HIERARCHY techView = GetTechById(techId);

                    //This is only for crytal
                    if (techId == Convert.ToInt32(ConfigurationManager.AppSettings["MAITestDispatch"]))
                    {
                        emailAddress = ConfigurationManager.AppSettings["CrystalEmailId"];
                    }
                    else if (techId == Convert.ToInt32(ConfigurationManager.AppSettings["MikeTestTechId"]))
                    {
                        emailAddress = ConfigurationManager.AppSettings["MikeEmailId"];
                    }
                    else
                    {
                        if (Convert.ToBoolean(ConfigurationManager.AppSettings["UseTestMails"]))
                        {
                            emailAddress = ConfigurationManager.AppSettings["TestEmail"];
                        }
                        else if (techView != null)
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
                        else
                        {
                            emailAddress = ConfigurationManager.AppSettings["TestEmail"];
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(emailAddress))
                    {

                        foreach (string note in notes)
                        {
                            string trNotes = note.Replace("\"", "").Replace("[", "").Replace("]", "");
                            if (!string.IsNullOrEmpty(trNotes))
                            {
                                NotesHistory notesHistory = new NotesHistory()
                                {
                                    AutomaticNotes = 0,
                                    EntryDate = currentTime,
                                    Notes = trNotes,
                                    Userid = 99999,
                                    UserName = SubmittedBy,//"WEB",
                                    WorkorderID = workOrder.WorkorderID,
                                    isDispatchNotes= 1
                                };
                                addtionalNotes = trNotes;
                                FarmerBrothersEntitites.NotesHistories.Add(notesHistory);
                            }

                        }

                        bool result = SendWorkOrderMail(workOrder, subject.ToString(), emailAddress, ConfigurationManager.AppSettings["DispatchMailFromAddress"], techId, MailType.DISPATCH, isResponsible, addtionalNotes, FarmerBrothersEntitites, "TRANSMIT");
                        if (result == true)
                        {
                            workOrder.ResponsibleTechid = techHierarchyView.TechID;
                            workOrder.ResponsibleTechName = techHierarchyView.PreferredProvider;

                            workOrder.WorkorderModifiedDate = currentTime;
                            workOrder.ModifiedUserName = SubmittedBy;// "WEB";
                        }
                        returnValue = FarmerBrothersEntitites.SaveChanges();

                    }

                    workOrderStatus = workOrder.WorkorderCallstatus;

                    if (IsAutoDispatched == true)
                    {
                        AgentDispatchLog autodispatchLog = new AgentDispatchLog()
                        {
                            TDate = currentTime,
                            UserID = 99999,
                            UserName = SubmittedBy,//"WEB",
                            WorkorderID = workOrder.WorkorderID
                        };
                        FarmerBrothersEntitites.AgentDispatchLogs.Add(autodispatchLog);
                        FarmerBrothersEntitites.SaveChanges();
                    }
                }


            }

            if (isFromAutoDispatch)
            {
                //redirectUrl = new UrlHelper(Request.RequestContext).Action("WorkorderManagement", "Workorder", new { customerId = workOrder.CustomerID, workOrderId = workOrder.WorkorderID });
                redirectUrl = new UrlHelper(Request.RequestContext).Action("WorkorderSearch", "Workorder", new { @IsBack = 1 });
            }

            JsonResult jsonResult = new JsonResult();
            jsonResult.Data = new { success = true, serverError = ErrorCode.SUCCESS, returnValue = returnValue > 0 ? 1 : 0, WorkorderCallstatus = workOrderStatus, Url = redirectUrl, message = message };
            jsonResult.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            return jsonResult;
        }


        public bool UpdateTechAssignedStatus(int techId, WorkOrder workOrder, string assignedStatus, FBWWOCallEntities FarmerBrothersEntitites, int isResponsible = -1, int isAssist = -1)
        {
            bool result = false;
            WorkorderSchedule techWorkOrderSchedule = workOrder.WorkorderSchedules.Where(ws => ws.Techid == techId).FirstOrDefault();

            DateTime currentTime = Utility.GetCurrentTime(workOrder.CustomerZipCode, FarmerBrothersEntitites);

            if (isResponsible == -1 && isAssist == -1)
            {
                if (string.Compare(techWorkOrderSchedule.AssignedStatus, "Sent", 0) == 0)
                {
                    string notesMessage = "";
                    if (string.Compare(assignedStatus, "Accepted", 0) == 0)
                    {
                        notesMessage = "Work order Accepted by " + techWorkOrderSchedule.TechName;

                        if (techWorkOrderSchedule.PrimaryTech >= 0)
                        {
                            techWorkOrderSchedule.PrimaryTech = 1;
                        }
                        else if (techWorkOrderSchedule.AssistTech >= 0)
                        {
                            techWorkOrderSchedule.AssistTech = 1;
                        }
                        techWorkOrderSchedule.EntryDate = currentTime;
                        techWorkOrderSchedule.ScheduleDate = currentTime;
                        techWorkOrderSchedule.ModifiedScheduleDate = currentTime;
                        techWorkOrderSchedule.ScheduleUserid = 99999;
                    }
                    else if (string.Compare(assignedStatus, "Declined", true) == 0)
                    {
                        notesMessage = "Work order Rejected by " + techWorkOrderSchedule.TechName;
                    }
                    techWorkOrderSchedule.AssignedStatus = assignedStatus;

                    NotesHistory notesHistory = new NotesHistory()
                    {
                        AutomaticNotes = 1,
                        EntryDate = currentTime,
                        Notes = notesMessage,
                        Userid = 99999,
                        UserName = SubmittedBy,//"WEB"
                        isDispatchNotes = 1
                    };
                    workOrder.NotesHistories.Add(notesHistory);
                    result = true;
                }
            }

            if (isResponsible >= 0)
            {
                TechHierarchyView techHierarchyView = Utility.GetTechDataByResponsibleTechId(FarmerBrothersEntitites, techId);

                //Responsible tech dispatch
                if (techWorkOrderSchedule != null)
                {
                    techWorkOrderSchedule.PrimaryTech = Convert.ToInt16(isResponsible);
                    techWorkOrderSchedule.AssignedStatus = assignedStatus;
                    techWorkOrderSchedule.ModifiedScheduleDate = currentTime;
                    techWorkOrderSchedule.ScheduleUserid = 99999;
                }
                else
                {
                    IndexCounter scheduleCounter = Utility.GetIndexCounter("ScheduleID", 1);
                    scheduleCounter.IndexValue++;
                    //FarmerBrothersEntitites.Entry(scheduleCounter).State = System.Data.Entity.EntityState.Modified;

                    WorkorderSchedule newworkOrderSchedule = new WorkorderSchedule()
                    {
                        Scheduleid = scheduleCounter.IndexValue.Value,
                        Techid = Convert.ToInt32(techHierarchyView.TechID),
                        TechName = techHierarchyView.PreferredProvider,
                        WorkorderID = workOrder.WorkorderID,
                        TechPhone = techHierarchyView.AreaCode + techHierarchyView.ProviderPhone,
                        ServiceCenterName = techHierarchyView.BranchName,
                        ServiceCenterID = Convert.ToInt32(techHierarchyView.TechID),
                        FSMName = techHierarchyView.DSMName,
                        FSMID = techHierarchyView.DSMId != 0 ? Convert.ToInt32(techHierarchyView.DSMId) : new Nullable<int>(),
                        EntryDate = currentTime,
                        ScheduleDate = currentTime,
                        TeamLeadName = WebConfigurationManager.AppSettings["ManagerName"],

                        PrimaryTech = Convert.ToInt16(isResponsible),
                        AssistTech = -1,
                        AssignedStatus = assignedStatus,
                        ModifiedScheduleDate = currentTime,
                        ScheduleUserid = 99999
                    };



                    workOrder.WorkorderSchedules.Add(newworkOrderSchedule);

                }

                bool redirected = false;
                string oldTechName = string.Empty;
                IEnumerable<WorkorderSchedule> primaryTechSchedules = workOrder.WorkorderSchedules.Where(ws => ws.PrimaryTech >= 0);
                foreach (WorkorderSchedule workOrderSchedule in primaryTechSchedules)
                {
                    if ((string.Compare(workOrderSchedule.AssignedStatus, "Sent", true) == 0
                        || string.Compare(workOrderSchedule.AssignedStatus, "Accepted", true) == 0)
                        && workOrderSchedule.Techid != techId && workOrderSchedule.AssistTech < 0)
                    {
                        redirected = true;
                        workOrderSchedule.AssignedStatus = "Redirected";
                        workOrderSchedule.PrimaryTech = -1;
                        workOrderSchedule.ModifiedScheduleDate = currentTime;
                        oldTechName = workOrderSchedule.TechName;
                        if (workOrderSchedule.Techid != techId)
                        {
                            StringBuilder subject = new StringBuilder();

                            subject.Append("Call has been redirected! WO: ");
                            subject.Append(workOrder.WorkorderID);
                            subject.Append(" ST: ");
                            subject.Append(workOrder.CustomerState);
                            subject.Append(" Call Type: ");
                            subject.Append(workOrder.WorkorderCalltypeDesc);

                            string emailAddress = string.Empty;

                            TECH_HIERARCHY techView = GetTechById(workOrderSchedule.Techid);

                            //This is only for crytal
                            if (techId == Convert.ToInt32(ConfigurationManager.AppSettings["MAITestDispatch"]))
                            {
                                emailAddress = ConfigurationManager.AppSettings["CrystalEmailId"];
                            }
                            else if (techId == Convert.ToInt32(ConfigurationManager.AppSettings["MikeTestTechId"]))
                            {
                                emailAddress = ConfigurationManager.AppSettings["MikeEmailId"];
                            }
                            else
                            {
                                if (Convert.ToBoolean(ConfigurationManager.AppSettings["UseTestMails"]))
                                {
                                    emailAddress = ConfigurationManager.AppSettings["TestEmail"];
                                }
                                else if (techView != null)
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
                                else
                                {
                                    emailAddress = ConfigurationManager.AppSettings["TestEmail"];
                                }
                            }

                            if (!string.IsNullOrWhiteSpace(emailAddress))
                            {
                                SendWorkOrderMail(workOrder, subject.ToString(), emailAddress, ConfigurationManager.AppSettings["DispatchMailFromAddress"], workOrderSchedule.Techid, MailType.REDIRECTED, false, "This Work Order has been redirected!", FarmerBrothersEntitites);
                            }

                        }
                    }
                }

                string notes = string.Empty;
                if (redirected == true)
                {
                    notes = oldTechName + " redirected work order to " + techHierarchyView.PreferredProvider;
                }
                else
                {
                    notes = "Work order sent to " + techHierarchyView.PreferredProvider;
                }

                NotesHistory notesHistory = new NotesHistory()
                {
                    AutomaticNotes = 1,
                    EntryDate = currentTime,
                    Notes = notes,
                    Userid = 99999,
                    UserName = SubmittedBy,//"WEB"
                    isDispatchNotes = 1
                };
                workOrder.NotesHistories.Add(notesHistory);


                result = true;
            }

            if (isAssist >= 0)
            {
                TechHierarchyView techHierarchyView = Utility.GetTechDataByResponsibleTechId(FarmerBrothersEntitites, techId);

                //assist tech dispatch
                if (techWorkOrderSchedule != null)
                {
                    techWorkOrderSchedule.PrimaryTech = Convert.ToInt16(isAssist);
                    techWorkOrderSchedule.AssignedStatus = assignedStatus;
                    techWorkOrderSchedule.ModifiedScheduleDate = currentTime;
                }
                else
                {
                    IndexCounter scheduleCounter = Utility.GetIndexCounter("ScheduleID", 1);
                    scheduleCounter.IndexValue++;
                    //FarmerBrothersEntitites.Entry(scheduleCounter).State = System.Data.Entity.EntityState.Modified;

                    WorkorderSchedule newworkOrderSchedule = new WorkorderSchedule()
                    {
                        Scheduleid = scheduleCounter.IndexValue.Value,
                        Techid = Convert.ToInt32(techHierarchyView.TechID),
                        TechName = techHierarchyView.PreferredProvider,
                        WorkorderID = workOrder.WorkorderID,
                        TechPhone = techHierarchyView.ProviderPhone,
                        ServiceCenterName = techHierarchyView.BranchName,
                        ServiceCenterID = Convert.ToInt32(techHierarchyView.TechID),
                        FSMName = techHierarchyView.DSMName,
                        FSMID = techHierarchyView.DSMId != 0 ? Convert.ToInt32(techHierarchyView.DSMId) : new Nullable<int>(),
                        EntryDate = currentTime,
                        ScheduleDate = currentTime,
                        TeamLeadName = WebConfigurationManager.AppSettings["ManagerName"],

                        AssistTech = Convert.ToInt16(isAssist),
                        PrimaryTech = -1,
                        AssignedStatus = assignedStatus,
                        ModifiedScheduleDate = currentTime,
                        ScheduleUserid = 99999
                    };

                    workOrder.WorkorderSchedules.Add(newworkOrderSchedule);

                }

                string notes = string.Empty;
                notes = "Work order sent to " + techHierarchyView.PreferredProvider;

                NotesHistory notesHistory = new NotesHistory()
                {
                    AutomaticNotes = 1,
                    EntryDate = currentTime,
                    Notes = notes,
                    Userid = 99999,
                    UserName = SubmittedBy,//"WEB"
                    isDispatchNotes = 1
                };
                workOrder.NotesHistories.Add(notesHistory);


                result = true;
            }

            int numberOfAssistAccepted = workOrder.WorkorderSchedules.Where(ws => ws.AssignedStatus == "Accepted" && ws.AssistTech >= 0).Count();
            int numberOfAssistRejected = workOrder.WorkorderSchedules.Where(ws => ws.AssignedStatus == "Declined" && ws.AssistTech >= 0).Count();
            int numberOfAssistDispatches = workOrder.WorkorderSchedules.Where(ws => ws.AssistTech >= 0).Count();
            int numberOfAssistRedirected = workOrder.WorkorderSchedules.Where(ws => ws.AssignedStatus == "Redirected" && ws.AssistTech >= 0).Count();

            int numberOfPrimaryAccepted = workOrder.WorkorderSchedules.Where(ws => ws.AssignedStatus == "Accepted" && ws.PrimaryTech >= 0).Count();
            int numberOfPrimaryRejected = workOrder.WorkorderSchedules.Where(ws => ws.AssignedStatus == "Declined" && ws.PrimaryTech >= 0).Count();
            int numberOfPrimaryDispatches = workOrder.WorkorderSchedules.Where(ws => ws.PrimaryTech >= 0).Count();
            int numberOfPrimaryRedirected = workOrder.WorkorderSchedules.Where(ws => ws.AssignedStatus == "Redirected" && ws.PrimaryTech >= 0).Count();

            string currentStatus = workOrder.WorkorderCallstatus;
            if ((numberOfPrimaryDispatches > 0 || numberOfPrimaryAccepted > 0) && (numberOfAssistDispatches > 0 || numberOfAssistAccepted > 0))
            {
                if (numberOfPrimaryAccepted > 0 && numberOfAssistAccepted > 0)
                {
                    workOrder.WorkorderCallstatus = "Accepted";
                }
                else if (numberOfPrimaryAccepted > 0 || numberOfAssistAccepted > 0)
                {
                    workOrder.WorkorderCallstatus = "Accepted-Partial";
                }
                else
                {
                    workOrder.WorkorderCallstatus = "Pending Acceptance";
                }
            }
            else if (numberOfPrimaryDispatches > 0 && numberOfPrimaryAccepted > 0)
            {
                workOrder.WorkorderCallstatus = "Accepted";
            }
            else if (numberOfAssistDispatches > 0 && numberOfAssistAccepted > 0)
            {
                workOrder.WorkorderCallstatus = "Accepted";
            }
            else if (string.Compare(assignedStatus, "Declined", true) == 0)
            {
                workOrder.WorkorderCallstatus = "Open";
            }
            else if (string.Compare(assignedStatus, "Sent", true) == 0)
            {
                workOrder.WorkorderCallstatus = "Pending Acceptance";
            }

            WorkorderStatusLog statusLog = new WorkorderStatusLog() { StatusFrom = currentStatus, StatusTo = workOrder.WorkorderCallstatus, StausChangeDate = currentTime, WorkorderID = workOrder.WorkorderID };
            workOrder.WorkorderStatusLogs.Add(statusLog);

            return result;
        }

        public static int GetCallsTotalCount(FBWWOCallEntities FBE, string CustomerID)
        {
            DateTime Last12Months = DateTime.Now.AddMonths(-12);
            return FBE.Set<WorkOrder>().Where(w => w.CustomerID.ToString() == CustomerID && w.WorkorderCalltypeid == 1200 && w.WorkorderEntryDate >= Last12Months).Count();
        }

        public static string IsBillableService(string BillingCode, int TotalCallCount)
        {
            string flag = "False";

            //string BillingDesc = FBE.FbBillableFeed.Where(b => b.Code == BillingCode).Select(d => d.Description).ToString();

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

        public static string GetServiceLevelDesc(FBWWOCallEntities FBE, string BillingCode)
        {
            return BillingCode + "  -  " + (FBE.FBBillableFeeds.Where(b => b.Code == BillingCode).FirstOrDefault()).Description;
        }

        public bool SendWorkOrderMail(WorkOrder workOrder, string subject, string toAddress, string fromAddress, int? techId, MailType mailType, bool isResponsible, string additionalMessage, FBWWOCallEntities FarmerBrothersEntitites, string mailFrom = "", bool isFromEmailCloserLink = false)
        {
            Contact customer = FarmerBrothersEntitites.Contacts.Where(c => c.ContactID == workOrder.CustomerID).FirstOrDefault();
            int TotalCallsCount = GetCallsTotalCount(FarmerBrothersEntitites, workOrder.CustomerID.ToString());

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

            StringBuilder salesEmailBody = new StringBuilder();

            salesEmailBody.Append(@"<img src='cid:logo' width='15%' height='15%'>");

            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("<BR>");
            string url = ConfigurationManager.AppSettings["DispatchResponseUrl"];
            string Redircturl = ConfigurationManager.AppSettings["RedirectResponseUrl"];
            string Closureurl = ConfigurationManager.AppSettings["CallClosureUrl"];
            //string finalUrl = string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=@response&isResponsible=" + isResponsible.ToString()));
            salesEmailBody.Append("<a href=&nbsp;></a>");
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
                        if (mailType == MailType.DISPATCH)
                        {
                            //salesEmailBody.Append("<a href=\"" + url + "?workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=0&isResponsible=" + isResponsible + "\">ACCEPT</a>");                        
                            salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=0&isResponsible=" + isResponsible.ToString())) + "\">ACCEPT</a>");
                            salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
                        }
                        if (workOrder.WorkorderCallstatus == "Pending Acceptance")
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

            if (!string.IsNullOrEmpty(additionalMessage) && mailFrom == "TRANSMIT")
            {
                salesEmailBody.Append("<b>ADDITIONAL NOTES: </b>");
                salesEmailBody.Append(Utility.GetStringWithNewLine(additionalMessage));
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
            salesEmailBody.Append("Billable: ");
            //salesEmailBody.Append(IsBillable);
            if (IsBillable == "True")
                salesEmailBody.Append("Billable");
            else if (IsBillable == "False")
                salesEmailBody.Append("Non-Billable");
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

            IEnumerable<WorkOrder> previousWorkOrders = FarmerBrothersEntitites.WorkOrders.
                Where(w => w.CustomerID == workOrder.CustomerID && (DbFunctions.DiffDays(w.WorkorderEntryDate, currentTime) < 90
                              && DbFunctions.DiffDays(w.WorkorderEntryDate, currentTime) > -90));


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
                        if (mailType == MailType.DISPATCH)
                        {
                            //salesEmailBody.Append("<a href=\"" + url + "?workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=0&isResponsible=" + isResponsible + "\">ACCEPT</a>");
                            salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=0&isResponsible=" + isResponsible.ToString())) + "\">ACCEPT</a>");
                            salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
                        }
                        if (workOrder.WorkorderCallstatus == "Pending Acceptance")
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
            string logoPath = string.Empty;
            if (Server == null)
            {
                logoPath = Path.Combine(HttpRuntime.AppDomainAppPath, "img/mainlogo.jpg");
            }
            else
            {
                logoPath = Server.MapPath("~/img/mainlogo.jpg");
            }


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

            bool result = true;
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

                message.From = new MailAddress(fromAddress);
                message.Subject = subject;
                message.IsBodyHtml = true;

                using (var smtp = new SmtpClient())
                {
                    smtp.Host = ConfigurationManager.AppSettings["MailServer"];
                    smtp.Port = 25;

                    try
                    {
                        smtp.Send(message);
                    }
                    catch (Exception ex)
                    {
                        result = false;
                    }
                }
            }
            return result;
        }

        protected Contact GetCustomerById(int? customerId)
        {
            Contact customer = null;
            using (FBWWOCallEntities FarmerBrothersEntitites = new FBWWOCallEntities())
            {
                customer = FarmerBrothersEntitites.Contacts.Where(x => x.ContactID == customerId).FirstOrDefault();
            }

            return customer;
        }

        private void SaveRemovalDetails(WorkorderManagementModel workorderManagement, WorkOrder workOrder, FBWWOCallEntities MarsServiceEntitites)
        {
            if (workorderManagement.RemovalCount > 0)
            {
                AllFBStatu status = MarsServiceEntitites.AllFBStatus.Where(a => a.FBStatusID == workorderManagement.RemovalReason).FirstOrDefault();
                RemovalSurvey survey = MarsServiceEntitites.RemovalSurveys.Where(r => r.WorkorderID == workOrder.WorkorderID).FirstOrDefault();
                if (survey != null)
                {
                    survey.JMSOwnedMachines = workorderManagement.RemovalCount;
                    survey.RemovalDate = workorderManagement.RemovalDate;
                    if (status != null)
                    {
                        survey.RemovalReason = status.FBStatus;
                    }
                    survey.RemoveAllMachines = workorderManagement.RemovaAll.ToString();
                    survey.BeveragesSupplier = workorderManagement.BeveragesSupplier;
                }
                else
                {
                    RemovalSurvey newSurvey = new RemovalSurvey()
                    {
                        BeveragesSupplier = workorderManagement.BeveragesSupplier,
                        JMSOwnedMachines = workorderManagement.RemovalCount,
                        RemovalDate = workorderManagement.RemovalDate,
                        RemoveAllMachines = workorderManagement.RemovaAll.ToString(),
                        WorkorderID = workOrder.WorkorderID,
                        RemovalReason = status.FBStatus
                    };
                    MarsServiceEntitites.RemovalSurveys.Add(newSurvey);

                    TimeZoneInfo newTimeZoneInfo = null;
                    Utility.GetCustomerTimeZone(workorderManagement.Customer.ZipCode, MarsServiceEntitites);
                    DateTime currentTime = Utility.GetCurrentTime(workorderManagement.Customer.ZipCode, MarsServiceEntitites);

                    NotesHistory notesHistory = new NotesHistory()
                    {
                        AutomaticNotes = 0,
                        EntryDate = currentTime,
                        Notes = "How many Smucker owned machines will we be removing? - " + workorderManagement.RemovalCount,
                        Userid = 99999,
                        UserName = SubmittedBy,//"WEB"
                        isDispatchNotes = 1
                    };
                    workOrder.NotesHistories.Add(notesHistory);

                    if (workorderManagement.RemovalDate.HasValue)
                    {
                        notesHistory = new NotesHistory()
                        {
                            AutomaticNotes = 0,
                            EntryDate = currentTime,
                            Notes = "What date will you need these machines removed by? - " + workorderManagement.RemovalDate.Value.ToString("MM/dd/yyyy"),
                            Userid = 99999,
                            UserName = SubmittedBy,//"WEB"
                            isDispatchNotes = 1
                        };
                        workOrder.NotesHistories.Add(notesHistory);
                    }

                    notesHistory = new NotesHistory()
                    {
                        AutomaticNotes = 0,
                        EntryDate = currentTime,
                        Notes = "Are we removing all machines from your facility? - " + workorderManagement.RemovaAll.ToString(),
                        Userid = 99999,
                        UserName = SubmittedBy,//"WEB"
                        isDispatchNotes = 1
                    };
                    workOrder.NotesHistories.Add(notesHistory);

                    if (workorderManagement.RemovaAll)
                    {

                        notesHistory = new NotesHistory()
                        {
                            AutomaticNotes = 0,
                            EntryDate = currentTime,
                            Notes = "May I ask the reason you have chosen to remove our machines from your location? - " + status.FBStatus,
                            Userid = 99999,
                            UserName = SubmittedBy,//"WEB"
                            isDispatchNotes = 1
                        };
                        workOrder.NotesHistories.Add(notesHistory);

                        if (!string.IsNullOrWhiteSpace(workorderManagement.BeveragesSupplier))
                        {
                            notesHistory = new NotesHistory()
                            {
                                AutomaticNotes = 0,
                                EntryDate = currentTime,
                                Notes = "Who will be supplying your beverages going forward? - " + workorderManagement.BeveragesSupplier,
                                Userid = 99999,
                                UserName = SubmittedBy,//"WEB"
                                isDispatchNotes = 1
                            };
                            workOrder.NotesHistories.Add(notesHistory);
                        }

                        StringBuilder notes = new StringBuilder();
                        if (workorderManagement.ClosingBusiness)
                        {
                            notes.Append("Closing Business;");
                        }
                        if (workorderManagement.FlavorOrTasteOfCoffee)
                        {
                            notes.Append("Flavor/Taste of Coffee;");
                        }
                        if (workorderManagement.EquipmentServiceReliabilityorResponseTime)
                        {
                            notes.Append("Equipment service reliability / response time;");
                        }
                        if (workorderManagement.EquipmentReliability)
                        {
                            notes.Append("Equipment reliability;");
                        }
                        if (workorderManagement.CostPerCup)
                        {
                            notes.Append("Cost per Cup;");
                        }
                        if (workorderManagement.ChangingGroupPurchasingProgram)
                        {
                            notes.Append("Changing group purchasing program;");
                        }
                        if (workorderManagement.ChangingDistributor)
                        {
                            notes.Append("Changing Distributor;");
                        }

                        if (!string.IsNullOrWhiteSpace(notes.ToString()))
                        {
                            notesHistory = new NotesHistory()
                            {
                                AutomaticNotes = 0,
                                EntryDate = currentTime,
                                Notes = "What were the main reasons to change your beverage solution? - " + notes.ToString(),
                                Userid = 99999,
                                UserName = SubmittedBy,//"WEB"
                                isDispatchNotes = 1
                            };
                            workOrder.NotesHistories.Add(notesHistory);
                        }
                    }
                }

                if (workorderManagement.RemovalCount > 1 && workorderManagement.RowId.HasValue && workorderManagement.RowId.Value < workorderManagement.WorkOrderEquipmentsRequested.Count())
                {
                    WorkOrderManagementEquipmentModel equipmentFromModel = workorderManagement.WorkOrderEquipmentsRequested.ElementAt(workorderManagement.RowId.Value);
                    IEnumerable<WorkorderEquipmentRequested> workOrderEquipments = MarsServiceEntitites.WorkorderEquipmentRequesteds.Where(we => we.WorkorderID == workOrder.WorkorderID);
                    if (workOrderEquipments != null)
                    {
                        IndexCounter counter = Utility.GetIndexCounter("AssetID", MarsServiceEntitites);
                        for (int count = 0; count < workorderManagement.RemovalCount - 1; count++)
                        {
                            counter.IndexValue++;
                            WorkorderEquipmentRequested equipment = new WorkorderEquipmentRequested()
                            {
                                Assetid = counter.IndexValue.Value,
                                CallTypeid = 1400,
                                Category = equipmentFromModel.Category,
                                Location = equipmentFromModel.Location,
                                SerialNumber = equipmentFromModel.SerialNumber,
                                Model = equipmentFromModel.Model,
                                CatalogID = equipmentFromModel.CatelogID,
                                Symptomid = equipmentFromModel.SymptomID
                            };
                            workOrder.WorkorderEquipmentRequesteds.Add(equipment);
                        }
                        MarsServiceEntitites.Entry(counter).State = System.Data.Entity.EntityState.Modified;
                    }
                }
            }
        }

        protected TECH_HIERARCHY GetTechById(int? techId)
        {
            using (FBWWOCallEntities MarsServiceEntitites = new FBWWOCallEntities())
            {
                TECH_HIERARCHY techView = MarsServiceEntitites.TECH_HIERARCHY.Where(x => x.DealerId == techId).FirstOrDefault();
                return techView;
            }

        }

        public static bool IsHoliday(DateTime StartTime)
        {
            bool isExist = false;
            using (FBWWOCallEntities FarmerBrothersEntitites = new FBWWOCallEntities())
            {
                var holiday = (from holday in FarmerBrothersEntitites.HolidayLists
                               where DbFunctions.TruncateTime(holday.HolidayDate) == DbFunctions.TruncateTime(StartTime)
                               select holday).FirstOrDefault();

                if (holiday != null)
                {
                    isExist = true;
                }
            }
            return isExist;
        }
        public bool FindAvailableDealers(string sPostalCode, bool bDefaultDealer, out DataTable rsReferralList)
        {
            SqlHelper helper = new SqlHelper();
            string sSQL;
            DataTable rsHierarchy;
            string sTableName;
            bool bFinished;
            long lReferralID;
            double dDistance;
            DataTable rsLatitudeLongitude;
            string sDealerLatLongFactor;
            double dDealerLatLongFactor;
            double dLatitude;
            double dLongitude;
            double dDealerLatitude;
            double dDealerLongitude;

            rsReferralList = new DataTable();


            // TODO: On Error GoTo Warning!!!: The statement is not translatable 
            bool FindAvailableDealers = true;
            bDefaultDealer = false;
            dLatitude = -1;
            dLongitude = -1;
            dDealerLatLongFactor = 2.5;

            if (GetPreference("ReferralLatLongDegrees", out sDealerLatLongFactor))
            {
                dDealerLatLongFactor = double.Parse(sDealerLatLongFactor);
            }
            else
            {
                dDealerLatLongFactor = 2.5;
            }


            if (ReferByAvailableDealersDistance(sPostalCode, dDealerLatLongFactor, out rsReferralList))
            {
                // no errors - check if any referrals were found
                if ((rsReferralList.Rows.Count > 0))
                {
                    bFinished = true;
                }

            }
            else
            {
                // error occurred - exit now
                FindAvailableDealers = false;
                bFinished = true;
            }


            return FindAvailableDealers;
        }

        // ******************************************************************
        // * Description:         Distance-based referral.
        // ******************************************************************
        private bool ReferByAvailableDealersDistance(string customerzipCode, double dDealerLatLongFactor, out DataTable rsReferralList)
        {
            SqlHelper helper = new SqlHelper();
            bool IsTechDetailsExist = false;

            rsReferralList = new DataTable();


            rsReferralList = helper.GetTechDispatchDetails(customerzipCode, dDealerLatLongFactor);
            if (rsReferralList.Rows.Count > 0)
            {
                IsTechDetailsExist = true;
            }
            return IsTechDetailsExist;
        }

        // *********************************************************************************
        // * Description:         Retrieves a record from the Preference table.
        // *********************************************************************************
        public bool GetPreference(string sPreferenceName, out string sPreferenceValue)
        {
            // Define necessary variables
            bool IsPreferenceExist = false;
            sPreferenceValue = string.Empty;
            string ReferralLatLongDegrees = ConfigurationManager.AppSettings[sPreferenceName];
            if (String.IsNullOrEmpty(ReferralLatLongDegrees))
            {
                using (FBWWOCallEntities entity = new FBWWOCallEntities())
                {
                    FBActivityLog log = new FBActivityLog();
                    log.LogDate = DateTime.UtcNow;
                    log.UserId = 99999;
                    log.ErrorDetails = "Auto Dispatch - No value found for the requested variable name: ReferralLatLongDegrees ";
                    entity.FBActivityLogs.Add(log);
                    entity.SaveChanges();
                }
            }
            else
            {
                IsPreferenceExist = true;
                sPreferenceValue = ReferralLatLongDegrees;
            }
            return IsPreferenceExist;
        }
        public static bool IsTechUnAvailable(int techId, DateTime StartTime, out int replaceTech)
        {
            bool isAvilable = false;
            replaceTech = techId;
            using (FBWWOCallEntities FarmerBrothersEntitites = new FBWWOCallEntities())
            {
                List<TechSchedule> holidays = (from sc in FarmerBrothersEntitites.TechSchedules
                                               where DbFunctions.TruncateTime(sc.ScheduleDate) == DbFunctions.TruncateTime(StartTime) && sc.TechId == techId
                                               select sc).ToList();

                if (holidays != null)
                {
                    foreach (TechSchedule holiday in holidays)
                    {
                        DateTime UnavailableStartDate = Convert.ToDateTime(StartTime.ToString("MM/dd/yyyy") + " " + new DateTime().AddHours(Convert.ToDouble(holiday.ScheduleStartTime)).ToString("hh:mm tt"));
                        DateTime UnavailableEndDate = Convert.ToDateTime(StartTime.ToString("MM/dd/yyyy") + " " + new DateTime().AddHours(Convert.ToDouble(holiday.ScheduleEndTime)).ToString("hh:mm tt"));

                        if ((UnavailableStartDate <= StartTime) && (UnavailableEndDate > StartTime))
                        {
                            if (holiday.ReplaceTech != null && holiday.ReplaceTech != 0)
                            {
                                replaceTech = Convert.ToInt32(holiday.ReplaceTech);
                                IsTechUnAvailable(replaceTech, StartTime, out replaceTech);
                            }
                            else
                            { return true; }
                        }
                        else
                        {
                            isAvilable = false;
                        }
                    }
                }
            }
            return isAvilable;
        }

    }
}