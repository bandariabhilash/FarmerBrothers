using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FBAutoDispatch
{
    public enum ErrorCode
    {
        SUCCESS = 0,
        ERROR = 1,
    }

    public enum MailType
    {
        INFO,
        DISPATCH,
        REDIRECTED,
        SPAWN,
        SALESNOTIFICATION
    }

    public class Utility
    {

        public static DateTime GetCurrentTime(string zipCode, FBEntities FarmerBrothersEntitites)
        {
            if (zipCode != null && zipCode.Length > 5)
            {
                zipCode = zipCode.Substring(0, 5);
            }

            string query = @"Select dbo.getCustDateTime('" + zipCode + "')";
            DbRawSqlQuery<DateTime> result = FarmerBrothersEntitites.Database.SqlQuery<DateTime>(query);

            return result.ElementAt(0);
        }

        public static bool IsHoliday(DateTime StartTime)
        {
            bool isExist = false;
            using (FBEntities FarmerBrothersEntitites = new FBEntities())
            {
                var holiday = (from holday in FarmerBrothersEntitites.HolidayLists
                               where DbFunctions.TruncateTime(holday.HolidayDate) == DbFunctions.TruncateTime(StartTime)
                               select holday).FirstOrDefault();

                if (holiday != null)
                {
                    isExist = true;
                }
            }
            return isExist;
        }

        public static TechHierarchyView GetTechDataByResponsibleTechId(FBEntities FarmerBrothersEntities, int responsibleTechId)
        {
            string query = @"SELECT * FROM vw_tech_hierarchy where TechID = " + responsibleTechId.ToString();

            return FarmerBrothersEntities.Database.SqlQuery<TechHierarchyView>(query).FirstOrDefault();
        }

        public static IndexCounter GetIndexCounter(string indexName, int countValue)
        {
            using (FBEntities newEntity = new FBEntities())
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

    }
}
