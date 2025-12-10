using FBWindowsServiceAsConsole;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WOScheduleEventService
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

        public static TechHierarchyView GetTechDataByResponsibleTechId(FarmerBrothersEntities FarmerBrothersEntities, int? responsibleTechId)
        {
            string query = @"SELECT * FROM vw_tech_hierarchy where TechID = " + responsibleTechId.ToString();

            return FarmerBrothersEntities.Database.SqlQuery<TechHierarchyView>(query).FirstOrDefault();
        }

        public static DateTime GetCurrentTime(string zipCode, FarmerBrothersEntities FarmerBrothersEntitites)
        {
            if (zipCode.Length > 5)
            {
                zipCode = zipCode.Substring(0, 5);
            }

            string query = @"Select dbo.getCustDateTime('" + zipCode + "')";
            DbRawSqlQuery<DateTime> result = FarmerBrothersEntitites.Database.SqlQuery<DateTime>(query);

            return result.ElementAt(0);
        }
    }
}
