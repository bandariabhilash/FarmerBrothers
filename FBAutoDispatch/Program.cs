using Microsoft.Graph;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;
using DayOfWeek = System.DayOfWeek;
using LinkedResource = System.Net.Mail.LinkedResource;

namespace FBAutoDispatch
{
    public class Program
    {
        public static FBEntities FarmerBrothersEntitites = new FBEntities();
        public static int UserId = 0;
        public static string UserName = "";

        static void Main(string[] args)
        {

            DateTime currentDate = DateTime.Now.AddDays(-10);
            List<WorkOrder> workorderList = FarmerBrothersEntitites.WorkOrders.Where(w => w.WorkorderCallstatus.ToLower() == "open"
                && w.WorkorderEntryDate >= currentDate && (w.IsAutoDispatched == false || w.IsAutoDispatched == null))
                .OrderByDescending(o => o.WorkorderEntryDate).ToList();


            //int[] ids = new int[] {
            //};

            //List<WorkOrder> workorderList = FarmerBrothersEntitites.WorkOrders.Where(i => ids.Contains(i.WorkorderID)).ToList();

            Program po = new Program();

            FbUserMaster fbUser = FarmerBrothersEntitites.FbUserMasters.Where(u => u.FirstName.ToLower() == "autodispatch" && u.Email == "autodispatch").FirstOrDefault();
            if (fbUser != null)
            {
                UserId = fbUser.UserId;
                UserName = fbUser.FirstName;
            }
            else
            {
                UserId = 1234;
                UserName = "Autodispatch";
            }

            foreach (WorkOrder wo in workorderList)
            {
                //if (wo.WorkorderID != 2017579497) continue;
                if (wo.WorkorderCalltypeDesc != null && wo.WorkorderCalltypeDesc.ToLower() == "parts request")
                {
                    bool flag = po.SendPartsOrderMail(wo.WorkorderID);
                    if (flag)
                    {
                        FBEntities fbEntity = new FBEntities();
                        WorkOrder wrkord = fbEntity.WorkOrders.Where(w => w.WorkorderID == wo.WorkorderID).FirstOrDefault();
                        if (wrkord != null)
                        {
                            wrkord.IsAutoDispatched = true;
                        }
                        fbEntity.SaveChanges();
                        fbEntity.Dispose();
                    }
                }
                else
                {
                    if (wo.IsSpecificTechnician == true && wo.SpecificTechnician != null)
                    {
                        DateTime currentTime = Utility.GetCurrentTime(wo.CustomerZipCode, FarmerBrothersEntitites);
                        int TechID = Convert.ToInt32(wo.SpecificTechnician);
                        TECH_HIERARCHY THV = FarmerBrothersEntitites.TECH_HIERARCHY.Where(x => x.SearchType == "SP" && x.DealerId == TechID).FirstOrDefault();
                        //if (THV.FamilyAff == "SPT")
                        //{
                        //    if (wo.WorkorderCalltypeid == 1300)
                        //    {
                        //        TECH_HIERARCHY th = FarmerBrothersEntitites.TECH_HIERARCHY.Where(a => a.DealerId == 909360).FirstOrDefault();
                        //        if (th != null)
                        //        {
                        //            TechID = 909360; // If the 3rd party call type is 1300 installation, those events will be sent to Christina Ware – SP 909360(Email from Mike on July 15, 2020)
                        //        }
                        //    }
                        //}

                        new Program().DispatchMail(wo.WorkorderID, TechID, true, new List<string>(), true, false);

                        WorkOrder wrkord = FarmerBrothersEntitites.WorkOrders.Where(w => w.WorkorderID == wo.WorkorderID).FirstOrDefault();
                        if (wrkord != null)
                        {
                            wrkord.IsAutoDispatched = true;
                        }

                        TECH_HIERARCHY techView = new Program().GetTechById(TechID);
                        if (TechID != 0)
                        {
                            NotesHistory notesHistory = new NotesHistory()
                            {
                                AutomaticNotes = 1,
                                EntryDate = currentTime,
                                Notes = "Auto Dispatch E-mail  Sent to " + techView.RimEmail + " " + techView.EmailCC,
                                Userid = UserId,
                                UserName = UserName,
                                WorkorderID = wrkord.WorkorderID,
                                isDispatchNotes = 1
                            };
                            FarmerBrothersEntitites.NotesHistories.Add(notesHistory);
                        }
                        else
                        {
                            NotesHistory notesHistory = new NotesHistory()
                            {
                                AutomaticNotes = 1,
                                EntryDate = currentTime,
                                Notes = "Auto Dispatch E-mail  Sent to " + ConfigurationManager.AppSettings["MikeEmailId"] + ";" + ConfigurationManager.AppSettings["DarrylEmailId"],
                                Userid = UserId,
                                UserName = UserName,
                                WorkorderID = wrkord.WorkorderID,
                                isDispatchNotes = 1
                            };
                            FarmerBrothersEntitites.NotesHistories.Add(notesHistory);
                        }

                        FarmerBrothersEntitites.SaveChanges();
                    }
                    else
                    {
                        if (wo.IsAutoGenerated != null && Convert.ToBoolean(wo.IsAutoGenerated))
                        {
                            po.StartAutoDispatchProcessForAutoGenEvents(wo);

                        }
                        else
                        {
                            po.StartAutoDispatchProcess(wo);
                        }
                    }
                }
            }


            List<WorkOrder> fetcoEventList = (from w in FarmerBrothersEntitites.WorkOrders
                                              join c in FarmerBrothersEntitites.Contacts on w.CustomerID equals c.ContactID
                                              where c.PricingParentID == "9001250" && w.FetcoCreateEmailSent != true
                                              select w).ToList();
            foreach(WorkOrder w in fetcoEventList)
            {
                bool emailSent = po.SendFetcoEventCreateMail(w.WorkorderID);

                if (emailSent)
                {
                    WorkOrder evnt = FarmerBrothersEntitites.WorkOrders.Where(evt => evt.WorkorderID == w.WorkorderID).FirstOrDefault();
                    evnt.FetcoCreateEmailSent = true;
                    FarmerBrothersEntitites.SaveChanges();
                }
            }
        }

        public string CustomerUserName = "";
        public void StartAutoDispatchProcess(WorkOrder workOrderModel)
        {
            #region properties initialization

            CustomerUserName = UserName;

            SqlHelper helper = new SqlHelper();
            DateTime currentTime = Utility.GetCurrentTime(workOrderModel.CustomerZipCode, FarmerBrothersEntitites);
            DataTable WorkOrderdt;
            DataTable rsAssetList;
            DataTable Contactdt;

            int TechID = -1;
            DataTable rsDealerEmail;
            int ContactId;

            #endregion

            int resultFlag = IsValidWorkOrderToStartAutoDispatch(workOrderModel);

            if (resultFlag != 0)
            {
                string WorkOrderQuery = "Select * from WorkOrder where WorkorderID = " + workOrderModel.WorkorderID;
                WorkOrderdt = helper.GetDatatable(WorkOrderQuery);

                ContactId = WorkOrderdt.Rows.Count > 0 ? Convert.ToInt32(WorkOrderdt.Rows[0]["CustomerID"]) : 0;
                string ContactQuery = "Select * from v_Contact where ContactID = " + ContactId;
                Contactdt = helper.GetDatatable(ContactQuery);

                if (Contactdt.Rows.Count <= 0)
                {
                    using (FBEntities entity = new FBEntities())
                    {
                        FBActivityLog log = new FBActivityLog();
                        log.LogDate = DateTime.UtcNow;
                        log.UserId = UserId;
                        log.ErrorDetails = "Auto Dispatch - Unable to get contact information";
                        entity.FBActivityLogs.Add(log);
                        entity.SaveChanges();
                    }
                    return;
                }
                else
                {

                    string WorkOrderHistoryQuery = "Select * from v_ContactServiceHistory where WorkorderID = " + WorkOrderdt.Rows[0]["WorkorderID"];
                    rsAssetList = helper.GetDatatable(WorkOrderHistoryQuery);

                    if (rsAssetList.Rows.Count <= 0)
                    {
                        using (FBEntities entity = new FBEntities())
                        {
                            FBActivityLog log = new FBActivityLog();
                            log.LogDate = DateTime.UtcNow;
                            log.UserId = UserId;
                            log.ErrorDetails = "Auto Dispatch - Unable to get Asset information";
                            entity.FBActivityLogs.Add(log);
                            entity.SaveChanges();
                        }
                        return;
                    }
                    else
                    {

                    }

                    if (resultFlag == 1)
                    {
                        int replaceTechId = 0;
                        if (string.IsNullOrEmpty(Convert.ToString(Contactdt.Rows[0]["FBProviderID"])))
                        {
                            string postCode = Convert.ToString(Contactdt.Rows[0]["PostalCode"]);
                            TechID = getAvailableTechId(postCode, currentTime, ContactId);

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
                                    TechID = getAvailableTechId(postCode, currentTime, ContactId);
                                }
                            }
                            else
                            {
                                string postCode = Convert.ToString(Contactdt.Rows[0]["PostalCode"]);
                                TechID = getAvailableTechId(postCode, currentTime, ContactId);
                            }

                        }
                    }
                    else if (resultFlag == 2)
                    {
                        string postCode = Convert.ToString(Contactdt.Rows[0]["PostalCode"]);
                        TechID = getAvailableOnCallTechId(postCode, currentTime, workOrderModel.WorkorderID, ContactId);
                    }

                    if (TechID != -1 && TechID != 0)
                    {
                        TECH_HIERARCHY THV = FarmerBrothersEntitites.TECH_HIERARCHY.Where(x => x.SearchType == "SP" && x.DealerId == TechID).FirstOrDefault();
                        if (THV.FamilyAff == "SPT")
                        {
                            if (workOrderModel.WorkorderCalltypeid == 1300)
                            {
                                TECH_HIERARCHY th = FarmerBrothersEntitites.TECH_HIERARCHY.Where(a => a.DealerId == 909360).FirstOrDefault();
                                if (th != null)
                                {
                                    TechID = 909360; // If the 3rd party call type is 1300 installation, those events will be sent to Christina Ware – SP 909360(Email from Mike on July 15, 2020)
                                }
                            }
                        }

                        DispatchMail(workOrderModel.WorkorderID, TechID, true, new List<string>(), true, false);

                        WorkOrder wo = FarmerBrothersEntitites.WorkOrders.Where(w => w.WorkorderID == workOrderModel.WorkorderID).FirstOrDefault();
                        if(wo != null)
                        {
                            wo.IsAutoDispatched = true;
                        }

                        TECH_HIERARCHY techView = GetTechById(TechID);
                        if (TechID != 0)
                        {
                            NotesHistory notesHistory = new NotesHistory()
                            {
                                AutomaticNotes = 1,
                                EntryDate = currentTime,
                                Notes = "Auto Dispatch E-mail  Sent to " + techView.RimEmail + " " + techView.EmailCC,
                                Userid = UserId,
                                UserName = UserName,
                                WorkorderID = workOrderModel.WorkorderID,
                                isDispatchNotes = 1
                            };
                            FarmerBrothersEntitites.NotesHistories.Add(notesHistory);
                        }
                        else
                        {
                            NotesHistory notesHistory = new NotesHistory()
                            {
                                AutomaticNotes = 1,
                                EntryDate = currentTime,
                                Notes = "Auto Dispatch E-mail  Sent to " + ConfigurationManager.AppSettings["MikeEmailId"] + ";" + ConfigurationManager.AppSettings["DarrylEmailId"],
                                Userid = UserId,
                                UserName = UserName,
                                WorkorderID = workOrderModel.WorkorderID,
                                isDispatchNotes = 1
                            };
                            FarmerBrothersEntitites.NotesHistories.Add(notesHistory);
                        }
                    }


                    FarmerBrothersEntitites.SaveChanges();
                }
            }
            CustomerUserName = "";
        }

        public void StartAutoDispatchProcessForAutoGenEvents(WorkOrder workOrderModel)
        {
            #region properties initialization
            string SubmittedBy = workOrderModel.EntryUserName;
            FBEntities FarmerBrothersEntitites = new FBEntities();
            SqlHelper helper = new SqlHelper();
            DateTime currentTime = Utility.GetCurrentTime(workOrderModel.CustomerZipCode, FarmerBrothersEntitites);
            DataTable WorkOrderdt;
            DataTable rsAssetList;
            DataTable Contactdt;

            int TechID = -1;
            DataTable rsDealerEmail;
            int ContactId;

            #endregion

            int resultFlag = IsValidWorkOrderToStartAutoDispatch(workOrderModel);

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
                        TechID = getAvailableTechId(postCode, currentTime, ContactId);
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
                                TechID = getAvailableTechId(postCode, currentTime, ContactId);
                            }
                        }
                        else
                        {
                            string postCode = Convert.ToString(Contactdt.Rows[0]["PostalCode"]);
                            TechID = getAvailableTechId(postCode, currentTime, ContactId);

                        }
                    }
                }
                else if (resultFlag == 2)
                {
                    string postCode = Convert.ToString(Contactdt.Rows[0]["PostalCode"]);
                    TechID = getAvailableOnCallTechId(postCode, currentTime, workOrderModel.WorkorderID, ContactId);
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

                    DispatchMail(workOrderModel.WorkorderID, TechID, true, new List<string>(), true, false);

                    WorkOrder wo = FarmerBrothersEntitites.WorkOrders.Where(w => w.WorkorderID == workOrderModel.WorkorderID).FirstOrDefault();
                    if (wo != null)
                    {
                        wo.IsAutoDispatched = true;
                    }

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

        public int IsValidWorkOrderToStartAutoDispatch(WorkOrder workOrderModel)
        {
            int resultFlag = 0;
            bool success = false;
            SqlHelper helper = new SqlHelper();
            DateTime currentTime = Utility.GetCurrentTime(workOrderModel.CustomerZipCode, FarmerBrothersEntitites);
            //string customerSearchType = FarmerBrothersEntitites.Contacts.Where(c => c.ContactID == workOrderModel.CustomerID).Select(c => c.SearchType).FirstOrDefault();
            Contact customer = FarmerBrothersEntitites.Contacts.Where(c => c.ContactID == workOrderModel.CustomerID).FirstOrDefault();
            string customerSearchType = customer == null ? "" : customer.SearchType;
            string customerBranch = customer == null ? "" : customer.Branch;
            /*
             * Checking for Branch 311 base don the Email "FB Branch 311 no auto rim" from Connie
             */
            //if ((workOrderModel.IsSpecificTechnician == false || workOrderModel.IsSpecificTechnician == null) && !string.IsNullOrEmpty(customerSearchType) && customerSearchType.Trim() != "CCP" && customerSearchType.Trim() != "LEGACY" && customerBranch != "311")

            //Removed above condition, to autodispatch all events without any restriction : Mike's Email "here another example Mike/ branch stock and erf but the call was never assigned..."
            if ((workOrderModel.IsSpecificTechnician == false || workOrderModel.IsSpecificTechnician == null))
            {
                //if (workOrderModel.WorkorderCalltypeid == 1100 || workOrderModel.WorkorderCalltypeid == 1110 || workOrderModel.WorkorderCalltypeid == 1120 || workOrderModel.WorkorderCalltypeid == 1130 ||
                //    workOrderModel.WorkorderCalltypeid == 1200 || workOrderModel.WorkorderCalltypeid == 1210 || workOrderModel.WorkorderCalltypeid == 1220 || workOrderModel.WorkorderCalltypeid == 1230 ||
                //    workOrderModel.WorkorderCalltypeid == 1300 || workOrderModel.WorkorderCalltypeid == 1310 ||
                //    workOrderModel.WorkorderCalltypeid == 1400 || workOrderModel.WorkorderCalltypeid == 1410 ||
                //    workOrderModel.WorkorderCalltypeid == 1600 ||
                //    workOrderModel.WorkorderCalltypeid == 1700 || workOrderModel.WorkorderCalltypeid == 1710 ||
                //    workOrderModel.WorkorderCalltypeid == 1800 || workOrderModel.WorkorderCalltypeid == 1810 || workOrderModel.WorkorderCalltypeid == 1820 || workOrderModel.WorkorderCalltypeid == 1830 || workOrderModel.WorkorderCalltypeid == 1840 || workOrderModel.WorkorderCalltypeid == 1850 || workOrderModel.WorkorderCalltypeid == 1860 ||
                //    workOrderModel.WorkorderCalltypeid == 1900 || workOrderModel.WorkorderCalltypeid == 1910)
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
                            using (FBEntities FarmerBrothersEntitites = new FBEntities())
                            {
                                dateTime = Utility.GetCurrentTime(workOrderModel.CustomerZipCode, FarmerBrothersEntitites);

                            }
                            AvailableEmailStartTime = (dateTime.ToString("MM/dd/yyyy") + " " + ConfigurationManager.AppSettings["AutoDispatchAvailableEmailStartTime"]);
                            AvailableEmailEndTime = (dateTime.ToString("MM/dd/yyyy") + " " + ConfigurationManager.AppSettings["AutoDispatchAvailableEmailEndTime"]);

                            if (((customerSearchType.Trim() == "CBI") && (customerSearchType.Trim() == "PFS")))
                            {
                                AvailableEmailStartTime = (dateTime.ToString("MM/dd/yyyy") + " " + ConfigurationManager.AppSettings["CBIAndPFSAvailableEmailStartTime"]);
                                AvailableEmailEndTime = (dateTime.ToString("MM/dd/yyyy") + " " + ConfigurationManager.AppSettings["CBIAndPFSAvailableEmailEndTime"]);
                            }


                            //********************************************************************(Feb 18, 2021) New Logic to Dispatch all the time Irrespective of Holidays **********************************
                            if (Convert.ToDateTime(dateTime).DayOfWeek == DayOfWeek.Saturday || Convert.ToDateTime(dateTime).DayOfWeek == DayOfWeek.Sunday
                                || Utility.IsHoliday(dateTime))
                            {
                                resultFlag = 2;
                            }
                            //else if (((DateTime.Parse(AvailableEmailStartTime) <= DateTime.Parse(workOrderModel.WorkorderEntryDate.ToString()))
                            //       && (DateTime.Parse(workOrderModel.WorkorderEntryDate.ToString()) <= DateTime.Parse(AvailableEmailEndTime))))
                            else if (((DateTime.Parse(AvailableEmailStartTime) <= DateTime.Parse(dateTime.ToString()))
                                   && (DateTime.Parse(dateTime.ToString()) <= DateTime.Parse(AvailableEmailEndTime))))
                            {
                                resultFlag = 1;
                            }
                            else
                            {
                                string updateAutoEmailQuery = "Update WorkOrder Set AutoEmailSent=0,HighlightDispatchBoard=2 where WorkOrderID = " + workOrderModel.WorkorderID;
                                helper.UpdateCommand(updateAutoEmailQuery);
                                resultFlag = 2;
                            }
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
                            Userid = UserId,
                            UserName = UserName,
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

        public static bool IsTechUnAvailable(int techId, DateTime StartTime, out int replaceTech)
        {
            bool isAvilable = false;
            replaceTech = techId;
            using (FBEntities FarmerBrothersEntitites = new FBEntities())
            {
                List<TechSchedule> holidays = (from sc in FarmerBrothersEntitites.TechSchedules
                                               join tech in FarmerBrothersEntitites.TECH_HIERARCHY on sc.TechId equals tech.DealerId
                                               where DbFunctions.TruncateTime(sc.ScheduleDate) == DbFunctions.TruncateTime(StartTime) && sc.TechId == techId
                                               && tech.SearchType == "SP" && tech.PostalCode != null
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

        private int getAvailableTechId(string PostalCode, DateTime currentTime, int CustomrId)
        {
            int availableTechId = 0;
            int replaceTechId = 0;

            DataTable rsReferralList = null;
            FindAvailableDealers(PostalCode, false, out rsReferralList);

            Contact contct = FarmerBrothersEntitites.Contacts.Where(c => c.ContactID == CustomrId).FirstOrDefault();
            NonFBCustomer nonFbCust = null; bool? isApprovedThirdPartyDispatch = true;
            if (contct != null)
            {
                if (contct.PricingParentID != null)
                {
                    nonFbCust = FarmerBrothersEntitites.NonFBCustomers.Where(nfb => nfb.NonFBCustomerId == contct.PricingParentID).FirstOrDefault();
                    if (nonFbCust != null)
                    {
                        isApprovedThirdPartyDispatch = FarmerBrothersEntitites.PricingDetails.Where(p => p.PricingEntityId == nonFbCust.NonFBCustomerId).Select(s => s.Approved3rdPartyUse).FirstOrDefault();
                    }
                }
            }

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
                            if (nonFbCust != null && THV.FamilyAff == "SPT" && (isApprovedThirdPartyDispatch == null || isApprovedThirdPartyDispatch == false)) // Avoid dispatching to 3rd party(replacement tech) if the customer is NonFBCustomer
                            {
                                continue;                               
                            }

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
                                if (nonFbCust != null && THV.FamilyAff == "SPT" && (isApprovedThirdPartyDispatch == null || isApprovedThirdPartyDispatch == false)) // Avoid dispatching to 3rd party(replacement tech) if the customer is NonFBCustomer
                                {
                                    continue;
                                }

                                availableTechId = replaceTechId;
                                break;
                            }
                        }
                        else
                        {
                            TECH_HIERARCHY THV = FarmerBrothersEntitites.TECH_HIERARCHY.Where(x => x.SearchType == "SP" && x.DealerId == techId).FirstOrDefault();

                            if (THV != null)
                            {
                                if (nonFbCust != null && THV.FamilyAff == "SPT" && (isApprovedThirdPartyDispatch == null || isApprovedThirdPartyDispatch == false)) // Avoid dispatching to 3rd party(replacement tech) if the customer is NonFBCustomer
                                {
                                    continue;
                                }

                                availableTechId = techId;
                                break;
                            }
                        }

                    }
                }
            }

            return availableTechId;
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

        public bool GetPreference(string sPreferenceName, out string sPreferenceValue)
        {
            // Define necessary variables
            bool IsPreferenceExist = false;
            sPreferenceValue = string.Empty;
            string ReferralLatLongDegrees = ConfigurationManager.AppSettings[sPreferenceName];
            if (String.IsNullOrEmpty(ReferralLatLongDegrees))
            {
                using (FBEntities entity = new FBEntities())
                {
                    FBActivityLog log = new FBActivityLog();
                    log.LogDate = DateTime.UtcNow;
                    log.UserId = UserId;
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

        private int getAvailableOnCallTechId(string PostalCode, DateTime currentTime, int WorkorderId, int CustomrId)
        {
            int availableTechId = -1;
            int replaceTechId = 0;

            // DataTable rsReferralList =
            SqlHelper helper = new SqlHelper();
            DataTable rsReferralList = helper.GetAfterHoursOnCallTechDetails(PostalCode, WorkorderId);

            Contact contct = FarmerBrothersEntitites.Contacts.Where(c => c.ContactID == CustomrId).FirstOrDefault();
            NonFBCustomer nonFbCust = null; bool? isApprovedThirdPartyDispatch = true;
            if (contct != null)
            {
                if (contct.PricingParentID != null)
                {
                    nonFbCust = FarmerBrothersEntitites.NonFBCustomers.Where(nfb => nfb.NonFBCustomerId == contct.PricingParentID).FirstOrDefault();
                    if (nonFbCust != null)
                    {
                        isApprovedThirdPartyDispatch = FarmerBrothersEntitites.PricingDetails.Where(p => p.PricingEntityId == nonFbCust.NonFBCustomerId).Select(s => s.Approved3rdPartyUse).FirstOrDefault();
                    }
                }
            }

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
                            if (nonFbCust != null && THV.FamilyAff == "SPT" && (isApprovedThirdPartyDispatch == null || isApprovedThirdPartyDispatch == false)) // Avoid dispatching to 3rd party(replacement tech) if the customer is NonFBCustomer
                            {
                                continue;
                            }
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
                                if (nonFbCust != null && THV.FamilyAff == "SPT" && (isApprovedThirdPartyDispatch == null || isApprovedThirdPartyDispatch == false)) // Avoid dispatching to 3rd party(replacement tech) if the customer is NonFBCustomer
                                {
                                    continue;
                                }
                                availableTechId = replaceTechId;
                                break;
                            }
                        }
                        else
                        {
                            TECH_HIERARCHY THV = FarmerBrothersEntitites.TECH_HIERARCHY.Where(x => x.SearchType == "SP" && x.DealerId == techId).FirstOrDefault();

                            if (THV != null)
                            {
                                if (nonFbCust != null && THV.FamilyAff == "SPT" && (isApprovedThirdPartyDispatch == null || isApprovedThirdPartyDispatch == false)) // Avoid dispatching to 3rd party(replacement tech) if the customer is NonFBCustomer
                                {
                                    continue;
                                }
                                availableTechId = techId;
                                break;
                            }
                        }

                    }
                }
            }

            return availableTechId;
        }

        protected TECH_HIERARCHY GetTechById(int? techId)
        {
            TECH_HIERARCHY techView = FarmerBrothersEntitites.TECH_HIERARCHY.Where(x => x.DealerId == techId).FirstOrDefault();
            return techView;
        }

        protected Contact GetCustomerById(int? customerId)
        {
            Contact customer = FarmerBrothersEntitites.Contacts.Where(x => x.ContactID == customerId).FirstOrDefault();
            return customer;
        }

        public JsonResult DispatchMail(int workOrderId, int techId, bool isResponsible, List<String> notes, bool IsAutoDispatched, bool isFromAutoDispatch = true)
        {
            int returnValue = -1;
            TechHierarchyView techHierarchyView = Utility.GetTechDataByResponsibleTechId(FarmerBrothersEntitites, techId);
            StringBuilder salesEmailBody = new StringBuilder();
            WorkOrder workOrder = FarmerBrothersEntitites.WorkOrders.Where(w => w.WorkorderID == workOrderId).FirstOrDefault();
            string workOrderStatus = "";
            string addtionalNotes = string.Empty;

            DateTime currentTime = techHierarchyView == null ? DateTime.Now : Utility.GetCurrentTime(techHierarchyView.TechZip, FarmerBrothersEntitites);

            string message = string.Empty;
            string redirectUrl = string.Empty;

            if (workOrder != null)
            {
                if (//string.Compare(workOrder.WorkorderCallstatus, "Closed", true) != 0
                  string.Compare(workOrder.WorkorderCallstatus, "Invoiced", true) != 0
                //&& string.Compare(workOrder.WorkorderCallstatus, "Completed", true) != 0
                && string.Compare(workOrder.WorkorderCallstatus, "Attempting", true) != 0)
                {
                    if (isResponsible == true)
                    {
                        UpdateTechAssignedStatus(techId, workOrder, "Sent", 0, -1);
                    }
                    else
                    {
                        UpdateTechAssignedStatus(techId, workOrder, "Sent", -1, 0);
                    }

                    StringBuilder subject = new StringBuilder();
                    AllFBStatu priority = FarmerBrothersEntitites.AllFBStatus.Where(p => p.FBStatusID == workOrder.PriorityCode).First();

                    if (priority.FBStatus.Contains("critical"))
                    //if (workOrder.PriorityCode == 1 || workOrder.PriorityCode == 2 || workOrder.PriorityCode == 3 || workOrder.PriorityCode == 4)
                    {
                        subject.Append("CRITICAL WO: ");
                    }
                    else
                    {
                        subject.Append("WO: ");
                    }

                    subject.Append(workOrder.WorkorderID);
                    subject.Append(" Customer: ");
                    subject.Append(workOrder.CustomerName);
                    subject.Append(" ST: ");
                    subject.Append(workOrder.CustomerState);
                    subject.Append(" Call Type: ");
                    subject.Append(workOrder.WorkorderCalltypeDesc);

                    string emailAddress = string.Empty;
                    string salesEmailAddress = string.Empty;
                    string esmEmailAddress = string.Empty;
                    int userId = UserId;
                    TECH_HIERARCHY techView = GetTechById(techId);

                    Contact customer = FarmerBrothersEntitites.Contacts.Where(cont => cont.ContactID == workOrder.CustomerID).FirstOrDefault();

                    if (techId == 0)
                    {
                        emailAddress = ConfigurationManager.AppSettings["MikeEmailId"] + ";" + ConfigurationManager.AppSettings["DarrylEmailId"];
                    }
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

                    if (customer != null)
                    {
                        if (!string.IsNullOrEmpty(customer.SalesEmail))
                        {
                            salesEmailAddress = customer.SalesEmail;
                        }
                    }
                    ESMCCMRSMEscalation esmEscalation = FarmerBrothersEntitites.ESMCCMRSMEscalations.Where(e => e.ZIPCode == workOrder.CustomerZipCode).FirstOrDefault();
                    if(esmEscalation != null && !string.IsNullOrEmpty(esmEscalation.ESMEmail))
                    {
                        esmEmailAddress = esmEscalation.ESMEmail;
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
                                    Userid = UserId,
                                    UserName = UserName,
                                    WorkorderID = workOrder.WorkorderID,
                                    isDispatchNotes = 1
                                };
                                addtionalNotes = trNotes;
                                FarmerBrothersEntitites.NotesHistories.Add(notesHistory);
                            }

                        }

                        bool result = SendWorkOrderMail(workOrder, subject.ToString(), emailAddress, ConfigurationManager.AppSettings["DispatchMailFromAddress"], techId, MailType.DISPATCH, isResponsible, addtionalNotes, "TRANSMIT", false, salesEmailAddress, esmEmailAddress);
                        if (result == true)
                        {
                            if (techHierarchyView != null)
                            {
                                workOrder.ResponsibleTechid = techHierarchyView.TechID;
                                workOrder.ResponsibleTechName = techHierarchyView.PreferredProvider;
                            }

                            workOrder.WorkorderModifiedDate = currentTime;
                            workOrder.ModifiedUserName = UserName;
                        }
                        returnValue = FarmerBrothersEntitites.SaveChanges();

                    }

                    workOrderStatus = workOrder.WorkorderCallstatus;

                    if (IsAutoDispatched == true)
                    {
                        AgentDispatchLog autodispatchLog = new AgentDispatchLog()
                        {
                            TDate = currentTime,
                            UserID = UserId,
                            UserName = UserName,
                            WorkorderID = workOrder.WorkorderID
                        };
                        FarmerBrothersEntitites.AgentDispatchLogs.Add(autodispatchLog);
                        FarmerBrothersEntitites.SaveChanges();
                    }
                }
            }

            JsonResult jsonResult = new JsonResult();
            jsonResult.Data = new { success = true, serverError = ErrorCode.SUCCESS, returnValue = returnValue > 0 ? 1 : 0, WorkorderCallstatus = workOrderStatus, Url = redirectUrl, message = message };
            jsonResult.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            return jsonResult;
        }

        public bool UpdateTechAssignedStatus(int techId, WorkOrder workOrder, string assignedStatus, int isResponsible = -1, int isAssist = -1)
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
                        techWorkOrderSchedule.ScheduleUserid = UserId;
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
                        Userid = UserId,
                        UserName = UserName,
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
                    techWorkOrderSchedule.ScheduleUserid = UserId;
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
                        TeamLeadName = ConfigurationManager.AppSettings["ManagerName"],

                        PrimaryTech = Convert.ToInt16(isResponsible),
                        AssistTech = -1,
                        AssignedStatus = assignedStatus,
                        ModifiedScheduleDate = currentTime,
                        ScheduleUserid = UserId
                    };



                    workOrder.WorkorderSchedules.Add(newworkOrderSchedule);

                }

                bool redirected = false;
                string oldTechName = string.Empty;
                IEnumerable<WorkorderSchedule> primaryTechSchedules = workOrder.WorkorderSchedules.Where(ws => ws.PrimaryTech >= 0);
                foreach (WorkorderSchedule workOrderSchedule in primaryTechSchedules)
                {
                    if ((string.Compare(workOrderSchedule.AssignedStatus, "Sent", true) == 0
                        || string.Compare(workOrderSchedule.AssignedStatus, "Accepted", true) == 0
                        || string.Compare(workOrderSchedule.AssignedStatus, "Scheduled", true) == 0)
                        && workOrderSchedule.Techid != techId)
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
                            subject.Append(" Customer: ");
                            subject.Append(workOrder.CustomerName);
                            subject.Append(" ST: ");
                            subject.Append(workOrder.CustomerState);
                            subject.Append(" Call Type: ");
                            subject.Append(workOrder.WorkorderCalltypeDesc);

                            string emailAddress = string.Empty;
                            string salesEmailAddress = string.Empty;
                            string esmEmailAddress = string.Empty;

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

                            Contact customer = FarmerBrothersEntitites.Contacts.Where(cont => cont.ContactID == workOrder.CustomerID).FirstOrDefault();
                            if (!string.IsNullOrEmpty(customer.SalesEmail))
                            {
                                salesEmailAddress = customer.SalesEmail;
                            }
                            ESMCCMRSMEscalation esmEscalation = FarmerBrothersEntitites.ESMCCMRSMEscalations.Where(e => e.ZIPCode == workOrder.CustomerZipCode).FirstOrDefault();
                            if (esmEscalation != null && !string.IsNullOrEmpty(esmEscalation.ESMEmail))
                            {
                                esmEmailAddress = esmEscalation.ESMEmail;
                            }


                            if (!string.IsNullOrWhiteSpace(emailAddress))
                            {
                                SendWorkOrderMail(workOrder, subject.ToString(), emailAddress, ConfigurationManager.AppSettings["DispatchMailFromAddress"], workOrderSchedule.Techid, MailType.REDIRECTED, false, "This Work Order has been redirected!", string.Empty, false, salesEmailAddress, esmEmailAddress);
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
                    Userid = UserId,
                    UserName = UserName,
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
                        TeamLeadName = ConfigurationManager.AppSettings["ManagerName"],

                        AssistTech = Convert.ToInt16(isAssist),
                        PrimaryTech = -1,
                        AssignedStatus = assignedStatus,
                        ModifiedScheduleDate = currentTime,
                        ScheduleUserid = UserId
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
                    Userid = UserId,
                    UserName = UserName,
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

        public static int GetCallsTotalCount(FBEntities FBE, string CustomerID)
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

        public static string GetServiceLevelDesc(FBEntities FBE, string BillingCode)
        {
            string Description = "";
            FBBillableFeed fbFeed = FBE.FBBillableFeeds.Where(b => b.Code == BillingCode).FirstOrDefault();
            if (fbFeed != null)
                Description = fbFeed.Description;

            return BillingCode + "  -  " + Description;
        }

        public bool SendWorkOrderMail_New_Bkp(WorkOrder workOrder, string subject, string toAddress, string fromAddress, int? techId, MailType mailType, bool isResponsible, string additionalMessage, string mailFrom = "", bool isFromEmailCloserLink = false, string SalesEmailAddress = "", string esmEmailAddress = "")
        {
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

            StringBuilder salesEmailBody = new StringBuilder();

            salesEmailBody.Append(@"<img src='cid:logo' width='80' height='100'>");

            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("<BR>");
            string url = ConfigurationManager.AppSettings["DispatchResponseUrl"];
            string Redircturl = ConfigurationManager.AppSettings["RedirectResponseUrl"];
            string Closureurl = ConfigurationManager.AppSettings["CallClosureUrl"];
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
                    if (history.Notes.ToLower().Contains("redirected") || history.Notes.ToLower().Contains("rejected") || history.Notes.ToLower().Contains("declined"))
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
                        /*salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=6&isResponsible=" + isResponsible.ToString())) + "\">START</a>");
                        salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
                        salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=2&isResponsible=" + isResponsible.ToString())) + "\">ARRIVAL</a>");
                        salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
                        salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=3&isResponsible=" + isResponsible.ToString())) + "\">COMPLETED</a>");
                        salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
                        salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=7&isResponsible=" + isResponsible.ToString() + "&isBillable=" + (IsBillable == "True" ? "True" : "False"))) + "\">CLOSE WORK ORDER</a>");
                        salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
                        salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=8&isResponsible=" + isResponsible.ToString())) + "\">SCHEDULE EVENT</a>");
                        salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");*/
                        // }
                    }
                }
                else if (mailType == MailType.REDIRECTED)
                {
                    //salesEmailBody.Append("<a href=\"" + url + "?workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=4&isResponsible=" + isResponsible + "\">DISREGARD</a>");
                    salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=4&isResponsible=" + isResponsible.ToString())) + "\">DISREGARD</a>");
                }
            }


            string contentId = Guid.NewGuid().ToString();
            string logoPath = System.AppDomain.CurrentDomain.BaseDirectory.ToString() + "img\\mainlogo.jpg";


            //if (Server == null)
            //{
            //    logoPath = System.IO.Path.Combine(HttpRuntime.AppDomainAppPath, "img/mainlogo.jpg");
            //}
            //else
            //{
            //    logoPath = System.IO.Path("~/img/mainlogo.jpg");
            //}


            //var basePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);            
            //var basePath = Environment.CurrentDirectory.Split(new String[] { "bin" }, StringSplitOptions.None)[0];

            //logoPath = basePath + "img\\mainlogo.jpg";

            salesEmailBody = salesEmailBody.Replace("cid:logo", "cid:" + contentId);

            AlternateView avHtml = AlternateView.CreateAlternateViewFromString
               (salesEmailBody.ToString(), null, MediaTypeNames.Text.Html);

            LinkedResource inline = new LinkedResource(logoPath, MediaTypeNames.Image.Jpeg);
            inline.ContentId = contentId;
            avHtml.LinkedResources.Add(inline);

            var message = new MailMessage();

            message.AlternateViews.Add(avHtml);

            //message.IsBodyHtml = true;
            //message.Body = salesEmailBody.Replace("cid:logo", "cid:" + inline.ContentId).ToString();

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
                            if (address.ToLower().Contains("@jmsmucker.com")) continue;
                            if (!string.IsNullOrWhiteSpace(address))
                            {
                                message.CC.Add(new MailAddress(address));
                            }
                        }
                        string[] addresses = mailCCAddress[0].Split(';');
                        foreach (string address in addresses)
                        {
                            if (address.ToLower().Contains("@jmsmucker.com")) continue;
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
                            if (address.ToLower().Contains("@jmsmucker.com")) continue;

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


                /*if (!string.IsNullOrEmpty(esmEmailAddress) && !Convert.ToBoolean(ConfigurationManager.AppSettings["UseTestMails"]))
                {
                    if (esmEmailAddress.Contains(";"))
                    {
                        string[] addresses = esmEmailAddress.Split(';');
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
                        message.CC.Add(esmEmailAddress);
                    }
                }*/

                //string IsNonFBCustomerParentId = ConfigurationManager.AppSettings["NonFBCustomerParentID"];
                //if (customer.PricingParentID == IsNonFBCustomerParentId)
                //{
                //    message.CC.Clear();
                //}
                NonFBCustomer nonFBCustomer = FarmerBrothersEntitites.NonFBCustomers.Where(n => n.NonFBCustomerId == customer.PricingParentID).FirstOrDefault();
                if (nonFBCustomer != null)
                {
                    message.CC.Clear();
                }

                message.From = new MailAddress(fromAddress);
                message.Subject = subject;
                message.IsBodyHtml = true;

                if (tchView != null && tchView.FamilyAff != "SP")
                {
                    message.Priority = MailPriority.High;
                }

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

        public bool SendWorkOrderMail_old(WorkOrder workOrder, string subject, string toAddress, string fromAddress, int? techId, MailType mailType, bool isResponsible, string additionalMessage, string mailFrom = "", bool isFromEmailCloserLink = false, string SalesEmailAddress = "", string esmEmailAddress = "")
        {
            Contact customer = FarmerBrothersEntitites.Contacts.Where(c => c.ContactID == workOrder.CustomerID).FirstOrDefault();
            int TotalCallsCount = GetCallsTotalCount(FarmerBrothersEntitites, workOrder.CustomerID.ToString());

            //List<CustomerNotesModel> CustomerNotesResults = new List<CustomerNotesModel>();
            int? custId = Convert.ToInt32(workOrder.CustomerID);
            var custNotes = FarmerBrothersEntitites.FBCustomerNotes.Where(c => c.CustomerId == custId && c.IsActive == true).ToList();
            
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

                        salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=9&isResponsible=" + isResponsible.ToString())) + "\">ESM ESCALATION</a>");
                        salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");

                        if (mailType == MailType.DISPATCH)
                        {
                            salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=0&isResponsible=" + isResponsible.ToString())) + "\">ACCEPT</a>");
                            salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
                        }
                        if (workOrder.WorkorderCallstatus == "Pending Acceptance" && techView.FamilyAff != "SPT")
                        {
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
                    if (history.Notes.ToLower().Contains("redirected") || history.Notes.ToLower().Contains("rejected") || history.Notes.ToLower().Contains("declined"))
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
                            salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=0&isResponsible=" + isResponsible.ToString())) + "\">ACCEPT</a>");
                            salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
                        }
                        if (workOrder.WorkorderCallstatus == "Pending Acceptance" && techView.FamilyAff != "SPT")
                        {                         
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
                        // }
                    }
                }
                else if (mailType == MailType.REDIRECTED)
                {
                    //salesEmailBody.Append("<a href=\"" + url + "?workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=4&isResponsible=" + isResponsible + "\">DISREGARD</a>");
                    salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", url, new Encrypt_Decrypt().Encrypt("workOrderId=" + workOrder.WorkorderID + "&techId=" + techId.Value + "&response=4&isResponsible=" + isResponsible.ToString())) + "\">DISREGARD</a>");
                }
            }


            string contentId = Guid.NewGuid().ToString();
            string logoPath = System.AppDomain.CurrentDomain.BaseDirectory.ToString() + "img\\mainlogo.jpg";


            //if (Server == null)
            //{
            //    logoPath = System.IO.Path.Combine(HttpRuntime.AppDomainAppPath, "img/mainlogo.jpg");
            //}
            //else
            //{
            //    logoPath = System.IO.Path("~/img/mainlogo.jpg");
            //}


            //var basePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);            
            //var basePath = Environment.CurrentDirectory.Split(new String[] { "bin" }, StringSplitOptions.None)[0];

            //logoPath = basePath + "img\\mainlogo.jpg";

            salesEmailBody = salesEmailBody.Replace("cid:logo", "cid:" + contentId);

            AlternateView avHtml = AlternateView.CreateAlternateViewFromString
               (salesEmailBody.ToString(), null, MediaTypeNames.Text.Html);

            LinkedResource inline = new LinkedResource(logoPath, MediaTypeNames.Image.Jpeg);
            inline.ContentId = contentId;
            avHtml.LinkedResources.Add(inline);

            var message = new MailMessage();

            message.AlternateViews.Add(avHtml);

            //message.IsBodyHtml = true;
            //message.Body = salesEmailBody.Replace("cid:logo", "cid:" + inline.ContentId).ToString();

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
                            if (address.ToLower().Contains("@jmsmucker.com")) continue;
                            if (!string.IsNullOrWhiteSpace(address))
                            {
                                message.CC.Add(new MailAddress(address));
                            }
                        }
                        string[] addresses = mailCCAddress[0].Split(';');
                        foreach (string address in addresses)
                        {
                            if (address.ToLower().Contains("@jmsmucker.com")) continue;
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
                            if (address.ToLower().Contains("@jmsmucker.com")) continue;

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


                /*if (!string.IsNullOrEmpty(esmEmailAddress) && !Convert.ToBoolean(ConfigurationManager.AppSettings["UseTestMails"]))
                {
                    if (esmEmailAddress.Contains(";"))
                    {
                        string[] addresses = esmEmailAddress.Split(';');
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
                        message.CC.Add(esmEmailAddress);
                    }
                }*/

                //string IsNonFBCustomerParentId = ConfigurationManager.AppSettings["NonFBCustomerParentID"];
                //if (customer.PricingParentID == IsNonFBCustomerParentId)
                //{
                //    message.CC.Clear();
                //}
                NonFBCustomer nonFBCustomer = FarmerBrothersEntitites.NonFBCustomers.Where(n => n.NonFBCustomerId == customer.PricingParentID).FirstOrDefault();
                if (nonFBCustomer != null)
                {
                    message.CC.Clear();
                }

                message.From = new MailAddress(fromAddress);
                message.ReplyToList.Add(new MailAddress(ConfigurationManager.AppSettings["DispatchMailReplyToAddress"], "ReviveService"));
                message.Subject = subject;
                message.IsBodyHtml = true;

                if (tchView != null && tchView.FamilyAff != "SP")
                {
                    message.Priority = MailPriority.High;
                }

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

        public bool SendWorkOrderMail(WorkOrder workOrder, string subject, string toAddress, string fromAddress, int? techId, MailType mailType, bool isResponsible, string additionalMessage, string mailFrom = "", bool isFromEmailCloserLink = false, string SalesEmailAddress = "", string esmEmailAddress = "")
        {
            Contact customer = FarmerBrothersEntitites.Contacts.Where(c => c.ContactID == workOrder.CustomerID).FirstOrDefault();
            int TotalCallsCount = GetCallsTotalCount(FarmerBrothersEntitites, workOrder.CustomerID.ToString());

            //List<CustomerNotesModel> CustomerNotesResults = new List<CustomerNotesModel>();
            int? custId = Convert.ToInt32(workOrder.CustomerID);
            var custNotes = FarmerBrothersEntitites.FBCustomerNotes.Where(c => c.CustomerId == custId && c.IsActive == true).ToList();


            //Removed Temporarily on Mike's say
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

            NonFBCustomer nonfbcust = FarmerBrothersEntitites.NonFBCustomers.Where(c => c.NonFBCustomerId == customer.PricingParentID).FirstOrDefault();
            if (nonfbcust != null)
            {
                if (!string.IsNullOrWhiteSpace(nonfbcust.email))
                {
                    ccMailAddress += nonfbcust.email + ";";
                }
            }

            /*
            //Included as per Email "Hardcode to Revive Parent #'s" received on Feb 24th, 2024
            if (customer.PricingParentID == "9001228")
            {
                ccMailAddress += "cfrancis@reviveservice.com";
            }
            if (customer.PricingParentID == "9001239")
            {
                ccMailAddress += "cfrancis@reviveservice.com";
            }*/
            //Included as per Email "Conserv Fuel_ Service Calls" received on Oct 28, 2024
            if (customer.PricingParentID == "9341102")
            {
                ccMailAddress += "mgalicia@farmerbros.com";
            }
            if (customer.PricingParentID == "7380755")
            {
                ccMailAddress += "tim.brigham@ellisnv.com";
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


            //string BccEmailAddress = fromAddress;
            //ESMCCMRSMEscalation esmEscalation = FarmerBrothersEntitites.ESMCCMRSMEscalations.Where(e => e.ZIPCode == workOrder.CustomerZipCode).FirstOrDefault();
            //if (esmEscalation != null)
            //{
            //    fromAddress = esmEscalation.ESMEmail != null ? esmEscalation.ESMEmail : BccEmailAddress;
            //}
            //else
            //{
            //    fromAddress = BccEmailAddress;
            //}

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

            string contactName = string.IsNullOrEmpty(workOrder.WorkorderContactName) ? (string.IsNullOrEmpty(workOrder.CustomerMainContactName) ? "" : workOrder.CustomerMainContactName) : workOrder.WorkorderContactName;
            string contactPhone = string.IsNullOrEmpty(workOrder.WorkorderContactPhone) ? (string.IsNullOrEmpty(workOrder.CustomerPhone) ? "" : workOrder.CustomerPhone) : workOrder.WorkorderContactPhone;

            salesEmailBody.Append("Contact Name: ");
            salesEmailBody.Append(contactName);
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("PHONE: ");
            salesEmailBody.Append(contactPhone);
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

            string contactName = string.IsNullOrEmpty(workOrder.WorkorderContactName) ? (string.IsNullOrEmpty(workOrder.CustomerMainContactName) ? "" : workOrder.CustomerMainContactName) : workOrder.WorkorderContactName;
            string contactPhone = string.IsNullOrEmpty(workOrder.WorkorderContactPhone) ? (string.IsNullOrEmpty(workOrder.CustomerPhone) ? "" : workOrder.CustomerPhone) : workOrder.WorkorderContactPhone;

            salesEmailBody.Append("Contact Name: ");
            salesEmailBody.Append(contactName);
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("PHONE: ");
            salesEmailBody.Append(contactPhone);
            salesEmailBody.Append("<BR>");

            salesEmailBody.Append("Contact: ");
            salesEmailBody.Append(workOrder.CustomerPO);

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

                EmailUtility eu = new EmailUtility();
                eu.SendEmail(fromAddress, ToAddr, CcAddr, subject, salesEmailBody.ToString());

                //#region Comment out this sectiona nd uncomment the above when Needed

                //string contentId = Guid.NewGuid().ToString();
                //string logoPath = System.AppDomain.CurrentDomain.BaseDirectory.ToString() + "img\\mainlogo.jpg";



                //salesEmailBody = salesEmailBody.Replace("cid:logo", "cid:" + contentId);

                //AlternateView avHtml = AlternateView.CreateAlternateViewFromString
                //   (salesEmailBody.ToString(), null, MediaTypeNames.Text.Html);

                //LinkedResource inline = new LinkedResource(logoPath, MediaTypeNames.Image.Jpeg);
                //inline.ContentId = contentId;
                //avHtml.LinkedResources.Add(inline);

                //var message = new MailMessage();

                //message.AlternateViews.Add(avHtml);


                //message.Body = salesEmailBody.ToString();

                //if (tchView != null && tchView.FamilyAff != "SP")
                //{
                //    message.Priority = MailPriority.High;
                //}

                //string mailTo = ToAddr;
                //string mailCC = string.Empty;
                //if (!string.IsNullOrWhiteSpace(mailTo))
                //{
                //    if (mailTo.Contains("#"))
                //    {
                //        string[] mailCCAddress = mailTo.Split('#');
                //        if (mailCCAddress.Count() > 0)
                //        {
                //            string[] addresses = mailCCAddress[0].Split(';');
                //            foreach (string address in addresses)
                //            {
                //                if (!string.IsNullOrWhiteSpace(address))
                //                {
                //                    message.To.Add(new MailAddress(address));
                //                }
                //            }
                //        }
                //    }
                //    else
                //    {
                //        string[] addresses = mailTo.Split(';');
                //        foreach (string address in addresses)
                //        {
                //            if (!string.IsNullOrWhiteSpace(address))
                //            {
                //                message.To.Add(new MailAddress(address));
                //            }
                //        }
                //    }

                //    message.From = new MailAddress(fromAddress);
                //    message.Subject = subject.ToString();
                //    message.IsBodyHtml = true;
                //    System.Net.ServicePointManager.SecurityProtocol = (System.Net.SecurityProtocolType)3072;
                //    System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
                //    using (var smtp = new SmtpClient())
                //    {
                //        smtp.Host = ConfigurationManager.AppSettings["MailServer"];
                //        smtp.Port = 25;

                //        try
                //        {
                //            smtp.Send(message);
                //        }
                //        catch (Exception ex)
                //        {
                //            result = false;
                //        }
                //    }

                //}
                //#endregion
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
            if(!string.IsNullOrEmpty(ccMailAddress))
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



                //#region Comment out this sectiona nd uncomment the above when Needed
                //string contentId = Guid.NewGuid().ToString();
                //string logoPath = System.AppDomain.CurrentDomain.BaseDirectory.ToString() + "img\\mainlogo.jpg";



                //salesEmailBody = salesEmailBody.Replace("cid:logo", "cid:" + contentId);

                //AlternateView avHtml = AlternateView.CreateAlternateViewFromString
                //   (salesEmailBody.ToString(), null, MediaTypeNames.Text.Html);

                //LinkedResource inline = new LinkedResource(logoPath, MediaTypeNames.Image.Jpeg);
                //inline.ContentId = contentId;
                //avHtml.LinkedResources.Add(inline);

                //var message = new MailMessage();

                //message.AlternateViews.Add(avHtml);
                //message.Body = salesEmailBody.ToString();

                //if (tchView != null && tchView.FamilyAff != "SP")
                //{
                //    message.Priority = MailPriority.High;
                //}


                //string mailCC = CcAddr;
                //if (!string.IsNullOrWhiteSpace(mailCC))
                //{
                //    if (mailCC.Contains("#"))
                //    {
                //        string[] mailCCAddress = mailCC.Split('#');
                //        if (mailCCAddress.Count() > 0)
                //        {
                //            string[] addresses = mailCCAddress[1].Split(';');
                //            foreach (string address in addresses)
                //            {
                //                if (!string.IsNullOrWhiteSpace(address))
                //                {
                //                    message.CC.Add(new MailAddress(address));
                //                }
                //            }
                //        }
                //    }
                //    else
                //    {
                //        string[] addresses = mailCC.Split(';');
                //        foreach (string address in addresses)
                //        {
                //            if (!string.IsNullOrWhiteSpace(address))
                //            {
                //                message.CC.Add(new MailAddress(address));
                //            }
                //        }
                //    }


                //    message.From = new MailAddress(fromAddress);
                //    message.Subject = subject.ToString();
                //    message.IsBodyHtml = true;

                //    using (var smtp = new SmtpClient())
                //    {
                //        smtp.Host = ConfigurationManager.AppSettings["MailServer"];
                //        smtp.Port = 25;

                //        try
                //        {
                //            smtp.Send(message);
                //        }
                //        catch (Exception ex)
                //        {
                //            result = false;
                //        }
                //    }

                //}
                //#endregion
            }
            return result;
        }

        public bool SendPartsOrderMail(int WorkorderId)
        {
            bool result = true;
            StringBuilder salesEmailBody = new StringBuilder();
            StringBuilder subject = new StringBuilder();

            WorkOrder workOrder = FarmerBrothersEntitites.WorkOrders.Where(w => w.WorkorderID == WorkorderId).FirstOrDefault();
            Contact contact = FarmerBrothersEntitites.Contacts.Where(c => c.ContactID == workOrder.CustomerID).FirstOrDefault();

            subject.Append("PARTS ORDER - WO: ");
            subject.Append(workOrder.WorkorderID);
            subject.Append(" Customer: ");
            subject.Append(workOrder.CustomerName);
            subject.Append(" ST: ");
            subject.Append(workOrder.CustomerState);
            subject.Append(" Call Type: ");
            subject.Append(workOrder.WorkorderCalltypeDesc);


            salesEmailBody.Append(@"<img src='cid:logo' width='80' height='100' style='margin-right: 100px;margin-bottom: 10px;'>");

            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("<BR>");

            salesEmailBody.Append("CALL TIME: ");
            salesEmailBody.Append(workOrder.WorkorderEntryDate);
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("Work Order ID#: ");
            salesEmailBody.Append(workOrder.WorkorderID);
            salesEmailBody.Append("<BR>");

            salesEmailBody.Append("<span style='color:#ff0000'><b>");
            salesEmailBody.Append("Date Needed: ");
            salesEmailBody.Append(workOrder.DateNeeded);
            salesEmailBody.Append("</b></span>");
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("Customer PO: ");
            salesEmailBody.Append(workOrder.CustomerPO);
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("Customer Email: ");
            salesEmailBody.Append(workOrder.CustomerMainEmail);
            salesEmailBody.Append("<BR>");

            string shippingPriority = "";
            if (!string.IsNullOrEmpty(workOrder.ShippingPriority))
            {
                int statusid = Convert.ToInt32(workOrder.ShippingPriority);
                AllFBStatu fbstatus = FarmerBrothersEntitites.AllFBStatus.Where(f => f.FBStatusID == statusid).FirstOrDefault();
                if (fbstatus != null)
                {
                    shippingPriority = fbstatus.FBStatus;
                }
            }
            salesEmailBody.Append("Shipping Priority: ");
            salesEmailBody.Append(shippingPriority);
            salesEmailBody.Append("<BR>");

            string username = "", useremail = "";
            FbUserMaster user = FarmerBrothersEntitites.FbUserMasters.Where(u => (u.FirstName + " " + u.LastName).Equals(workOrder.EntryUserName)).FirstOrDefault();
            if(user != null)
            {
                username = (!string.IsNullOrEmpty(user.FirstName) ? user.FirstName : "") + " " + (!string.IsNullOrEmpty(user.LastName) ? user.LastName : "");
                useremail = !string.IsNullOrEmpty(user.EmailId) ? user.EmailId : "";
            }

            salesEmailBody.Append("Requested By: ");
            salesEmailBody.Append(username);
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("Requested By Email: ");
            salesEmailBody.Append(useremail);
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("<BR>");

            salesEmailBody.Append("<span style='color:#ff0000'><b>");
            salesEmailBody.Append("CUSTOMER INFORMATION: ");
            salesEmailBody.Append("</b></span>");
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("Route: ");
            salesEmailBody.Append(contact.Route);
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("Customer #: ");
            salesEmailBody.Append(contact.ContactID);
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append(contact.Address1);
            salesEmailBody.Append("<BR>");
            if (!string.IsNullOrEmpty(contact.Address2))
            {
                salesEmailBody.Append(contact.Address2);
                salesEmailBody.Append("<BR>");
            }
            salesEmailBody.Append(contact.City);
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append(contact.State);
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append(contact.PostalCode);
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("PHONE: ");
            salesEmailBody.Append(contact.Phone);
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("<BR>");

            salesEmailBody.Append("<span style='color:#ff0000'><b>");
            salesEmailBody.Append("SHIP TO LOCATION: ");
            salesEmailBody.Append("</b></span>");
            salesEmailBody.Append(workOrder.OtherPartsContactName);
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append(workOrder.OtherPartsAddress1);
            salesEmailBody.Append("<BR>");
            if (!string.IsNullOrEmpty(workOrder.OtherPartsAddress2))
            {
                salesEmailBody.Append(workOrder.OtherPartsAddress2);
                salesEmailBody.Append("<BR>");
            }
            salesEmailBody.Append(workOrder.OtherPartsCity);
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append(workOrder.OtherPartsState);
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append(workOrder.OtherPartsZip);
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("PHONE: ");
            salesEmailBody.Append(workOrder.OtherPartsPhone);
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("<BR>");


            salesEmailBody.Append("NOTES: ");
            salesEmailBody.Append("<BR>");
            IEnumerable<NotesHistory> histories = workOrder.NotesHistories.Where(n => n.AutomaticNotes == 0).OrderByDescending(n => n.EntryDate);

            foreach (NotesHistory history in histories)
            {
                salesEmailBody.Append(history.UserName);
                salesEmailBody.Append(" ");
                salesEmailBody.Append(history.EntryDate);
                salesEmailBody.Append(" ");
                salesEmailBody.Append(history.Notes.Replace("\\n", " ").Replace("\\t", " ").Replace("\\r", " ").Replace("\n", " ").Replace("\t", " ").Replace("\r", " "));
                salesEmailBody.Append("<BR>");
            }

            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("<BR>");

            salesEmailBody.Append("<table>");
            salesEmailBody.Append("<tbody>");
            salesEmailBody.Append("<tr>");
            salesEmailBody.Append("<th style='border: solid 1px;padding: 0.5em;background: #d9d5d5;'>Manufacturer</th>");
            salesEmailBody.Append("<th style='border: solid 1px;padding: 0.5em;background: #d9d5d5;'>Quantity</th>");
            salesEmailBody.Append("<th style='border: solid 1px;padding: 0.5em;background: #d9d5d5;'>Vendor#</th>");
            salesEmailBody.Append("<th style='border: solid 1px;padding: 0.5em;background: #d9d5d5;'>Description</th>");
            //salesEmailBody.Append("<th style='border: solid 1px;padding: 0.5em;background: #d9d5d5;'>Unit Cost </th>");
            //salesEmailBody.Append("<th style='border: solid 1px;padding: 0.5em;background: #d9d5d5;'>Total </th>");
            salesEmailBody.Append("</tr>");

            /*var partsList = (from wp in FarmerBrothersEntitites.WorkorderParts
                                             join fbp in FarmerBrothersEntitites.FBClosureParts on wp.Sku equals fbp.ItemNo
                                             join sk in FarmerBrothersEntitites.Skus on wp.Sku equals sk.Sku1 
                                             where wp.WorkorderID == WorkorderId
                                             select new {
                                                 Manufacturer = wp.Manufacturer,
                                                 Quantity = wp.Quantity,
                                                 Vendor = fbp.VendorNo,
                                                 Desc = wp.Description,
                                                 Unit = sk.SKUCost,
                                                 Total = wp.Quantity * sk.SKUCost
                                             }).ToList();
            if(partsList != null)
            {
                salesEmailBody.Append("<tr>");
                foreach (var wp in partsList)
                {
                    salesEmailBody.Append("<td>" + wp.Manufacturer + "</td>");
                    salesEmailBody.Append("<td>" + wp.Quantity + "</td>");
                    salesEmailBody.Append("<td>" + wp.Vendor + "</td>");
                    salesEmailBody.Append("<td>" + wp.Desc + "</td>");
                    salesEmailBody.Append("<td>" + wp.Unit + "</td>");
                    salesEmailBody.Append("<td>" + wp.Total + "</td>");
                }
                salesEmailBody.Append("</tr>");
            }*/


            List<WorkorderPart> partsList = FarmerBrothersEntitites.WorkorderParts.Where(p => p.WorkorderID == WorkorderId).ToList();

            if (partsList != null)
            {

                foreach (var wp in partsList)
                {
                    salesEmailBody.Append("<tr>");
                    salesEmailBody.Append("<td style='border: solid 1px;padding: 0.5em;'>" + wp.Manufacturer + "</td>");
                    salesEmailBody.Append("<td style='border: solid 1px;padding: 0.5em;'>" + wp.Quantity + "</td>");

                    FBClosurePart prt = FarmerBrothersEntitites.FBClosureParts.Where(s => s.ItemNo == wp.Sku).FirstOrDefault();

                    decimal totl = 0;

                    string vendorno = prt == null ? wp.Sku : String.IsNullOrEmpty(prt.VendorNo) ? wp.Sku : prt.VendorNo;

                    salesEmailBody.Append("<td style='border: solid 1px;padding: 0.5em;'>" + vendorno + "</td>");
                    salesEmailBody.Append("<td style='border: solid 1px;padding: 0.5em;'>" + wp.Description + "</td>");

                    Sku sk = FarmerBrothersEntitites.Skus.Where(s => s.Sku1 == wp.Sku).FirstOrDefault();

                    decimal? unitCost = sk == null ? 0 : sk.SKUCost;
                    //salesEmailBody.Append("<td style='border: solid 1px;padding: 0.5em;'>" + unitCost + "</td>");

                    totl = Convert.ToDecimal(wp.Quantity * unitCost);

                    //salesEmailBody.Append("<td style='border: solid 1px;padding: 0.5em;'>" + totl + "</td>");
                    salesEmailBody.Append("</tr>");
                }

            }

            salesEmailBody.Append("<tbody>");
            salesEmailBody.Append("</table>");

            string contentId = Guid.NewGuid().ToString();
            string logoPath = System.AppDomain.CurrentDomain.BaseDirectory.ToString() + "img\\mainlogo.jpg";
            //if (Server == null)
            //{
            //    logoPath = Path.Combine(HttpRuntime.AppDomainAppPath, "img/mainlogo.jpg");
            //}
            //else
            //{
            //    logoPath = Server.MapPath("~/img/mainlogo.jpg");
            //}

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

            string toAddress = string.Empty;
            if (Convert.ToBoolean(ConfigurationManager.AppSettings["UseTestMails"]))
            {
                toAddress = ConfigurationManager.AppSettings["TestEmail"];
            }
            else
            {
                if (workOrder != null)
                {
                    toAddress = ConfigurationManager.AppSettings["PartsorderToAddress"]; 
                }
            }

            string fromAddress = ConfigurationManager.AppSettings["DispatchMailFromAddress"];

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
                message.Subject = subject.ToString();
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


            //string fromAddress = ConfigurationManager.AppSettings["DispatchMailFromAddress"];
            //string ToAddr = string.Empty;
            //string CcAddr = string.Empty;
            //if (Convert.ToBoolean(ConfigurationManager.AppSettings["UseTestMails"]))
            //{
            //    ToAddr = ConfigurationManager.AppSettings["TestEmail"];
            //}
            //else
            //{
            //    if (workOrder != null)
            //    {
            //        ToAddr = "Partsorders@farmerbros.com";
            //    }
            //}

            //EmailUtility eu = new EmailUtility();
            //eu.SendEmail(fromAddress, ToAddr, CcAddr, subject.ToString(), salesEmailBody.ToString());

            return result;
        }

        public bool SendFetcoEventCreateMail(int WorkOrderID)
        {
            WorkOrder wo = FarmerBrothersEntitites.WorkOrders.Where(w => w.WorkorderID == WorkOrderID).FirstOrDefault();
            DateTime currentTime = Utility.GetCurrentTime(wo.CustomerZipCode, FarmerBrothersEntitites);

            string fromAddress = ConfigurationManager.AppSettings["DispatchMailFromAddress"];

            int cid = Convert.ToInt32(wo.CustomerID);
            Contact customer = FarmerBrothersEntitites.Contacts.Where(c => c.ContactID == cid).FirstOrDefault();

            string mailTo = ConfigurationManager.AppSettings["supportEmailId"];
            string ccTo = string.Empty;
            string mailToName = string.Empty;
            string NotesMsg = "";

            IDictionary<string, string> mailToUserIds = new Dictionary<string, string>();

            string toAddress = string.Empty;
            if (Convert.ToBoolean(ConfigurationManager.AppSettings["UseTestMails"]))
            {
                toAddress = ConfigurationManager.AppSettings["TestEmail"];
            }
            else
            {
                if (!string.IsNullOrEmpty(mailTo))
                {
                    toAddress = mailTo;
                }
            }

            var message = new MailMessage();
            message.From = new MailAddress(fromAddress);
            bool result = false;

            StringBuilder salesEmailBody = new StringBuilder();

            //string mailToUserName = mailToUserIds[address];

            salesEmailBody.Append(@"<img src='cid:logo' width='80' height='100'>");

            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("<BR>");



            #region customer details

            salesEmailBody.Append("<table>");

            salesEmailBody.Append("<tr>");
            salesEmailBody.Append("<td><b>");
            salesEmailBody.Append("Customer Details:");
            salesEmailBody.Append("</b></td>");
            salesEmailBody.Append("</tr>");

            salesEmailBody.Append("<tr>");
            salesEmailBody.Append("</tr>");

            salesEmailBody.Append("<tr>");
            salesEmailBody.Append("<td><b>");
            salesEmailBody.Append("AccountNumber:");
            salesEmailBody.Append("</b></td>");
            salesEmailBody.Append("<td>");
            salesEmailBody.Append(wo.CustomerID);
            salesEmailBody.Append("</td>");
            salesEmailBody.Append("</tr>");

            salesEmailBody.Append("<tr>");
            salesEmailBody.Append("<td><b>");
            salesEmailBody.Append("CustomerName:");
            salesEmailBody.Append("</b></td>");
            salesEmailBody.Append("<td>");
            salesEmailBody.Append(customer.CompanyName);
            salesEmailBody.Append("</td>");
            salesEmailBody.Append("</tr>");


            salesEmailBody.Append("<tr>");
            salesEmailBody.Append("<td><b>");
            salesEmailBody.Append("Address1:");
            salesEmailBody.Append("</b></td>");
            salesEmailBody.Append("<td>");
            salesEmailBody.Append(customer.Address1);
            salesEmailBody.Append("</td>");
            salesEmailBody.Append("</tr>");

            salesEmailBody.Append("<tr>");
            salesEmailBody.Append("<td><b>");
            salesEmailBody.Append("Address2:");
            salesEmailBody.Append("</b></td>");
            salesEmailBody.Append("<td>");
            salesEmailBody.Append(customer.Address2);
            salesEmailBody.Append("</td>");
            salesEmailBody.Append("</tr>");


            salesEmailBody.Append("<tr>");
            salesEmailBody.Append("<td><b>");
            salesEmailBody.Append("City:");
            salesEmailBody.Append("</b></td>");
            salesEmailBody.Append("<td>");
            salesEmailBody.Append(customer.City);
            salesEmailBody.Append("</td>");
            salesEmailBody.Append("</tr>");


            salesEmailBody.Append("<tr>");
            salesEmailBody.Append("<td><b>");
            salesEmailBody.Append("State:");
            salesEmailBody.Append("</b></td>");
            salesEmailBody.Append("<td>");
            salesEmailBody.Append(customer.State);
            salesEmailBody.Append("</td>");
            salesEmailBody.Append("</tr>");

            salesEmailBody.Append("<tr>");
            salesEmailBody.Append("<td><b>");
            salesEmailBody.Append("Postal Code:");
            salesEmailBody.Append("</b></td>");
            salesEmailBody.Append("<td>");
            salesEmailBody.Append(customer.PostalCode);
            salesEmailBody.Append("</td>");
            salesEmailBody.Append("</tr>");

            salesEmailBody.Append("<tr>");
            salesEmailBody.Append("<td><b>");
            salesEmailBody.Append("Phone:");
            salesEmailBody.Append("</b></td>");
            salesEmailBody.Append("<td>");
            salesEmailBody.Append(customer.Phone);
            salesEmailBody.Append("</td>");
            salesEmailBody.Append("</tr>");

            salesEmailBody.Append("<tr>");
            salesEmailBody.Append("<td><b>");
            salesEmailBody.Append("Main Email Address:");
            salesEmailBody.Append("</b></td>");
            salesEmailBody.Append("<td>");
            salesEmailBody.Append(customer.Email);
            salesEmailBody.Append("</td>");
            salesEmailBody.Append("</tr>");

            salesEmailBody.Append("<tr>");
            salesEmailBody.Append("<td><b>");
            salesEmailBody.Append("Branch:");
            salesEmailBody.Append("</b></td>");
            salesEmailBody.Append("<td>");
            salesEmailBody.Append(customer.Branch);
            salesEmailBody.Append("</td>");
            salesEmailBody.Append("</tr>");

            salesEmailBody.Append("<tr>");
            salesEmailBody.Append("<td><b>");
            salesEmailBody.Append("Route:");
            salesEmailBody.Append("</b></td>");
            salesEmailBody.Append("<td>");
            salesEmailBody.Append(customer.Route);
            salesEmailBody.Append("</td>");
            salesEmailBody.Append("</tr>");

            salesEmailBody.Append("</table>");

            #endregion

            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("<BR>");

            salesEmailBody.Append("<table>");
            salesEmailBody.Append("<tr>");
            salesEmailBody.Append("<td><b>");
            salesEmailBody.Append("Workorder Details:");
            salesEmailBody.Append("</b></td>");
            salesEmailBody.Append("</tr>");

            salesEmailBody.Append("<tr>");
            salesEmailBody.Append("</tr>");

            salesEmailBody.Append("<tr>");
            salesEmailBody.Append("<td><b>");
            salesEmailBody.Append("Workorder#");
            salesEmailBody.Append("</b></td>");
            salesEmailBody.Append("<td>");
            salesEmailBody.Append(wo.WorkorderID);
            salesEmailBody.Append("</td>");
            salesEmailBody.Append("</tr>");

            salesEmailBody.Append("<tr>");
            salesEmailBody.Append("<td><b>");
            salesEmailBody.Append("Payment Type");
            salesEmailBody.Append("</b></td>");
            salesEmailBody.Append("<td>");
            salesEmailBody.Append(wo.PaymentTerm);
            salesEmailBody.Append("</td>");
            salesEmailBody.Append("</tr>");

            salesEmailBody.Append("<tr>");
            salesEmailBody.Append("<td><b>");
            salesEmailBody.Append("Equipment Brand");
            salesEmailBody.Append("</b></td>");
            salesEmailBody.Append("<td>");
            salesEmailBody.Append(wo.EquipmentBrand);
            salesEmailBody.Append("</td>");
            salesEmailBody.Append("</tr>");

            salesEmailBody.Append("<tr>");
            salesEmailBody.Append("<td><b>");
            salesEmailBody.Append("Equipment Model");
            salesEmailBody.Append("</b></td>");
            salesEmailBody.Append("<td>");
            salesEmailBody.Append(wo.EquipmentModel);
            salesEmailBody.Append("</td>");
            salesEmailBody.Append("</tr>");

            salesEmailBody.Append("<tr>");
            salesEmailBody.Append("<td><b>");
            salesEmailBody.Append("Name of Person Submitting Service Request");
            salesEmailBody.Append("</b></td>");
            salesEmailBody.Append("<td>");
            salesEmailBody.Append(wo.CallerName);
            salesEmailBody.Append("</td>");
            salesEmailBody.Append("</tr>");

            salesEmailBody.Append("<tr>");
            salesEmailBody.Append("<td><b>");
            salesEmailBody.Append("Submitting Party’s Phone Number");
            salesEmailBody.Append("</b></td>");
            salesEmailBody.Append("<td>");
            salesEmailBody.Append(wo.CallerPhone);
            salesEmailBody.Append("</td>");
            salesEmailBody.Append("</tr>");

            salesEmailBody.Append("<tr>");
            salesEmailBody.Append("<td><b>");
            salesEmailBody.Append("Onsite Contact Name");
            salesEmailBody.Append("</b></td>");
            salesEmailBody.Append("<td>");
            salesEmailBody.Append(wo.WorkorderContactName);
            salesEmailBody.Append("</td>");
            salesEmailBody.Append("</tr>");

            salesEmailBody.Append("<tr>");
            salesEmailBody.Append("<td><b>");
            salesEmailBody.Append("Onsite Contact’s Phone Number");
            salesEmailBody.Append("</b></td>");
            salesEmailBody.Append("<td>");
            salesEmailBody.Append(wo.WorkorderContactPhone);
            salesEmailBody.Append("</td>");
            salesEmailBody.Append("</tr>");

            salesEmailBody.Append("</table>");

            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("<BR>");


            #region Notes

            salesEmailBody.Append("<table>");
            salesEmailBody.Append("<tr>");
            salesEmailBody.Append("<td><b>");
            salesEmailBody.Append("Notes");
            salesEmailBody.Append("<b></td>");


            IEnumerable<NotesHistory> histories = FarmerBrothersEntitites.NotesHistories.Where(w => w.WorkorderID == WorkOrderID).OrderByDescending(n => n.EntryDate);

            foreach (NotesHistory history in histories)
            {
                salesEmailBody.Append("<tr>");
                salesEmailBody.Append("<td>");
                salesEmailBody.Append(history.UserName);
                salesEmailBody.Append(" ");
                salesEmailBody.Append(history.EntryDate);
                salesEmailBody.Append(" ");
                salesEmailBody.Append(history.Notes);
                salesEmailBody.Append("</td>");
                salesEmailBody.Append("</tr>");
            }

            salesEmailBody.Append("</tr>");
            salesEmailBody.Append("</table>");


            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("<BR>");

            string contentId = Guid.NewGuid().ToString();
            string logoPath = System.AppDomain.CurrentDomain.BaseDirectory.ToString() + "img\\Fetco.png";

            salesEmailBody = salesEmailBody.Replace("cid:logo", "cid:" + contentId);

            AlternateView avHtml = AlternateView.CreateAlternateViewFromString
              (salesEmailBody.ToString(), null, MediaTypeNames.Text.Html);

            LinkedResource inline = new LinkedResource(logoPath, MediaTypeNames.Image.Jpeg);
            inline.ContentId = contentId;
            avHtml.LinkedResources.Add(inline);



            message.AlternateViews.Add(avHtml);

            message.IsBodyHtml = true;
            message.Body = salesEmailBody.Replace("cid:logo", "cid:" + inline.ContentId).ToString();

            #endregion


            
            string mailCC = string.Empty;
            string mainSentTo = string.Empty;
            if (!string.IsNullOrWhiteSpace(toAddress))
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
                                message.Bcc.Add(new MailAddress(address));
                            }
                        }
                        string[] addresses = mailCCAddress[0].Split(';');
                        foreach (string address in addresses)
                        {
                            if (!string.IsNullOrWhiteSpace(address))
                            {
                                message.To.Add(new MailAddress(address));
                                mainSentTo += address+ ";";
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
                            mainSentTo += address + ";";
                        }
                    }
                }

                message.From = new MailAddress(fromAddress);
                message.Subject = "Fetco Service Event#: " + WorkOrderID;
                message.IsBodyHtml = true;

                using (var smtp = new SmtpClient())
                {
                    smtp.Host = ConfigurationManager.AppSettings["MailServer"];
                    smtp.Port = 25;

                    try
                    {
                        smtp.Send(message);

                        NotesMsg += "Fetco Event Create Mail sent to " + mainSentTo;
                        NotesHistory notesHistory = new NotesHistory()
                        {
                            AutomaticNotes = 1,
                            EntryDate = currentTime,
                            Notes = NotesMsg,
                            Userid = 99999,
                            UserName = "Fetco WEB",
                            isDispatchNotes = 1,
                            WorkorderID = wo.WorkorderID
                        };
                        
                        wo.NotesHistories.Add(notesHistory);
                        FarmerBrothersEntitites.SaveChanges();

                        result = true;
                    }
                    catch (Exception ex)
                    {
                        result = false;
                    }
                }

            }



            return result;
        }

    }
}
