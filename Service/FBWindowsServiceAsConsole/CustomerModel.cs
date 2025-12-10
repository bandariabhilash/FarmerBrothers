using FBWindowsServiceAsConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WOScheduleEventService
{
    public class CustomerModel
    {
        public static int GetCallsTotalCount(FarmerBrothersEntities FBE, string CustomerID)
        {
            DateTime Last12Months = DateTime.Now.AddMonths(-12);
            return FBE.Set<WorkOrder>().Where(w => w.CustomerID.ToString() == CustomerID && w.WorkorderCalltypeid == 1200 && w.WorkorderEntryDate >= Last12Months).Count();
        }

        public static string GetServiceLevelDesc(FarmerBrothersEntities FBE, string BillingCode)
        {
            return BillingCode + "  -  " + (FBE.FBBillableFeeds.Where(b => b.Code == BillingCode).FirstOrDefault()).Description;
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

    }
}
