using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBPowerReport
{
    class PowerReportDataModel
    {
        public string WorkorderId { get; set; }
        public string Customerid { get; set; }
        public string EntryDate { get; set; }
        public string Status { get; set; }
        public string PurchaseOrder { get; set; }
        public string BillingId { get; set; }
        public string ErfId { get; set; }
        public string CloseDate { get; set; }
        public string EqpCount { get; set; }
        public string State { get; set; }
        public string ThirdPartyPO { get; set; }
        public string Estimate { get; set; }
        public string FinalEstimate { get; set; }
        public string JREvents { get; set; }
        public string OriginalWOId { get; set; }
        public string CallTypeId { get; set; }
        public string TechCalled { get; set; }
        public string DESCF { get; set; }
        public string AppointmentDate { get; set; }
        public string ERFOriginalReqDate { get; set; }
        public string ERFReceivedDate { get; set; }
        public string ProjectNumber { get; set; }
        public string EqpETADate { get; set; }
        public string ERFCompletedate { get; set; }
        public string NoServiceRequired { get; set; }
        public string NoServiceReason { get; set; }
        public string Delta { get; set; }

        public string Techid { get; set; }
        public string Techname { get; set; }
        public string ScheduleDate { get; set; }
        public string TechState { get; set; }
        public string FSMID { get; set; }
        public string DispatchTechID { get; set; }
        public string DispatchTechName { get; set; }

        public string StartTime { get; set; }
        public string ArrivalTime { get; set; }
        public string CompletionTime { get; set; }

        public string CallTypeDescription { get; set; }
        public string FieldServiceManager { get; set; }
        public string FSMJDE { get; set; }
        public string CompanyName { get; set; }
        public string SolutionId { get; set; }
        public string SystemId { get; set; }
        public string SymptomId { get; set; }
        public string serialNumber { get; set; }
        public string ProductNumber { get; set; }
        public string Manufacturer { get; set; }
        public string Catagory { get; set; }
        public string InvoiceNumber { get; set; }
        public string FamilyAff { get; set; }
        public string SearchType { get; set; }
        public string SearchDesc { get; set; }
        public string ServicePriority { get; set; }
        public string EventScheduleDate { get; set; }


        public List<PowerReportDataModel> GetReportsListData()
        {
            List<PowerReportDataModel> prmList = new List<PowerReportDataModel>();
            try
            {

                string constr = ConfigurationManager.ConnectionStrings["FB_Connection"].ConnectionString;
                DataTable dt = new DataTable();

                DateTime currentDate = DateTime.Now;
                Double NoOfDaysFilter = Convert.ToDouble(ConfigurationManager.AppSettings["DaysFilter"]);

                string startDate = currentDate.AddDays(NoOfDaysFilter).ToString();
                //string endDate = currentDate.AddDays(-1).ToString();

                //string sqlQuery = @"SELECT WorkOrder.WorkorderID, WorkOrder.WorkorderEntryDate, WorkOrder.CustomerID, 
                //                    --TechInfromationforFB.Techid, TechInfromationforFB.TechName, 
                //                    WorkOrder.WorkorderCallstatus, WorkOrder.PurchaseOrder, 'N' AS BillingID, WorkOrder.WorkorderErfid,
                //                    --TechInfromationforFB.ScheduleDate, 
                //                    WorkOrder.WorkorderCloseDate, WorkOrder.WorkorderEquipCount, Contact.State,
                //                     --TechInfromationforFB.TechState, 
                //                     WorkOrder.ThirdPartyPO, WorkOrder.Estimate, WorkOrder.FinalEstimate, 'No' AS JREvents, WorkOrder.OriginalWorkorderid, 
                //                    WorkOrder.WorkorderCalltypeid, '' AS TechCalled, '' AS DESCF,
                //                     --TechInfromationforFB.FSMID, 
                //                     WorkOrder.AppointmentDate, Erf.OriginalRequestedDate, Erf.DateERFReceived, 
                //                     --TechInfromationforFB.Techid AS DispatchTechID, TechInfromationforFB.TechName AS DispatchTechName, 
                //                     '' AS ProjectNumber, Erf.OriginalRequestedDate AS OrgCustReqDate, Erf.EquipETADate, '' AS ERFComplete,
                //                      WorkOrder.NoServiceRequired, '' AS NoServiceReason, 'DELTA' AS DELTA 
                //                      --INTO FBReport 
                //                    FROM((WorkOrder INNER JOIN Contact ON WorkOrder.CustomerID = Contact.ContactID) 
                //                    --LEFT JOIN TechInfromationforFB ON WorkOrder.WorkorderID = TechInfromationforFB.WorkorderID
                //                    ) 
                //                    LEFT JOIN Erf ON WorkOrder.WorkorderID = Erf.WorkorderID
                //                    WHERE(((WorkOrder.WorkorderEntryDate) >= '" + startDate + "'));";

                string PowerDataQuery = ConfigurationManager.AppSettings["PowerDataQuery"];

                string sqlQuery = PowerDataQuery + " WHERE(((WorkOrder.WorkorderEntryDate) >= '" + startDate + "')); ";
                //string sqlQuery = PowerDataQuery + " WHERE WorkOrder.WorkorderEntryDate >= '2018-07-01' and Workorder.WorkorderEntryDate <= '2019-07-26' order by WorkOrder.WorkorderEntryDate desc";
                //string sqlQuery = PowerDataQuery + " WHERE WorkorderSchedule.AssignedStatus = 'Accepted' and WorkOrder.WorkorderEntryDate >= '2018-07-01'  and Workorder.WorkorderEntryDate <= '2019-08-04' order by WorkOrder.WorkorderEntryDate desc";

                using (SqlConnection con = new SqlConnection(constr))
                {
                    using (SqlCommand cmd = new SqlCommand(sqlQuery, con))
                    {
                        con.Open();
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            da.Fill(dt);
                        }

                    }
                    con.Close();
                }

                foreach (DataRow dr in dt.Rows)
                {
                    PowerReportDataModel pr = new PowerReportDataModel();
                    pr.WorkorderId = dr["WorkorderID"] == DBNull.Value ? "0" : dr["WorkorderID"].ToString();
                    pr.Customerid = dr["CustomerID"] == DBNull.Value ? "0" : dr["CustomerID"].ToString();
                    pr.EntryDate = dr["WorkorderEntryDate"] == DBNull.Value ? "" : dr["WorkorderEntryDate"].ToString();
                    pr.Status = dr["WorkorderCallstatus"] == DBNull.Value ? "" : dr["WorkorderCallstatus"].ToString();
                    pr.PurchaseOrder = dr["PurchaseOrder"] == DBNull.Value ? "" : dr["PurchaseOrder"].ToString();
                    pr.BillingId = dr["BillingID"] == DBNull.Value ? "" : dr["BillingID"].ToString();
                    pr.ErfId =  dr["WorkorderErfid"] == DBNull.Value ? "0" : dr["WorkorderErfid"].ToString();
                    pr.CloseDate = dr["WorkorderCloseDate"] == DBNull.Value ? "" : dr["WorkorderCloseDate"].ToString();
                    pr.EqpCount = dr["WorkorderEquipCount"] == DBNull.Value ? "0" : dr["WorkorderEquipCount"].ToString();
                    pr.State = dr["CustomerState"] == DBNull.Value ? "" : dr["CustomerState"].ToString();
                    pr.ThirdPartyPO = dr["ThirdPartyPO"] == DBNull.Value ? "" : dr["ThirdPartyPO"].ToString();
                    pr.Estimate = dr["Estimate"] == DBNull.Value ? "" : dr["Estimate"].ToString();
                    pr.FinalEstimate = dr["FinalEstimate"] == DBNull.Value ? "" : dr["FinalEstimate"].ToString();
                    pr.JREvents = dr["JREvents"] == DBNull.Value ? "" : dr["JREvents"].ToString();
                    pr.OriginalWOId = dr["OriginalWorkorderid"] == DBNull.Value ? "0" : dr["OriginalWorkorderid"].ToString();
                    pr.CallTypeId = dr["WorkorderCalltypeid"] == DBNull.Value ? "" : dr["WorkorderCalltypeid"].ToString();
                    pr.TechCalled = dr["TechCalled"] == DBNull.Value ? "" : dr["TechCalled"].ToString();
                    pr.DESCF = dr["DESCF"] == DBNull.Value ? "" : dr["DESCF"].ToString();
                    pr.AppointmentDate = dr["AppointmentDate"] == DBNull.Value ? "" : dr["AppointmentDate"].ToString();
                    pr.StartTime = dr["StartDateTime"] == DBNull.Value ? "" : dr["StartDateTime"].ToString();
                    pr.ArrivalTime = dr["ArrivalDateTime"] == DBNull.Value ? "" : dr["ArrivalDateTime"].ToString();
                    pr.CompletionTime = dr["CompletionDateTime"] == DBNull.Value ? "" : dr["CompletionDateTime"].ToString();

                    pr.ERFOriginalReqDate = dr["OriginalRequestedDate"] == DBNull.Value ? "" : dr["OriginalRequestedDate"].ToString();
                    pr.ERFReceivedDate = dr["DateERFReceived"] == DBNull.Value ? "" : dr["DateERFReceived"].ToString();
                    pr.ProjectNumber = dr["ProjectNumber"] == DBNull.Value ? "0" : dr["ProjectNumber"].ToString();
                    pr.EqpETADate = dr["EquipETADate"] == DBNull.Value ? "" : dr["EquipETADate"].ToString();
                    pr.ERFCompletedate = dr["ERFComplete"] == DBNull.Value ? "" : dr["ERFComplete"].ToString();
                    pr.NoServiceRequired = dr["NoServiceRequired"] == DBNull.Value ? "" : dr["NoServiceRequired"].ToString();
                    pr.NoServiceReason = dr["NoServiceReason"] == DBNull.Value ? "" : dr["NoServiceReason"].ToString();                    
                    pr.Delta = dr["DELTA"] == DBNull.Value ? "" : dr["DELTA"].ToString();

                    pr.Techid = dr["Techid"] == DBNull.Value ? "" : dr["Techid"].ToString();
                    pr.Techname = dr["Techname"] == DBNull.Value ? "" : dr["Techname"].ToString();
                    pr.ScheduleDate = dr["ScheduleDate"] == DBNull.Value ? "" : dr["ScheduleDate"].ToString();
                    pr.TechState = dr["TechState"] == DBNull.Value ? "" : dr["TechState"].ToString();
                    pr.FSMID = dr["FSMID"] == DBNull.Value ? "" : dr["FSMID"].ToString();
                    pr.DispatchTechID = dr["DispatchTechID"] == DBNull.Value ? "" : dr["DispatchTechID"].ToString();
                    pr.DispatchTechName = dr["DispatchTechName"] == DBNull.Value ? "" : dr["DispatchTechName"].ToString();


                    pr.CallTypeDescription = dr["WorkorderCalltypeDesc"] == DBNull.Value ? "" : dr["WorkorderCalltypeDesc"].ToString();
                    pr.FieldServiceManager = dr["FieldServiceManager"] == DBNull.Value ? "" : dr["FieldServiceManager"].ToString();
                    pr.FSMJDE = dr["FSMJDE"] == DBNull.Value ? "" : dr["FSMJDE"].ToString();
                    pr.CompanyName = dr["CompanyName"] == DBNull.Value ? "" : dr["CompanyName"].ToString();
                    pr.SolutionId = dr["SolutionId"] == DBNull.Value ? "" : dr["SolutionId"].ToString();
                    pr.SystemId = dr["Systemid"] == DBNull.Value ? "" : dr["Systemid"].ToString();
                    pr.SymptomId = dr["Symptomid"] == DBNull.Value ? "" : dr["Symptomid"].ToString();
                    pr.serialNumber = dr["SerialNumber"] == DBNull.Value ? "" : dr["SerialNumber"].ToString();
                    pr.ProductNumber = dr["ProdNo"] == DBNull.Value ? "" : dr["ProdNo"].ToString();
                    pr.Manufacturer = dr["Manufacturer"] == DBNull.Value ? "" : dr["Manufacturer"].ToString();
                    pr.Catagory = dr["Category"] == DBNull.Value ? "" : dr["Category"].ToString();
                    pr.InvoiceNumber = dr["InvoiceNo"] == DBNull.Value ? "" : dr["InvoiceNo"].ToString();
                    pr.FamilyAff = dr["FamilyAff"] == DBNull.Value ? "" : dr["FamilyAff"].ToString();
                    pr.SearchType = dr["SearchType"] == DBNull.Value ? "" : dr["SearchType"].ToString();
                    pr.SearchDesc = dr["SearchDesc"] == DBNull.Value ? "" : dr["SearchDesc"].ToString();
                    pr.ServicePriority = dr["ServicePriority"] == DBNull.Value ? "" : dr["ServicePriority"].ToString();
                    pr.EventScheduleDate = dr["EventScheduleDate"] == DBNull.Value ? "" : dr["EventScheduleDate"].ToString();


        prmList.Add(pr);
                }

            }
            catch(Exception ex)
            {

            }

            return prmList;
        }

    }

    class WorkorderInfoModel
    {
        public string WorkorderID { get; set; }
        public string WorkorderCallstatus { get; set; }
        public string CustomerID { get; set; }
        public string WorkorderEntryDate { get; set; }
        public string WorkorderCloseDate { get; set; }
        public string WorkorderErfid { get; set; }
        public string WorkorderCalltypeid { get; set; }
        public string WorkorderCalltypeDesc { get; set; }

        public string WorkorderSpawnEvent { get; set; }
        public string WorkorderClosureConfirmationNo { get; set; }
        public string PurchaseOrder { get; set; }
        public string CallerName { get; set; }

        public string OvertimeRequest { get; set; }
        public string WorkorderContactPhone { get; set; }
        public string WorkorderContactName { get; set; }
        public string NoServiceRequired { get; set; }

        public string ParentWorkorderid { get; set; }
        public string ThirdPartyPO { get; set; }
        public string Techid { get; set; }
        public string ScheduleDate { get; set; }

        public string TechName { get; set; }
        public string TechPhone { get; set; }
        public string ServiceCenterName { get; set; }
        public string ServiceCenterID { get; set; }

        public string AppointmentDate { get; set; }
        public string SearchType { get; set; }
        public string FamilyAff { get; set; }
        public string FSMJDE { get; set; }
        public string FieldServiceManager { get; set; }

        public string StartDateTime { get; set; }
        public string ArrivalDateTime { get; set; }
        public string CompletionDateTime { get; set; }
        public string OriginalRequestedDate { get; set; }

        public string RepeatCallEvent { get; set; }
        public string WorkorderEquipCount { get; set; }
        public string PPID { get; set; }
        public string PPIDDesc { get; set; }
        public string ServicePriority { get; set; }
        public string FilterReplaced { get; set; }
        public string FilterReplacedDate { get; set; }
        public string NextFilterReplacementDate { get; set; }
        public string WaterTested { get; set; }
        public string HardnessRating { get; set; }
        public string RescheduleReason { get; set; }
        public string RepeatRepair { get; set; }


        public List<WorkorderInfoModel> GetWorkordersListData()
        {
            List<WorkorderInfoModel> prmList = new List<WorkorderInfoModel>();
            try
            {
                string constr = ConfigurationManager.ConnectionStrings["FB_Connection"].ConnectionString;
                DataTable dt = new DataTable();

                DateTime currentDate = DateTime.Now;
                Double NoOfDaysFilter = Convert.ToDouble(ConfigurationManager.AppSettings["DaysFilter"]);

                string startDate = currentDate.AddDays(NoOfDaysFilter).ToString();                

                string WorkorderDataQuery = ConfigurationManager.AppSettings["WorkorderDataQuery"];


                //string sqlQuery = WorkorderDataQuery + " WHERE(((dbo.WorkOrder.WorkorderCallstatus) = 'closed') AND((dbo.WorkOrder.WorkorderCloseDate) >= '" + startDate + "') ";
                //string sqlQuery = WorkorderDataQuery + " WHERE(((dbo.WorkOrder.WorkorderCallstatus) = 'closed') AND((dbo.WorkOrder.WorkorderCloseDate) >= '2020-01-02') AND((dbo.WorkOrder.WorkorderCloseDate) < '2020-01-03') ";
                //" WHERE WorkOrder.WorkorderEntryDate >= '2018-07-01' and Workorder.WorkorderEntryDate <= '2019-07-26' order by WorkOrder.WorkorderEntryDate desc";


                string sqlQuery = WorkorderDataQuery + " WHERE((dbo.WorkOrder.WorkorderEntryDate >= '" + startDate + "')) ";
                //sqlQuery = sqlQuery + " AND((dbo.WorkorderSchedule.PrimaryTech) = 1) AND((dbo.WorkorderSchedule.AssignedStatus) = 'accepted')); ";
                
                using (SqlConnection con = new SqlConnection(constr))
                {
                    using (SqlCommand cmd = new SqlCommand(sqlQuery, con))
                    {
                        con.Open();
                        cmd.CommandTimeout = 3600;
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            da.Fill(dt);
                        }

                    }
                    con.Close();
                }

                foreach (DataRow dr in dt.Rows)
                {
                    WorkorderInfoModel pr = new WorkorderInfoModel();
                    pr.WorkorderID = dr["WorkorderID"] == DBNull.Value ? "0" : dr["WorkorderID"].ToString();
                    pr.WorkorderCallstatus = dr["WorkorderCallstatus"] == DBNull.Value ? "" : dr["WorkorderCallstatus"].ToString();
                    pr.CustomerID = dr["CustomerID"] == DBNull.Value ? "0" : dr["CustomerID"].ToString();
                    pr.WorkorderEntryDate = dr["WorkorderEntryDate"] == DBNull.Value ? "" : dr["WorkorderEntryDate"].ToString();
                    pr.WorkorderCloseDate = dr["WorkorderCloseDate"] == DBNull.Value ? "" : dr["WorkorderCloseDate"].ToString();
                    pr.WorkorderErfid = dr["WorkorderErfid"] == DBNull.Value ? "0" : dr["WorkorderErfid"].ToString();
                    pr.WorkorderCalltypeid = dr["WorkorderCalltypeid"] == DBNull.Value ? "" : dr["WorkorderCalltypeid"].ToString();
                    pr.WorkorderCalltypeDesc = dr["WorkorderCalltypeDesc"] == DBNull.Value ? "" : dr["WorkorderCalltypeDesc"].ToString();
                    pr.WorkorderSpawnEvent = dr["WorkorderSpawnEvent"] == DBNull.Value ? "" : dr["WorkorderSpawnEvent"].ToString();
                    pr.WorkorderClosureConfirmationNo = dr["WorkorderClosureConfirmationNo"] == DBNull.Value ? "" : dr["WorkorderClosureConfirmationNo"].ToString();
                    pr.PurchaseOrder = dr["PurchaseOrder"] == DBNull.Value ? "" : dr["PurchaseOrder"].ToString();
                    pr.CallerName = dr["CallerName"] == DBNull.Value ? "" : dr["CallerName"].ToString();
                    pr.OvertimeRequest = dr["OvertimeRequest"] == DBNull.Value ? "" : dr["OvertimeRequest"].ToString();
                    pr.WorkorderContactPhone = dr["WorkorderContactPhone"] == DBNull.Value ? "" : dr["WorkorderContactPhone"].ToString();
                    pr.WorkorderContactName = dr["WorkorderContactName"] == DBNull.Value ? "" : dr["WorkorderContactName"].ToString();
                    pr.NoServiceRequired = dr["NoServiceRequired"] == DBNull.Value ? "" : dr["NoServiceRequired"].ToString();
                    pr.ParentWorkorderid = dr["ParentWorkorderid"] == DBNull.Value ? "" : dr["ParentWorkorderid"].ToString();
                    pr.ThirdPartyPO = dr["ThirdPartyPO"] == DBNull.Value ? "" : dr["ThirdPartyPO"].ToString();
                    pr.Techid = dr["Techid"] == DBNull.Value ? "0" : dr["Techid"].ToString();
                    pr.ScheduleDate = dr["ScheduleDate"] == DBNull.Value ? "" : dr["ScheduleDate"].ToString();
                    pr.TechName = dr["TechName"] == DBNull.Value ? "" : dr["TechName"].ToString();
                    pr.TechPhone = dr["TechPhone"] == DBNull.Value ? "" : dr["TechPhone"].ToString();
                    pr.ServiceCenterName = dr["ServiceCenterName"] == DBNull.Value ? "" : dr["ServiceCenterName"].ToString();
                    pr.ServiceCenterID = dr["ServiceCenterID"] == DBNull.Value ? "" : dr["ServiceCenterID"].ToString();
                    pr.AppointmentDate = dr["AppointmentDate"] == DBNull.Value ? "" : dr["AppointmentDate"].ToString();
                    pr.SearchType = dr["SearchType"] == DBNull.Value ? "" : dr["SearchType"].ToString();
                    pr.FamilyAff = dr["FamilyAff"] == DBNull.Value ? "" : dr["FamilyAff"].ToString();
                    pr.FSMJDE = dr["FSMJDE"] == DBNull.Value ? "" : dr["FSMJDE"].ToString();
                    pr.FieldServiceManager = dr["FieldServiceManager"] == DBNull.Value ? "" : dr["FieldServiceManager"].ToString();
                    pr.StartDateTime = dr["StartDateTime"] == DBNull.Value ? "" : dr["StartDateTime"].ToString();
                    pr.ArrivalDateTime = dr["ArrivalDateTime"] == DBNull.Value ? "" : dr["ArrivalDateTime"].ToString();
                    pr.CompletionDateTime = dr["CompletionDateTime"] == DBNull.Value ? "" : dr["CompletionDateTime"].ToString();
                    pr.OriginalRequestedDate = dr["OriginalRequestedDate"] == DBNull.Value ? "" : dr["OriginalRequestedDate"].ToString();

                    pr.RepeatCallEvent = dr["RepeatCallEvent"] == DBNull.Value ? "" : dr["RepeatCallEvent"].ToString();
                    pr.WorkorderEquipCount = dr["WorkorderEquipCount"] == DBNull.Value ? "" : dr["WorkorderEquipCount"].ToString();
                    pr.PPID = dr["PPID"] == DBNull.Value ? "" : dr["PPID"].ToString();
                    pr.PPIDDesc = dr["PPIDDesc"] == DBNull.Value ? "" : dr["PPIDDesc"].ToString();
                    pr.ServicePriority = dr["ServicePriority"] == DBNull.Value ? "" : dr["ServicePriority"].ToString();
                    pr.FilterReplaced = dr["FilterReplaced"] == DBNull.Value ? "" : dr["FilterReplaced"].ToString();
                    pr.FilterReplacedDate = dr["FilterReplacedDate"] == DBNull.Value ? "" : dr["FilterReplacedDate"].ToString();
                    pr.NextFilterReplacementDate = dr["NextFilterReplacementDate"] == DBNull.Value ? "" : dr["NextFilterReplacementDate"].ToString();
                    pr.WaterTested = dr["WaterTested"] == DBNull.Value ? "" : dr["WaterTested"].ToString();
                    pr.HardnessRating = dr["HardnessRating"] == DBNull.Value ? "" : dr["HardnessRating"].ToString();
                    pr.RescheduleReason = dr["RescheduleReason"] == DBNull.Value ? "" : dr["RescheduleReason"].ToString();
                    pr.RepeatRepair = dr["RepeatRepair"] == DBNull.Value ? "" : dr["RepeatRepair"].ToString();

                    prmList.Add(pr);
                }

            }
            catch (Exception ex)
            {

            }

            return prmList;
        }

    }
    class WorkorderEquipmentModel
    {
        public string WorkorderID { get; set; }
        public string Assetid { get; set; }
        public string CallTypeid { get; set; }

        public string Category { get; set; }
        public string Manufacturer { get; set; }
        public string Model { get; set; }
        public string Location { get; set; }
        public string SerialNumber { get; set; }
        public string Solutionid { get; set; }
        public string Temperature { get; set; }
        public string Systemid { get; set; }
        public string Symptomid { get; set; }

        public List<WorkorderEquipmentModel> GetWorkorderEquipmentsListData()
        {
            List<WorkorderEquipmentModel> prmList = new List<WorkorderEquipmentModel>();
            try
            {
                string constr = ConfigurationManager.ConnectionStrings["FB_Connection"].ConnectionString;
                DataTable dt = new DataTable();

                DateTime currentDate = DateTime.Now;
                Double NoOfDaysFilter = Convert.ToDouble(ConfigurationManager.AppSettings["DaysFilter"]);

                string startDate = currentDate.AddDays(NoOfDaysFilter).ToString();

                string WorkorderEqpDataQuery = ConfigurationManager.AppSettings["WorkorderEquipmentDataQuery"];

                //string sqlQuery = WorkorderEqpDataQuery + " WHERE(((dbo.WorkOrder.WorkorderCallstatus) = 'closed') AND((dbo.WorkOrder.WorkorderCloseDate) >= '" + startDate + "') ";
                //string sqlQuery = WorkorderEqpDataQuery + " WHERE(((dbo.WorkOrder.WorkorderCallstatus) = 'closed') AND((dbo.WorkOrder.WorkorderCloseDate) >= '2020-01-02') AND((dbo.WorkOrder.WorkorderCloseDate) < '2020-01-03') ";
                //string sqlQuery = WorkorderEqpDataQuery + " WHERE(((dbo.WorkOrder.WorkorderCallstatus) = 'closed') AND((dbo.WorkOrder.WorkorderCloseDate) >= '2018-07-01') AND((dbo.WorkOrder.WorkorderCloseDate) <= '2019-10-14') ";


                string sqlQuery = WorkorderEqpDataQuery + " WHERE((dbo.WorkOrder.WorkorderEntryDate >= '" + startDate + "')) ";
                //sqlQuery = sqlQuery + " AND((dbo.WorkorderSchedule.PrimaryTech) = 1) AND((dbo.WorkorderSchedule.AssignedStatus) = 'accepted')); ";

                using (SqlConnection con = new SqlConnection(constr))
                {
                    using (SqlCommand cmd = new SqlCommand(sqlQuery, con))
                    {
                        con.Open();
                        cmd.CommandTimeout = 3600;
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            da.Fill(dt);
                        }

                    }
                    con.Close();
                }

                foreach (DataRow dr in dt.Rows)
                {
                    WorkorderEquipmentModel pr = new WorkorderEquipmentModel();
                    pr.WorkorderID = dr["WorkorderID"] == DBNull.Value ? "0" : dr["WorkorderID"].ToString();
                    pr.Assetid = dr["Assetid"] == DBNull.Value ? "0" : dr["Assetid"].ToString();
                    pr.CallTypeid = dr["CallTypeid"] == DBNull.Value ? "0" : dr["CallTypeid"].ToString();
                    pr.Category = dr["Category"] == DBNull.Value ? "" : dr["Category"].ToString();
                    pr.Manufacturer = dr["Manufacturer"] == DBNull.Value ? "" : dr["Manufacturer"].ToString();
                    pr.Model = dr["Model"] == DBNull.Value ? "" : dr["Model"].ToString();
                    pr.Location = dr["Location"] == DBNull.Value ? "" : dr["Location"].ToString().Length > 50 ? dr["Location"].ToString().Substring(0,50) : dr["Location"].ToString();
                    pr.SerialNumber = dr["SerialNumber"] == DBNull.Value ? "" : dr["SerialNumber"].ToString();
                    pr.Solutionid = dr["Solutionid"] == DBNull.Value ? "" : dr["Solutionid"].ToString();
                    pr.Temperature = dr["Temperature"] == DBNull.Value ? "" : dr["Temperature"].ToString();
                    pr.Systemid = dr["Systemid"] == DBNull.Value ? "" : dr["Systemid"].ToString();
                    pr.Symptomid = dr["Symptomid"] == DBNull.Value ? "" : dr["Symptomid"].ToString();

                    prmList.Add(pr);
                }

            }
            catch (Exception ex)
            {

            }

            return prmList;
        }
    }
    class WorkorderPartsModel
    {
        public string WorkorderID { get; set; }
        public string Sku { get; set; }
        public string Description { get; set; }
        public string Quantity { get; set; }
        public string AssetID { get; set; }

        public List<WorkorderPartsModel> GetWorkorderPartsListData()
        {
            List<WorkorderPartsModel> prmList = new List<WorkorderPartsModel>();
            try
            {
                string constr = ConfigurationManager.ConnectionStrings["FB_Connection"].ConnectionString;
                DataTable dt = new DataTable();

                DateTime currentDate = DateTime.Now;
                Double NoOfDaysFilter = Convert.ToDouble(ConfigurationManager.AppSettings["DaysFilter"]);

                string startDate = currentDate.AddDays(NoOfDaysFilter).ToString();

                string WorkorderPartsDataQuery = ConfigurationManager.AppSettings["WorkorderPartsDataQuery"];

                //string sqlQuery = WorkorderPartsDataQuery + " WHERE(((dbo.WorkOrder.WorkorderCallstatus) = 'closed') AND((dbo.WorkOrder.WorkorderCloseDate) >= '" + startDate + "') ";
                //string sqlQuery = WorkorderPartsDataQuery + " WHERE(((dbo.WorkOrder.WorkorderCallstatus) = 'closed') AND((dbo.WorkOrder.WorkorderCloseDate) >= '2020-01-02') AND((dbo.WorkOrder.WorkorderCloseDate) < '2020-01-03') ";
                //string sqlQuery = WorkorderPartsDataQuery + " WHERE(((dbo.WorkOrder.WorkorderCallstatus) = 'closed') AND((dbo.WorkOrder.WorkorderCloseDate) >= '2018-07-01') AND((dbo.WorkOrder.WorkorderCloseDate) <= '2019-10-14') ";


                string sqlQuery = WorkorderPartsDataQuery + " WHERE((dbo.WorkOrder.WorkorderEntryDate >= '" + startDate + "')) ";
                //sqlQuery = sqlQuery + " AND((dbo.WorkorderSchedule.PrimaryTech) = 1) AND((dbo.WorkorderSchedule.AssignedStatus) = 'accepted')) ";
                sqlQuery = sqlQuery + " GROUP BY dbo.WorkorderParts.WorkorderID, dbo.WorkorderParts.Sku, dbo.WorkorderParts.Description,dbo.WorkorderParts.Quantity, dbo.WorkorderParts.AssetID; ";

                using (SqlConnection con = new SqlConnection(constr))
                {
                    using (SqlCommand cmd = new SqlCommand(sqlQuery, con))
                    {
                        con.Open();
                        cmd.CommandTimeout = 3600;
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            da.Fill(dt);
                        }

                    }
                    con.Close();
                }

                foreach (DataRow dr in dt.Rows)
                {
                    WorkorderPartsModel pr = new WorkorderPartsModel();                   
                    pr.WorkorderID = dr["WorkorderID"] == DBNull.Value ? "0" : dr["WorkorderID"].ToString();
                    pr.Sku = dr["Sku"] == DBNull.Value ? "" : dr["Sku"].ToString();
                    pr.Description = dr["Description"] == DBNull.Value ? "" : dr["Description"].ToString();
                    pr.Quantity = dr["Quantity"] == DBNull.Value ? "0" : dr["Quantity"].ToString();
                    pr.AssetID = dr["AssetID"] == DBNull.Value ? "0" : dr["AssetID"].ToString();

                    prmList.Add(pr);
                }

            }
            catch (Exception ex)
            {

            }

            return prmList;
        }
    }


    class ERFModel
    {
        public int WorkorderID { get; set; }
        public string WorkorderErfid { get; set; }
        public string WorkorderCalltypeDesc { get; set; }
        public string WorkorderCallstatus { get; set; }
        public string PriorityCode { get; set; }
        public string WorkorderEntryDate { get; set; }
        public string ElapsedTime { get; set; }

        public string OriginatorName { get; set; }
        public string CustomerName { get; set; }
        public string ServiceTier { get; set; }
        public string CustomerCity { get; set; }
        public string CustomerState { get; set; }
        public string CustomerZipCode { get; set; }
        public Nullable<int> CustomerID { get; set; }
        public string AssignedTech { get; set; }
        public string TechPhone { get; set; }
        public string TechBranch { get; set; }
        public string FSMName { get; set; }
        public string FSMPhone { get; set; }
        public int? OriginalMarsWorkOrderId { get; set; }
        public int? ParentWorkOrderId { get; set; }
        public string SpawnReason { get; set; }

        public bool SearchInNonServiceWorkOrder { get; set; }

        public string WorkOrderAcceptDate { get; set; }
        public string AcceptElapsedTime { get; set; }
        public string ScheduledDate { get; set; }
        public string DispatchElapsedTime { get; set; }


        //Newly added columsn in program status reprot 
        public string CustomerType { get; set; }
        public string CustomerRegion { get; set; }
        public string AppointmentDate { get; set; }
        public string EventScheduleDate { get; set; }
        public string WorkorderCloseDate { get; set; }
        public string StartDateTime { get; set; }
        public string ArrivalDateTime { get; set; }
        public string CompletionDateTime { get; set; }
        public string NoService { get; set; }
        public string ErfOriginalScheduleDate { get; set; }

        public string EventCallTypeID { get; set; }
        public string Address1 { get; set; }
        public string FieldServiceManager { get; set; }
        public string FSMJDE { get; set; }
        public string PricingParentName { get; set; }
        public string DeliveryDesc { get; set; }
        public string ERFNO { get; set; }
        public string TechId { get; set; }
        public string RepeatcallEvent { get; set; }
        public string RepeatRepairEvent { get; set; }
        public string EquipCount { get; set; }
        public string DealerCompany { get; set; }
        public string DealerCity { get; set; }
        public string DealerState { get; set; }
        public string CallTypeID { get; set; }
        public string SymptomID { get; set; }
        public string SolutionId { get; set; }
        public string SystemId { get; set; }
        public string SerialNo { get; set; }
        public string ProductNo { get; set; }
        public string Manufacturer { get; set; }
        public string ManufacturerDesc { get; set; }
        public string EquipmentType { get; set; }
        public string CategoryDesc { get; set; }
        public string InvoiceNo { get; set; }
        public string FamilyAff { get; set; }
        public string DriveTimeMin { get; set; }
        public string OnSiteTimeMin { get; set; }
        public string BranchName { get; set; }


        public string RegionNumber { get; set; }
        public string CustomerBranch { get; set; }
        public string Branch { get; set; }
        public string ContactSearchType { get; set; }
        public string ContactSearchDesc { get; set; }
        public string RouteNumber { get; set; }

        public string Route { get; set; }
        public string PricingParentID { get; set; }
        public string PricingParentDescription { get; set; }

        public string DoNotPay { get; set; }
        public string ParentAccount { get; set; }

        //public string IsBillable { get; set; }
        //public string TotalUnitPrice { get; set; }
        public string ServicePriority { get; set; }
        public string PPID { get; set; }
        public string PPIDDESC { get; set; }

        public string CustomerMainContactName { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerMainEmail { get; set; }


        public string OrderType { get; set; }
        public string ShipToBranch { get; set; }
        public string SiteReady { get; set; }
        public string EqpQty { get; set; }
        public string EqpTotal { get; set; }
        public string ExpTotal { get; set; }
        public string ExpQty { get; set; }
        public string Total { get; set; }
        public string TotalNSV { get; set; }
        public string ApprovalStatus { get; set; }
        public string WOStatus { get; set; }
        public string WOClosedDate { get; set; }
        public string ContactName { get; set; }
        public string EqpType { get; set; }
        public string EqpName { get; set; }
        public string EqpCategoryName { get; set; }
        public string ExpType { get; set; }
        public string ExpName { get; set; }
        public string ExpCategoryName { get; set; }

        public string EqpInternalOrderType { get; set; }
        public string EqpVendorOrderType { get; set; }
        public string ExpInternalOrderType { get; set; }
        public string ExpVendorOrderType { get; set; }

        //Closure Filter Data
        public string FilterReplaced { get; set; }
        public string FilterReplacedDate { get; set; }
        public string NextFilterReplacementDate { get; set; }
        public string WaterTested { get; set; }
        public string HardnessRating { get; set; }

        public string RescheduleReason { get; set; }

        public string ModifiedUser { get; set; }
        public string ModifiedDate { get; set; }

        public string DispatchDate { get; set; }
        public string AcceptedDate { get; set; }
        public string DispatchTech { get; set; }
        public string Tracking { get; set; }


        public List<ERFModel> GetERFData()
        {
            FBEntities fbEntity = new FBEntities();
            List<ERFModel> searchResults = new List<ERFModel>();
            try
            {

                //---------------------------------------
                string constr = ConfigurationManager.ConnectionStrings["FB_Connection"].ConnectionString;
                DataTable dt = new DataTable();

                DateTime currentDate = DateTime.Now;
                Double NoOfDaysFilter = Convert.ToDouble(ConfigurationManager.AppSettings["DaysFilter"]);

                string startDate = currentDate.AddDays(NoOfDaysFilter).ToString();

                string ErfDataQuery = ConfigurationManager.AppSettings["ERFDataQuery"];

                string sqlQuery = ErfDataQuery + " WHERE((dbo.erf.EntryDate >= '" + startDate + "')) ";
                //sqlQuery = sqlQuery + " GROUP BY dbo.WorkorderParts.WorkorderID, dbo.WorkorderParts.Sku, dbo.WorkorderParts.Description,dbo.WorkorderParts.Quantity, dbo.WorkorderParts.AssetID; ";

                using (SqlConnection con = new SqlConnection(constr))
                {
                    using (SqlCommand cmd = new SqlCommand(sqlQuery, con))
                    {
                        con.Open();
                        cmd.CommandTimeout = 3600;
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            da.Fill(dt);
                        }

                    }
                    con.Close();
                }

                //---------------------------------------

                
                foreach (DataRow dr in dt.Rows)
                {
                    ERFModel woresults = new ERFModel();

                    woresults.WorkorderID = dr["WorkorderID"] != DBNull.Value ? Convert.ToInt32(dr["WorkorderID"]) : 0;
                    woresults.CustomerID = dr["CustomerID"] != DBNull.Value ? Convert.ToInt32(dr["CustomerID"]) : 0;

                    //Contact customer = FarmerBrothersEntitites.Contacts.Where(con => con.ContactID == woresults.CustomerID).FirstOrDefault();


                    woresults.OriginatorName = dr["ERFEntryUser"] != DBNull.Value ? dr["ERFEntryUser"].ToString() : "";
                    woresults.ModifiedUser = dr["ERFLastUpdatedUser"] != DBNull.Value ? dr["ERFLastUpdatedUser"].ToString() : "";
                    woresults.ModifiedDate = dr["ERFLastUpdatedDate"] != DBNull.Value ? dr["ERFLastUpdatedDate"].ToString() : "";
                    woresults.CustomerName = dr["CompanyName"] != DBNull.Value ? dr["CompanyName"].ToString() : "";
                    woresults.CustomerCity = dr["City"] != DBNull.Value ? dr["City"].ToString() : "";
                    woresults.Address1 = dr["Address1"] != DBNull.Value ? dr["Address1"].ToString() : "";
                    woresults.CustomerState = dr["State"] != DBNull.Value ? dr["State"].ToString() : "";
                    woresults.CustomerZipCode = dr["PostalCode"] != DBNull.Value ? dr["PostalCode"].ToString() : "";

                    woresults.WorkorderCallstatus = dr["ERFStatus"] != DBNull.Value ? dr["ERFStatus"].ToString() : "";
                    woresults.AppointmentDate = dr["OriginalRequestedDate"] != DBNull.Value ? dr["OriginalRequestedDate"].ToString() : "";
                    woresults.WorkorderEntryDate = dr["ERFEntryDate"] != DBNull.Value ? dr["ERFEntryDate"].ToString() : "";
                    //woresults.WorkorderCloseDate = dr["WorkorderCloseDate"] != DBNull.Value ? dr["WorkorderCloseDate"].ToString() : "";

                    //woresults.EventCallTypeID = dr["WorkorderCalltypeid"] != DBNull.Value ? dr["WorkorderCalltypeid"].ToString() : "";
                    //woresults.WorkorderCalltypeDesc = dr["WorkorderCalltypeDesc"] != DBNull.Value ? dr["WorkorderCalltypeDesc"].ToString() : "";

                    string erfId = dr["ErfID"] != DBNull.Value ? dr["ErfID"].ToString() : "";

                    woresults.ERFNO = erfId;
                    woresults.FSMJDE = dr["FSMJDE"] != DBNull.Value ? dr["FSMJDE"].ToString() : "";
                    woresults.CustomerRegion = dr["CustomerRegion"] != DBNull.Value ? dr["CustomerRegion"].ToString() : "";
                    woresults.RegionNumber = dr["RegionNumber"] != DBNull.Value ? dr["RegionNumber"].ToString() : "";
                    woresults.CustomerBranch = dr["CustomerBranch"] != DBNull.Value ? dr["CustomerBranch"].ToString() : "";
                    woresults.Branch = dr["Branch"] != DBNull.Value ? dr["Branch"].ToString() : "";

                    woresults.OrderType = dr["OrderType"] != DBNull.Value ? dr["OrderType"].ToString() : "";
                    woresults.ShipToBranch = dr["ShipToBranch"] != DBNull.Value ? dr["ShipToBranch"].ToString() : "";
                    woresults.SiteReady = dr["SiteReady"] != DBNull.Value ? dr["SiteReady"].ToString() : "";

                    woresults.TotalNSV = dr["TotalNSV"] != DBNull.Value ? dr["TotalNSV"].ToString() : "";

                    woresults.ApprovalStatus = dr["ApprovalStatus"] != DBNull.Value ? dr["ApprovalStatus"].ToString() : "";
                    woresults.ContactName = dr["CompanyName"] != DBNull.Value ? dr["CompanyName"].ToString() : "";
                    woresults.WOClosedDate = dr["WorkorderCloseDate"] != DBNull.Value ? dr["WorkorderCloseDate"].ToString() : "";
                    string status = dr["WorkorderCallstatus"] != DBNull.Value ? dr["WorkorderCallstatus"].ToString() : "";
                    woresults.WOStatus = status;
                    woresults.EqpType = dr["EqpType"] != DBNull.Value ? dr["EqpType"].ToString() : ""; ;
                    woresults.EqpName = dr["EqpName"] != DBNull.Value ? dr["EqpName"].ToString() : ""; ;
                    woresults.EqpCategoryName = dr["EqpCategoryName"] != DBNull.Value ? dr["EqpCategoryName"].ToString() : ""; ;
                    woresults.ExpType = dr["ExpType"] != DBNull.Value ? dr["ExpType"].ToString() : ""; ;
                    woresults.ExpName = dr["ExpName"] != DBNull.Value ? dr["ExpName"].ToString() : ""; ;
                    woresults.ExpCategoryName = dr["ExpCategoryName"] != DBNull.Value ? dr["ExpCategoryName"].ToString() : "";
                    woresults.EqpInternalOrderType = dr["EqpInternalType"] != DBNull.Value ? dr["EqpInternalType"].ToString() : "";
                    woresults.EqpVendorOrderType = dr["EqpVendorType"] != DBNull.Value ? dr["EqpVendorType"].ToString() : "";
                    woresults.ExpInternalOrderType = dr["ExpInternalType"] != DBNull.Value ? dr["ExpInternalType"].ToString() : "";
                    woresults.ExpVendorOrderType = dr["ExpVendorType"] != DBNull.Value ? dr["ExpVendorType"].ToString() : "";
                    woresults.EqpQty = dr["EqpQty"] != DBNull.Value ? dr["EqpQty"].ToString() : "";
                    woresults.ExpQty = dr["ExpQty"] != DBNull.Value ? dr["ExpQty"].ToString() : "";
                    woresults.DispatchDate = dr["DispatchDate"] != DBNull.Value ? dr["DispatchDate"].ToString() : "";
                    if (!string.IsNullOrEmpty(status) && (status.ToLower() == "accepted" || status.ToLower() == "completed" || status.ToLower() == "closed"))
                    {
                        woresults.AcceptedDate = dr["AcceptedDate"] != DBNull.Value ? dr["AcceptedDate"].ToString() : "";
                    }
                    else
                    {
                        woresults.AcceptedDate = "";
                    }
                    woresults.DispatchTech = dr["DispatchTech"] != DBNull.Value ? dr["DispatchTech"].ToString() : "";

                    decimal? eqpTotal = erfId == "" ? 0 : fbEntity.FBERFEquipments.Where(eqp => eqp.ERFId == erfId).Sum(x => x.TotalCost);
                    decimal? expTotal = erfId == "" ? 0 : fbEntity.FBERFExpendables.Where(eqp => eqp.ERFId == erfId).Sum(x => x.TotalCost);

                    decimal GrandTotal = Convert.ToDecimal(eqpTotal + expTotal);

                    woresults.EqpTotal = eqpTotal.ToString();
                    woresults.ExpTotal = expTotal.ToString();

                    if (eqpTotal != null && expTotal != null)
                    {
                        woresults.Total = (eqpTotal + expTotal).ToString();
                    }
                    else if (eqpTotal != null && expTotal == null)
                    {
                        woresults.Total = (eqpTotal).ToString();
                    }
                    else if (eqpTotal == null && expTotal != null)
                    {
                        woresults.Total = (expTotal).ToString();
                    }
                    else
                    {
                        woresults.Total = "";
                    }

                    woresults.Tracking = dr["Tracking"] != DBNull.Value ? dr["Tracking"].ToString() : "";

                    searchResults.Add(woresults);
                }
                
            }
            catch (Exception ex)
            {
            }
            return searchResults;
        }

    }

    class TechModel
    {
        public string TechId { get; set; }
        public string CompanyName { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public bool IsActive { get; set; }
        public string FamilyAff { get; set; }
        public string ESM { get; set; }
        public string BranchNumber { get; set; }
        public string BranchName { get; set; }
        public string RegionNumber { get; set; }
        public string RegionName { get; set; }


        public List<TechModel> GetTechnicianListData()
        {
            List<TechModel> techList = new List<TechModel>();
            try
            {
                string constr = ConfigurationManager.ConnectionStrings["FB_Connection"].ConnectionString;
                DataTable dt = new DataTable();

                string TechDataQuery = ConfigurationManager.AppSettings["TechDataQuery"];

                using (SqlConnection con = new SqlConnection(constr))
                {
                    using (SqlCommand cmd = new SqlCommand(TechDataQuery, con))
                    {
                        con.Open();
                        cmd.CommandTimeout = 3600;
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            da.Fill(dt);
                        }

                    }
                    con.Close();
                }

                foreach (DataRow dr in dt.Rows)
                {
                    TechModel th = new TechModel();
                    th.TechId = dr["DealerId"] == DBNull.Value ? "0" : dr["DealerId"].ToString();
                    th.CompanyName = dr["CompanyName"] == DBNull.Value ? "" : dr["CompanyName"].ToString();
                    th.City = dr["City"] == DBNull.Value ? "" : dr["City"].ToString();
                    th.State = dr["State"] == DBNull.Value ? "" : dr["State"].ToString();
                    th.IsActive = Convert.ToBoolean(dr["IsActive"]);
                    th.FamilyAff = dr["FamilyAff"] == DBNull.Value ? "" : dr["FamilyAff"].ToString();
                    th.ESM = dr["FieldServiceManager"] == DBNull.Value ? "" : dr["FieldServiceManager"].ToString();
                    th.BranchNumber = dr["BranchNumber"] == DBNull.Value ? "" : dr["BranchNumber"].ToString();
                    th.BranchName = dr["BranchName"] == DBNull.Value ? "" : dr["BranchName"].ToString();
                    th.RegionNumber = dr["RegionNumber"] == DBNull.Value ? "" : dr["RegionNumber"].ToString();
                    th.RegionName = dr["RegionName"] == DBNull.Value ? "" : dr["RegionName"].ToString();

                    techList.Add(th);
                }

            }
            catch (Exception ex)
            {

            }

            return techList;
        }
    }
}
