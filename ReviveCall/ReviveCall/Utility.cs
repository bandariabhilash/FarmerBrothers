using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Configuration;
using System;
using System.Xml;
using System.Net.Mime;
using System.IO;
using System.Xml.Serialization;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web.Helpers;
using Newtonsoft.Json.Linq;
//using FarmerBrothers.FeastLocationService;
using System.Data.Entity.Infrastructure;
using System.Text.RegularExpressions;
using System.Data.SqlClient;
using System.Web.Configuration;
using System.Data.Common;
using System.Data;
using System.Data.Entity.Validation;

namespace ReviveCall
{
    public class Utility
    {
        public static List<string> UserGroups = new List<string> { "CallCenter", "FBAccess", "WOMaintenance", "TPSPContract", "Scheduler", "Administration" };

        public static string FormatPhoneNumber(string phoneNumber)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(phoneNumber))
                {
                    phoneNumber = Regex.Replace(phoneNumber, @"[^\d]", "");
                    phoneNumber = Regex.Replace(phoneNumber, @"\s+", "");
                    if (!string.IsNullOrWhiteSpace(phoneNumber) && phoneNumber.Length > 0)
                    {
                        phoneNumber = phoneNumber.Substring(0, 10);
                    }
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

        

        public static IList<UserType> GetUserType(ReviveEntities FarmerBrothersEntitites)
        {
            IList<UserType> userType = FarmerBrothersEntitites.UserTypes.ToList();

            UserType blankType = new UserType() { TypeId = 0, TypeName = "Please Select" };
            userType.Insert(0, blankType);

            return userType;
        }
        
        public static IList<State> GetStates(ReviveEntities ReviveEntitites)
        {
            IList<State> states = ReviveEntitites.States.ToList();

            State blankState = new State() { StateCode = "n/a", StateName = "Please Select" };
            states.Insert(0, blankState);

            return states;
        }

        

        public static ZonePriority GetCustomerZonePriority(ReviveEntities FarmerBrothersEntitites, string zipCode)
        {
            ZonePriority zonePriority = null;
            ZoneZip zonezip = FarmerBrothersEntitites.ZoneZips.FirstOrDefault(z => z.ZipCode == zipCode.Substring(0, 5));
            if (zonezip != null)
            {
                zonePriority = FarmerBrothersEntitites.ZonePriorities.FirstOrDefault(zp => zp.ZoneIndex == zonezip.ZoneIndex);
            }
            return zonePriority;
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

            using (ReviveEntities newEntity = new ReviveEntities())
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

        

        public static bool isValidEmail(string inputEmail)
        {
            Regex re = new Regex(@"^[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,4}$",
                          RegexOptions.IgnoreCase);
            return re.IsMatch(inputEmail);
        }

        public static DateTime GetCurrentTime(string zipCode, ReviveEntities ReviveEntity)
        {
            if (zipCode != null && zipCode.Length > 5)
            {
                zipCode = zipCode.Substring(0, 5);
            }

            string query = @"Select dbo.getCustDateTime('" + zipCode + "')";
            DbRawSqlQuery<DateTime> result = ReviveEntity.Database.SqlQuery<DateTime>(query);

            return result.ElementAt(0);
        }
    }

    public enum ErrorCode
    {
        SUCCESS = 0,
        ERROR = 1,
    }
}