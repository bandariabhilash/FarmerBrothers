using ReviveCall.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace ReviveCall.Controllers
{
    public class HomeController : Controller
    {
        ReviveEntities reviveEntity = new ReviveEntities();

        public ActionResult Index()
        {
            DateTime currentTime = DateTime.Now;
            CustomerServiceModel csm = new CustomerServiceModel();

            List<FBCallReason> fbcallreasonlist = reviveEntity.FBCallReasons.Where(c => DbFunctions.TruncateTime(c.DateActive) < DbFunctions.TruncateTime(currentTime)
                     && DbFunctions.TruncateTime(c.DateInactive) > DbFunctions.TruncateTime(currentTime)).ToList();

            csm.CallReasonList = new List<FBCallReason>();
            foreach (FBCallReason fbc in fbcallreasonlist)
            {
                //if (fbc.SourceCode == "9027" || fbc.SourceCode == "9028" || fbc.SourceCode == "9032") continue;
                if (fbc.Description.ToLower() == "product or delivery" || fbc.Description.ToLower() == "underperforming account" || fbc.Description.ToLower() == "dropped call") continue;

                fbc.Description = fbc.SourceCode + " - " + fbc.Description;
                csm.CallReasonList.Add(fbc);
            }

            FBCallReason fbReason = new FBCallReason()
            {
                SourceCode = "-1",
                Description = "Please Select Call Reason"
            };
            csm.CallReasonList.Insert(0, fbReason);

            csm.StateList = reviveEntity.States.OrderBy(s => s.StateName).ToList();

            State st = new State()
            {
                StateCode = "-1",
                StateName = "Please Select"
            };
            csm.StateList.Insert(0, st);

            return View(csm);
        }

        [HttpPost]
        public JsonResult SaveNonServiceEvent([ModelBinder(typeof(CustomerServiceModelBinder))]CustomerServiceModel NonService)
        {
            int returnValue = 0;
            NonServiceworkorder workOrder = null;
            string message = string.Empty;
            bool isValid = true;


            if (NonService.CallReason == "-1")
            {
                message = @"|Please select Call Reason!";
                returnValue = -1;
                isValid = false;

            }
            if (string.IsNullOrEmpty(NonService.PostalCode))
            {
                message = @"|Please Enter Valid Customer ZipCode and Update Customer details!";
                returnValue = -1;
                isValid = false;
            }

            if (string.IsNullOrEmpty(NonService.MainContactName) || (NonService.MainContactName != null && string.IsNullOrEmpty(NonService.MainContactName.Trim())))
            {
                message = @"|Please Enter Main Caller Name!";
                returnValue = -1;
                isValid = false;
            }

            if (string.IsNullOrEmpty(NonService.PhoneNumber) || (NonService.PhoneNumber != null && string.IsNullOrEmpty(NonService.PhoneNumber.Trim())))
            {
                message = @"|Please Enter CallBack Number!";
                returnValue = -1;
                isValid = false;
            }

            if (isValid == true)
            {
                CustomerModel customerdata = new CustomerModel();
                customerdata.CustomerName = NonService.CustomerName;
                customerdata.Address = NonService.Address1;
                customerdata.Address2 = NonService.Address2;
                customerdata.City = NonService.City;
                customerdata.State = NonService.State;
                customerdata.ZipCode = NonService.PostalCode;
                customerdata.MainContactName = NonService.MainContactName;
                customerdata.PhoneNumber = NonService.PhoneNumber;
                customerdata.MainEmailAddress = NonService.Email;


                returnValue = NonServiceWorkOrderSave(NonService, customerdata, out workOrder, out message);
            }

            if (returnValue == -1)
            {
                string callStatus = workOrder == null ? "" : "";
                JsonResult jsonResult = new JsonResult();
                jsonResult.Data = new { success = true, serverError = ErrorCode.SUCCESS, WorkOrderId = 0, returnValue = returnValue, WorkorderCallstatus = callStatus, message = message };
                jsonResult.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
                return jsonResult;
            }
            else
            {
                if (returnValue >= 0)
                {
                    SendMail(workOrder.WorkOrderID, reviveEntity);

                    message = "Customer Service - " + workOrder.WorkOrderID + " Created Successfully";

                    JsonResult jsonResult = new JsonResult();
                    jsonResult.Data = new { success = true, serverError = ErrorCode.SUCCESS, WorkOrderId = workOrder.WorkOrderID, returnValue = returnValue, WorkorderCallstatus = "", message = message };
                    jsonResult.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
                    return jsonResult;
                }
                else
                {
                    string callStatus = workOrder == null ? "" : "";
                    JsonResult jsonResult = new JsonResult();
                    jsonResult.Data = new { success = true, serverError = ErrorCode.SUCCESS, WorkOrderId = 0, returnValue = returnValue, WorkorderCallstatus = callStatus, message = message };
                    jsonResult.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
                    return jsonResult;
                }
            }

        }


        public int NonServiceWorkOrderSave(CustomerServiceModel NonService, CustomerModel customerdata, out NonServiceworkorder workOrder, out string message)
        {
            int returnValue = 0;
            message = string.Empty;
            workOrder = new NonServiceworkorder();


            //if (NonService.Operation == WorkOrderManagementSubmitType.SAVE)
            {
                try
                {
                    //NonService.Customer = customerdata;
                    NonServiceworkorder nsw = new NonServiceworkorder();

                    DateTime currentTime = Utility.GetCurrentTime(NonService.PostalCode, reviveEntity);
                    //if (NonService.Customer != null)
                    //{
                    //    nsw.CustomerID = Convert.ToInt32(NonService.Customer.CustomerId);
                    //    nsw.CustomerCity = NonService.Customer.City;
                    //    nsw.CustomerState = NonService.Customer.State;
                    //    nsw.CustomerZipCode = NonService.Customer.ZipCode;
                    //}

                    if (NonService.WorkorderId == 0)
                    {

                        if (!nsw.CustomerID.HasValue || nsw.CustomerID.Value <= 0)
                        {

                            CustomerModel custmodel = new CustomerModel();
                            custmodel.CreateUnknownCustomer(customerdata, reviveEntity);
                            nsw.CustomerID = Convert.ToInt32(customerdata.CustomerId);
                            nsw.IsUnknownWorkOrder = true;
                        }

                        nsw.CreatedDate = currentTime;
                        IndexCounter counter = Utility.GetIndexCounter("NonServiceWorkOrderID", 1);
                        counter.IndexValue++;

                        nsw.WorkOrderID = counter.IndexValue.Value;
                        nsw.CallReason = NonService.CallReason;
                        //nsw.CreatedBy = (int)System.Web.HttpContext.Current.Session["UserId"];
                        //nsw.IsAutoDispatched = NonService.Notes.IsAutoDispatched;
                        nsw.CallerName = NonService.CustomerName;
                        nsw.CallBack = NonService.PhoneNumber;
                        nsw.NonServiceEventStatus = "Open";
                        nsw.MainContactName = NonService.MainContactName;
                        nsw.PhoneNumber = NonService.PhoneNumber;

                        reviveEntity.NonServiceworkorders.Add(nsw);
                        workOrder = nsw;
                        NonService.WorkorderId = nsw.WorkOrderID;

                        NotesHistory notesHistory = new NotesHistory()
                        {
                            AutomaticNotes = 1,
                            EntryDate = currentTime,
                            Notes = @"Created Customer Service WO#: " + workOrder.WorkOrderID + @" in “MARS”!",
                            Userid = System.Web.HttpContext.Current.Session["UserId"] != null ? Convert.ToInt32(System.Web.HttpContext.Current.Session["UserId"]) : 1234,
                            UserName = "WEB",
                            isDispatchNotes = 0
                        };
                        notesHistory.NonServiceWorkorderID = workOrder.WorkOrderID;
                        workOrder.NotesHistories.Add(notesHistory);
                    }

                    //foreach (NewNotesModel newNotesModel in NonService.NewNotes)
                    {
                        NotesHistory newnotesHistory = new NotesHistory()
                        {
                            AutomaticNotes = 0,
                            EntryDate = currentTime,
                            Notes = NonService.Comments,
                            Userid = System.Web.HttpContext.Current.Session["UserId"] != null ? Convert.ToInt32(System.Web.HttpContext.Current.Session["UserId"]) : 1234,
                            UserName = "WEB",
                            NonServiceWorkorderID = workOrder.WorkOrderID
                        };
                        reviveEntity.NotesHistories.Add(newnotesHistory);

                    }
                    returnValue = reviveEntity.SaveChanges();
                }
                catch (Exception ex)
                {
                    message = "Unable to Create Customer Service Work Order!";
                    returnValue = -1;
                }

            }
            return returnValue;
        }

        public bool SendMail(int WorkOrderID, ReviveEntities ReviveEntitites)
        {
            NonServiceworkorder nswo = ReviveEntitites.NonServiceworkorders.Where(nwo => nwo.WorkOrderID == WorkOrderID).FirstOrDefault();
            DateTime currentTime = Utility.GetCurrentTime(nswo.CustomerZipCode, ReviveEntitites);

            string fromAddress = ConfigurationManager.AppSettings["CustomerUpdateMailFromAddress"];
            FBCallReason callReason = ReviveEntitites.FBCallReasons.Where(c => c.SourceCode == nswo.CallReason).FirstOrDefault();

            int cid = Convert.ToInt32(nswo.CustomerID);
            Contact customer = ReviveEntitites.Contacts.Where(c => c.ContactID == cid).FirstOrDefault();
            string ESMMails = string.Empty;
            string CCMMails = string.Empty;

            if (!string.IsNullOrEmpty(customer.ESMEmail))
            {
                ESMMails = customer.ESMEmail;
            }

            if (!string.IsNullOrEmpty(customer.CCMEmail))
            {
                CCMMails = customer.CCMEmail;
            }

            string mailTo = string.Empty;
            string ccTo = string.Empty;
            string mailToName = string.Empty;

            IDictionary<string, string> mailToUserIds = new Dictionary<string, string>();

            FBCustomerServiceDistribution csd = ReviveEntitites.FBCustomerServiceDistributions.Where(cs => cs.Route == customer.Route).FirstOrDefault();
            string NotesMsg = "";

            //Removed this Block on March 29, 2023 as per Mike's email (Subject : Customer form from Revive Site)
            /*if ((nswo != null && nswo.IsUnknownWorkOrder == true) || nswo.CustomerID.ToString().StartsWith("1000")
                && callReason != null && callReason.Description.ToLower() != "dropped call")
            {
                mailTo += ConfigurationManager.AppSettings["CallReasonUnknowunCustomerEmail"].ToString();
                NotesMsg += "Customer Service Escalation Mail sent to " + mailTo;

                string[] emailIds = mailTo.Split(';');

                if (!mailToUserIds.ContainsKey(emailIds[0])) mailToUserIds.Add(new KeyValuePair<string, string>(emailIds[0], "CustomerService"));
                if (!mailToUserIds.ContainsKey(emailIds[1])) mailToUserIds.Add(new KeyValuePair<string, string>(emailIds[1], "Mike"));

                mailToName += ConfigurationManager.AppSettings["CallReasonUnknowunCustomerEmail"].ToString();
            }
            else
            {
                if (callReason != null && csd != null && callReason.Description.ToLower() != "dropped call")
                {
                    if (callReason.EmailRegional == true && !string.IsNullOrEmpty(csd.RegionalsEmail))
                    {
                        mailTo += csd.RegionalsEmail;
                        mailTo += ";";

                        if (!mailToUserIds.ContainsKey(csd.RegionalsEmail)) mailToUserIds.Add(new KeyValuePair<string, string>(csd.RegionalsEmail, csd.RegionalsName));
                        mailToName += "Regionals => " + csd.RegionalsName + " - " + csd.RegionalsEmail + ";    ";
                    }
                    if (callReason.EmailSalesManager == true && !string.IsNullOrEmpty(csd.SalesMmanagerEmail))
                    {
                        mailTo += csd.SalesMmanagerEmail;
                        mailTo += ";";

                        if (!mailToUserIds.ContainsKey(csd.SalesMmanagerEmail)) mailToUserIds.Add(new KeyValuePair<string, string>(csd.SalesMmanagerEmail, csd.SalesManagerName));
                        mailToName += "SalesManager => " + csd.SalesManagerName + " - " + csd.SalesMmanagerEmail + ";    ";
                    }
                    if (callReason.EmailRSR == true && !string.IsNullOrEmpty(csd.RSREmail))
                    {
                        mailTo += csd.RSREmail;
                        mailTo += ";";

                        if (!mailToUserIds.ContainsKey(csd.RSREmail)) mailToUserIds.Add(new KeyValuePair<string, string>(csd.RSREmail, csd.RSRName));
                        mailToName += "RSR => " + csd.RSRName + " - " + csd.RSREmail + ";    ";
                    }

                    NotesMsg += "Customer Service Escalation Mail sent to " + mailToName;
                }
                else
                {
                    if (callReason != null && callReason.Description.ToLower() != "dropped call")
                    {
                        mailTo += ConfigurationManager.AppSettings["MikeEmailId"].ToString();
                        mailTo += ";";

                        if (!mailToUserIds.ContainsKey(ConfigurationManager.AppSettings["MikeEmailId"].ToString())) mailToUserIds.Add(new KeyValuePair<string, string>(ConfigurationManager.AppSettings["MikeEmailId"].ToString(), "Mike"));
                        mailToName += "Mike - " + ConfigurationManager.AppSettings["MikeEmailId"].ToString() + ";    ";
                    }
                    mailTo += ConfigurationManager.AppSettings["DarrylEmailId"].ToString();

                    if (!mailToUserIds.ContainsKey(ConfigurationManager.AppSettings["DarrylEmailId"].ToString())) mailToUserIds.Add(new KeyValuePair<string, string>(ConfigurationManager.AppSettings["DarrylEmailId"].ToString(), "Darryl"));
                    mailToName += "Darryl - " + ConfigurationManager.AppSettings["DarrylEmailId"].ToString() + ";    ";

                    NotesMsg += "Customer Service Escalation Mail sent to " + mailToName;
                }
            }

            if (callReason != null && callReason.Description.ToLower() == "dropped call")
            {
                mailTo = "thelms@mktalt.onmicrosoft.com";
                mailToName = "Tim - " + "thelms@mktalt.onmicrosoft.com";

                mailTo += "ssheedy@mktalt.com";
                mailToName += "Shannon - " + "ssheedy@mktalt.com";

                NotesMsg = "Customer Service Escalation Mail sent to " + mailToName;
            }

            if (string.IsNullOrEmpty(mailTo))
            {
                mailTo += "thelms@mktalt.onmicrosoft.com";
                mailToName += "Tim - " + "thelms@mktalt.onmicrosoft.com";
                mailTo += "ssheedy@mktalt.com";
                mailToName += "Shannon - " + "ssheedy@mktalt.com";

                NotesMsg += "Customer Service Escalation Mail sent to " + mailToName;
            }
            */

            mailTo += ConfigurationManager.AppSettings["MikeEmailId"].ToString();
            mailToName = "Mike - " + ConfigurationManager.AppSettings["MikeEmailId"].ToString() + "; ";

            mailTo += ConfigurationManager.AppSettings["DONKite"].ToString();
            mailToName += "Don Kite - " + ConfigurationManager.AppSettings["DONKite"].ToString() + "; ";

            mailTo += ConfigurationManager.AppSettings["Support"].ToString();
            mailToName += "Support - " + ConfigurationManager.AppSettings["Support"].ToString() + "; ";

            NotesMsg = "Customer Service Escalation Mail sent to " + mailToName;

            string bcc = "";
            if (callReason != null && callReason.AdditionalEmail != null)
            {
                bcc = callReason.AdditionalEmail;
            }
            bcc += ConfigurationManager.AppSettings["TestEmail"]; //To check if the emails are going to the list added above

            NotesHistory notesHistory = new NotesHistory()
            {
                AutomaticNotes = 1,
                EntryDate = currentTime,
                Notes = NotesMsg,
                Userid = System.Web.HttpContext.Current.Session["UserId"] != null ? Convert.ToInt32(System.Web.HttpContext.Current.Session["UserId"]) : 1234,
                UserName = "Revive Website Request",
                isDispatchNotes = 1
            };
            notesHistory.NonServiceWorkorderID = nswo.WorkOrderID;
            nswo.NotesHistories.Add(notesHistory);
            nswo.EmailSentTo = mailToName;
            ReviveEntitites.SaveChanges();

            if (Convert.ToBoolean(ConfigurationManager.AppSettings["UseTestMails"]))
            {
                mailTo = ConfigurationManager.AppSettings["TestEmail"];

                string[] emailIds = mailTo.Split(';');
                foreach (string em in emailIds)
                {
                    if (!string.IsNullOrEmpty(em))
                    {
                        string[] name = em.Split('@');
                        if (!mailToUserIds.ContainsKey(em)) mailToUserIds.Add(new KeyValuePair<string, string>(em, name[0]));
                    }
                }

            }


            bool result = true;
            if (!string.IsNullOrWhiteSpace(mailTo))
            {
                using (var smtp = new SmtpClient())
                {
                    smtp.Host = ConfigurationManager.AppSettings["MailServer"];
                    smtp.Port = 25;

                    string[] addresses = mailTo.Split(';');
                    foreach (string address in addresses)
                    {
                        if (!string.IsNullOrWhiteSpace(address))
                        {
                            var message = new MailMessage();
                            message.From = new MailAddress(fromAddress);
                            result = true;
                            message.To.Add(new MailAddress(address));

                            string[] BCCAddresses = bcc.Split(';');
                            foreach (string bccaddress in BCCAddresses)
                            {
                                if (bccaddress.ToLower().Contains("@jmsmucker.com")) continue;
                                if (!string.IsNullOrWhiteSpace(bccaddress))
                                {
                                    message.Bcc.Add(new MailAddress(bccaddress));
                                }
                            }


                            string completeUrl = ConfigurationManager.AppSettings["CompleteNonServiceEventUrl"];
                            string createServiceEventUrl = ConfigurationManager.AppSettings["CreateServiceEventUrl"];
                            StringBuilder salesEmailBody = new StringBuilder();

                            string mailToUserName = mailToUserIds[address];

                            salesEmailBody.Append(@"<img src='cid:logo' width='15%' height='15%'>");

                            salesEmailBody.Append("<BR>");
                            salesEmailBody.Append("<BR>");
                            salesEmailBody.Append("<BR>");

                            //salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", completeUrl, new Encrypt_Decrypt().Encrypt("workOrderId=" + WorkOrderID)) + "\">COMPLETE</a>");
                            //salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
                            //salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", createServiceEventUrl, new Encrypt_Decrypt().Encrypt("workOrderId=" + WorkOrderID + "&techId=0&response=0&isResponsible=false&isBillable=" + mailToUserName)) + "\">CREATE SERVICE EVENT</a>");
                            //salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");

                            salesEmailBody.Append("<BR>");
                            salesEmailBody.Append("<BR>");

                            #region customer details

                            salesEmailBody.Append("<table>");

                            salesEmailBody.Append("<tr>");
                            salesEmailBody.Append("<td><b>");
                            salesEmailBody.Append("CustomerDetails:");
                            salesEmailBody.Append("</b></td>");
                            salesEmailBody.Append("</tr>");

                            salesEmailBody.Append("<tr>");
                            salesEmailBody.Append("</tr>");

                            salesEmailBody.Append("<tr>");
                            salesEmailBody.Append("<td><b>");
                            salesEmailBody.Append("AccountNumber:");
                            salesEmailBody.Append("</b></td>");
                            salesEmailBody.Append("<td>");
                            salesEmailBody.Append(nswo.CustomerID);
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

                            string CallReasonDesc = ReviveEntitites.FBCallReasons.Where(c => c.SourceCode == nswo.CallReason).Select(r => r.Description).FirstOrDefault();

                            salesEmailBody.Append("<b>Call Reason: </b>");
                            salesEmailBody.Append(callReason.SourceCode + " - " + CallReasonDesc);
                            salesEmailBody.Append("<BR>");
                            salesEmailBody.Append("<b>Caller Name: </b>");
                            salesEmailBody.Append(nswo.CallerName);
                            salesEmailBody.Append("<BR>");
                            salesEmailBody.Append("<b>Call Back #: </b>");
                            salesEmailBody.Append(Utility.FormatPhoneNumber(nswo.CallBack));

                            salesEmailBody.Append("<BR>");
                            salesEmailBody.Append("<BR>");


                            #region Notes

                            salesEmailBody.Append("<table>");
                            salesEmailBody.Append("<tr>");
                            salesEmailBody.Append("<td><b>");
                            salesEmailBody.Append("Notes");
                            salesEmailBody.Append("<b></td>");


                            IEnumerable<NotesHistory> histories = ReviveEntitites.NotesHistories.Where(w => w.NonServiceWorkorderID == WorkOrderID).OrderByDescending(n => n.EntryDate);

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

                            //salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", completeUrl, new Encrypt_Decrypt().Encrypt("workOrderId=" + WorkOrderID)) + "\">COMPLETE</a>");
                            //salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
                            //salesEmailBody.Append("<a href=\"" + string.Format("{0}{1}&encrypt=yes", createServiceEventUrl, new Encrypt_Decrypt().Encrypt("workOrderId=" + WorkOrderID + "&techId=0&response=0&isResponsible=false&isBillable=" + mailToUserName)) + "\">CREATE SERVICE EVENT</a>");
                            //salesEmailBody.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");

                            salesEmailBody.Append("<BR>");
                            salesEmailBody.Append("<BR>");

                            string contentId = Guid.NewGuid().ToString();
                            string logoPath = Path.Combine(HttpRuntime.AppDomainAppPath, "images/FB0913_Logo_Revive_FINAL_Color.png");

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
                            

                            message.Subject = "(Revive Website Request) - Call Reason: " + callReason.SourceCode + " - " + CallReasonDesc + ", Customer Service Event#: " + WorkOrderID;
                            message.IsBodyHtml = true;

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
                }
            }

            

            return result;
        }


    }



}