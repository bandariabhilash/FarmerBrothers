using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FBPowerReport
{
    static class Program
    {   
        [STAThread]
        static void Main()
        {            
            PowerReportDataModel pm = new PowerReportDataModel();

            //Not in Use
            /*List<PowerReportDataModel> prm = pm.GetReportsListData();
            Program.WriteTextFile(prm);*/


            WorkorderInfoModel WIM = new WorkorderInfoModel();
            WorkorderEquipmentModel WEM = new WorkorderEquipmentModel();
            WorkorderPartsModel WPM = new WorkorderPartsModel();

            List<WorkorderInfoModel> wi = WIM.GetWorkordersListData();
            List<WorkorderEquipmentModel> we = WEM.GetWorkorderEquipmentsListData();
            List<WorkorderPartsModel> wp = WPM.GetWorkorderPartsListData();

            WriteDataToTextFile(wi, we, wp);

            ERFModel erf = new ERFModel();
            List<ERFModel> erfData = erf.GetERFData();
            WriteERFDataToTextFile(erfData);

            TechModel th = new TechModel();
            List<TechModel> techData = th.GetTechnicianListData();
            WriteTechDataToTextFile(techData);
        }

        /*public static void WriteTextFile(List<PowerReportDataModel> TxtDataList)
        {
            string delimiter = "|";
            string FilePath = ConfigurationManager.AppSettings["OutputFilePath"];

            DateTime currentDatetime = DateTime.Now;

            string FileName = "FBDATA" + currentDatetime.ToString("MMddyyyyHHmm"); //ConfigurationManager.AppSettings["OutputFileName"];
            //string FileName = "FBDATAHistoricalData_20180101-20190804_With_ModifiedQuery";

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(FilePath + FileName + ".txt"))
            {
                string headerLine = "Workorder#" + delimiter + "Entry Date" + delimiter + "Customer#" + delimiter + "WO Status" + delimiter + "Purchase Order"
                        + delimiter + "BillingId" + delimiter + "Erf# " + delimiter + "CloseDate" + delimiter + "Eqp Count" + delimiter + "Customer State"
                        + delimiter + "ThirdParty PO" + delimiter + "Estimate" + delimiter + "Final Estimate" + delimiter + "JR Events" + delimiter + "Original WO#"
                        + delimiter + "CallType#" + delimiter + "Tech Called" + delimiter + "DESCF" + delimiter + "Appointment Date" + delimiter + "Start Date"
                        + delimiter + "Arrival Date" + delimiter + "Completion Date" + delimiter + "ERF OriginalReq Date" + delimiter + "ERF ReceivedDate"
                        + delimiter + "Project#" + delimiter + "Eqp ETADate" + delimiter + "ERF Completedate" + delimiter + "No Service Required" + delimiter + "No Service Reason"
                        + delimiter + "Tech#" + delimiter + "Tech Name" + delimiter + "Schedule Date" + delimiter + "Tech State" + delimiter + "FSM#" + delimiter + "Dispatch Tech#"
                        + delimiter + "Dispatch TechName" + delimiter + "Workorder Calltype Desc" + delimiter + "FieldServiceManager" + delimiter + "FSMJDE" + delimiter + "CompanyName"
                        + delimiter + "SolutionId" + delimiter + "Systemid" + delimiter + "Symptomid" + delimiter + "SerialNumber" + delimiter + "ProdNo" + delimiter + "Manufacturer"
                        + delimiter + "Category" + delimiter + "InvoiceNo" + delimiter + "FamilyAff" + delimiter + "SearchType" + delimiter + "SearchDesc" + delimiter + "ServicePriority"
                        + delimiter + "EventScheduleDate";
                           
                file.WriteLine(headerLine);
                foreach (PowerReportDataModel txtItem in TxtDataList)
                {
                    string line = txtItem.WorkorderId + delimiter + txtItem.EntryDate + delimiter + txtItem.Customerid + delimiter + txtItem.Status + delimiter + txtItem.PurchaseOrder 
                        + delimiter + txtItem.BillingId + delimiter + txtItem.ErfId + delimiter + txtItem.CloseDate + delimiter + txtItem.EqpCount + delimiter + txtItem.State 
                        + delimiter + txtItem.ThirdPartyPO + delimiter + txtItem.Estimate + delimiter + txtItem.FinalEstimate + delimiter + txtItem.JREvents + delimiter + txtItem.OriginalWOId
                        + delimiter + txtItem.CallTypeId + delimiter + txtItem.TechCalled + delimiter + txtItem.DESCF + delimiter + txtItem.AppointmentDate + delimiter + txtItem.StartTime
                        + delimiter + txtItem.ArrivalTime + delimiter + txtItem.CompletionTime + delimiter + txtItem.ERFOriginalReqDate + delimiter + txtItem.ERFReceivedDate 
                        + delimiter + txtItem.ProjectNumber + delimiter + txtItem.EqpETADate + delimiter + txtItem.ERFCompletedate + delimiter + txtItem.NoServiceRequired + delimiter + txtItem.NoServiceReason 
                        + delimiter + txtItem.Techid + delimiter + txtItem.Techname + delimiter + txtItem.ScheduleDate + delimiter + txtItem.TechState + delimiter + txtItem.FSMID + delimiter + txtItem.DispatchTechID 
                        + delimiter + txtItem.DispatchTechName + delimiter + txtItem.CallTypeDescription + delimiter + txtItem.FieldServiceManager + delimiter + txtItem.FSMJDE + delimiter + txtItem.CompanyName
                        + delimiter + txtItem.SolutionId + delimiter + txtItem.SystemId + delimiter + txtItem.SymptomId + delimiter + txtItem.serialNumber + delimiter + txtItem.ProductNumber + delimiter + txtItem.Manufacturer
                        + delimiter + txtItem.Catagory + delimiter + txtItem.InvoiceNumber + delimiter + txtItem.FamilyAff + delimiter + txtItem.SearchType + delimiter + txtItem.SearchDesc + delimiter + txtItem.ServicePriority
                        + delimiter + txtItem.EventScheduleDate;

                    file.WriteLine(line);
                }
            }


        }
        */

        public static void WriteDataToTextFile(List<WorkorderInfoModel> WIMList, List<WorkorderEquipmentModel> WEMList, List<WorkorderPartsModel> WPMList)
        {
            string delimiter = "|";
            string FilePath = ConfigurationManager.AppSettings["OutputFilePath"];

            DateTime currentDatetime = DateTime.Now;

            //string DirName = "FBDATA" + currentDatetime.ToString("MMddyyyyHHmm");
            string DirName = "FBDATA" + currentDatetime.ToString("MMddyyyy");

            string WorkorderInfoFileName = "FBWorkorderInfoDATA";// + currentDatetime.ToString("MMddyyyyHHmm"); //Removed timestam as per the request by "Meyyappan" (Sub : Request to Modify File Naming Convention) on May 14, 2025 
            string WorkorderEqpFileName = "FBWorkorderEqpDATA";// + currentDatetime.ToString("MMddyyyyHHmm");
            string WorkorderPartsFileName = "FBWorkorderPartsDATA";// + currentDatetime.ToString("MMddyyyyHHmm");


            //string DirName = "FBDATA030820200200";

            //string WorkorderInfoFileName = "FBWorkorderInfoDATA030820200200";
            //string WorkorderEqpFileName = "FBWorkorderEqpDATA030820200200";
            //string WorkorderPartsFileName = "FBWorkorderPartsDATA030820200200";

            /*string DirName = "FBDATAHistoricalData_20180701-20191014";
            string WorkorderInfoFileName = "FBWorkorderInfoHistoricalDATA" + currentDatetime.ToString("MMddyyyyHHmm");
            string WorkorderEqpFileName = "FBWorkorderEqpHistoricalDATA" + currentDatetime.ToString("MMddyyyyHHmm");
            string WorkorderPartsFileName = "FBWorkorderPartsHistoricalDATA" + currentDatetime.ToString("MMddyyyyHHmm");*/


            string DirPath = FilePath + DirName;
            System.IO.Directory.CreateDirectory(DirPath);


            string host = "sftp.farmerbros.com";
            int port = 22;
            string username = "fbmars-sftp";
            string privateKeyPath = @"D:\Websites\FarmerBrothers\FarmerBrothersSourceCode\FBPowerReport\fbmars-sftp-private-key.ppk";
            string SFTPFBDataFilePath = "Outgoing/FBDATA/";

            var privateKey = new PrivateKeyFile(privateKeyPath);
            var authMethod = new PrivateKeyAuthenticationMethod(username, privateKey);

            var connectionInfo = new ConnectionInfo(host, port, username, authMethod);

            using (var sftp = new SftpClient(connectionInfo))
            {
                sftp.Connect();

                string WOInfoFileName = WorkorderInfoFileName + ".txt";
                string WOEqpFileName = WorkorderEqpFileName + ".txt";
                string WOPartsFileName = WorkorderPartsFileName + ".txt";

                string sftpDir = SFTPFBDataFilePath + DirName + "/";
                sftp.CreateDirectory(sftpDir);


                string infoFilePath = DirPath + "\\" + WOInfoFileName;
                using (System.IO.StreamWriter WIfile = new System.IO.StreamWriter(infoFilePath))
                {
                    string WIHeaderLine = "WorkorderID" + delimiter + "WorkorderCallstatus" + delimiter + "CustomerID" + delimiter + "WorkorderEntryDate" + delimiter + "WorkorderCloseDate" +
                                delimiter + "WorkorderErfid" + delimiter + "WorkorderCalltypeid" + delimiter + "WorkorderCalltypeDesc" + delimiter + "WorkorderSpawnEvent" +
                                delimiter + "WorkorderClosureConfirmationNo" + delimiter + "PurchaseOrder" + delimiter + "CallerName" + delimiter + "OvertimeRequest" +
                                delimiter + "WorkorderContactPhone" + delimiter + "WorkorderContactName" + delimiter + "NoServiceRequired" + delimiter + "ParentWorkorderid" +
                                delimiter + "ThirdPartyPO" + delimiter + "Techid" + delimiter + "ScheduleDate" + delimiter + "TechName" +
                                delimiter + "TechPhone" + delimiter + "ServiceCenterName" + delimiter + "ServiceCenterID" + delimiter + "AppointmentDate" +
                                delimiter + "SearchType" + delimiter + "FamilyAff" + delimiter + "FSMJDE" + delimiter + "FieldServiceManager" +
                                delimiter + "StartDateTime" + delimiter + "ArrivalDateTime" + delimiter + "CompletionDateTime" + delimiter + "OriginalRequestedDate" +
                                delimiter + "RepeatCallEvent" + delimiter + "WorkorderEquipCount" + delimiter + "PPID" + delimiter + "PPIDDesc" +
                                delimiter + "ServicePriority" + delimiter + "FilterReplaced" + delimiter + "FilterReplacedDate" + delimiter + "NextFilterReplacementDate" +
                                delimiter + "WaterTested" + delimiter + "HardnessRating" + delimiter + "RescheduleReason" + delimiter + "RepeatRepair";

                    WIfile.WriteLine(WIHeaderLine);
                    foreach (WorkorderInfoModel txtItem in WIMList)
                    {
                        string line = txtItem.WorkorderID + delimiter + txtItem.WorkorderCallstatus + delimiter + txtItem.CustomerID + delimiter + txtItem.WorkorderEntryDate + delimiter + txtItem.WorkorderCloseDate
                            + delimiter + txtItem.WorkorderErfid + delimiter + txtItem.WorkorderCalltypeid + delimiter + txtItem.WorkorderCalltypeDesc + delimiter + txtItem.WorkorderSpawnEvent
                            + delimiter + txtItem.WorkorderClosureConfirmationNo + delimiter + txtItem.PurchaseOrder + delimiter + txtItem.CallerName + delimiter + txtItem.OvertimeRequest
                            + delimiter + txtItem.WorkorderContactPhone + delimiter + txtItem.WorkorderContactName + delimiter + txtItem.NoServiceRequired + delimiter + txtItem.ParentWorkorderid
                            + delimiter + txtItem.ThirdPartyPO + delimiter + txtItem.Techid + delimiter + txtItem.ScheduleDate + delimiter + txtItem.TechName
                            + delimiter + txtItem.TechPhone + delimiter + txtItem.ServiceCenterName + delimiter + txtItem.ServiceCenterID + delimiter + txtItem.AppointmentDate
                            + delimiter + txtItem.SearchType + delimiter + txtItem.FamilyAff + delimiter + txtItem.FSMJDE + delimiter + txtItem.FieldServiceManager
                            + delimiter + txtItem.StartDateTime + delimiter + txtItem.ArrivalDateTime + delimiter + txtItem.CompletionDateTime + delimiter + txtItem.OriginalRequestedDate
                            + delimiter + txtItem.RepeatCallEvent + delimiter + txtItem.WorkorderEquipCount + delimiter + txtItem.PPID + delimiter + txtItem.PPIDDesc
                            + delimiter + txtItem.ServicePriority + delimiter + txtItem.FilterReplaced + delimiter + txtItem.FilterReplacedDate + delimiter + txtItem.NextFilterReplacementDate
                            + delimiter + txtItem.WaterTested + delimiter + txtItem.HardnessRating + delimiter + txtItem.RescheduleReason + delimiter + txtItem.RepeatRepair;

                        WIfile.WriteLine(line);
                    }
                    WIfile.Close();

                    using (var fileStream = new FileStream(infoFilePath, FileMode.Open))
                    {
                        sftp.UploadFile(fileStream, Path.Combine(sftpDir, WOInfoFileName));
                    }
                }

                string eqpFilePath = DirPath + "\\" + WOEqpFileName;
                using (System.IO.StreamWriter WEfile = new System.IO.StreamWriter(eqpFilePath))
                {
                    string WEHeaderLine = "WorkorderID" + delimiter + "Assetid" + delimiter + "CallTypeid" + delimiter +
                                         "Category" + delimiter + "Manufacturer" + delimiter + "Model" + delimiter +
                                         "Location" + delimiter + "SerialNumber" + delimiter + "Solutionid" + delimiter +
                                         "Temperature" + delimiter + "Systemid" + delimiter + "Symptomid";

                    WEfile.WriteLine(WEHeaderLine);
                    foreach (WorkorderEquipmentModel txtItem in WEMList)
                    {
                        string line = txtItem.WorkorderID + delimiter + txtItem.Assetid + delimiter + txtItem.CallTypeid
                            + delimiter + txtItem.Category + delimiter + txtItem.Manufacturer + delimiter + txtItem.Model
                            + delimiter + txtItem.Location + delimiter + txtItem.SerialNumber + delimiter + txtItem.Solutionid
                            + delimiter + txtItem.Temperature + delimiter + txtItem.Systemid + delimiter + txtItem.Symptomid;

                        WEfile.WriteLine(line);
                    }
                    WEfile.Close();

                    using (var fileStream = new FileStream(eqpFilePath, FileMode.Open))
                    {
                        sftp.UploadFile(fileStream, Path.Combine(sftpDir, WOEqpFileName));
                    }
                }

                string partsFilePath = DirPath + "\\" + WOPartsFileName;
                using (System.IO.StreamWriter WPfile = new System.IO.StreamWriter(partsFilePath))
                {
                    string WPHeaderLine = "WorkorderID" + delimiter + "Sku" + delimiter + "Description" + delimiter +
                                            "Quantity" + delimiter + "AssetID";

                    WPfile.WriteLine(WPHeaderLine);
                    foreach (WorkorderPartsModel txtItem in WPMList)
                    {
                        string line = txtItem.WorkorderID + delimiter + txtItem.Sku + delimiter + txtItem.Description
                            + delimiter + txtItem.Quantity + delimiter + txtItem.AssetID;

                        WPfile.WriteLine(line);
                    }
                    WPfile.Close();

                    using (var fileStream = new FileStream(partsFilePath, FileMode.Open))
                    {
                        sftp.UploadFile(fileStream, Path.Combine(sftpDir, WOPartsFileName));
                    }
                }

                sftp.Disconnect();
            }
        }

        public static void WriteERFDataToTextFile(List<ERFModel> erfDataList)
        {
            string delimiter = "|";
            string FilePath = ConfigurationManager.AppSettings["ERFOutputFilePath"];

            DateTime currentDatetime = DateTime.Now;

            string DirName = "FBERFDATA" + currentDatetime.ToString("MMddyyyyHHmm");

            string ERFFileName = "FBERFDATA" + currentDatetime.ToString("MMddyyyyHHmm");


            string DirPath = FilePath + DirName;
            System.IO.Directory.CreateDirectory(DirPath);
            string erfFile = ERFFileName + ".txt";
            string erfFilePath = DirPath + "\\" + erfFile;
            using (System.IO.StreamWriter WIfile = new System.IO.StreamWriter(erfFilePath))
            {
                string WIHeaderLine = "WorkorderID" + delimiter + "ERFID" + delimiter + "OriginatorName" + delimiter + "CompanyName" + delimiter + "WorkorderCloseDate" + delimiter + "WorkorderCallstatus" +
                    delimiter + "ERFStatus" + delimiter + "CustomerID" + delimiter + "ERFEntryDate" + delimiter + "ERFLastUpdatedUser" + delimiter + "ERFLastUpdatedDate" +
                    delimiter + "OrderType" + delimiter + "ShipToBranch" + delimiter + "SiteReady" + delimiter + "TotalNSV" + delimiter + "ApprovalStatus" + delimiter + "Address1" +
                    delimiter + "City" + delimiter + "State" + delimiter + "PostalCode" + delimiter + "FSMJDE" + delimiter + "CustomerRegion" + delimiter + "RegionNumber" +
                    delimiter + "CustomerBranch" + delimiter + "Branch" + delimiter + "AppointmentDate" + delimiter + "EqpQty" + delimiter + "EquipmentTotal" +
                    delimiter + "ExpQty" + delimiter + "ExpandableTotal" + delimiter + "Total" + delimiter + "EqpType" + delimiter + "EqpName" + delimiter + "EqpCategoryName" +
                    delimiter + "ExpType" + delimiter + "ExpName" + delimiter + "ExpCategoryName" + delimiter + "EqpInternalType" + delimiter + "EqpVendorType" +
                    delimiter + "ExpInternalType" + delimiter + "ExpVendorType" + delimiter + "DispatchDate" + delimiter + "AcceptedDate" + delimiter + "DispatchTech" + delimiter + "Tracking";



                WIfile.WriteLine(WIHeaderLine);
                foreach (ERFModel txtItem in erfDataList)
                {
                    string line = txtItem.WorkorderID + delimiter + txtItem.ERFNO + delimiter + txtItem.OriginatorName + delimiter + txtItem.CustomerName + delimiter + txtItem.WOClosedDate + delimiter + txtItem.WOStatus
                        + delimiter + txtItem.WorkorderCallstatus + delimiter + txtItem.CustomerID + delimiter + txtItem.WorkorderEntryDate + delimiter + txtItem.ModifiedUser + txtItem.ModifiedDate
                        + delimiter + txtItem.OrderType + delimiter + txtItem.ShipToBranch + delimiter + txtItem.SiteReady + delimiter + txtItem.TotalNSV + delimiter + txtItem.ApprovalStatus + delimiter + txtItem.Address1
                        + delimiter + txtItem.CustomerCity + delimiter + txtItem.CustomerState + delimiter + txtItem.CustomerZipCode + delimiter + txtItem.FSMJDE + delimiter + txtItem.CustomerRegion + delimiter + txtItem.RegionNumber
                        + delimiter + txtItem.CustomerBranch + delimiter + txtItem.Branch + delimiter + txtItem.AppointmentDate + delimiter + txtItem.EqpQty + delimiter + txtItem.EqpTotal
                        + delimiter + txtItem.ExpQty + delimiter + txtItem.ExpTotal + delimiter + txtItem.Total + delimiter + txtItem.EqpType + delimiter + txtItem.EqpName + delimiter + txtItem.EqpCategoryName
                        + delimiter + txtItem.ExpType + delimiter + txtItem.ExpName + delimiter + txtItem.ExpCategoryName + delimiter + txtItem.EqpInternalOrderType + delimiter + txtItem.EqpVendorOrderType
                        + delimiter + txtItem.ExpInternalOrderType + delimiter + txtItem.ExpVendorOrderType + delimiter + txtItem.DispatchDate + delimiter + txtItem.AcceptedDate + delimiter + txtItem.DispatchTech + delimiter + txtItem.Tracking;

                    WIfile.WriteLine(line);
                }
            }

            string host = "sftp.farmerbros.com";
            int port = 22;
            string username = "fbmars-sftp";
            string privateKeyPath = @"D:\Websites\FarmerBrothers\FarmerBrothersSourceCode\FBPowerReport\fbmars-sftp-private-key.ppk";
            string SFTPErfFilePath = "Outgoing/ERFData/";

            var privateKey = new PrivateKeyFile(privateKeyPath);
            var authMethod = new PrivateKeyAuthenticationMethod(username, privateKey);

            var connectionInfo = new ConnectionInfo(host, port, username, authMethod);

            using (var sftp = new SftpClient(connectionInfo))
            {
                sftp.Connect();

                string sftpDir = SFTPErfFilePath + DirName + "/";
                sftp.CreateDirectory(sftpDir);

                using (var fileStream = new FileStream(erfFilePath, FileMode.Open))
                {
                    sftp.UploadFile(fileStream, Path.Combine(sftpDir, erfFile));
                }

                sftp.Disconnect();
            }
        }

        public static void WriteTechDataToTextFile(List<TechModel> techDataList)
        {
            string delimiter = "|";
            string FilePath = ConfigurationManager.AppSettings["TechOutputFilePath"];

            DateTime currentDatetime = DateTime.Now;

            if (!Directory.Exists(FilePath))
            {
                Directory.CreateDirectory(FilePath);
            }

            string TechFileName = "FBTECHDATA" + currentDatetime.ToString("MMddyyyyHHmm");
            string TechFilePath = FilePath + "\\" + TechFileName + ".txt";


            using (System.IO.StreamWriter WIfile = new System.IO.StreamWriter(TechFilePath))
            {
                string WIHeaderLine = "TechID" + delimiter + "Company Name" + delimiter + "City" + delimiter + "State" + delimiter + "IsActive" + delimiter + "FamilyAff" +
                    delimiter + "ESM" + delimiter + "Branch Number" + delimiter + "Branch Name" + delimiter + "Region Number" + delimiter + "Region Name";



                WIfile.WriteLine(WIHeaderLine);
                foreach (TechModel txtItem in techDataList)
                {
                    string line = txtItem.TechId + delimiter + txtItem.CompanyName + delimiter + txtItem.City + delimiter + txtItem.State + delimiter + txtItem.IsActive + delimiter + txtItem.FamilyAff
                        + delimiter + txtItem.ESM + delimiter + txtItem.BranchNumber + delimiter + txtItem.BranchName + delimiter + txtItem.RegionNumber + txtItem.RegionName;

                    WIfile.WriteLine(line);
                }
            }

            string host = "sftp.farmerbros.com";
            int port = 22;
            string username = "fbmars-sftp";
            string privateKeyPath = @"D:\Websites\FarmerBrothers\FarmerBrothersSourceCode\FBPowerReport\fbmars-sftp-private-key.ppk";
            string SFTPTechFilePath = "Outgoing/TechData/";

            var privateKey = new PrivateKeyFile(privateKeyPath);
            var authMethod = new PrivateKeyAuthenticationMethod(username, privateKey);

            var connectionInfo = new ConnectionInfo(host, port, username, authMethod);

            using (var sftp = new SftpClient(connectionInfo))
            {
                sftp.Connect();

                using (var fileStream = new FileStream(TechFilePath, FileMode.Open))
                {
                    sftp.UploadFile(fileStream, Path.Combine(SFTPTechFilePath, TechFileName + ".txt"));
                }

                sftp.Disconnect();
            }
        }

    }
}
