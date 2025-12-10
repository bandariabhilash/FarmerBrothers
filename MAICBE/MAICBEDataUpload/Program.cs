using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MAICBEDataUpload
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            int resultFlag = Program.ProcessFile();
        }

        private static int ProcessFile()
        {
            string FilePath = ConfigurationManager.AppSettings["FullPath"];
            string[] files = System.IO.Directory.GetFiles(FilePath, "*.txt");

            foreach (string file in files)
            {
                string errorItemNumbers = "";
                string FileName = "";
                //string[] lines = System.IO.File.ReadAllLines(FilePath + "MAICBE.txt");
                string[] lines = null;
                //if (files.Count() > 0)
                {
                    FileName = System.IO.Path.GetFileName(file);
                    lines = System.IO.File.ReadAllLines(FilePath + FileName);
                }
                //else
                //{
                //    return 0;
                //}

                //string constr = ConfigurationManager.ConnectionStrings["FB_Connection"].ConnectionString;

                //using (SqlConnection con = new SqlConnection(constr))
                {
                    //con.Open();

                    //string TruncateQuery = @"TRUNCATE Table FBCBE";
                    //SqlCommand Truncatecmd = new SqlCommand(TruncateQuery, con);
                    //Truncatecmd.ExecuteNonQuery();


                    foreach (string line in lines)
                    {
                        string ItemNumber = line.Substring(0, 25).Trim();
                        string[] ItemNumberStr = null;
                        string ItemDescription = line.Substring(25, 30).Trim();
                        string[] ItemDescriptionStr = null;
                        string SerialNumber = line.Substring(55, 30).Trim();
                        string[] SerialNumberStr = null;
                        string AssetStatus = line.Substring(85, 1).Trim();
                        string[] AssetStatusStr = null;
                        string CurrentLOC = line.Substring(86, 12).Trim();
                        string[] CurrentLOCStr = null;
                        string CurrentCustomer = line.Substring(98, 8).Trim();
                        string[] CurrentCustomerStr = null;
                        string CurrentCustomerName = line.Substring(106, 40).Trim();
                        string[] CurrentCustomerNameStr = null;
                        string TransDate = line.Substring(146, 10).Trim();
                        string[] TransDateStr = null;
                        string InitialValue = line.Substring(156, 15).Trim();
                        string[] InitialValueStr = null;
                        string InitialDate = line.Substring(171, 10).Trim();
                        string[] InitialDateStr = null;
                        string CurrentGLCode = line.Substring(181, 4).Trim();
                        string[] CurrentGLCodeStr = null;
                        string CurrentGLObject = line.Substring(185, 4).Trim();
                        string[] CurrentGLObjectStr = null;
                        try
                        {
                            //string ItemNumber = line.Substring(0, 25).Trim();
                            //string[] ItemNumberStr = null;
                            if (ItemNumber.Contains("'"))
                            {
                                ItemNumberStr = ItemNumber.Split('\'');
                                ItemNumber = ItemNumberStr[0] + "''" + ItemNumberStr[1];
                            }

                            //string ItemDescription = line.Substring(25, 30).Trim();
                            // string[] ItemDescriptionStr = null;
                            if (ItemDescription.Contains("'"))
                            {
                                ItemDescriptionStr = ItemDescription.Split('\'');
                                ItemDescription = ItemDescriptionStr[0] + "''" + ItemDescriptionStr[1];
                            }

                            //string SerialNumber = line.Substring(55, 30).Trim();
                            //string[] SerialNumberStr = null;
                            if (SerialNumber.Contains("'"))
                            {
                                SerialNumberStr = SerialNumber.Split('\'');
                                SerialNumber = SerialNumberStr[0] + "''" + SerialNumberStr[1];
                            }

                            //string AssetStatus = line.Substring(85, 1).Trim();
                            //string[] AssetStatusStr = null;
                            if (AssetStatus.Contains("'"))
                            {
                                AssetStatusStr = AssetStatus.Split('\'');
                                AssetStatus = AssetStatusStr[0] + "''" + AssetStatusStr[1];
                            }

                            //string CurrentLOC = line.Substring(86, 12).Trim();
                            //string[] CurrentLOCStr = null;
                            if (CurrentLOC.Contains("'"))
                            {
                                CurrentLOCStr = CurrentLOC.Split('\'');
                                CurrentLOC = CurrentLOCStr[0] + "''" + CurrentLOCStr[1];
                            }

                            //string CurrentCustomer = line.Substring(98, 8).Trim();
                            //string[] CurrentCustomerStr = null;
                            if (CurrentCustomer.Contains("'"))
                            {
                                CurrentCustomerStr = CurrentCustomer.Split('\'');
                                CurrentCustomer = CurrentCustomerStr[0] + "''" + CurrentCustomerStr[1];
                            }

                            //string CurrentCustomerName = line.Substring(106, 40).Trim();
                            //string[] CurrentCustomerNameStr = null;
                            if (CurrentCustomerName.Contains("'"))
                            {
                                CurrentCustomerNameStr = CurrentCustomerName.Split('\'');
                                CurrentCustomerName = CurrentCustomerNameStr[0] + "''" + CurrentCustomerNameStr[1];
                            }

                            //string TransDate = line.Substring(146, 10).Trim();
                            //string[] TransDateStr = null;
                            if (TransDate.Contains("'"))
                            {
                                TransDateStr = TransDate.Split('\'');
                                TransDate = TransDateStr[0] + "''" + TransDateStr[1];
                            }

                            //string InitialValue = line.Substring(156, 15).Trim();
                            //string[] InitialValueStr = null;
                            if (InitialValue.Contains("'"))
                            {
                                InitialValueStr = InitialValue.Split('\'');
                                InitialValue = InitialValueStr[0] + "''" + InitialValueStr[1];
                            }

                            //string InitialDate = line.Substring(171, 10).Trim();
                            //string[] InitialDateStr = null;
                            if (InitialDate.Contains("'"))
                            {
                                InitialDateStr = InitialDate.Split('\'');
                                InitialDate = InitialDateStr[0] + "''" + InitialDateStr[1];
                            }

                            //string CurrentGLCode = line.Substring(181, 4).Trim();
                            //string[] CurrentGLCodeStr = null;
                            if (CurrentGLCode.Contains("'"))
                            {
                                CurrentGLCodeStr = CurrentGLCode.Split('\'');
                                CurrentGLCode = CurrentGLCodeStr[0] + "''" + CurrentGLCodeStr[1];
                            }

                            //string CurrentGLObject = line.Substring(185, 4).Trim();
                            //string[] CurrentGLObjectStr = null;
                            if (CurrentGLObject.Contains("'"))
                            {
                                CurrentGLObjectStr = CurrentGLObject.Split('\'');
                                CurrentGLObject = CurrentGLObjectStr[0] + "''" + CurrentGLObjectStr[1];
                            }

                            FBEntities fbEntity = new FBEntities();
                            FBCBE_Tmp cbe = fbEntity.FBCBE_Tmp.Where(cb => cb.ItemNumber == ItemNumber && cb.SerialNumber == SerialNumber).FirstOrDefault();

                            DateTime currentDate = DateTime.Now;
                            if (cbe == null)
                            {
                                FBCBE_Tmp cb = new FBCBE_Tmp();
                                cb.ItemNumber = string.IsNullOrEmpty(ItemNumber) ? "" : ItemNumber;
                                cb.ItemDescription = string.IsNullOrEmpty(ItemDescription) ? "" : ItemDescription;
                                cb.SerialNumber = string.IsNullOrEmpty(SerialNumber) ? "" : SerialNumber;
                                cb.AssetStatus = string.IsNullOrEmpty(AssetStatus) ? "" : AssetStatus;
                                cb.CurrentLocation = string.IsNullOrEmpty(CurrentLOC) ? "" : CurrentLOC;
                                cb.CurrentCustomerId = string.IsNullOrEmpty(CurrentCustomer) ? 0 : Convert.ToInt32(CurrentCustomer);
                                cb.CurrentCustomerName = string.IsNullOrEmpty(CurrentCustomerName) ? "" : CurrentCustomerName;
                                cb.TransDate = string.IsNullOrEmpty(TransDate) ? DateTime.Now : Convert.ToDateTime(TransDate);
                                cb.InitialDate = string.IsNullOrEmpty(InitialDate) ? DateTime.Now : Convert.ToDateTime(InitialDate);
                                cb.InitialValue = string.IsNullOrEmpty(InitialValue) ? 0 : Convert.ToDecimal(InitialValue);
                                cb.CurrentGLCode = string.IsNullOrEmpty(CurrentGLCode) ? "" : CurrentGLCode;
                                cb.CurrentGLObject = string.IsNullOrEmpty(CurrentGLObject) ? "" : CurrentGLObject;
                                cb.EntryDate = currentDate;
                                cb.LastUpdatedDate = currentDate;

                                fbEntity.FBCBE_Tmp.Add(cb);
                            }
                            else
                            {
                                cbe.ItemNumber = ItemNumber;
                                cbe.ItemDescription = string.IsNullOrEmpty(ItemDescription) ? "" : ItemDescription;
                                cbe.SerialNumber = SerialNumber;
                                cbe.SerialNumber = string.IsNullOrEmpty(SerialNumber) ? "" : SerialNumber;
                                cbe.AssetStatus = string.IsNullOrEmpty(AssetStatus) ? "" : AssetStatus;
                                cbe.CurrentLocation = string.IsNullOrEmpty(CurrentLOC) ? "" : CurrentLOC;
                                cbe.CurrentCustomerId = string.IsNullOrEmpty(CurrentCustomer) ? 0 : Convert.ToInt32(CurrentCustomer);
                                cbe.CurrentCustomerName = string.IsNullOrEmpty(CurrentCustomerName) ? "" : CurrentCustomerName;
                                cbe.TransDate = string.IsNullOrEmpty(TransDate) ? DateTime.Now : Convert.ToDateTime(TransDate);
                                cbe.InitialDate = string.IsNullOrEmpty(InitialDate) ? DateTime.Now : Convert.ToDateTime(InitialDate);
                                cbe.InitialValue = string.IsNullOrEmpty(InitialValue) ? 0 : Convert.ToDecimal(InitialValue);
                                cbe.CurrentGLCode = string.IsNullOrEmpty(CurrentGLCode) ? "" : CurrentGLCode;
                                cbe.CurrentGLObject = string.IsNullOrEmpty(CurrentGLObject) ? "" : CurrentGLObject;
                                cbe.LastUpdatedDate = currentDate;
                            }


                            fbEntity.SaveChanges();
                            //string sqlQuery = @"Insert Into FBCBE([ItemNumber],[ItemDescription],[SerialNumber],[AssetStatus],[CurrentLocation]
                            //                            ,[CurrentCustomerId],[CurrentCustomerName],[TransDate],[InitialValue],[InitialDate],[CurrentGLCode],[CurrentGLObject])
                            //                            VALUES('" + ItemNumber + "','" + ItemDescription + "','" + SerialNumber + "','" + AssetStatus + "','" + CurrentLOC +
                            //                                "'," + CurrentCustomer + ",'" + CurrentCustomerName + "','" + TransDate + "'," + InitialValue + ",'" + InitialDate + "','" + CurrentGLCode + "','" + CurrentGLObject  + "')";

                            //SqlCommand cmd = new SqlCommand(sqlQuery, con);

                            //cmd.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            errorItemNumbers += ItemNumber + "; ";
                            continue;
                            //return 0;
                        }
                    }
                    //con.Close();
                }

                sendEmail(FileName, errorItemNumbers);

                //System.IO.File.Move(FilePath + FileName, FilePath + "Completed/" + FileName);
                DirectoryInfo FileToBeMoved = new DirectoryInfo(FilePath + FileName);
                string CompletedDir = FilePath + @"Completed\" + FileName;
                FileToBeMoved.MoveTo(CompletedDir);

                //streamWriter.Close();
            }
            return 1;
        }


        private static void sendEmail(string FileName, string ErrorDetails)
        {
            StringBuilder salesEmailBody = new StringBuilder();

            salesEmailBody.Append(@"<img src='cid:logo' width='15%' height='15%'>");

            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("<BR>");

            salesEmailBody.Append("FileName : ");
            salesEmailBody.Append(FileName);
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("<BR>");
            salesEmailBody.Append("Error Details");
            salesEmailBody.Append(ErrorDetails);
            salesEmailBody.Append("<BR>");

            //string contentId = Guid.NewGuid().ToString();
            //string logoPath = string.Empty;
            //if (Server == null)
            //{
            //    logoPath = Path.Combine(HttpRuntime.AppDomainAppPath, "img/mainlogo.jpg");
            //}
            //else
            //{
            //    logoPath = Server.MapPath("~/img/mainlogo.jpg");
            //}


            //salesEmailBody = salesEmailBody.Replace("cid:logo", "cid:" + contentId);

            AlternateView avHtml = AlternateView.CreateAlternateViewFromString
               (salesEmailBody.ToString(), null, MediaTypeNames.Text.Html);

            //LinkedResource inline = new LinkedResource(logoPath, MediaTypeNames.Image.Jpeg);
            //inline.ContentId = contentId;
            //avHtml.LinkedResources.Add(inline);

            var message = new MailMessage();

            message.AlternateViews.Add(avHtml);

            message.IsBodyHtml = true;
            //message.Body = salesEmailBody.Replace("cid:logo", "cid:" + inline.ContentId).ToString();

            bool result = true;
            string mailTo = ConfigurationManager.AppSettings["ToAddress"]; ;
            string fromAddress = ConfigurationManager.AppSettings["FromAddress"];
            string mailCC = string.Empty;
            if (!string.IsNullOrWhiteSpace(mailTo))
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

            StringBuilder subject = new StringBuilder();
            subject.Append("MAICBE File Import Error; FileName: ");
            subject.Append(FileName);


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

                }
            }
        }

    }

}