using FBCall.Models;
using Syncfusion.JavaScript;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace FBCall.Controllers
{
    public enum ErrorCode
    {
        SUCCESS = 0,
        ERROR = 1,
    }

    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        #region captch

        public ActionResult Refresh(CaptchaParams parameters)
        {

            return parameters.CaptchaActions();

        }
        #endregion

        public ActionResult IsCustomerExist(string customerId, string userName)
        {
            string message = string.Empty;
            string redirectUrl = string.Empty;
            if (!string.IsNullOrEmpty(customerId))
            {
                if (!IsValidCustomer(customerId))
                {
                    message = "Customer Account Number is not valid, Please Enter Valid Account Number!";
                }                
            }
            if (string.IsNullOrEmpty(userName))
            {
                message = "Please Enter the SubmittedBy Name!";
            }

            redirectUrl = new UrlHelper(Request.RequestContext).Action("WorkOrder", "Home", new { customerId = customerId, submittedBy = userName });

            JsonResult jsonResult = new JsonResult();
            jsonResult.Data = new { Url = redirectUrl, message = message };
            jsonResult.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            return jsonResult;
        }

        public ActionResult CustomerExist(string customerId, string userName)
        {
            string message = string.Empty;
            int responseCode = 200; bool isSuccess = true;

            if (string.IsNullOrEmpty(customerId))
            {               
                    message = "Please Enter CustomerId!";
                    responseCode = 500;
                    isSuccess = false;               
            }
            if (string.IsNullOrEmpty(userName))
            {
                message = "Please Enter the SubmittedBy Name!";
                responseCode = 500;
                isSuccess = false;
            }

            Contact CustomerDetails = ValidCustomerDetails(customerId);
            if (CustomerDetails == null)
            {
                message = "InValid Account Number!";
                responseCode = 500;
                isSuccess = false;
            }

            List<WorkorderType> CallReasonsList = null; List<AllFBStatu> PriorityList = null; 
            using (FBWWOCallEntities MarsServiceEntitites = new FBWWOCallEntities())
            {
                CallReasonsList = MarsServiceEntitites.WorkorderTypes.
                    Where(wt => wt.Active == 1 && (wt.CallTypeID != 1300 && wt.CallTypeID != 1310 && wt.CallTypeID != 1210
                    && wt.CallTypeID != 1130 && wt.CallTypeID != 1230)).OrderBy(wt => wt.Sequence).ToList();

                PriorityList = MarsServiceEntitites.AllFBStatus.Where(p => p.StatusFor == "Priority" && p.Active == 1).OrderBy(p => p.StatusSequence).ToList();
            }

            var data = new
            {
                customerData = CustomerDetails,
                CallReasonsList = CallReasonsList.Select(s => new
                {
                    s.CallTypeID,
                    s.Description
                }).OrderBy(o => o.Description).ToList(),
                PriorityList= PriorityList.Select(s => new
                {
                    s.FBStatusID,
                    s.FBStatus
                }).ToList()
            };

            JsonResult jsonResult = new JsonResult();
            jsonResult.Data = new { responseCode = responseCode,  responseMessage = message, Success = isSuccess, Data = data };
            jsonResult.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            return jsonResult;
        }

        public ActionResult GetTechnicians(string PostalCode)
        {
            string message = string.Empty;
            int responseCode = 200; bool isSuccess = true;

            List<TechHierarchyView> TechnicianList = null;
            using (FBWWOCallEntities MarsServiceEntitites = new FBWWOCallEntities())
            {
                IEnumerable<TechHierarchyView> Techlist = Utility.GetTechDataByBranchType(MarsServiceEntitites, null, null);
                DateTime currentTime = Utility.GetCurrentTime(PostalCode, MarsServiceEntitites);
                List<TechHierarchyView> newTechlistCollection = new List<TechHierarchyView>();

                /*Removed this part as the IsTechUnAvailable method is taking time to load and sometimes going into infinite loop, Also this process will be taken care by the autodispatch process
                  int replaceTechId = 0;
                foreach (TechHierarchyView thv in Techlist)
                {
                    int tchId = Convert.ToInt32(thv.TechID);
                    if (!WorkorderController.IsTechUnAvailable(tchId, currentTime, out replaceTechId))
                    {
                        newTechlistCollection.Add(thv);
                    }

                }*/

                foreach (TechHierarchyView thv in Techlist)
                {
                    int tchId = Convert.ToInt32(thv.TechID);

                    List<TechSchedule> holidays = (from sc in MarsServiceEntitites.TechSchedules
                                                   where DbFunctions.TruncateTime(sc.ScheduleDate) == DbFunctions.TruncateTime(currentTime) && sc.TechId == tchId
                                                   select sc).ToList();

                    if (holidays != null && holidays.Count > 0)
                    {
                        foreach (TechSchedule holiday in holidays)
                        {
                            DateTime UnavailableStartDate = Convert.ToDateTime(currentTime.ToString("MM/dd/yyyy") + " " + new DateTime().AddHours(Convert.ToDouble(holiday.ScheduleStartTime)).ToString("hh:mm tt"));
                            DateTime UnavailableEndDate = Convert.ToDateTime(currentTime.ToString("MM/dd/yyyy") + " " + new DateTime().AddHours(Convert.ToDouble(holiday.ScheduleEndTime)).ToString("hh:mm tt"));

                            if ((UnavailableStartDate <= currentTime) && (UnavailableEndDate > currentTime))
                            {
                                continue;
                            }
                            else
                            {
                                newTechlistCollection.Add(thv);
                            }
                        }
                    }
                    else
                    {
                        newTechlistCollection.Add(thv);
                    }
                }

                TechnicianList = newTechlistCollection;
            }


            var TechList = TechnicianList.Select(s => new { s.PreferredProvider, s.TechID }).ToList();


            JsonResult jsonResult = new JsonResult();
            jsonResult.Data = new { responseCode = responseCode, responseMessage = message, Success = isSuccess, Data = TechList };
            jsonResult.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            return jsonResult;
        }

        public Contact ValidCustomerDetails(string customerId)
        {
            Contact customer = null;
            using (FBWWOCallEntities MarsServiceEntitites = new FBWWOCallEntities())
            {
                int cid = Convert.ToInt32(customerId);
                customer = MarsServiceEntitites.Contacts.Where(e => e.ContactID == cid && e.IsUnknownUser != 1
                  && (e.SearchType.ToString().Equals("C") || e.SearchType.ToString().Equals("CA") || e.SearchType.ToString().Equals("XC") || e.SearchType.ToString().Equals("XCA") ||
                  e.SearchType.ToString().Equals("XCI") || e.SearchType.ToString().Equals("CCS") || e.SearchType.ToString().Equals("CFS") || e.SearchType.ToString().Equals("CB") ||
                  e.SearchType.ToString().Equals("CE") || e.SearchType.ToString().Equals("CFD") || e.SearchType.ToString().Equals("PFS") || e.SearchType.ToString().Equals("BR"))).FirstOrDefault();               
            }

            return customer;
        }

        public bool IsValidCustomer(string customerId)
        {
            bool isExist = false;
            using (FBWWOCallEntities MarsServiceEntitites = new FBWWOCallEntities())
            {
                int cid = Convert.ToInt32(customerId);
                var customer = MarsServiceEntitites.Contacts.Where(e => e.ContactID == cid  && e.IsUnknownUser != 1
                  && (e.SearchType.ToString().Equals("C") || e.SearchType.ToString().Equals("CA") || e.SearchType.ToString().Equals("XC") || e.SearchType.ToString().Equals("XCA") ||
                  e.SearchType.ToString().Equals("XCI") || e.SearchType.ToString().Equals("CCS") || e.SearchType.ToString().Equals("CFS") || e.SearchType.ToString().Equals("CB") ||
                  e.SearchType.ToString().Equals("CE") || e.SearchType.ToString().Equals("CFD") || e.SearchType.ToString().Equals("PFS") || e.SearchType.ToString().Equals("BR"))).FirstOrDefault();

                if (customer != null)
                {
                    isExist = true;
                }
            }


            return isExist;
        }

        public ActionResult WorkOrder(int customerId, string submittedBy)
        {
            AutoGenerateWorkorderModel autowoModel = new AutoGenerateWorkorderModel();
            using (FBWWOCallEntities MarsServiceEntitites = new FBWWOCallEntities())
            {
                //List<WorkorderType> WorkorderTypes = MarsServiceEntitites.WorkorderTypes.
                //    Where(wt => wt.Active == 1 &&( wt.CallTypeID == 1200 || wt.CallTypeID == 1100  || wt.CallTypeID == 1400 || wt.CallTypeID == 1800 || wt.CallTypeID == 1810 || wt.CallTypeID == 1900)).OrderBy(wt => wt.Sequence).ToList();

                List<WorkorderType> WorkorderTypes = MarsServiceEntitites.WorkorderTypes.
                    Where(wt => wt.Active == 1 && (wt.CallTypeID != 1300 && wt.CallTypeID != 1310 && wt.CallTypeID != 1210 
                    && wt.CallTypeID != 1130 && wt.CallTypeID != 1230)).OrderBy(wt => wt.Sequence).ToList();

                WorkorderType woType = new WorkorderType()
                {
                    CallTypeID = -1,
                    Description = "Please Select Call Reason"
                };
                WorkorderTypes.Insert(0, woType);
                autowoModel.WorkorderTypes = WorkorderTypes;

                CustomerModel customerModel = new CustomerModel();

                IList<Contact> customers = new List<Contact>();

                customers = MarsServiceEntitites.Contacts.Where(x => x.ContactID == customerId).ToList();

                if (customers != null)
                {
                    if (customers.Count > 0)
                    {
                        customerModel = new CustomerModel(customers[0], MarsServiceEntitites);
                    }
                }

                autowoModel.Customer = customerModel;

                NotesModel notes = new NotesModel();
                notes.NotesHistory = new List<NotesHistoryModel>();
                notes.RecordHistory = new List<NotesHistoryModel>();
                notes.CustomerNotesResults = new List<CustomerNotesModel>();

                notes.CustomerZipCode = autowoModel.Customer.ZipCode;
                autowoModel.Notes = notes;
                autowoModel.UserName = "WEB - " + submittedBy;//"WEB";
                autowoModel.Notes.isFromAutoGenerateWorkOrder = true;

                autowoModel.PriorityList = MarsServiceEntitites.AllFBStatus.Where(p => p.StatusFor == "Priority" && p.Active == 1).OrderBy(p => p.StatusSequence).ToList();

                //****************************
                IEnumerable<TechHierarchyView> Techlist = Utility.GetTechDataByBranchType(MarsServiceEntitites, null, null);
                DateTime currentTime = Utility.GetCurrentTime(autowoModel.Customer.ZipCode, MarsServiceEntitites);
                List<TechHierarchyView> newTechlistCollection = new List<TechHierarchyView>();

                //int replaceTechId = 0;
                //foreach (TechHierarchyView thv in Techlist)
                //{
                //    int tchId = Convert.ToInt32(thv.TechID);
                //    if (!WorkorderController.IsTechUnAvailable(tchId, currentTime, out replaceTechId))
                //    {
                //        newTechlistCollection.Add(thv);
                //    }

                //}

                foreach (TechHierarchyView thv in Techlist)
                {
                    int tchId = Convert.ToInt32(thv.TechID);

                    List<TechSchedule> holidays = (from sc in MarsServiceEntitites.TechSchedules
                                                   where DbFunctions.TruncateTime(sc.ScheduleDate) == DbFunctions.TruncateTime(currentTime) && sc.TechId == tchId
                                                   select sc).ToList();

                    if (holidays != null && holidays.Count > 0)
                    {
                        foreach (TechSchedule holiday in holidays)
                        {
                            DateTime UnavailableStartDate = Convert.ToDateTime(currentTime.ToString("MM/dd/yyyy") + " " + new DateTime().AddHours(Convert.ToDouble(holiday.ScheduleStartTime)).ToString("hh:mm tt"));
                            DateTime UnavailableEndDate = Convert.ToDateTime(currentTime.ToString("MM/dd/yyyy") + " " + new DateTime().AddHours(Convert.ToDouble(holiday.ScheduleEndTime)).ToString("hh:mm tt"));

                            if ((UnavailableStartDate <= currentTime) && (UnavailableEndDate > currentTime))
                            {
                                continue;
                            }
                            else
                            {
                                newTechlistCollection.Add(thv);
                            }
                        }
                    }
                    else
                    {
                        newTechlistCollection.Add(thv);
                    }
                }

                string requstUrl = string.Empty;               
                TechHierarchyView techhierarchy = new TechHierarchyView()
                {
                    TechID = -1,
                    PreferredProvider = "Please select Technician"
                };
                newTechlistCollection.Insert(0, techhierarchy);

                autowoModel.Notes.Technicianlist = newTechlistCollection;
                //**************************************
            }

            return View(autowoModel);
        }

        [HttpPost]
        [MultipleButton(Name = "action", Argument = "WorkorderSave")]
        public JsonResult SaveWorkOrder([ModelBinder(typeof(AutoCallGenerateModelBinder))] AutoGenerateWorkorderModel workorderManagement)
        {
            var redirectUrl = string.Empty;
            var message = string.Empty;
            JsonResult jsonResult = new JsonResult();
            WorkorderController wc = new WorkorderController();
            WorkOrder workOrder = null;

            using (FBWWOCallEntities MarsServiceEntitites = new FBWWOCallEntities())
            {
                Contact customer = MarsServiceEntitites.Contacts.Where(x => x.ContactID == workorderManagement.CustomerID).FirstOrDefault();
                TimeZoneInfo newTimeZoneInfo = null;
                Utility.GetCustomerTimeZone(customer.PostalCode, MarsServiceEntitites);
                DateTime CurrentTime;
                DateTime.TryParse(Utility.GetCurrentTime(customer.PostalCode, MarsServiceEntitites).ToString("hh:mm tt"), out CurrentTime);


                #region save notes

                if (workorderManagement.NewNotes != null)
                {

                    foreach (NewNotesModel newNotesModel in workorderManagement.NewNotes)
                    {
                        NotesHistory notesHistory = new NotesHistory()
                        {
                            AutomaticNotes = 0,
                            EntryDate = CurrentTime,
                            Notes = newNotesModel.Text,
                            Userid = 99999,
                            UserName = "WEB",
                            isDispatchNotes = 0
                        };
                        MarsServiceEntitites.NotesHistories.Add(notesHistory);
                    }
                }
                #endregion

                #region create work order
              

                try
                {

                    WorkorderManagementModel workorderModel = new WorkorderManagementModel();

                    CustomerModel customerModel = new CustomerModel();
                    workorderModel.Closure = new WorkOrderClosureModel();
                    if (customer != null)
                    {
                        customerModel = new CustomerModel(customer, MarsServiceEntitites);
                        //customerModel = Utility.PopulateCustomerWithZonePriorityDetails(MarsServiceEntitites, customerModel);
                        customerModel.PhoneNumber = Utility.FormatPhoneNumber(customer.PhoneWithAreaCode);
                    }

                    workorderModel.Customer = customerModel;
                    workorderModel.Customer.CustomerId = customerModel.CustomerId;
                    workorderModel.Notes = workorderManagement.Notes;
                    workorderModel.Operation = WorkOrderManagementSubmitType.CREATEWORKORDER;
                    workorderModel.WorkOrder = new WorkOrder();
                    workorderModel.WorkOrder.WorkorderCalltypeid = Convert.ToInt32(workorderManagement.callReason);
                    if (workorderModel.WorkOrder.WorkorderCalltypeid != null)
                    {
                        workorderModel.WorkOrder.WorkorderCalltypeDesc = MarsServiceEntitites.WorkorderTypes.Where(t => t.CallTypeID == workorderModel.WorkOrder.WorkorderCalltypeid).Select(td => td.Description).FirstOrDefault();
                    }
                    workorderModel.WorkOrder.CallerName = workorderManagement.CallerName;
                    workorderModel.WorkOrder.WorkorderContactName = workorderManagement.CallerName;
                    workorderModel.WorkOrder.WorkorderContactPhone = workorderManagement.WorkorderContactPhone;
                    workorderModel.WorkOrder.HoursOfOperation = "N/A";
                    workorderModel.WorkOrder.PriorityCode = workorderManagement.PriorityCode;
                    workorderModel.WorkOrder.WorkOrderBrands = new List<WorkOrderBrand>();
                    WorkOrderBrand brand = new WorkOrderBrand();
                    brand.BrandID = 997;
                    workorderModel.WorkOrder.WorkOrderBrands.Add(brand);
                    workorderModel.PriorityList = new List<AllFBStatu>();
                    AllFBStatu priority = new AllFBStatu();
                    priority.FBStatusID = Convert.ToInt32(workorderManagement.PriorityCode);
                    priority.FBStatus = workorderManagement.PriorityName;
                    workorderModel.PriorityList.Add(priority);
                    workorderModel.NewNotes = new List<NewNotesModel>();
                    workorderModel.NewNotes = workorderManagement.NewNotes;
                    //Used it to save in work order Equipment table
                    workorderModel.WorkOrder.ClosedUserName = workorderManagement.EquipmentLocation;                    


                    workorderModel.WorkOrderEquipments = new List<WorkOrderManagementEquipmentModel>();
                    workorderModel.WorkOrderEquipmentsRequested = new List<WorkOrderManagementEquipmentModel>();
                    workorderModel.WorkOrderParts = new List<WorkOrderPartModel>();
                    workorderModel.SubmittedBy = workorderManagement.Customer.SubmittedBy;
                    workorderModel.SubmittedBy = workorderManagement.Customer.SubmittedBy;

                    jsonResult = wc.SaveWorkOrder(workorderModel, null, string.Empty, MarsServiceEntitites, out workOrder, true);
                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    WorkOrderResults result = serializer.Deserialize<WorkOrderResults>(serializer.Serialize(jsonResult.Data));
                    if (result.returnValue > 0)
                    {
                        workorderManagement.WorkOrderID = Convert.ToInt32(result.WorkOrderId);
                        message = @"|Work Order created successfully! Work Order ID#: " + workorderManagement.WorkOrderID;
                        NotesHistory notesHistory = new NotesHistory()
                        {
                            AutomaticNotes = 1,
                            EntryDate = CurrentTime,
                            Notes = @"Work Order created from MARS WO#: " + Convert.ToInt32(result.WorkOrderId) + @" in “MARS”!",
                            Userid = 99999,
                            UserName = "WEB",
                            isDispatchNotes = 0
                        };
                        MarsServiceEntitites.NotesHistories.Add(notesHistory);
                        MarsServiceEntitites.SaveChanges();
                    }
                }
                catch (Exception e)
                {
                    message = "|There is a problem in Work Order Creation! Please contact support.";
                }
                #endregion

            }


            redirectUrl = new UrlHelper(Request.RequestContext).Action("Index", "Home");            
            //wc.StartAutoDispatchProcess(workOrder);

            jsonResult.Data = new { success = true, serverError = ErrorCode.SUCCESS, Url = redirectUrl, message = message };
            jsonResult.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            return jsonResult;
        }
    }
}