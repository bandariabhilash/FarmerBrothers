using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Configuration;

namespace FBCall.Models
{
    public class Utility
    {

        public static string GetStringWithNewLine(string note)
        {
            string result = string.Empty;
            string[] notes = note.Replace("\\n", "@").Split('@');
            foreach (string item in notes)
            {
                result += item + Environment.NewLine;
            }

            return result;
        }

        public static IndexCounter GetIndexCounter(string indexName, int countValue)
        {
            /*IndexCounter counter = FarmerBrothersEntities.IndexCounters.Where(i => i.IndexName == indexName).FirstOrDefault();
            if (counter == null)
            {
                counter = new IndexCounter()
                {
                    IndexName = indexName,
                    IndexValue = 10000000
                };
                FarmerBrothersEntities.IndexCounters.Add(counter);
            }

            return counter;*/

            using (FBWWOCallEntities newEntity = new FBWWOCallEntities())
            {
                IndexCounter counter = newEntity.IndexCounters.Where(i => i.IndexName == indexName).FirstOrDefault();
                if (counter == null)
                {
                    counter = new IndexCounter()
                    {
                        IndexName = indexName,
                        IndexValue = 10000000
                    };
                    newEntity.IndexCounters.Add(counter);
                }

                string query = @"UPDATE IndexCounter SET IndexValue = " + (counter.IndexValue + countValue) + " WHERE IndexName = '" + indexName + "'";
                newEntity.Database.ExecuteSqlCommand(query);

                return counter;
            }

        }

        public static IndexCounter GetIndexCounter(string indexName, FBWWOCallEntities MarsServiceEntities)
        {
            IndexCounter counter = MarsServiceEntities.IndexCounters.Where(i => i.IndexName == indexName).FirstOrDefault();
            if (counter == null)
            {
                counter = new IndexCounter()
                {
                    IndexName = indexName,
                    IndexValue = 10000000
                };
                MarsServiceEntities.IndexCounters.Add(counter);
            }

            return counter;
        }

        public static ZonePriority GetCustomerZonePriority(FBWWOCallEntities MarsServiceEntitites, string zipCode)
        {
            ZonePriority zonePriority = null;
            ZoneZip zonezip = MarsServiceEntitites.ZoneZips.FirstOrDefault(z => z.ZipCode == zipCode.Substring(0, 5));
            if (zonezip != null)
            {
                zonePriority = MarsServiceEntitites.ZonePriorities.FirstOrDefault(zp => zp.ZoneIndex == zonezip.ZoneIndex);
            }
            return zonePriority;
        }

        public static string FormatPhoneNumber(string phoneNumber)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(phoneNumber))
                {
                    phoneNumber = Regex.Replace(phoneNumber, @"[^\d]", "");
                    phoneNumber = Regex.Replace(phoneNumber, @"\s+", "");
                    phoneNumber = phoneNumber.Substring(0, 10);
                    int xposition = phoneNumber.ToUpper().IndexOf('X');
                    if (phoneNumber.Length == 10)
                    {
                        phoneNumber = Regex.Replace(phoneNumber, @"-+", "");
                        phoneNumber = Regex.Replace(phoneNumber, @"\s+", "");
                        phoneNumber = String.Format("{0:(###)###-#### }", double.Parse(phoneNumber));
                    }
                    else if (xposition > 0)
                    {
                        string newPhoneNumber = phoneNumber.Substring(0, xposition);

                        newPhoneNumber = Regex.Replace(newPhoneNumber, @"-+", "");
                        newPhoneNumber = Regex.Replace(newPhoneNumber, @"\s+", "");
                        newPhoneNumber = String.Format("{0:(###)###-#### }", double.Parse(newPhoneNumber));

                        phoneNumber = newPhoneNumber + phoneNumber.Substring(xposition);

                    }
                }
            }
            catch (Exception e)
            {

            }
            return phoneNumber;
        }
        public static TechHierarchyView GetTechDataByResponsibleTechId(FBWWOCallEntities MarsServiceEntities, int responsibleTechId)
        {
            string query = @"SELECT * FROM vw_tech_hierarchy where TechID = " + responsibleTechId.ToString();

            return MarsServiceEntities.Database.SqlQuery<TechHierarchyView>(query).FirstOrDefault();
        }
        public static CustomerModel PopulateCustomerWithZonePriorityDetails(FBWWOCallEntities MarsServiceEntitites, CustomerModel customerModel)
        {
            TechHierarchyView techView = null;
            Contact customer = null;
            customerModel.ManagerName = WebConfigurationManager.AppSettings["ManagerName"];
            customerModel.ManagerPhone = WebConfigurationManager.AppSettings["ManagerPhone"];
            if (!string.IsNullOrEmpty(customerModel.CustomerId))
            {
                int customerId = Convert<Int32>(customerModel.CustomerId);
                customer = MarsServiceEntitites.Contacts.Where(x => x.ContactID == customerId).FirstOrDefault();
                customerModel = new CustomerModel(customer, MarsServiceEntitites);
                int FBProviderID = customer.FBProviderID == null ? 0 : Convert<Int32>(customer.FBProviderID.ToString());
                techView = Utility.GetTechDataByResponsibleTechId(MarsServiceEntitites, FBProviderID);
            }

            if (techView != null)
            {
                customerModel.PreferredProvider = techView.PreferredProvider;
                customerModel.ProviderPhone = techView.ProviderPhone;
                customerModel.DSMName = techView.DSMName;
                customerModel.DSMPhone = Utility.FormatPhoneNumber(techView.DSMPhone);
                customerModel.Branch = techView.Branch;
                customerModel.Region = techView.RegionName;
                customerModel.PricingParent = techView.PricingParent;
                customerModel.DistributorName = techView.DistributorName;
                if (!string.IsNullOrEmpty(customer.ServiceLevelCode))
                {
                    customerModel.ServiceLevel = customer.ServiceLevelCode;
                    customerModel.ServiceTier = CustomerModel.GetServiceTier(customer.ServiceLevelCode);
                }

                customerModel.Route = techView.Route;

                using (FBWWOCallEntities entities = new FBWWOCallEntities())
                {
                    int regionNum = Convert<Int32>(customer.RegionNumber);
                    var ESMDSMRSMs = entities.ESMDSMRSMs.FirstOrDefault(x => x.BranchNO == customer.Branch);
                    if (ESMDSMRSMs != null)
                    {
                        customerModel.ESMName = ESMDSMRSMs.ESMName;
                        customerModel.ESMphone = Utility.FormatPhoneNumber(ESMDSMRSMs.ESMPhone);
                        customerModel.DSMName = ESMDSMRSMs.CCMName;
                        customerModel.DSMPhone = Utility.FormatPhoneNumber(ESMDSMRSMs.CCMPhone);
                        customerModel.RSMName = ESMDSMRSMs.RSM;
                        customerModel.RSMphone = Utility.FormatPhoneNumber(ESMDSMRSMs.RSMPhone);
                    }
                    var Providers = entities.TECH_HIERARCHY.FirstOrDefault(x => x.DealerId == customer.FBProviderID);
                    if (Providers != null)
                    {
                        customerModel.FBProviderID = Providers.DealerId;
                        customerModel.PreferredProvider = Providers.CompanyName;
                        string providerPhone = string.Empty;
                        if (Providers.Phone.Replace("-", "").Length == 7)
                        {
                            providerPhone = Providers.AreaCode + Providers.Phone.Replace("-", "");
                        }
                        else
                        {
                            providerPhone = Providers.Phone;
                        }
                        customerModel.ProviderPhone = Utility.FormatPhoneNumber(providerPhone); ;
                    }
                }


            }

            return customerModel;
        }
        public static string GetCustomerTimeZone(string zipCode, FBWWOCallEntities MarsServiceEntitites)
        {
            if (zipCode.Length > 5)
            {
                zipCode = zipCode.Substring(0, 5);
            }

            string timeZone = "Eastern Standard Time";
            Zip zip = MarsServiceEntitites.Zips.Where(z => z.ZIP1 == zipCode).FirstOrDefault();
            if (zip != null)
            {
                timeZone = zip.TimeZoneName;
            }

            return GetTimeZoneShortCut(timeZone);
        }
        public static string GetTimeZoneShortCut(string zone)
        {
            string timeZone = string.Empty;
            switch (zone)
            {
                case "Mountain Standard Time":
                    timeZone = "MST";
                    break;
                case "Alaskan Standard Time":
                    timeZone = "AST";
                    break;
                case "Pacific Standard Time":
                    timeZone = "PST";
                    break;
                case "Hawaiian Standard Time":
                    timeZone = "HST";
                    break;
                case "Eastern Standard Time":
                    timeZone = "EST";
                    break;
                case "Central Standard Time":
                    timeZone = "CST";
                    break;
            }
            return timeZone;
        }
        public static DateTime GetCurrentTime(string zipCode, FBWWOCallEntities MarsServiceEntitites)
        {
            if (zipCode.Length > 5)
            {
                zipCode = zipCode.Substring(0, 5);
            }

            string query = @"Select dbo.getCustDateTime('" + zipCode + "')";
            DbRawSqlQuery<DateTime> result = MarsServiceEntitites.Database.SqlQuery<DateTime>(query);

            return result.ElementAt(0);
        }
        public static Func<string, T> GetConverter<T>()
        {
            return (x) => Convert<T>(x);
        }

        public static T Convert<T>(string val)
        {
            Type destiny = typeof(T);

            // See if we can cast           
            try
            {
                return (T)(object)val;
            }
            catch { }

            // See if we can parse
            try
            {
                return (T)destiny.InvokeMember("Parse", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.Public, null, null, new object[] { val });
            }
            catch { }

            // See if we can convert
            try
            {
                Type convertType = typeof(Convert);
                return (T)convertType.InvokeMember("To" + destiny.Name, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.Public, null, null, new object[] { val });
            }
            catch { }

            // Give up
            return default(T);
        }

        public static IEnumerable<TechHierarchyView> GetTechDataByBranchType(FBWWOCallEntities MarsServiceEntitites, string branchDesc, string branchType)
        {
            string query = string.Empty;
            if (string.IsNullOrWhiteSpace(branchType))
            {
                query = @"select distinct d.dealerid AS TechID, d.CompanyName +' - '+ d.city AS PreferredProvider from TECH_HIERARCHY d where searchType='SP' and FamilyAff !='SPT' 
                        and dealerID NOT IN (8888888,8888889,8888890,8888891,8888892,8888893,8888894,8888895,8888907,8888908,8888911,8888917,
                        8888918,8888941,8888942,8888945,8888953,9999999,8888897,9999995,9999998,8888980,8888981,8888984,9990061,9990062,9990065) order by PreferredProvider asc";
            }
            else
            {
                query = @"select distinct d.dealerid AS TechID, d.CompanyName +' - '+ d.city AS PreferredProvider from TECH_HIERARCHY d 
                            where searchType='SP' and dealerID NOT IN (8888888,8888889,8888890,8888891,8888892,8888893,8888894,8888895,8888907,8888908,8888911,8888917,
                        8888918,8888941,8888942,8888945,8888953,9999999,8888897,9999995,9999998,8888980,8888981,8888984,9990061,9990062,9990065) and FamilyAff = '" + branchType + "'"
                        + "order by PreferredProvider asc";
            }
            return MarsServiceEntitites.Database.SqlQuery<TechHierarchyView>(query);
        }
    }
}