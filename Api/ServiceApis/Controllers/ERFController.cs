using DataAccess.Db;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.IdentityModel.Tokens;
using ServiceApis.IRepository;
using ServiceApis.ISecurity;
using ServiceApis.Models;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Validation;
using System.Globalization;
using System.Security.Claims;

namespace ServiceApis.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ERFController : BaseController
    {
        private readonly IERFRepository _erfRepository;
        private readonly IConfiguration _configuration;
        public ERFController(IERFRepository erfRepository, IConfiguration configuration, ILogger<AuthController> logger, IWebHostEnvironment webHostEnvironment)
        {
            _erfRepository = erfRepository;
            _configuration = configuration;
        }

        [HttpPost]
        [Authorize]
        [Route("CreateERF")]
        public JsonResult CreateERF([FromBody] ERFRequestModel RequestData)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User.FindFirstValue(ClaimTypes.GivenName);

            ResultResponse<ERFResponseClass> result = _erfRepository.SaveERFData(RequestData, Convert.ToInt32(userId), userName);

            return Json(result);
        }

        [HttpPost]
        [Authorize]
        [Route("ERFStatusUpdate")]
        public JsonResult ERFStatusUpdate([FromBody] ERFStatusChangeRequestModel RequestData)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User.FindFirstValue(ClaimTypes.GivenName);

            ResultResponse<ERFResponseClass> result = _erfRepository.ERFStatusUpdate(RequestData, Convert.ToInt32(userId), userName);

            return Json(result);
        }

        [HttpPost]
        [Authorize]
        [Route("ERFMaintenanceDataUpsert")]
        public JsonResult ERFMaintenanceDataUpsert([FromBody] ERFMaintenanceDataModel RequestData)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User.FindFirstValue(ClaimTypes.GivenName);

            ResultResponse<ErfMaintenanceResponse> result = _erfRepository.ERFMaintenanceDataUpsert(RequestData, Convert.ToInt32(userId), userName);

            return Json(result);
        }

        //[HttpPost]
        //[Authorize]
        //[Route("ERFBrandUpsert")]
        //public JsonResult ERFBrandUpsert([FromBody] ERFMaintenanceDataModel RequestData)
        //{
        //    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        //    var userName = User.FindFirstValue(ClaimTypes.GivenName);

        //    ResultResponse<ERFResponseClass> result = _erfRepository.ERFBrandUpsert(RequestData, Convert.ToInt32(userId), userName);

        //    return Json(result);
        //}

        //[HttpPost]
        //[Authorize]
        //[Route("ERFCategoryUpsert")]
        //public JsonResult ERFCategoryUpsert([FromBody] ERFCategory RequestData)
        //{
        //    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        //    var userName = User.FindFirstValue(ClaimTypes.GivenName);

        //    ResultResponse<ERFResponseClass> result = _erfRepository.ERFCategoryUpsert(RequestData, Convert.ToInt32(userId), userName);

        //    return Json(result);
        //}

    }
}
