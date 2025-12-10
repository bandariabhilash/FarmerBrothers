using FBWindowsServiceAsConsole;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WOScheduleEventService
{
    public class Error
    {
        #region Constants

        public const string LOG_ERROR_IN = "Error in: ";
        public const string LOG_ERROR_MESSAGE = "Error Message: ";
        public const string LOG_STACK_TRACE = "Stack Trace: ";
        public const string LOG_SOURCE = "Source: ";
        public const string LOG_TARGET_SITE = "Target Site: ";
        public const string LOG_USER_UID = "User Uid: ";

        #endregion

        public static string FormatException(Exception ex)
        {
            DbException dbEx = ex as DbException;
            string message = String.Empty;
            if (dbEx == null)
            {
                message =
                    LOG_ERROR_MESSAGE + ex.Message + Environment.NewLine +
                    LOG_STACK_TRACE + ex.StackTrace + Environment.NewLine +
                    LOG_SOURCE + ex.Source + Environment.NewLine +
                    LOG_TARGET_SITE + ex.TargetSite + Environment.NewLine +
                    LOG_USER_UID + "SCHEDULEEVENTSERVICE" + Environment.NewLine + Environment.NewLine;
            }
            else
            {
                message =
                    LOG_ERROR_MESSAGE + dbEx.Message + Environment.NewLine +
                    LOG_STACK_TRACE + dbEx.StackTrace + Environment.NewLine +
                    LOG_SOURCE + dbEx.Source + Environment.NewLine +
                    LOG_TARGET_SITE + dbEx.TargetSite + Environment.NewLine +
                    LOG_USER_UID + "SCHEDULEEVENTSERVICE" + Environment.NewLine + Environment.NewLine;
            }
            return message;
        }
        public static void LogError(Exception ex)
        {
            string Message = FormatException(ex);
            Program.WriteToFile(Message + "{0}");
            if (!Message.Contains("arterySignalR/ping"))
            {
                using (FarmerBrothersEntities entity = new FarmerBrothersEntities())
                {
                    FBActivityLog log = new FBActivityLog();
                    log.LogDate = DateTime.UtcNow;
                    log.UserId = 1;
                    log.ErrorDetails = "SCHEDULE EVENT Log: " + Message;
                    entity.FBActivityLogs.Add(log);
                    entity.SaveChanges();
                }
            }

        }
        public static void WriteLogIntoDB(string logMessage)
        {
            using (FarmerBrothersEntities entity = new FarmerBrothersEntities())
            {
                FBActivityLog log = new FBActivityLog();
                log.LogDate = DateTime.UtcNow;
                log.UserId = 1;
                log.ErrorDetails = "SCHEDULE EVENT Log: "+logMessage;
                entity.FBActivityLogs.Add(log);
                entity.SaveChanges();
            }

        }
    }
}
