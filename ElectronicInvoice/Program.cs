using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.AxHost;

namespace ElectronicInvoice
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            //sendElectronicMail(2017837231, "abhilashb@mktalt.com#abhilash@teqdatum.com"); return;
            //sendElectronicMail(2017425319, "abhilashb@mktalt.com#jfraser@farmerbros.com;ram@maibpo.com"); return;
            //sendElectronicMail(2017671765, "abhilashb@mktalt.com"); return;
            FB_DevEntities fbEntities = new FB_DevEntities();
            DateTime dateRange = DateTime.Now.AddDays(-30);

            List<WorkOrder> WOList = fbEntities.WorkOrders.Where(wo => wo.WorkorderCalltypeid != 1820 && wo.WorkorderCallstatus == "Closed"
            && (wo.ElectronicEmailSent == false || wo.ElectronicEmailSent == null)
            && wo.NoServiceRequired != true && wo.WorkorderCloseDate > dateRange).ToList();

            if (WOList != null)
            {
                foreach (WorkOrder wrkord in WOList)
                {
                    string toMailAddress = "";
                    WorkorderDetail wrd = fbEntities.WorkorderDetails.Where(wd => wd.WorkorderID == wrkord.WorkorderID).FirstOrDefault();

                    Contact contact = fbEntities.Contacts.Where(c => c.ContactID == wrkord.CustomerID).FirstOrDefault();
                    if (contact != null)
                    {
                        //Placed this condition on Mike's request on "04-21-2022"
                        if (!string.IsNullOrEmpty(contact.CompanyName) && contact.CompanyName.ToLower() == "franke")
                        {
                            toMailAddress += ";fs-servicecenter.us@franke.com";
                        }
                        else if(contact.PricingParentID == "9001234") //Placed this condition on Mike's request on "11-10-2022"
                        {
                            toMailAddress += ";JBaldwin@cadillaccoffee.com;MMachnak@cadillaccoffee.com";
                        }
                        else if (contact.PricingParentID == "9001245") //Placed this condition on Mike's request on "11-10-2022"
                        {
                            toMailAddress += ";mvoss@AVIFoodsystems.com";
                        }
                        else if (contact.PricingParentID == "9001239") //Placed this condition on Mike's request on "11-10-2022"
                        {
                            toMailAddress += ";dispatch-us@seb-professional.com";
                        }
                        else
                        {
                            if (wrd != null && !string.IsNullOrEmpty(wrd.CustomerEmail))
                            {
                                toMailAddress += ";" + wrd.CustomerEmail;
                            }
                            if (!string.IsNullOrEmpty(wrkord.CustomerMainEmail))
                            {
                                toMailAddress += ";" + wrkord.CustomerMainEmail;
                            }
                        }
                    }
                    else
                    {
                        if (wrd != null && !string.IsNullOrEmpty(wrd.CustomerEmail))
                        {
                            toMailAddress += ";" + wrd.CustomerEmail;
                        }
                        if (!string.IsNullOrEmpty(wrkord.CustomerMainEmail))
                        {
                            toMailAddress += ";" + wrkord.CustomerMainEmail;
                        }
                    }

                    string CCEmail = ConfigurationManager.AppSettings["EmailCC"];
                    if (toMailAddress.Contains("jamie.adler@state.mn.us"))
                    {
                        if (!string.IsNullOrEmpty(CCEmail))
                        {
                            toMailAddress += "#" + CCEmail;
                        }
                    }
                    else if(contact.PricingParentID == "9001201")
                    {
                        toMailAddress += "#noelle.kawaguchi@nordstrom.com";
                    }
                    else if (contact.PricingParentID == "9001217")
                    {
                        toMailAddress += "#Veronique.voyer@evocagroup.com";
                    }
                    else if (contact.PricingParentID == "9250992")
                    {
                        toMailAddress += "#mgalicia@farmerbros.com";
                    }
                    else if (contact.PricingParentID == "9001209")
                    {
                        toMailAddress += "#servicecall@floridasnatural.com";
                    }
                    else if (contact.PricingParentID == "9001259")
                    {
                        toMailAddress += "#farmerbros@vivreau.com";
                    }
                    else if (contact.PricingParentID == "7380755")
                    {
                        toMailAddress += "#tim.brigham@ellisnv.com";
                    }

                    if (ConfigurationManager.AppSettings["UseTestMails"].ToLower() == "true")
                    {
                        toMailAddress = ConfigurationManager.AppSettings["TestEmail"];
                    }

                    if (!string.IsNullOrEmpty(toMailAddress))
                    {   
                        bool emailSent = Program.sendElectronicMail(wrkord.WorkorderID, toMailAddress);
                        if (emailSent)
                        {
                            wrkord.ElectronicEmailSent = true;
                            fbEntities.SaveChanges();
                        }
                    }
                    else
                    {
                        wrkord.ElectronicEmailSent = true;
                        fbEntities.SaveChanges();
                    }
                }
            }
        }

        private static bool sendElectronicMail(int WorkorderId, string ToMailAddress)
        {
            try
            {
                MailMessage MyMailMessage = new MailMessage();
                MyMailMessage.From = new MailAddress(ConfigurationManager.AppSettings["DispatchMailFromAddress"]);
                string toAddress = ToMailAddress;

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
                                    MyMailMessage.CC.Add(new MailAddress(address));
                                }
                            }
                            string[] addresses = mailCCAddress[0].Split(';');
                            foreach (string address in addresses)
                            {
                                if (!string.IsNullOrWhiteSpace(address))
                                {
                                    MyMailMessage.To.Add(new MailAddress(address));
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
                                MyMailMessage.To.Add(new MailAddress(address));
                            }
                        }
                    }
                    //MyMailMessage.Bcc.Add(new MailAddress("abhilash@teqdatum.com"));

                    //MyMailMessage.To.Add(ToMailAddress);
                    MyMailMessage.Subject = "Work Performed Summary " /*"Electronic Invoice "*/ + WorkorderId;
                    MyMailMessage.IsBodyHtml = true;

                    string htmlContent = Program.GetEmailString(WorkorderId);

                    StringBuilder salesEmailBody = new StringBuilder();
                    //salesEmailBody.Append(@"<img src='cid:logo' width='15%' height='15%' style='float: right;margin-right: 100px;margin-bottom: 10px;'>");
                    salesEmailBody.Append(htmlContent);

                    FB_DevEntities fbEntities = new FB_DevEntities();
                    WorkorderDetail workOrderDetail = fbEntities.WorkorderDetails.Where(w => w.WorkorderID == WorkorderId).FirstOrDefault();

                    string CustomerSignature = ""; byte[] imageData = null;
                    if (workOrderDetail != null && !string.IsNullOrEmpty(workOrderDetail.CustomerSignatureDetails))
                    {
                        CustomerSignature = workOrderDetail.CustomerSignatureDetails == null ? "" : workOrderDetail.CustomerSignatureDetails.Trim();

                        string[] result = CustomerSignature.Split(new string[] { "base64," }, StringSplitOptions.None);
                        imageData = Convert.FromBase64String(result[1]);
                    }

                    string TechnicianSignature = ""; byte[] TechImageData = null;
                    if (workOrderDetail != null && !string.IsNullOrEmpty(workOrderDetail.TechnicianSignatureDetails))
                    {
                        TechnicianSignature = workOrderDetail.TechnicianSignatureDetails == null ? "" : workOrderDetail.TechnicianSignatureDetails.Trim();

                        string[] result = TechnicianSignature.Split(new string[] { "base64," }, StringSplitOptions.None);
                        TechImageData = Convert.FromBase64String(result[1]);
                    }

                    string contentId = Guid.NewGuid().ToString();
                    string logoPath = string.Empty;

                    logoPath = Path.GetFullPath(ConfigurationManager.AppSettings["ImagePath"]);

                    salesEmailBody = salesEmailBody.Replace("cid:logo", "cid:" + contentId);

                    AlternateView avHtml = null;
                    if (!string.IsNullOrEmpty(CustomerSignature) && imageData != null)
                    {
                        avHtml = AlternateView.CreateAlternateViewFromString
                           (salesEmailBody.ToString(), null, MediaTypeNames.Text.Plain);
                    }
                    else
                    {
                        avHtml = AlternateView.CreateAlternateViewFromString
                           (salesEmailBody.ToString(), null, MediaTypeNames.Text.Html);
                    }

                    LinkedResource inline = new LinkedResource(logoPath, MediaTypeNames.Image.Jpeg);
                    inline.ContentId = contentId;
                    avHtml.LinkedResources.Add(inline);

                    MyMailMessage.AlternateViews.Add(avHtml);

                    //======== 


                    if (!string.IsNullOrEmpty(CustomerSignature) && imageData != null)
                    {
                        string contentId1 = Guid.NewGuid().ToString();

                        salesEmailBody = salesEmailBody.Replace("cid:sig", "cid:" + contentId1);
                        AlternateView avHtml1 = null;
                        if (!string.IsNullOrEmpty(TechnicianSignature) && TechImageData != null)
                        {
                            avHtml1 = AlternateView.CreateAlternateViewFromString
                               (salesEmailBody.ToString(), null, MediaTypeNames.Text.Plain);
                        }
                        else
                        {
                            avHtml1 = AlternateView.CreateAlternateViewFromString
                               (salesEmailBody.ToString(), null, MediaTypeNames.Text.Html);
                        }

                        LinkedResource inline1 = new LinkedResource(new MemoryStream(imageData), MediaTypeNames.Image.Jpeg);
                        inline1.TransferEncoding = TransferEncoding.Base64;
                        inline1.ContentId = contentId1;
                        avHtml1.LinkedResources.Add(inline1);

                        MyMailMessage.AlternateViews.Add(avHtml1);
                    }

                    if (!string.IsNullOrEmpty(TechnicianSignature) && TechImageData != null)
                    {
                        string contentId2 = Guid.NewGuid().ToString();

                        salesEmailBody = salesEmailBody.Replace("cid:Techsig", "cid:" + contentId2);
                        AlternateView avHtml2 = AlternateView.CreateAlternateViewFromString
                               (salesEmailBody.ToString(), null, MediaTypeNames.Text.Html);

                        LinkedResource inline2 = new LinkedResource(new MemoryStream(TechImageData), MediaTypeNames.Image.Jpeg);
                        inline2.TransferEncoding = TransferEncoding.Base64;
                        inline2.ContentId = contentId2;
                        avHtml2.LinkedResources.Add(inline2);

                        MyMailMessage.AlternateViews.Add(avHtml2);
                    }


                    //========                  


                    using (var smtp = new SmtpClient())
                    {
                        smtp.Host = ConfigurationManager.AppSettings["MailServer"];
                        smtp.Port = Convert.ToInt32(ConfigurationManager.AppSettings["ServerPort"]);

                        try
                        {
                            smtp.Send(MyMailMessage);
                        }
                        catch (Exception ex)
                        {
                            return false;
                        }
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                //System.IO.File.WriteAllText(@"D:\Websites\FarmerBrothers\FarmerBrothersSourceCode\ElectronicInvoiceTest\bin\Release\LogFolder\Log.txt", ex.Message);
                return false;
            }
        }

        private static string GetEmailString(int WorkorderID)
        {
            FB_DevEntities fbEntities = new FB_DevEntities();

            string htmlString = string.Empty;

            WorkOrder workorder = fbEntities.WorkOrders.Where(wo => wo.WorkorderID == WorkorderID).FirstOrDefault();
            string CustomerAcNo = "", WOEntryDate = "", CustomerName = "", CustomerAddress = "", CustomerCity = "", CustomerPO = "", CallPriority = "";
            string CustomerState = "", CustomerZipCode = "", WOContactNm = "", WOContactPh = "", WorkorderId = "", TotalUnitPrice = "", WOEntryUser = "";
            if (workorder != null)
            {
                CustomerAcNo = workorder.CustomerID == null ? "" : workorder.CustomerID.ToString().Trim();
                WOEntryDate = workorder.WorkorderEntryDate == null ? "" : workorder.WorkorderEntryDate.ToString().Trim();
                CustomerName = workorder.CustomerName == null ? "" : workorder.CustomerName.ToString().Trim();
                CustomerAddress = workorder.CustomerAddress == null ? "" : workorder.CustomerAddress.ToString().Trim();
                CustomerCity = workorder.CustomerCity == null ? "" : workorder.CustomerCity.ToString().Trim();
                CustomerState = workorder.CustomerState == null ? "" : workorder.CustomerState.ToString().Trim();
                CustomerZipCode = workorder.CustomerZipCode == null ? "" : workorder.CustomerZipCode.ToString().Trim();
                WOContactNm = workorder.CustomerMainContactName == null ? "" : workorder.CustomerMainContactName.ToString().Trim();
                WOContactPh = workorder.CustomerPhone == null ? "" : workorder.CustomerPhone.ToString().Trim();
                WorkorderId = workorder.WorkorderID.ToString().Trim();
                TotalUnitPrice = workorder.TotalUnitPrice == null ? "" : workorder.TotalUnitPrice.ToString().Trim();
                CustomerPO = workorder.CustomerPO == null ? "" : workorder.CustomerPO;
                WOEntryUser = workorder.EntryUserName == null ? "" : workorder.EntryUserName;

                if (workorder.PriorityCode != null)
                {
                    AllFBStatu afb = fbEntities.AllFBStatus.Where(f => f.FBStatusID == workorder.PriorityCode).FirstOrDefault();
                    CallPriority = afb == null ? "" : afb.FBStatus;
                }
                else
                {
                    CallPriority = "";
                }
            }

            string Route = "";
            Contact contact = fbEntities.Contacts.Where(c => c.ContactID == workorder.CustomerID).FirstOrDefault();
            Route = contact == null ? "" : contact.Route;

            WorkorderSchedule workorderSch = fbEntities.WorkorderSchedules.Where(w => w.WorkorderID == WorkorderID && w.AssignedStatus != "Redirected" && w.AssignedStatus != "Declined").FirstOrDefault();
            string WOScheduleTechName = "";
            if (workorderSch != null)
            {
                WOScheduleTechName = workorderSch.TechName == null ? "" : workorderSch.TechName.Trim();
            }

            WorkorderDetail workOrderDetail = fbEntities.WorkorderDetails.Where(w => w.WorkorderID == WorkorderID).FirstOrDefault();
            string TravelTime = "", StartTime = "", ArrivalTime = "", CompletionTime = "";
            string trvlTime = "", CustomerSignature = "", CustomerSignatureBy = "", TechnicianSignature = "";
            string StateOfEqp = "", ServiceDelay = "", TroubleshootSteps = "", Operational = "", UnderWarrenty = "", WarrentyFor = "", AdditionalFollowup = "", FollowupComments = "", ReviewedBy = "";
            string ServiceTime = "";
            if (workOrderDetail != null)
            {
                /*trvlTime = workOrderDetail.TravelTime == null ? "" : workOrderDetail.TravelTime.ToString();
                if (!string.IsNullOrEmpty(trvlTime))
                {
                    if (trvlTime.Contains(':'))
                    {
                        string[] timeSpllit = trvlTime.Split(':');
                        string hours = string.IsNullOrEmpty(timeSpllit[0]) ? "0" : timeSpllit[0].Trim();
                        string minutes = string.IsNullOrEmpty(timeSpllit[1]) ? "0" : timeSpllit[1].Trim();

                        TravelTime = hours + " Hrs : " + minutes + " Min";
                    }
                    else
                    {
                        TravelTime = trvlTime.Trim();
                    }
                }*/
                StateOfEqp = workOrderDetail.StateofEquipment == null ? "" : workOrderDetail.StateofEquipment.ToString().Trim();
                ServiceDelay = workOrderDetail.ServiceDelayReason == null ? "" : workOrderDetail.ServiceDelayReason.ToString().Trim();
                TroubleshootSteps = workOrderDetail.TroubleshootSteps == null ? "" : workOrderDetail.TroubleshootSteps.ToString().Trim();
                Operational = workOrderDetail.IsOperational == null ? "" : workOrderDetail.IsOperational.ToString().Trim();
                UnderWarrenty = workOrderDetail.IsUnderWarrenty == null ? "" : workOrderDetail.IsUnderWarrenty.ToString().Trim();
                WarrentyFor = workOrderDetail.WarrentyFor == null ? "" : workOrderDetail.WarrentyFor.ToString().Trim();
                AdditionalFollowup = workOrderDetail.AdditionalFollowupReq == null ? "" : workOrderDetail.AdditionalFollowupReq.ToString().Trim();
                FollowupComments = workOrderDetail.FollowupComments == null ? "" : workOrderDetail.FollowupComments.ToString().Trim();
                ReviewedBy = workOrderDetail.ReviewedBy == null ? "" : workOrderDetail.ReviewedBy.ToString().Trim();

                StartTime = workOrderDetail.StartDateTime == null ? "" : workOrderDetail.StartDateTime.ToString().Trim();
                ArrivalTime = workOrderDetail.ArrivalDateTime == null ? "" : workOrderDetail.ArrivalDateTime.ToString().Trim();
                CompletionTime = workOrderDetail.CompletionDateTime == null ? "" : workOrderDetail.CompletionDateTime.ToString().Trim();
                CustomerSignature = workOrderDetail.CustomerSignatureDetails == null ? "" : workOrderDetail.CustomerSignatureDetails.Trim();
                CustomerSignatureBy = workOrderDetail.CustomerSignatureBy == null ? "" : workOrderDetail.CustomerSignatureBy.Trim();
                TechnicianSignature = workOrderDetail.TechnicianSignatureDetails == null ? "" : workOrderDetail.TechnicianSignatureDetails.Trim();

                if (!string.IsNullOrEmpty(StartTime) && !string.IsNullOrEmpty(ArrivalTime))
                {
                    DateTime arrival = Convert.ToDateTime(ArrivalTime);
                    DateTime strt = Convert.ToDateTime(StartTime);
                    TimeSpan timeDiff = arrival.Subtract(strt);

                    trvlTime = timeDiff.Hours + " : " + timeDiff.Minutes;
                    TravelTime = timeDiff.Hours + " Hrs : " + timeDiff.Minutes + " Min";
                }
                else
                {
                    trvlTime = "0 : 0";
                    TravelTime = "0 Hrs : 0 Min";
                }

                if (!string.IsNullOrEmpty(CompletionTime) && !string.IsNullOrEmpty(ArrivalTime))
                {
                    DateTime arrival = Convert.ToDateTime(ArrivalTime);
                    DateTime cmplt = Convert.ToDateTime(CompletionTime);
                    TimeSpan servicetimeDiff = cmplt.Subtract(arrival);

                    ServiceTime = servicetimeDiff.Hours + " : " + servicetimeDiff.Minutes;
                }
                else
                {
                    ServiceTime = "0 : 0";
                }
            }

            List<eqpModel> eqpReqObjList = new List<eqpModel>();
            eqpReqObjList = (from WorkEquipment in fbEntities.WorkorderEquipmentRequesteds
                             join WorkType in fbEntities.WorkorderTypes
                             on WorkEquipment.CallTypeid equals WorkType.CallTypeID into temp1
                             from we in temp1.DefaultIfEmpty()
                             join Symptom in fbEntities.Symptoms
                             on WorkEquipment.Symptomid equals Symptom.SymptomID into sympt
                             from sy in sympt.DefaultIfEmpty()
                             where WorkEquipment.WorkorderID == WorkorderID
                             select new eqpModel()
                             {
                                 Assetid = WorkEquipment.Assetid,
                                 WorkOrderType = we.Description,
                                 Temperature = WorkEquipment.Temperature,
                                 Weight = WorkEquipment.Weight,
                                 Ratio = WorkEquipment.Ratio,
                                 WorkPerformedCounter = WorkEquipment.WorkPerformedCounter,
                                 WorkDescription = WorkEquipment.WorkDescription,
                                 Category = WorkEquipment.Category,
                                 Manufacturer = WorkEquipment.Manufacturer,
                                 Model = WorkEquipment.Model,
                                 Location = WorkEquipment.Location,
                                 SerialNumber = WorkEquipment.SerialNumber,
                                 QualityIssue = WorkEquipment.QualityIssue,
                                 Email = WorkEquipment.Email,
                                 SymptomDesc = sy.Description
                             }).ToList();

            List<eqpModel> eqpObjList = new List<eqpModel>();
            eqpObjList = (from WorkEquipment in fbEntities.WorkorderEquipments
                          join WorkType in fbEntities.WorkorderTypes
                          on WorkEquipment.CallTypeid equals WorkType.CallTypeID into temp1
                          from we in temp1.DefaultIfEmpty()
                          join Solution in fbEntities.Solutions
                          on WorkEquipment.Solutionid equals Solution.SolutionId into solnum
                          from soln in solnum.DefaultIfEmpty()
                          where WorkEquipment.WorkorderID == WorkorderID
                          select new eqpModel()
                          {
                              Assetid = WorkEquipment.Assetid,
                              WorkOrderType = we.Description,
                              Temperature = WorkEquipment.Temperature,
                              Weight = WorkEquipment.Weight,
                              Ratio = WorkEquipment.Ratio,
                              WorkPerformedCounter = WorkEquipment.WorkPerformedCounter,
                              WorkDescription = WorkEquipment.WorkDescription,
                              Category = WorkEquipment.Category,
                              Manufacturer = WorkEquipment.Manufacturer,
                              Model = WorkEquipment.Model,
                              Location = WorkEquipment.Location,
                              SerialNumber = WorkEquipment.SerialNumber,
                              QualityIssue = WorkEquipment.QualityIssue,
                              Email = WorkEquipment.Email,
                              SolutionDesc = soln.Description
                          }).ToList();


            //===================================================================================================
            IList<WOParts> ClosureEquipmentParts = new List<WOParts>(); ;

            decimal partsTotal = 0; string machineNotes = "";
            foreach (var item in eqpObjList)
            {
                machineNotes += item.SerialNumber + " :- " + item.WorkDescription + System.Environment.NewLine;

                ClosureEquipmentParts = new List<WOParts>();
                IQueryable<WorkorderPart> workOrderParts = fbEntities.WorkorderParts.Where(wp => wp.AssetID == item.Assetid);
                foreach (WorkorderPart workOrderPart in workOrderParts)
                {
                    WOParts workOrderPartModel = new WOParts(workOrderPart);

                    if (!string.IsNullOrEmpty(workOrderPartModel.Sku))
                    {
                        Sku sk = fbEntities.Skus.Where(w => w.Sku1 == workOrderPartModel.Sku).FirstOrDefault();
                        if (sk != null)
                        {
                            workOrderPartModel.skuCost = sk.SKUCost == null ? 0 : Convert.ToDecimal(sk.SKUCost);
                        }
                    }
                    else
                    {
                        workOrderPartModel.skuCost = 0;
                    }

                    workOrderPartModel.partsTotal = Convert.ToDecimal(workOrderPartModel.skuCost * workOrderPartModel.Quantity);
                    partsTotal += workOrderPartModel.partsTotal;

                    ClosureEquipmentParts.Add(workOrderPartModel);
                }

                item.Parts = ClosureEquipmentParts;
            }

            decimal PartsTotal = Math.Round(partsTotal, 2);
            //double LaborCost = 112.50;// TotalUnitPrice == "" ? "0" : TotalUnitPrice;

            decimal LaborCost = 0;
            decimal TravelRate = 0;
            decimal rate = 75;
            if (!string.IsNullOrEmpty(trvlTime))
            {
                if (trvlTime.Contains(':'))
                {
                    string[] timeSpllit = trvlTime.Split(':');
                    string hours = string.IsNullOrEmpty(timeSpllit[0]) ? "0" : timeSpllit[0].Trim();
                    string minutes = string.IsNullOrEmpty(timeSpllit[1]) ? "0" : timeSpllit[1].Trim();

                    TravelRate = Math.Round(((Convert.ToDecimal(hours) * rate) + ((Convert.ToDecimal(minutes) / 60) * rate)), 2);
                }
                else
                {
                    TravelRate = Math.Round((Convert.ToDecimal(trvlTime) * rate), 2);
                }
            }
            if (!string.IsNullOrEmpty(ServiceTime))
            {
                if (ServiceTime.Contains(':'))
                {
                    string[] timeSpllit = ServiceTime.Split(':');
                    string hours = string.IsNullOrEmpty(timeSpllit[0]) ? "0" : timeSpllit[0].Trim();
                    string minutes = string.IsNullOrEmpty(timeSpllit[1]) ? "0" : timeSpllit[1].Trim();

                    LaborCost = Math.Round(((Convert.ToDecimal(hours) * rate) + ((Convert.ToDecimal(minutes) / 60) * rate)), 2);
                }
                else
                {
                    LaborCost = Math.Round((Convert.ToDecimal(ServiceTime) * rate), 2);
                }
            }

            decimal tmpTotal = Math.Round((partsTotal + TravelRate + Convert.ToDecimal(LaborCost)), 2);
            //===================================================================================================
            State state = fbEntities.States.Where(st => st.StateCode == CustomerState).FirstOrDefault();
            string taxValue = ""; decimal? taxcalculationValue = 0;
            if (state != null)
            {
                taxcalculationValue = state.TaxPercent == null ? 0 : state.TaxPercent;
                taxValue = taxcalculationValue + "%";
            }
            decimal TaxCostValue = Math.Round(Convert.ToDecimal((taxcalculationValue / 100)) * tmpTotal, 2);

            decimal Total = Math.Round((tmpTotal + TaxCostValue), 2);

            decimal BalanceDue = 0;
            if (Convert.ToBoolean(workorder.IsBillable))
            {
                BalanceDue = Total;
            }

            List<NotesHistory> NHList = fbEntities.NotesHistories.Where(nt => nt.WorkorderID == WorkorderID).ToList();
            if (NHList != null)
            {
                foreach (NotesHistory nh in NHList)
                {
                    if (nh.Notes.Contains("Comments"))
                    {
                        string[] commentStr = nh.Notes.Split(':');
                        machineNotes += string.IsNullOrEmpty(commentStr[1]) ? "" : (commentStr[1].Trim() + "\n");
                    }
                }
            }

            //-------------------------------------------------------------------------------------------------------
            htmlString += @"<html><body>";
            htmlString += @"<table border='0' width='100%' cellpadding='0' cellspacing='0'>
  <tr>
    <td height='30'>&nbsp;</td>
  </tr>
  <tr>
    <td width='100%' align='center' valign='top'><table width='850' border='0' cellpadding='0' cellspacing='0' align='center' style='background:#ECECEC'>
        <tr bgcolor='#49463f'>
          <td height='10' bgcolor='#49463f'></td>
        </tr>
        <tr bgcolor='#49463f'>
          <td bgcolor='#49463f'><table border='0' width='820' align='center' cellpadding='0' cellspacing='0'>
              <tr>
                <td><table border='0' align='left' cellpadding='0' cellspacing='0'>
                    <tr style='width:100%'>
                      <td align='left' style='width:50%'><a href='#' style='display: block;'><img width='150' style='display:block;' src='cid:logo' alt='logo' /></a></td>
                      <td align='center'><div style='color:#fff; font-size: 1.5em; text-align:center; word-spacing: 0px !important;font-family:Arial;'>Work Performed Summary</div></td>
                    </tr>
                  </table>
                  </td>
              </tr>
            </table></td>
        </tr>
        <tr bgcolor='#49463f'>
          <td height='10' bgcolor='#49463f'></td>
        </tr>
        <tr>
          <td><table border='0' width='820' align='center' cellpadding='0' cellspacing='0'>
              <tr bgcolor='ffffff'>
                <td height='20'>&nbsp;</td>
              </tr>              
              <tr bgcolor='ffffff'>
                <td><table width='780' border='0' align='center' cellpadding='0' cellspacing='0' style='border-bottom:2px dotted #BEBEBE;'>
                    <tr>
                      <td><table border='0' width='48%' align='left' cellpadding='0' cellspacing='0'>
                          <tr>
                            <td style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;'> 
Customer Account Number#:&nbsp;<span style='font-weight:normal;text-align:left;'>" + CustomerAcNo + @"</span></td>
                          </tr>";
            /*<tr><td height='12'></td></tr>
             <tr>
               <td style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;'> 	
Technician Name:&nbsp;<span style='font-weight:normal;text-align:left;'>" + WOScheduleTechName + @"</span></td>
             </tr>*/
            htmlString += @"<tr><td height='12'></td></tr>
                          <tr>
                            <td style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;'> 
Service Contact:&nbsp;<span style='font-weight:normal;text-align:left;'>" + WOContactNm + @"</span></td>
                          </tr>

                          <tr><td height='12'>&nbsp;</td></tr>
<tr>
                            <td style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;'> 
Call Placed By:&nbsp;<span style='font-weight:normal;text-align:left;'>" + WOEntryUser + @"</span></td>
                          </tr>

                         <tr><td height='12'>&nbsp;</td></tr>
<tr>
                            <td style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;'> 
Route:&nbsp;<span style='font-weight:normal;text-align:left;'>" + Route + @"</span></td>
                          </tr>

                          <tr><td height='12'>&nbsp;</td></tr>
                        </table>
                        <table border='0' width='48%' style='float:right;' align='left' cellpadding='0' cellspacing='0' class='section-item'>
                          <tr>
                            <td style='color:#484848;font-size: 13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;'> Workorder ID:&nbsp;<span style='font-weight: normal;text-align:left;'>" + WorkorderId + @"</span></td>
                          </tr>
                          <tr><td height='12'></td></tr> 
                          <tr>
                            <td style='color:#484848;font-size: 13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;'> Phone Number:&nbsp;<span style='font-weight: normal;text-align:left;'>" + WOContactPh + @"</span></td>
                          </tr>
                          <tr><td height='12'></td></tr> 
                          <tr>
                            <td style='color:#484848;font-size: 13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;'> 	
Service Date:&nbsp;<span style='font-weight: normal;text-align:left;'>" + WOEntryDate + @"</span></td>
                          </tr>
                          <tr><td height='12'></td></tr>

 <tr>
                                                    <td style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;'>
                                                        Customer PO:&nbsp;<span style='font-weight:normal;text-align:left;'>" + CustomerPO + @"</span>
                                                    </td>
                                                </tr>
                                                <tr><td height='12'></td></tr>
                                                <tr>
                                                    <td style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;'>
                                                        Call Priority:&nbsp;<span style='font-weight:normal;text-align:left;'>" + CallPriority + @"</span>
                                                    </td>
                                                </tr>
                                                <tr><td height='12'></td></tr>

                        </table></td>
                    </tr>
                    <tr>
                       <td style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;'>Customer Location:<span style='font-weight:normal;text-align:left;'>
                                                        <label>" + CustomerName + @"</label></br>
                                                        <label>" + CustomerAddress + @"</label></br>
                                                        <label>" + CustomerCity + @" </label>
                                                        <label>" + CustomerState + @"</label>
                                                        <label>" + CustomerZipCode + @" </label>
                        </span></td>
                    </tr>
                    <tr><td height='12'>&nbsp;</td></tr>
                  </table></td>
              </tr>
              <tr bgcolor='ffffff'>
                <td height='10'></td>
              </tr>
               <tr bgcolor='ffffff'>
                <td><table width='780' border='0' align='center' cellpadding='0' cellspacing='0'>
                	<tr>
                    	<td><h4 style='color:#484848;font-size:15px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;margin:0;padding:0;'>Asset Details</h4></td>
                    </tr>
                    <tr><td height='10'></td></tr>
                    <tr>
                      <td><table border='1' bordercolor='#999999' width='100%' align='left' cellpadding='0' cellspacing='0'>
                          <tr bgcolor='#CCCCCC'>
                            <th style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;height: 26px;'>Service Code</th>
                            <th style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;height: 26px;'>Equipment Type</th>
                            <th style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;height: 26px;'>Manufacturer</th>
                            <th style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;height: 26px;'>Model</th>
                            <th style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;height: 26px;'>Serial Number</th>
                            <th style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;height: 26px;'>Completion Code</th>
                            <th style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;height: 26px;'>Temperature</th>
                            <th style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;height: 26px;'>Weight</th>
                            <th style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;height: 26px;'>Ratio</th>
                            
                          </tr>";
            //< th style = 'color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;height: 26px;' > Describe Work Performed</ th >
            foreach (eqpModel ep in eqpObjList)
            {
                htmlString += @"

                          <tr>
                          	<td style='color:#484848;font-size:13px;text-align:center;font-weight:normal;font-family:Helvetica, Arial, sans-serif;height: 26px;'>" + ep.WorkOrderType + @"</td>
                            <td style='color:#484848;font-size:13px;text-align:center;font-weight:normal;font-family:Helvetica, Arial, sans-serif;height: 26px;'>" + ep.Category + @"</td>
                            <td style='color:#484848;font-size:13px;text-align:center;font-weight:normal;font-family:Helvetica, Arial, sans-serif;height: 26px;'>" + ep.Manufacturer + @"</td>
                            <td style='color:#484848;font-size:13px;text-align:center;font-weight:normal;font-family:Helvetica, Arial, sans-serif;height: 26px;'>" + ep.Model + @"</td>
                            <td style='color:#484848;font-size:13px;text-align:center;font-weight:normal;font-family:Helvetica, Arial, sans-serif;height: 26px;'>" + ep.SerialNumber + @"</td>
                            <td style='color:#484848;font-size:13px;text-align:center;font-weight:normal;font-family:Helvetica, Arial, sans-serif;height: 26px;'>" + ep.SolutionDesc + @"</td>
                            <td style='color:#484848;font-size:13px;text-align:center;font-weight:normal;font-family:Helvetica, Arial, sans-serif;height: 26px;'>" + ep.Temperature + @"</td>
                            <td style='color:#484848;font-size:13px;text-align:center;font-weight:normal;font-family:Helvetica, Arial, sans-serif;height: 26px;'>" + ep.Weight + @"</td>
                            <td style='color:#484848;font-size:13px;text-align:center;font-weight:normal;font-family:Helvetica, Arial, sans-serif;height: 26px;'>" + ep.Ratio + @"</td>
                            
                          </tr>";
                //< td style = 'color:#484848;font-size:13px;text-align:center;font-weight:normal;font-family:Helvetica, Arial, sans-serif;height: 26px;' > " + ep.WorkDescription + @" </ td >
                if (ep.Parts.Count() > 0)
                {
                    htmlString += @"<tr>
                          <td></td>
                          <td colspan='8'>
                          		<table border='1' bgcolor='#fff1ca' bordercolor='#999999' width='100%' align='left' cellpadding='0' cellspacing='0'>
                                  <tr bgcolor='#ffc41e'>
                                    <th style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;height: 23px;'>Quantity</th>
                                    <th style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;height: 23px;'>Manufacturer</th>
                                    <th style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;height: 23px;'>SKU</th>
                                    <th style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;height: 23px;'>Description</th>                                                                        
                                  </tr>";
                    foreach (WOParts wop in ep.Parts)
                    {

                        htmlString += @"<tr>
                          	                <td style='color:#484848;font-size:13px;text-align:center;font-weight:normal;font-family:Helvetica, Arial, sans-serif;height: 23px;'>" + wop.Quantity + @"</td>
                                            <td style='color:#484848;font-size:13px;text-align:center;font-weight:normal;font-family:Helvetica, Arial, sans-serif;height: 23px;'>" + wop.Manufacturer + @"</td>
                                            <td style='color:#484848;font-size:13px;text-align:left;font-weight:normal;font-family:Helvetica, Arial, sans-serif;height: 23px;'>" + wop.Sku + @"</td>
                                            <td style='color:#484848;font-size:13px;text-align:center;font-weight:normal;font-family:Helvetica, Arial, sans-serif;height: 23px;'>" + wop.Description + @"</td>                                            
                                          </tr>";
                    }
                    htmlString += @"
</table>
                          </td>
                          </tr>";
                }
                htmlString += @"";
            }
            htmlString += @"</table>
                        </td>
                    </tr>
                    <tr><td height='15' style='border-bottom:2px dotted #BEBEBE;'></td></tr>
                  </table></td>
              </tr>
              <tr bgcolor='ffffff'>
                <td height='10'></td>
              </tr>
              <tr bgcolor='ffffff'>
                      <td><table width='780' border='0' align='center' cellpadding='0' cellspacing='0' style='border-bottom:2px dotted #BEBEBE;'>
                          <tr>
                            <td>";
            if (BalanceDue == 0)
            {
                /*htmlString += @"<table id='' border='0' cellspacing='0' cellpadding='0' class='table' style='width: 50%; float: right;'>
                                                  <tr>  
                                                      <td>  
                                                          <div style='transform:rotate(-18deg);color: #555;font-size: 15%;font-weight: bold;border: 5px solid #ea13139e;display: inline-block;padding: 12px 10px; text-transform: uppercase;border-radius: 10px;font-family: Courier;-webkit-mask-image: url(img/grunge.png);-webkit-mask-size: 944px 604px;mix-blend-mode:multiply; float: right; color: #C4C4C4;border: 10px double #C4C4C4;transform: rotate(-5deg);font-size: 8px;font-family: Open sans, Helvetica, Arial, sans-serif;border-radius: 15px;width: 60%;text-align: center;opacity: 0.4;float: right;transform:rotate(-20deg)'>                                       
                                                            <span style = 'display: block;color: coral;font-style: italic;font-family: auto;'> No Charge Invoice</span>                                            
                                                            <span style = 'display: block;color: darksalmon;font-style: italic;font-family: monospace;'> Thank you for being a </span>
                                                            <span style = 'display: block;color: tan;font-style: oblique;font-family: cursive;'> Farmer Brothers Customer</span>
                                                        </div>                                        
                                                    </td>
                                                </tr>
                                            </table>";*/

                htmlString += @"<table id='' border='0' cellspacing='0' cellpadding='0' class='table' style='width: 50%; float: right;'>
                                <tr>  
                                    <td>  
                                        <div style='transform:rotate(-18deg);color: #555;font-size: 15%;font-weight: bold;border: 5px solid #ea13139e;display: inline-block;padding: 12px 10px; text-transform: uppercase;border-radius: 10px;font-family: Courier;-webkit-mask-image: url(img/grunge.png);-webkit-mask-size: 944px 604px;mix-blend-mode:multiply; float: right; color: #C4C4C4;border: 10px double #C4C4C4;transform: rotate(-5deg);font-size: 8px;font-family: Open sans, Helvetica, Arial, sans-serif;border-radius: 15px;width: 60%;text-align: center;opacity: 0.4;float: right;transform:rotate(-20deg)'>                                                                               
                                        <span style = 'display: block;color: darksalmon;font-style: italic;font-family: monospace;'> Thank you for being a </span>
                                        <span style = 'display: block;color: tan;font-style: oblique;font-family: cursive;'> Loyal Customer</span>
                                    </div>                                        
                                </td>
                            </tr>
                        </table>";

            }

            htmlString += @"</td></tr>
                            </tr>
<tr>
</tr>
<tr>
</tr>
                        </table></td>";
            /*htmlString += @" </td>
                            <td><table border='0' width='100%' align='left' cellpadding='0' cellspacing='0' style='width:50%; float:right;'>
                                <tr>
                                  <td align='right' style='color:#484848;font-size:13px;font-family:Helvetica, Arial, sans-serif;text-align:right;'><b>Travel Rate:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;</b>" + String.Format("{0:C}", TravelRate) + @"</td>
                                </tr>    
                                <tr><td height='12'></td></tr>                            
                                <tr>
                                  <td align='right' style='color:#484848;font-size:13px;font-family:Helvetica, Arial, sans-serif;text-align:right;'><b>Labor:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;</b>" + String.Format("{0:C}", LaborCost) + @"</td>
                                </tr>
                                <tr><td height='12'></td></tr>
                                <tr>
                                  <td align='right' style='color:#484848;font-size:13px;font-family:Helvetica, Arial, sans-serif;text-align:right;'><b>Parts:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;</b>" + String.Format("{0:C}", partsTotal) + @"</td>
                                </tr>
                                <tr><td height='12'></td></tr>
                                <tr>
                                  <td align='right' style='color:#484848;font-size:13px;font-family:Helvetica, Arial, sans-serif;text-align:right;'><b>Tax:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;</b>" + String.Format("{0:C}", TaxCostValue) + @"</td>
                                </tr>                                
                                <tr><td height='12'></td></tr>
                                <tr>
                                  <td align='right' style='color:#e50322;font-size:13px;font-family:Helvetica, Arial, sans-serif;text-align:right;'><b>Total:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;" + String.Format("{0:C}", Total) + @"</b></td>
                                </tr>
                                <tr><td height='12'></td></tr>
                                <tr>
                                  <td align='right' style='color:#484848;font-size:13px;font-family:Helvetica, Arial, sans-serif;text-align:right;'><b>Balance Due:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;</b>" + String.Format("{0:C}", BalanceDue) + @"</td>
                                </tr>
                                <tr><td height='12'></td></tr>
                              </table></td>
                          </tr>
                        </table></td>
                    </tr>";*/


            /*htmlString += @"<tr bgcolor='ffffff'>
                <td height='10'></td>
              </tr>
<tr bgcolor='ffffff'>
                <td><table width='780' border='0' align='center' cellpadding='0' cellspacing='0'>
                	<tr>
                    	<td><h4 style='color:#484848;font-size:15px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;margin:0;padding:0;'>Accessories </h4></td>
                    </tr>
                    <tr><td height='10'></td></tr>
                    <tr>
                      <td><table border='1' bordercolor='#999999' width='100%' align='left' cellpadding='0' cellspacing='0'>
                          <tr bgcolor='#CCCCCC'>
                            <th style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;height: 26px;'>Equipment Type</th>
                            <th style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;height: 26px;'>Service Code</th>
                            <th style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;height: 26px;'>Symptom</th>
                            <th style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;height: 26px;'>Equipment Location</th>
                            <th style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;height: 26px;'>Serial Number</th>
                          </tr>";
            htmlString += " <tbody> ";
            foreach (eqpModel epreq in eqpReqObjList)
            {
                htmlString += @"<tr>
                          	<td style='color:#484848;font-size:13px;text-align:center;font-weight:normal;font-family:Helvetica, Arial, sans-serif;height: 26px;'>" + epreq.Category + @"</td>
                            <td style='color:#484848;font-size:13px;text-align:center;font-weight:normal;font-family:Helvetica, Arial, sans-serif;height: 26px;'> " + epreq.WorkOrderType + @"</td>
                            <td style='color:#484848;font-size:13px;text-align:center;font-weight:normal;font-family:Helvetica, Arial, sans-serif;height: 26px;'>" + epreq.SymptomDesc + @"</td>
                            <td style='color:#484848;font-size:13px;text-align:center;font-weight:normal;font-family:Helvetica, Arial, sans-serif;height: 26px;'>" + epreq.Location + @"</td>
                            <td style='color:#484848;font-size:13px;text-align:center;font-weight:normal;font-family:Helvetica, Arial, sans-serif;height: 26px;'>" + epreq.SerialNumber + @"</td>
                          </tr>";
            }

            htmlString += @" </table>
                        </td>
                    </tr>
                  </table></td>
              </tr> ";  */
            htmlString += @"<tr bgcolor='ffffff'>
                <td height='15'></td>
              </tr>
              <tr bgcolor='ffffff'>
                <td><table width='780' border='0' align='center' cellpadding='0' cellspacing='0'>
                    <tr>
                      <td><table border='0' width='100%' align='left' cellpadding='0' cellspacing='0'>
                          <tr>
                            <td style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;'> 
Start Time:&nbsp;<span style='font-weight:normal;text-align:left;'>" + StartTime + @"</span></td>
 
                            <td style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;'>	
Arrival Time:&nbsp;<span style='font-weight:normal;text-align:left;'>" + ArrivalTime + @"</span></td>
 </tr>
                          <tr>
                            <td style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;'> 
Completion Time:&nbsp;<span style='font-weight:normal;text-align:left;'>" + CompletionTime + @"</span></td>

                            <td style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;'> 
Travel Time:&nbsp;<span style='font-weight:normal;text-align:left;'>" + TravelTime + @"</span></td>
                          </tr>
<tr>
                                    <td style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;'> 
                                        State of Equipment upon Arrival:&nbsp;
                                        <span style='font-weight:normal;text-align:left;display: block; word-break: break-all; width:35%;'>" + StateOfEqp + @"</span>
                                    </td>
                                    <td style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;'> 
                                        Was Service Delayed:&nbsp;
                                       <span style='font-weight:normal;text-align:left;display: block; word-break: break-all; width:35%;'>" + ServiceDelay + @"</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;'> 
                                        Steps to Troubleshoot and Resolve:&nbsp;
                                        <span style='font-weight:normal;text-align:left;display: block; word-break: break-all; width:35%;'>" + TroubleshootSteps + @"</span>
                                    </td>
                                    <td style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;'> 
                                        Is Operational:&nbsp;
                                        <span style='font-weight:normal;text-align:left;'>" + Operational + @"</span>
                                    </td>
                                </tr>

                                <tr>
                                    <td style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;'> 
                                        Is Under Warrenty:&nbsp;
                                        <span style='font-weight:normal;text-align:left;'>" + UnderWarrenty + @"</span>
                                    </td>
                                    <td style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;'> 
                                        Warrenty For:&nbsp;
                                        <span style='font-weight:normal;text-align:left;'>" + WarrentyFor + @"</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;'> 
                                        Is Additional Followup Needed:&nbsp;
                                        <span style='font-weight:normal;text-align:left;'>" + AdditionalFollowup + @"</span>
                                    </td>
                                    <td style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;'> 
                                        Additional Followup Comments:&nbsp;
                                        <span style='font-weight:normal;text-align:left;display: block; word-break: break-all; width:35%;'>" + FollowupComments + @"</span>
                                    </td>
                                </tr>

                                <tr>
                                    <td style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;'> 
                                        Full Name of the person reviewed  the work:&nbsp;
                                        <span style='font-weight:normal;text-align:left;'>" + ReviewedBy + @"</span>
                                    </td>
                                </tr>

                          <tr><td colspan='3' height='15'></td></tr>
                          <tr>
                            <td colspan='3' style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;'> 
Machine Notes:&nbsp;<span style='font-weight:normal;width:674px;float: right;'><p style='white-space: pre-wrap; font-size:20px; border: black solid 0.1em; padding: 0.5em;' >" + machineNotes + @"</p></span></td>
                          </tr>
                          <tr><td height='15'></td></tr>
                        </table>
                        </td>
                    </tr>
                  </table></td>
              </tr>

                <tr bgcolor='#ffffff'>
                    <td bgcolor='#ffffff'>
                        <div class='CustomerSignatureBlock' style='padding: 0px 40px 0px 20px; '> 
                              <table border = '0' width = '100%' align = 'center' cellpadding = '0' cellspacing = '0' >
                                <tr>
                                    <td style='font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;' align='left'>
                                        Technician Signature:&nbsp;
                                    </td>
                                    <td style='font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;' align = 'right'>
                                        Customer Signature:
                                    </td>
                                </tr>
                                <tr>
                                    <td style='font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;' align='left'>   
                                        <span style='font-weight:normal;text-align:left;' ><b> TechName:</b> " + WOScheduleTechName + @"</span>
                                    </td>
                                    <td align='right'> 
                                         <span><b>Signature By:</b> " + CustomerSignatureBy + @"</span> 
                                     </td> 
                                 </tr>

                                 <!--<tr>
                                             <td style='font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;' align='left'>
                                        Technician Name:&nbsp;
                                    </td>
                                               <td align = 'right'>
                                                    Customer Signature:
                                    </td>
                                </tr>-->
                                <tr>
                                   <td style='font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;' align='left'> ";
            //if (string.Compare(workorder.WorkorderCallstatus.ToUpper(), "Open".ToUpper(), 0) != 0
            //    && string.Compare(workorder.WorkorderCallstatus.ToUpper(), "Hold for AB".ToUpper(), 0) != 0
            //    && string.Compare(workorder.WorkorderCalltypeDesc.ToUpper(), "Parts Request".ToUpper(), 0) != 0
            //    && string.Compare(workorder.WorkorderCallstatus.ToUpper(), "Hold".ToUpper(), 0) != 0)
            //{
            //    htmlString += @"<span style = 'font-weight:normal;text-align:left;' >" + WOScheduleTechName + @"</span>";
            //}
            //htmlString += @"</td>

            if (!string.IsNullOrEmpty(TechnicianSignature))
            {
                htmlString += @"<a href = '#' style = 'display: block;' ><img id='TechnicianSignatureImage' height='100' width='250' alt='Technician Signature' src='cid:Techsig' /></a>";
            }
            htmlString += @"</td>
                                    <td align = 'right'>";
            if (!string.IsNullOrEmpty(CustomerSignature))
            {
                htmlString += @"<a href = '#' style = 'display: block;' ><img id='CustomerSignatureImage' height='100' width='250' alt='Customer Signature' src='cid:sig' /></a>";
            }
            htmlString += @" </td>
                                </tr>
                            </table>       
                        </div>
                    </td>
                </tr>



              <tr bgcolor='ffffff'><td height='15'></td></tr>
              <tr bgcolor='ffffff'>
                <td align='center' style='color:#484848;font-size:12px;font-family:Helvetica, Arial, sans-serif;'>
                <a style='text-decoration: none;color:#2996f7' href='https://goo.gl/forms/KgvSAgobIEee3kEz2'>Click here for Customer Satisfaction Survey</a>
                </td>
              </tr>
              <tr bgcolor='ffffff'><td height='15'></td></tr>";
            //              <tr>
            //                <td height='20'>&nbsp;</td>
            //              </tr>
            //              <tr>
            //                <td><h4 style='color:#484848;font-size:16px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;margin:0;padding:0;text-align:center;'>TERMS AND CONDITIONS</h4></td>
            //              </tr>
            //              <tr><td height='10'>&nbsp;</td></tr>
            //              <tr>
            //                  <td colspan='3' style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;'> 
            //EQUIPMENT DESCRIPTION AND LOCATION:&nbsp;<span style='font-weight:normal;text-align:justify;display: block;'>The equipment covered by these Equipment Usage Terms and Conditions(this “Agreement”) shall consist of the equipment installed at Operator's location(s) by Farmer Bros. Co. (including any subsidiary or affiliate, hereinafter called “FBC”) as described on the reverse or in any addendum hereto. Operator shall not remove any equipment from the location installed by FBC without FBC's prior written consent.</td>
            //              </tr>
            //              <tr><td height='10'>&nbsp;</td></tr>
            //              <tr>
            //                  <td colspan='3' style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;'> 
            //EQUIPMENT OWNERSHIP:&nbsp;<span style='font-weight:normal;text-align:justify;display: block;'>Title to and ownership of the equipment shall at all times remain with FBC.Operator shall not remove or obscure labeling on the equipment indicating that it is the property of FBC. Operator shall not sell, assign, transfer, pledge, hypothecate or otherwise dispose of, encumber or permit a lien to be placed on the equipment.Upon termination of this Agreement, Operator shall provide FBC reasonable access to Operator's location(s) to permit FBC to remove the equipment.Operator shall be responsible for all federal, state or local taxes levied upon the equipment or upon its use, and shall reimburse FBC for any such taxes upon receipt of FBC's invoice for such taxes or pay such taxes directly. Operator shall indemnify FBC for any liability for such taxes.</td>
            //              </tr>
            //              <tr><td height='10'>&nbsp;</td></tr>
            //              <tr>
            //                  <td colspan='3' style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;'> 
            //USE OF EQUIPMENT:&nbsp;<span style='font-weight:normal;text-align:justify;display: block;'>Operator shall use the equipment only to dispense, brew, sell or store FBC products purchased from FBC(the ""Products""), and shall not use the equipment to dispense, brew, sell or store any products other than FBC products.</td>
            //              </tr>
            //              <tr><td height='10'>&nbsp;</td></tr>
            //              <tr>
            //                  <td colspan='3' style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;'> 
            //CARE AND OPERATION:&nbsp;<span style='font-weight:normal;text-align:justify;display: block;'> Operator shall maintain and use the equipment in a careful and proper manner in accordance with the written instructions of the equipment manufacturer and FBC, and shall not make any modifications to the equipment without FBC's prior written consent.  Any modifications to the equipment of any kind shall immediately become the property of FBC subject to the terms of this Agreement. Operator shall comply with all laws, ordinances and regulations relating to the possession, use and maintenance of the equipment. FBC shall not be responsible for any damages, claims, injury or liability (collectively, ""Damages"") relating to the operation of the equipment while it is in the possession of Operator (except for Damages caused by the negligence of FBC, its employees, agents or contractors). Operator shall be responsible for all Damages caused by its negligent use of the equipment and for the loss, theft or destruction of the equipment.</td>
            //              </tr>
            //              <tr><td height='10'>&nbsp;</td></tr>
            //              <tr>
            //                  <td colspan='3' style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;'> 
            //INSTALLATION AND SERVICE:&nbsp;<span style='font-weight:normal;text-align:justify;display: block;'>FBC will conduct a basic installation of the equipment on Operator's premises at no charge.A basic installation consists of connecting the equipment to an established water line with a shut-off valve and calibrating the equipment for optimum service level.Operator must ensure that the plumbing and electrical are in good working order and compliant with all applicable building codes, landlord requirements or other requirements. At Operator's request and expense, FBC will arrange the services of a licensed contractor to perform electrical or plumbing services.FBC will service the equipment at no additional cost to Operator to the extent FBC sees fit in its discretion. Operator will afford reasonable access to the equipment so that FBC may service the equipment. FBC shall not be responsible for any delays in repairing or replacing equipment.</td>
            //              </tr>
            //              <tr><td height='10'>&nbsp;</td></tr>
            //              <tr>
            //                  <td colspan='3' style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;'> 
            //INSPECTION AND VERIFICATION:&nbsp;<span style='font-weight:normal;text-align:justify;display: block;'>FBC or its representatives shall have the right at all reasonable times to enter the premises where the equipment is located for purposes of inspection. Operator agrees to provide a record of serial numbers of beverage equipment at Operator's location(s), upon request by FBC.</td>
            //              </tr>
            //              <tr><td height='10'>&nbsp;</td></tr>
            //              <tr>
            //                  <td colspan='3' style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;'> 
            //ACCEPTANCE OF EQUIPMENT:&nbsp;<span style='font-weight:normal;text-align:justify;display: block;'>Operator shall immediately inspect each piece of equipment delivered in accordance with this Agreement and immediately give notice to FBC if any equipment is damaged or different from the type of equipment described on the reverse or in any addendum hereto. If Operator gives no such notice within fourteen(14) days after delivery of any piece of equipment, it shall be conclusively presumed that such equipment was delivered in good condition. <b>THE EQUIPMENT AND ALL SERVICES ARE PROVIDED "" AS IS."" FBC MAKES NO REPRESENTATION OR WARRANTY OF ANY KIND AND EXPRESSLY DISCLAIMS ALL SUCH REPRESENTATIONS AND WARRANTIES, EXPRESS OR IMPLIED, WITH RESPECT TO THE EQUIPMENT AND THE SERVICES, THEIR SUITABILITY OR FITNESS FOR ANY PURPOSE AND THEIR MERCHANTABILITY</b>.</td>
            //              </tr>
            //              <tr><td height='10'>&nbsp;</td></tr>
            //              <tr>
            //                  <td colspan='3' style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;'> 
            //RISK OF LOSS OR DAMAGE:&nbsp;<span style='font-weight:normal;text-align:justify;display: block;'>Operator assumes all risk of loss or damage to the equipment from any cause, including but not limited to fire, theft, water damage, accidental overturning, dropping or negligence and agrees to return the equipment to FBC in as good condition as when received, normal wear and tear excepted.In the event of loss or damage to the equipment due to any cause other than ordinary wear and tear including fire, theft or otherwise, Operator shall place the equipment in good repair or pay FBC the value of the equipment.Operator will, to the full extent permitted by law, release, indemnify, defend and hold harmless FBC from any loss, damage, liability, cost, fine or expense, including reasonable attorneys' fees, incurred in connection with the services, or Operator's use, possession or operation of the equipment.</td>
            //              </tr>
            //              <tr><td height='10'>&nbsp;</td></tr>
            //              <tr>
            //                  <td colspan='3' style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;'> 
            //LIMITATION OF LIABILITY:&nbsp;<span style='font-weight:normal;text-align:justify;display: block;'>Notwithstanding any provisions in this Agreement or any other agreement between the parties to the contrary, the total overall liability of FBC, whether in contract, tort(including negligence and strict liability) or otherwise is limited to repair or replacement of the equipment, subject to FBC's rights in the paragraph entitled ""REMOVAL OF EQUIPMENT"" below. <b> IN NO EVENT SHALL FBC BE LIABLE IN ANY ACTION, INCLUDING WITHOUT LIMITATION, CONTRACT, TORT, STRICT LIABILITY OR OTHERWISE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, CONSEQUENTIAL, OR PUNITIVE DAMAGES OR PENALTIES, INCLUDING WITHOUT LIMITATION PROCUREMENT OF SUBSTITUTE EQUIPMENT, LOSS OF USE, PROFITS, REVENUE, OR DATA, OR BUSINESS INTERRUPTION ARISING OUT OF OR IN CONNECTION WITH THIS AGREEMENT OR THE EQUIPMENT, EVEN IF FBC WAS ADVISED OF THE POSSIBILITY OF SUCH DAMAGES. ANY ACTION RESULTING FROM ANY BREACH ON THE PART OF FBC AS TO THE EQUIPMENT DELIVERED HEREUNDER MUST BE COMMENCED WITHIN ONE(1) YEAR AFTER THE CAUSE OF ACTION HAS ACCRUED</b>.</td>
            //              </tr>
            //              <tr>
            //                <td height='20'>&nbsp;</td>
            //              </tr>
            //              <tr>
            //                <td><h4 style='color:#484848;font-size:16px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;margin:0;padding:0;text-align:left;'>OPERATOR’S REMEDIES: The remedies reserved to FBC in this Agreement shall be cumulative and additional to any other remedies in law or in equity.</h4></td>
            //              </tr>
            //              <tr><td height='10'>&nbsp;</td></tr>
            //              <tr>
            //                  <td colspan='3' style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;'> 
            //REMOVAL OF EQUIPMENT:&nbsp;<span style='font-weight:normal;text-align:justify;display: block;'>If any covenant of this Agreement is breached by Operator, or if any of Operator's property is subjected to levy or seizure by any creditor or government agency, or if bankruptcy proceedings are commenced by or against Operator, or if Operator discontinues business, this shall constitute a breach of this Agreement by Operator and FBC may without notice or demand remove and recover possession of the equipment.In addition, FBC may, without limitation, transfer, remove or reduce the equipment assigned to Operator at any time. Operator understands that FBC assigns equipment based on Operator’s expected volume of purchases from FBC and that Operator's failure to meet expected volumes, or failure to purchase exclusively from FBC, may result in a reduction or removal of equipment assigned by Operator.If Operator prevents FBC, either directly or indirectly, from retaking possession of equipment, Operator shall pay to FBC all costs of retaking said equipment, including reasonable attorneys' fees.</td>
            //              </tr>
            //              <tr><td height='10'>&nbsp;</td></tr>
            //              <tr>
            //                  <td colspan='3' style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;'> 
            //USE OF FBC MARKS:&nbsp;<span style='font-weight:normal;text-align:justify;display: block;'>FBC owns certain proprietary and other property rights and interests in and to trademarks, service marks, logo types, insignias, trade dress designs and commercial symbols relating to FBC and its products(the “FBC Marks”), which Operator acknowledges are the sole and exclusive property of FBC, with any goodwill arising from the use thereof to inure solely to the benefit of FBC. FBC may provide Operator with displays, signage and other advertising materials incorporating the FBC Marks or approve Operator’s use of the FBC Marks on Operator's menus. Operator shall use such materials solely in connection with the marketing and sale of FBC products and for no other purpose. If at any time Operator shall cease dispensing FBC products, whether in connection with the termination of this Agreement or otherwise, all rights granted hereunder to Operator to use the FBC Marks shall forthwith terminate, and Operator shall immediately and permanently cease to use, in any manner whatsoever, any FBC Marks and all displays, signage, advertising materials, menus and other materials incorporating the FBC Marks and, upon request, immediately return to FBC all such materials owned by FBC or destroy all such materials owned by Operator incorporating such marks.</td>
            //              </tr>
            //              <tr><td height='10'>&nbsp;</td></tr>
            //              <tr>
            //                  <td colspan='3' style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;'> 
            //CONFIDENTIALITY:&nbsp;<span style='font-weight:normal;text-align:justify;display: block;'>Operator agrees to maintain in strict confidence all of the terms and the existence of this Agreement.</td>
            //              </tr>
            //              <tr><td height='10'>&nbsp;</td></tr>
            //              <tr>
            //                  <td colspan='3' style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;'> 
            //GOVERNING LAW; ARBITRATION:&nbsp;<span style='font-weight:normal;text-align:justify;display: block;'>This Agreement will be governed by, and construed in accordance with, the laws of the State of Texas, USA without application of the conflict of law principles thereof.Any controversy or claim arising out of or relating to this Agreement, or the breach thereof, or any rights granted hereunder, will be exclusively settled by binding arbitration in Dallas, Texas, USA. The arbitration will be conducted in English and in accordance with the rules of the American Arbitration Association, which will administer the arbitration and act as appointing authority.The decision of the arbitrators will be binding upon the parties hereto, and the expense of the arbitration will be paid as the arbitrator determines.The decision of the arbitrator will be final, and judgment thereon may be entered by any court of competent jurisdiction and application may be made to any court for a judicial acceptance of the award or order of enforcement.</td>
            //              </tr>
            //              <tr><td height='10'>&nbsp;</td></tr>
            //              <tr>
            //                  <td colspan='3' style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;'> 
            //WAIVER; VALIDITY; SURVIVAL:&nbsp;<span style='font-weight:normal;text-align:justify;display: block;'>The failure of either Operator or FBC to insist upon the strict observance and performance of the terms and conditions set forth herein will not be deemed a waiver of other obligations hereunder, nor will it be considered a future or continuing waiver of the same terms. If any term of this Agreement, or any part hereof, not essential to the commercial purpose of this Agreement is held to be illegal, invalid, or unenforceable, it is the intention of the parties that the remaining terms will remain in full force and effect. To the extent legally permissible, any illegal, invalid, or unenforceable provision of this Agreement will be replaced by a valid provision that will implement the commercial purpose of the illegal, invalid, or unenforceable provision.Sections regarding remedies, governing law, disputes and such other sections that by their nature must survive termination in order to affect their intended purpose shall survive termination of this Agreement</td>
            //              </tr>
            //              <tr><td height='10'>&nbsp;</td></tr>
            //              <tr>
            //                  <td colspan='3' style='color:#484848;font-size:13px;font-weight:bold;font-family:Helvetica, Arial, sans-serif;'> 
            //ENTIRE AGREEMENT; ASSIGNMENT:&nbsp;<span style='font-weight:normal;text-align:justify;display: block;'>The terms and conditions set forth herein, or as changed or modified by a written agreement signed by Operator and FBC, shall constitute the entire contract between Operator and FBC with respect to the subject matter herein and shall supersede any additional or inconsistent terms and conditions contained in any proposals, invoices, orders, or any other documents or correspondence of Operator. For the avoidance of doubt, this Agreement does not apply to any goods(other than the equipment) that FBC may sell to Operator.All such goods shall be governed by FBC’s standard Terms and Conditions of Sale, which shall not be superseded by this Agreement.No changes or modifications to the terms and conditions set forth herein shall have any force or effect, unless otherwise agreed to in writing by Operator and FBC. Neither party may assign, delegate or otherwise transfer this Agreement, in whole or in part, without the prior written consent of the other party (not to be unreasonably withheld); provided, that FBC may assign this Agreement to any party controlling, controlled by or under common control with FBC or to any person acquiring all or substantially all of the assets or outstanding capital stock of FBC.Any attempted assignment, delegation, or other transfer of this Agreement in violation of this Section shall be null and void. An electronic image of this document and any signature or acknowledgement thereto will be considered an original (to the same extent as any paper or hard copy), including under evidentiary standards applicable to a proceeding between the parties hereto.[ The current version of this Agreement and any modifications or amendments supersede all prior versions of this Agreement.The most current version of this Agreement may be found at FBC’s website(www.farmerbros.com / ____) and is otherwise available upon request.]</td>
            //              </tr>
            htmlString += @"<tr><td height='10'>&nbsp;</td></tr>
              <tr><td height='20'>&nbsp;</td></tr>
            </table></td>
        </tr>
      </table></td>
  </tr>
</table>
</body>
</html>
";




            //-------------------------------------------------------------------------------------------------------
            /* htmlString += "<body>";
             htmlString += " <div id='mainBodyDiv' style='background-color:#8080801c; border:#a9a9a9b5 solid 1px;float:left; padding:5px;'>";
             htmlString += "<div class='invoice-form'>";
             htmlString += "<table width = '100%' border='0' cellspacing='0' cellpadding='0' class='table border-less'>";
             htmlString += " <tr>";
             htmlString += "<td style='padding: 2px; text-align: left; '>";
             htmlString += "<div class='invoice-header' style='margin-bottom: 20px;overflow: hidden; '>";
             htmlString += "<div class='work-order' style='padding:10px;'>";
             htmlString += "<table>";
             htmlString += "<tr>";
             htmlString += "<td width = '80%' style='padding: 2px; text-align: left; '>";
             htmlString += "<table width='100%' border='0' cellspacing='0' cellpadding='0'>";
             htmlString += @"<tr>
              <td style='padding: 2px; text-align: left; '>
                                                         <label>Customer Account Number&nbsp;:</label>
                                                     </td>
                                                     <td style='padding: 2px; text-align: left; '>
                                                         <label style = 'color:#0000ff;'>" + CustomerAcNo + @"</label>
                                                     </td>
                                                     <td style='padding: 2px; text-align: left; '>
                                                         <label> Service Date&nbsp;:</label>
                                                     </td>
                                                     <td style='padding: 2px; text-align: left; '>
                                                         <label>" + WOEntryDate + @"</label>
                                                     </td>
                                                 </tr>
                                                 <tr>
                                                     <td style='padding: 2px; text-align: left; '>
                                                         <label>Customer Location&nbsp;:</label>
                                                     </td>
                                                     <td style='padding: 2px; text-align: left; '>
                                                         <label>" + CustomerName + @"</label></br>
                                                         <label>" + CustomerAddress + @"</label></br>
                                                         <label>" + CustomerCity + @" </label>
                                                         <label>" + CustomerState + @"</label>
                                                         <label>" + CustomerZipCode + @" </label>
                                                     </td>
                                                     <td style='padding: 2px; text-align: left; '>
                                                         <label>Technician Name&nbsp;:</label>
                                                     </td>
                                                     <td style='padding: 2px; text-align: left; '>
                                                         <label>" + WOScheduleTechName + @"</label>
                                                     </td>
                                                 </tr>
                                                 <tr>
                                                     <td style='padding: 2px; text-align: left; '><label>Service Contact&nbsp;:</label></td>
                                                     <td style='padding: 2px; text-align: left; '>
                                                         <label>" + WOContactNm + @"</label>

                                                     </td>
                                                     <td style='padding: 2px; text-align: left; '><label>Phone Number&nbsp;:</label></td>
                                                     <td style='padding: 2px; text-align: left; '>
                                                         <label>" + WOContactPh + @"</label>
                                                     </td>
                                                 </tr>
                                                 <tr>
                                                     <td style='padding: 2px; text-align: left; '><label>Workorder ID&nbsp;:</label></td>
                                                     <td style='padding: 2px; text-align: left; '><label>" + WorkorderId + @"</label></td>
                                                 </tr>
                                             </table>
                                         </td>
                                     </tr>
                                 </table>
                             </div>
                         </div>
                     </td>
                 </tr>";

             htmlString += @"<tr>
                     <td style='padding: 2px; text-align: left; '>
                         <div class='reqEquipmentBlock'>

                             <table width = '100%' border='0' cellspacing='0' cellpadding='0' class='table'>
                                 <tr>
                                     <td style='padding: 2px; text-align: left; '>

                                         <div class='panel panel-default margin0'>
                                             <div class='panel-heading wrkReqHeading'>
                                                 <h4 class='panel-title'>Equipment</h4>
                                             </div>
                                             <div class='panel-body'>

                                                 <div id = 'eqpReqMain' style='background-color: white;padding:10px;' class='stripe-borders-table-grid reqEqpSec'>

                                                     <table id = 'gridWrkReq' width='100%' data-swhgajax='true' data-swhgcontainer='eqpReqMain' data-swhgcallback=''>
                                                         <thead>
                                                             <tr>
                                                                 <th scope = 'col' style='background-color: rgb(241,237,237); padding: 2px; text-align: left;     word-wrap: break-word;'>
                                                                     Equipment Type
                                                                 </th>
                                                                 <th scope = 'col' style='background-color: rgb(241,237,237); padding: 2px; text-align: left;     word-wrap: break-word;'>
                                                                     Service Code
                                                                 </th>
                                                                 <th scope = 'col' style='background-color: rgb(241,237,237); padding: 2px; text-align: left;     word-wrap: break-word;'>
                                                                     Symptom
                                                                 </th>
                                                                 <th scope='col' style='background-color: rgb(241,237,237); padding: 2px; text-align: left;     word-wrap: break-word;'>
                                                                     Equipment Location
                                                                 </th>
                                                                 <th scope = 'col' style='background-color: rgb(241,237,237); padding: 2px; text-align: left;     word-wrap: break-word;'>
                                                                     Serial Number
                                                                 </th>
                                                             </tr>
                                                         </thead>";

             htmlString += "<tbody>";
             foreach (eqpModel epreq in eqpReqObjList)
             {


                 htmlString += @"<tr>
                                             <td style='padding: 2px; text-align: left; '>" + epreq.Category + @"</td>
                                             <td style='padding: 2px; text-align: left; '> " + epreq.WorkOrderType + @"</td>
                                             <td style='padding: 2px; text-align: left; '> " + epreq.SymptomDesc + @"</td>
                                             <td style='padding: 2px; text-align: left; '> " + epreq.Location + @"</td>
                                             <td style='padding: 2px; text-align: left; '> " + epreq.SerialNumber + @"</td>
                                         </tr>";
             }


             htmlString += @"</tbody>
                                                     </table>

                                                 </div>

                                             </div>
                                         </div>

                                     </td>
                                 </tr>
                             </table>

                         </div>
                     </td>
                 </tr>
                <tr>
                     <td style='padding: 2px; text-align: left; '>
                         <div class='costDetailsBlock' style='padding: 30px; margin-top:25px'>
                             <table align = 'right' border='0' cellspacing='0' cellpadding='0' class='table' style='width: 50%;border: black solid 1px;'>

                                 <tr style = 'background:#cacbe6;'>
                                     <td style='padding: 2px; text-align: left; '><label> Labor </label></td>
                                     <td style='padding: 2px; text-align: left; '><label>$" + LaborCost + @"</label></td>
                                 </tr>
                                 <tr style = 'background:#cacbe6;'>
                                     <td style='padding: 2px; text-align: left; '><label> Parts </label></td>
                                     <td style='padding: 2px; text-align: left; '><label>$" + partsTotal + @"</label></td>
                                 </tr>
                                 <tr style = 'background:#cacbe6;'>
                                     <td style='padding: 2px; text-align: left; '><label> Tax </label></td>
                                     <td style='padding: 2px; text-align: left; '><label>" + taxValue + @"</label></td>
                                 </tr>
                                 <tr style = 'background:#cacbe6;'>
                                     <td style='padding: 2px; text-align: left; '><label> Total </label></td>
                                     <td style='padding: 2px; text-align: left; '><label>$" + Total + @"</label></td>
                                 </tr>

                             </table>
                         </div>
                     </td>
                 </tr>


                 <tr>
                     <td style='padding: 2px; text-align: left; '>
                         <div class='equipmentBlock'>
                             <table width = '100%' border='0' cellspacing='0' cellpadding='0' class='table'>
                                 <tr>
                                     <td style='padding: 2px; text-align: left; '>

                                         <div class='panel panel-default margin0'>
                                             <div class='panel-heading closureEqpHeading'>
                                                 <h4 class='panel-title'>Asset Details</h4>
                                             </div>

                                             <div class='panel-body' style='background:#ffffff;'>
                                                 <div id = 'main' style='background-color:#fffffff; padding: 0;' class='stripe-borders-table-grid eqpSec'>
                                                     <table id = 'closureAssetsTbl' style='background:#ffffff; width:100%;'>
                                                         <thead>
                                                         <th style='background-color: rgb(241,237,237); padding: 2px; text-align: left; word-wrap: break-word;'>service Code</th>
                                                         <th style='background-color: rgb(241,237,237); padding: 2px; text-align: left; word-wrap: break-word;'>Equipment Type</th>
                                                         <th style='background-color: rgb(241,237,237); padding: 2px; text-align: left; word-wrap: break-word;'>Manufacturer</th>
                                                         <th style='background-color: rgb(241,237,237); padding: 2px; text-align: left; word-wrap: break-word;'>Asset ID</th>
                                                         <th style='background-color: rgb(241,237,237); padding: 2px; text-align: left; word-wrap: break-word;'>Model</th>
                                                         <th style='background-color: rgb(241,237,237); padding: 2px; text-align: left; word-wrap: break-word;'>Serial Number</th>
                                                         <th style='background-color: rgb(241,237,237); padding: 2px; text-align: left; word-wrap: break-word;'>Completion Code</th>
                                                         <th style='background-color: rgb(241,237,237); padding: 2px; text-align: left; word-wrap: break-word;'>Temperature</th>
                                                         <th style='background-color: rgb(241,237,237); padding: 2px; text-align: left; word-wrap: break-word;'>Describe Work Performed</th>
                                                         </thead>
                                                         <tbody>";
             foreach (eqpModel ep in eqpObjList)
             {
                 htmlString += @" <tr>
                                                                 <td style='padding: 2px; text-align: left;border: 0.1px solid; border-color: #e3e1e1eb;word-wrap: break-word; '>" + ep.WorkOrderType + @"</td>
                                                                 <td style='padding: 2px; text-align: left;border: 0.1px solid; border-color: #e3e1e1eb;word-wrap: break-word; '>" + ep.Category + @"</td>
                                                                 <td style='padding: 2px; text-align: left;border: 0.1px solid; border-color: #e3e1e1eb;word-wrap: break-word; '>" + ep.Manufacturer + @"</td>
                                                                 <td style='padding: 2px; text-align: left;border: 0.1px solid; border-color: #e3e1e1eb;word-wrap: break-word; '>" + ep.Assetid + @"</td>
                                                                 <td style='padding: 2px; text-align: left;border: 0.1px solid; border-color: #e3e1e1eb;word-wrap: break-word; '>" + ep.Model + @"</td>
                                                                 <td style='padding: 2px; text-align: left;border: 0.1px solid; border-color: #e3e1e1eb;word-wrap: break-word; '>" + ep.SerialNumber + @"</td>
                                                                 <td style='padding: 2px; text-align: left;border: 0.1px solid; border-color: #e3e1e1eb;word-wrap: break-word; '>" + ep.SolutionDesc + @"</td>
                                                                 <td style='padding: 2px; text-align: left;border: 0.1px solid; border-color: #e3e1e1eb;word-wrap: break-word; '>" + ep.Temperature + @"</td>
                                                                 <td style='padding: 2px; text-align: left;border: 0.1px solid; border-color: #e3e1e1eb;word-wrap: break-word; '>" + ep.WorkDescription + @"</td>                                                                
                                                             </tr>
                                                             <tr>";
                 if (ep.Parts.Count() > 0)
                 {
                     htmlString += @"<tr>
                                                                 <td style='padding: 2px; text-align: left;border: 0.1px solid;border-color: #e3e1e1eb; word-wrap: break-word; '></td>
                                                                 <td style = 'padding:5px; margin:0px;border: 0.1px solid;border-color: #e3e1e1eb; word-wrap: break-word;' colspan= '9'>
                                                                     <table id= 'partsTbl' style='width: 100%;'>
                                                                         <thead>
                                                                             <tr>
                                                                                 <th style='background-color: rgb(241,237,237); padding: 2px; text-align: left;border:0.1px solid;border-color:#e3e1e1eb; word-wrap:break-word;'> Quantity </th>
                                                                                 <th style='background-color: rgb(241,237,237); padding: 2px; text-align: left;border:0.1px solid;border-color:#e3e1e1eb; word-wrap:break-word;'> Manufacturer </th>
                                                                                 <th style='background-color: rgb(241,237,237); padding: 2px; text-align: left;border:0.1px solid;border-color:#e3e1e1eb; word-wrap:break-word;'> SKU </th>
                                                                                 <th style='background-color: rgb(241,237,237); padding: 2px; text-align: left;border:0.1px solid;border-color:#e3e1e1eb; word-wrap:break-word;'> Description </th>
                                                                                 <th style='background-color: rgb(241,237,237); padding: 2px; text-align: left;border:0.1px solid;border-color:#e3e1e1eb; word-wrap:break-word;'> Parts Total</th>
                                                                             </tr>
                                                                         </thead>
                                                                         <tbody>";
                     foreach (WOParts wop in ep.Parts)
                     {

                         htmlString += @"  <tr>
                                                 <td style='padding:2px; text-align:left;border: 0.1px solid;border-color: #e3e1e1eb; word-wrap: break-word; '>" + wop.Quantity + @"</td>
                                                 <td style='padding:2px; text-align:left;border: 0.1px solid;border-color: #e3e1e1eb; word-wrap: break-word; '>" + wop.Manufacturer + @"</td>
                                                 <td style='padding:2px; text-align:left;border: 0.1px solid;border-color: #e3e1e1eb; word-wrap: break-word; '>" + wop.Sku + @"</td>
                                                 <td style='padding:2px; text-align:left;border: 0.1px solid;border-color: #e3e1e1eb; word-wrap: break-word; '>" + wop.Description + @"</td>
                                                 <td style='padding:2px; text-align:left;border: 0.1px solid;border-color: #e3e1e1eb; word-wrap: break-word; '>" + wop.partsTotal + @"</td>
                                             </tr>";
                     }
                     htmlString += @" </tbody>
                                                                     </table>
                                                                 </td>
                                                             </tr>";
                 }
                     htmlString += @" </tr>";
             }
             htmlString += @" </tbody>
             </table>
         </div>



     </div>
     </div>

     </td>
     </tr>


     </table>

     </div>
     </td>
     </tr>

     <tr></tr>

     <tr>
         <td style='padding: 2px; text-align: left; '>
             <div class='timeDetailsBlock' style='padding: 25px; margin-top:25px'>
                 <table width = '100%'>
                     <tr style='background:#fed5c5;'>
                         <td style='padding: 2px; text-align: left; '><label>Start Time</label></td>
                         <td style='padding: 2px; text-align: left; '><label>" + StartTime + @"</label></td>
                         <td style='padding: 2px; text-align: left; '><label>Arrival Time</label></td>
                         <td style='padding: 2px; text-align: left; '><label>" + ArrivalTime + @"</label></td>
                     </tr>
                     <tr style = 'background:#fed5c5;'>
                         <td style='padding: 2px; text-align: left; '><label>Completion Time</label></td>
                         <td style='padding: 2px; text-align: left; '><label>" + CompletionTime + @"</label></td>
                         <td style='padding: 2px; text-align: left; '><label></label></td>
                         <td style='padding: 2px; text-align: left; '><label></label></td>
                 </tr>

                 </table>
             </div>
         </td>
     </tr>
     <tr>
         <td style='padding: 2px; text-align: left; '>
             <div class='panel panel-default margin0'>

                 <div class='panel-body'>
                     <table style='float:right;'>
                         <tr><td>Machine Notes: </td><td style='padding: 2px; text-align: left; '>" + machineNotes + @"</td></tr>                        
                         <tr><td></td><td style='padding: 2px; text-align: left; '><a href = 'https://goo.gl/forms/KgvSAgobIEee3kEz2' target='_blank'>Click here for Customer Satisfaction Survey</a> </td></tr>
                     </table>

                 </div>
             </div>
         </td>
     </tr>

     </table>
     </div>
     <table align= 'right' class='signatureBlock'></table> </div></body> ";*/


            return htmlString;
        }

    }
}
