using ServiceApis.Models;

namespace ServiceApis.IRepository
{
    public interface IERFRepository
    {
        ResultResponse<ERFResponseClass> SaveERFData(ERFRequestModel ErfData, int userId, string userName);
        ResultResponse<ERFResponseClass> ERFStatusUpdate(ERFStatusChangeRequestModel ErfData, int userId, string userName);
        ResultResponse<ErfMaintenanceResponse> ERFMaintenanceDataUpsert(ERFMaintenanceDataModel RequestData, int userId, string userName);
    }
}
