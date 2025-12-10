using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Text;
using System.Globalization;

namespace FBCall.Models
{
    public class AutoCallGenerateModelBinder : IModelBinder
    {
        public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            HttpRequestBase request = controllerContext.HttpContext.Request;

            AutoGenerateWorkorderModel model = new AutoGenerateWorkorderModel();

            JavaScriptSerializer json_serializer = new JavaScriptSerializer();

            model.Customer = new CustomerModel();
            model.Operation = (WorkOrderManagementSubmitType)Convert.ToInt32(request.Unvalidated.Form.Get("Operation"));
            model.Notes = new NotesModel();
            model.Notes.Notes = request.Unvalidated.Form.Get("Notes");
            model.Notes.FollowUpRequestID = request.Unvalidated.Form.Get("FollowUpRequestID");
            model.callReason = request.Unvalidated.Form.Get("callReason");
            model.EquipmentLocation = request.Unvalidated.Form.Get("EquipmentLocation");
            model.CallerName = request.Unvalidated.Form.Get("CallerName");
            model.WorkorderContactPhone = request.Unvalidated.Form.Get("WorkorderContactPhone");
            model.PriorityCode = Convert.ToInt32(request.Unvalidated.Form.Get("WorkOrderPriorityHidden").Replace("\"", string.Empty).Trim());
            model.PriorityName = request.Unvalidated.Form.Get("WorkOrderPriorityNameHidden").Replace("\"", string.Empty).Trim();             

            if (!string.IsNullOrWhiteSpace(request.Unvalidated.Form.Get("WorkOrderNotesHidden")))
            {
                model.NewNotes = json_serializer.Deserialize<IList<NewNotesModel>>(request.Unvalidated.Form.Get("WorkOrderNotesHidden"));
            }
            else
            {
                model.NewNotes = new List<NewNotesModel>();
            }

            foreach (var property in model.Customer.GetType().GetProperties())
            {
                property.SetValue(model.Customer, request.Unvalidated.Form.Get(property.Name));
            }

            if (!string.IsNullOrWhiteSpace(request.Unvalidated.Form.Get("Customer.Zipcode")))
            {
                model.Customer.ZipCode = request.Unvalidated.Form.Get("Customer.Zipcode");
            }


            if (!string.IsNullOrWhiteSpace(request.Unvalidated.Form.Get("WorkOrderID")))
            {
                model.WorkOrderID = Convert.ToInt32(request.Unvalidated.Form.Get("WorkOrderID"));
            }
            if (!string.IsNullOrWhiteSpace(request.Unvalidated.Form.Get("CustomerID")))
            {
                model.CustomerID = Convert.ToInt32(request.Unvalidated.Form.Get("CustomerID"));
            }
            if (!string.IsNullOrWhiteSpace(request.Unvalidated.Form.Get("UserNameHidden")))
            {
                model.Customer.SubmittedBy = request.Unvalidated.Form.Get("UserNameHidden").Replace("\"", string.Empty).Trim(); ;
            }

            string isSpecificCheckValue = "false";
            if (request.Unvalidated.Form.Get("IsSpecificTechnician") != null)
                isSpecificCheckValue = (request.Unvalidated.Form.Get("IsSpecificTechnician").Split(','))[0];
            if (isSpecificCheckValue.Contains("true"))
                model.Notes.IsSpecificTechnician = true;
            else
                model.Notes.IsSpecificTechnician = false;
            if (!string.IsNullOrWhiteSpace(request.Unvalidated.Form.Get("PreferredProvider")))
            {
                model.Notes.TechID = request.Unvalidated.Form.Get("PreferredProvider");
            }
            return model;
        }
    }
}