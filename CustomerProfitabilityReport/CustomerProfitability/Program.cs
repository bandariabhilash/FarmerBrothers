using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CustomerProfitability
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            FBEntities fbEntity = new FBEntities();
            List<string> csvLines = new List<string>();
            try
            {
                string fileLocation = ConfigurationManager.AppSettings["FilePath"];
                List<string> Lines = null;
                DirectoryInfo d = new DirectoryInfo(fileLocation);//Assuming Test is your Folder
                FileInfo[] Files = d.GetFiles("*.csv");
                string filePath = "";
                foreach (FileInfo file in Files)
                {
                    Lines = new List<string>();
                    filePath = fileLocation + file.Name;
                    Lines = ReadCSVFile(filePath);

                    if (Lines.Count() > 0)
                    {
                        int result = InsertData(Lines, fbEntity);
                        //csvLines.AddRange(Lines);
                        if (result == 1)
                        {
                            DirectoryInfo FileToBeMoved = new DirectoryInfo(filePath);
                            string CompletedDir = fileLocation + @"CustomerProfitability_Completed\" + file.Name;
                            FileToBeMoved.MoveTo(CompletedDir);
                        }
                    }
                    else
                    {

                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        private static int InsertData(List<string> csvLines, FBEntities fbEntity)
        {
            int flag = 0;
            try
            {
                foreach (string line in csvLines)
                {
                    try
                    {
                        if (string.IsNullOrEmpty(line.Trim())) continue;

                        List<string> values = line.Trim().Split(';').ToList();
                        if (values.Count() > 0)
                        {
                            int customerId = string.IsNullOrEmpty(values[0]) ? 0 : Convert.ToInt32(values[0].Trim());
                            string type = string.IsNullOrEmpty(values[1]) ? "" : values[1].Trim();
                            string tier = string.IsNullOrEmpty(values[2]) ? "" : values[2].Trim();
                            string contrinutionMargin = string.IsNullOrEmpty(values[3]) ? "" : values[3].Trim();
                            decimal NetSales = string.IsNullOrEmpty(values[4]) ? 0 : Convert.ToDecimal(values[4].Trim());
                            string paymentTerm = string.IsNullOrEmpty(values[5]) ? "" : values[5].Trim();

                            Contact contactRec = fbEntity.Contacts.Where(c => c.ContactID == customerId).FirstOrDefault();
                            if (contactRec != null)
                            {
                                contactRec.ProfitabilityTier = tier;
                                contactRec.ContributionMargin = contrinutionMargin;
                                contactRec.NetSalesAmount = NetSales;
                                contactRec.PaymentTerm = paymentTerm;

                                fbEntity.SaveChanges();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        continue;
                    }
                }

                flag = 1;
            }
            catch
            {
                flag = 0;
            }
            return flag;
        }

        private static List<string> ReadCSVFile(string filePath)
        {
            //string fileLocation = ConfigurationManager.AppSettings["FilePath"];
            List<string> csvLines = new List<string>();
            //DirectoryInfo d = new DirectoryInfo(fileLocation);//Assuming Test is your Folder
            //FileInfo[] Files = d.GetFiles("*.csv");
            //string filePath = "";
            //foreach (FileInfo file in Files)
            {
                //filePath = fileLocation +  file.Name;

                StreamReader reader = null;
                if (File.Exists(filePath))
                {
                    reader = new StreamReader(File.OpenRead(filePath));
                    int i = 0;
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        if (i == 0) { i++; continue; }

                        csvLines.Add(line);
                        i++;
                    }

                    reader.Close();
                }
                else
                {
                    Console.WriteLine("File doesn't exist");
                }
            }

            return csvLines;
        }
    }
}
