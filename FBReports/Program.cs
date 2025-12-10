using log4net;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FBReports
{
    static class Program
    {
        public static FB_Entities fbEntity = null;
        private static readonly log4net.ILog log =
           log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {            
            fbEntity = new FB_Entities();

            OpenCallReport();
            EmailExcelSheet();
            MovefiletoCompleted();
        }

        public static void OpenCallReport()
        {
            try
            {

                var openCallsQ1 = (from w in fbEntity.WorkOrders
                                   join ws in fbEntity.WorkorderSchedules on w.WorkorderID equals ws.WorkorderID
                                   from esm in fbEntity.ESMCCMRSMEscalations.Where(e => e.ZIPCode == w.CustomerZipCode).DefaultIfEmpty()
                                   where w.WorkorderCallstatus != "Closed" && w.WorkorderCallstatus != "Deleted" && w.WorkorderCallstatus != "Open"
                                                && w.WorkorderCallstatus != "Hold for AB" && w.WorkorderCallstatus != "Hold"
                                                && (ws.AssignedStatus == "Accepted" || ws.AssignedStatus == "Scheduled" || ws.AssignedStatus == "Sent")
                                   select new
                                   {
                                       WorkorderId = w.WorkorderID,
                                       CallStatus = w.WorkorderCallstatus,
                                       CustomerId = w.CustomerID,
                                       CustomerName = w.CustomerName,
                                       ESM = string.IsNullOrEmpty(esm.ESMName) ? "" : esm.ESMName,
                                       Branch = w.ResponsibleTechBranch,
                                       CallTypeId = w.WorkorderCalltypeid,
                                       EventDate = w.WorkorderEntryDate,
                                       TechId = ws.Techid.ToString(),
                                       TechName = ws.TechName,
                                       TechScheduleDate = ws.EntryDate.ToString()
                                   }).ToList();

                var openCallsQ2 = (from w in fbEntity.WorkOrders
                                   from esm in fbEntity.ESMCCMRSMEscalations.Where(e => e.ZIPCode == w.CustomerZipCode).DefaultIfEmpty()
                                   where w.WorkorderCallstatus == "Open" || w.WorkorderCallstatus == "Hold"
                                   select new
                                   {
                                       WorkorderId = w.WorkorderID,
                                       CallStatus = w.WorkorderCallstatus,
                                       CustomerId = w.CustomerID,
                                       CustomerName = w.CustomerName,
                                       ESM = string.IsNullOrEmpty(esm.ESMName) ? "" : esm.ESMName,
                                       Branch = w.ResponsibleTechBranch,
                                       CallTypeId = w.WorkorderCalltypeid,
                                       EventDate = w.WorkorderEntryDate,
                                       TechId = "",
                                       TechName = "",
                                       TechScheduleDate = ""
                                   }).ToList();


                var openCallResults = openCallsQ1.Union(openCallsQ2).ToList();
                DataTable dt = ListToDataTable(openCallResults);

                string[] columns = { "WorkorderId", "CustomerId", "CustomerName", "ESM", "CallStatus", "CallTypeId", "EventDate", "Branch", "TechId", "TechName", "TechScheduleDate" };

                byte[] filecontent = ExportExcel(dt, "", false, columns);
                var fileStream = new MemoryStream(filecontent);
                string filePath = ConfigurationManager.AppSettings["FilePath"];
                File.WriteAllBytes(filePath + "OpenCallReportResults.xlsx", filecontent);
            }
            catch (Exception ex)
            {
                log.Error("Error Occured while Exporting to Excel: => " + ex.InnerException + "\n" + ex.Message);

            }
        }

        public static DataTable ListToDataTable<T>(List<T> data)
        {
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(typeof(T));
            DataTable dataTable = new DataTable();

            for (int i = 0; i < properties.Count; i++)
            {
                PropertyDescriptor property = properties[i];
                dataTable.Columns.Add(property.Name, Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType);
            }

            object[] values = new object[properties.Count];
            foreach (T item in data)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = properties[i].GetValue(item);
                }

                dataTable.Rows.Add(values);
            }
            return dataTable;
        }

        public static byte[] ExportExcel(DataTable dataTable, string heading = "", bool showSrNo = false, params string[] columnsToTake)
        {

            byte[] result = null;
            using (ExcelPackage package = new ExcelPackage())
            {
                ExcelWorksheet workSheet = package.Workbook.Worksheets.Add(String.Format("{0} Data", heading));
                int startRowFrom = String.IsNullOrEmpty(heading) ? 1 : 3;

                if (showSrNo)
                {
                    DataColumn dataColumn = dataTable.Columns.Add("#", typeof(int));
                    dataColumn.SetOrdinal(0);
                    int index = 1;
                    foreach (DataRow item in dataTable.Rows)
                    {
                        item[0] = index;
                        index++;
                    }
                }


                // add the content into the Excel file  
                workSheet.Cells["A" + startRowFrom].LoadFromDataTable(dataTable, true);


                int colNumber = 0;

                foreach (DataColumn col in dataTable.Columns)
                {
                    colNumber++;
                    if (col.DataType == typeof(DateTime))
                    {
                        workSheet.Column(colNumber).Style.Numberformat.Format = "MM/dd/yyyy hh:mm:ss";
                    }
                }



                // autofit width of cells with small content  
                int columnIndex = 1;
                foreach (DataColumn column in dataTable.Columns)
                {
                    ExcelRange columnCells = workSheet.Cells[workSheet.Dimension.Start.Row, columnIndex, workSheet.Dimension.End.Row, columnIndex];
                    //if (columnCells.cell.Value !=null)
                    {
                        int maxLength = columnCells.Max(cell => cell.Value != null ? cell.Value.ToString().Count() : 0);
                        if (maxLength < 150)
                        {
                            workSheet.Column(columnIndex).AutoFit();
                        }
                        else
                        {
                            workSheet.Column(columnIndex).Width = 200;
                        }
                    }


                    columnIndex++;
                }

                // format header - bold, yellow on black  
                using (ExcelRange r = workSheet.Cells[startRowFrom, 1, startRowFrom, dataTable.Columns.Count])
                {
                    r.Style.Font.Color.SetColor(System.Drawing.Color.White);
                    r.Style.Font.Bold = true;
                    r.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#1fb5ad"));
                }

                // format cells - add borders  
                using (ExcelRange r = workSheet.Cells[startRowFrom + 1, 1, startRowFrom + dataTable.Rows.Count, dataTable.Columns.Count])
                {
                    r.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    r.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    r.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    r.Style.Border.Right.Style = ExcelBorderStyle.Thin;

                    r.Style.Border.Top.Color.SetColor(System.Drawing.Color.Black);
                    r.Style.Border.Bottom.Color.SetColor(System.Drawing.Color.Black);
                    r.Style.Border.Left.Color.SetColor(System.Drawing.Color.Black);
                    r.Style.Border.Right.Color.SetColor(System.Drawing.Color.Black);
                    r.Style.WrapText = true;
                }

                // removed ignored columns  
                for (int i = dataTable.Columns.Count - 1; i >= 0; i--)
                {
                    if (i == 0 && showSrNo)
                    {
                        continue;
                    }
                    if (!columnsToTake.Contains(dataTable.Columns[i].ColumnName))
                    {
                        workSheet.DeleteColumn(i + 1);
                    }
                }

                if (!String.IsNullOrEmpty(heading))
                {
                    workSheet.Cells["A1"].Value = heading;
                    workSheet.Cells["A1"].Style.Font.Size = 20;

                    workSheet.InsertColumn(1, 1);
                    workSheet.InsertRow(1, 1);
                    workSheet.Column(1).Width = 5;
                }

                result = package.GetAsByteArray();
            }

            return result;
        }

        public static void EmailExcelSheet()
        {
            string FromAddress = ConfigurationManager.AppSettings["FromAddress"];
            string FromAddressDisplayName = ConfigurationManager.AppSettings["FromAddressDisplay"];
            string ToAddress = ConfigurationManager.AppSettings["ToAddress"];

            //var message = new MailMessage();

            MailMessage mail = new MailMessage();
            //SmtpClient SmtpServer = new SmtpClient("smtp.gmail.com");
            mail.From = new MailAddress(FromAddress, FromAddressDisplayName);
            //mail.To.Add(ToAddress);
            mail.Subject = "Weekly Open Call Report";
            mail.Body = "Please find the Open Call Report Attached";


            string[] addresses = ToAddress.Split(';');
            foreach (string address in addresses)
            {
                if (!string.IsNullOrWhiteSpace(address))
                {
                    mail.To.Add(new MailAddress(address));
                }
            }

            System.Net.Mail.Attachment attachment;
            string filePath = ConfigurationManager.AppSettings["FilePath"];
            attachment = new System.Net.Mail.Attachment(filePath + "OpenCallReportResults.xlsx");
            mail.Attachments.Add(attachment);

            //SmtpServer.Port = 587;
            //SmtpServer.Credentials = new System.Net.NetworkCredential("your mail@gmail.com", "your password");
            //SmtpServer.UseDefaultCredentials = true;
            //SmtpServer.EnableSsl = true;

            //SmtpServer.Send(mail);


            using (var smtp = new SmtpClient())
            {
                smtp.Host = ConfigurationManager.AppSettings["MailServer"];
                smtp.Port = 25;

                try
                {
                    smtp.Send(mail);

                    mail.Dispose();
                }
                catch (Exception ex)
                {
                    
                }
            }

        }

        public static void MovefiletoCompleted()
        {
            List<string> contentsList = new List<string>();
            string filePath = ConfigurationManager.AppSettings["FilePath"];
            string SourceFilePath = filePath + "OpenCallReportResults.xlsx";
            string DestinationFile = filePath + "Completed/OpenCallReportResults.xlsx";

            var contents = File.ReadAllText(SourceFilePath).Split('\n');

            foreach (string line in contents)
            {
                if (string.IsNullOrEmpty(line)) continue;
                string lineVal = line.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ').Replace('\\', ' ');
                contentsList.Add(lineVal);
            }

            using (var stream = File.CreateText(DestinationFile))
            {
                foreach (string row in contentsList)
                {
                    stream.WriteLine(row);
                }
            }

            File.Delete(SourceFilePath);
        }
    }
}
