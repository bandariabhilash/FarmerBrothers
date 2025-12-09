using DataAccess.Db;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using ServiceApis.IRepository;
using ServiceApis.Models;
using System.Data.Entity.Core.Objects;
using System.Globalization;
using ServiceApis.Utilities;
using System.Data.Entity.Validation;
using System.Security.Claims;
using System.Data.SqlClient;
using System.Data.Entity.Core.Metadata.Edm;
using Azure.Core;
using System.Collections.Generic;

namespace ServiceApis.Repository
{
    public class ERFRepository:IERFRepository
    {
        //private readonly FBContext _context;
        private readonly ICustomerRepository _customerRepository;
        private readonly IWorkorderRepository _workorderRepository;
        public ERFRepository(FBContext context, ICustomerRepository customerRepository, IWorkorderRepository workorderRepository)
        {
            //_context = context;
            _customerRepository = customerRepository;
            _workorderRepository = workorderRepository;
        }
        /*
        public ResultResponse<ERFResponseClass> SaveERFData_old(ERFRequestModel ErfData, int userId, string userName)
        {
            ErfModel erfMdl = new ErfModel();
            CustomerModel custMdl = _customerRepository.GetCustomerDetails(ErfData.AccountNumber);
            ResultResponse<ERFResponseClass> result = new ResultResponse<ERFResponseClass>();

            Erf erfResult = new Erf();
            if (custMdl == null)
            {
                result.responseCode = 500;
                result.Message = ErfData.AccountNumber + " - "+ ErfData.MainContactName + " : Invalid Customer";
                result.IsSuccess = false;

                return result;
            }

            DateTime currentDate = DateTime.Now;
            string message = "";
            bool validFlag = true;
            if (string.IsNullOrEmpty(ErfData.OrderType))
            {
                message += " | Order Type Required ";
                validFlag = false;
            }
            if (string.IsNullOrEmpty(ErfData.ShipToBranch))
            {
                message += " | ShipTo Branch Required";
                validFlag = false;
            }
            if (ErfData.InstallDate == null)
            {
                message += " | InstallDate Date Required";
                validFlag = false;
            }
            if (ErfData.MainContactName == null)
            {
                message += " | MainContact Name Required";
                validFlag = false;
            }
            if (ErfData.MainContactNum == null)
            {
                message += " | MainContact Number Required";
                validFlag = false;
            }
            if (string.IsNullOrEmpty(ErfData.HoursofOperation))
            {
                message += " | Hours Of Operation Required";
                validFlag = false;
            }
            if (string.IsNullOrEmpty(ErfData.InstallLocation))
            {
                message += " | Install Location Required";
                validFlag = false;
            }
            if (string.IsNullOrEmpty(ErfData.SiteReady))
            {
                message += " | Site Ready Value Required";
                validFlag = false;
            }
            if (ErfData.AdditionalNSV == null)
            {
                message += " | Additional NSV Required";
                validFlag = false;
            }
            if (string.IsNullOrWhiteSpace(ErfData.ApprovalStatus))
            {
                message += " | Approval Status Required";
                validFlag = false;
            }
            if ((ErfData.EquipmentData == null || ErfData.EquipmentData.Count <= 0)
                           && (ErfData.ExpendableData == null || ErfData.ExpendableData.Count <= 0))
            {
                message += " | Equipments Or Expendables are Required";
                validFlag = false;
            }

            if (validFlag)
            {
                erfMdl.Customer = custMdl;
                erfMdl.CreatedBy = userName;
                erfMdl.CreatedByUserId = userId;
                erfMdl.CrateWorkOrder = ErfData.CreateWorkorder == null ? false : Convert.ToBoolean(ErfData.CreateWorkorder);
                erfMdl.ApprovalStatus = "Approved for Processing";
                erfMdl.OrderType = ErfData.OrderType;
                erfMdl.BranchName = ErfData.ShipToBranch;

                NotesModel nm = new NotesModel();
                nm.Notes = ErfData.ErfNotes;

                erfMdl.Notes = nm;

                erfMdl.ErfAssetsModel = new ErfAssetsModel();
                erfMdl.ErfAssetsModel.Erf = new Erf();

                erfMdl.ErfAssetsModel.Erf.CustomerMainContactName = ErfData.MainContactName;
                erfMdl.Customer.MainContactName = ErfData.MainContactName;
                erfMdl.Customer.PhoneNumber = ErfData.MainContactNum;
                erfMdl.ErfAssetsModel.Erf.Phone = ErfData.MainContactNum;
                erfMdl.ErfAssetsModel.Erf.DateErfreceived = ErfData.ERFReceivedDate;
                erfMdl.ErfAssetsModel.Erf.DateErfprocessed = ErfData.ERFProcessedDate;
                erfMdl.ErfAssetsModel.Erf.DateOnErf = ErfData.FormDate;
                erfMdl.ErfAssetsModel.Erf.OriginalRequestedDate = ErfData.InstallDate == null ? currentDate : Convert.ToDateTime(ErfData.InstallDate);
                erfMdl.ErfAssetsModel.Erf.HoursofOperation = ErfData.HoursofOperation;
                erfMdl.ErfAssetsModel.Erf.InstallLocation = ErfData.InstallLocation;
                erfMdl.ErfAssetsModel.Erf.SiteReady = ErfData.SiteReady;

                List<Fbcbe> fbcbeList = Utility.GetFbcbeList(ErfData.AccountNumber, _context);//_context.Fbcbes.Where(cbe => cbe.CurrentCustomerId == ErfData.AccountNumber).ToList();
                if (fbcbeList != null)
                {
                    erfMdl.CurrentEquipmentTotal = fbcbeList.Sum(eq => eq.InitialValue).Value;
                }
                else
                {
                    erfMdl.CurrentEquipmentTotal = 0;
                }

                erfMdl.CurrentNSV = erfMdl.Customer.NetSalesAmt;
                erfMdl.ContributionMargin = string.IsNullOrEmpty(erfMdl.Customer.ContributionMargin) ? "" : (Convert.ToDouble(erfMdl.Customer.ContributionMargin) * 100) + "%";
                erfMdl.TotalNSV = ErfData.AdditionalNSV == null ? 0 : Convert.ToDecimal(ErfData.AdditionalNSV);



                List<ERFManagementEquipmentModel> erfEqpList = new List<ERFManagementEquipmentModel>();
                List<ERFManagementExpendableModel> erfExpList = new List<ERFManagementExpendableModel>();
                foreach (ERFAccessoryModel dataEqp in ErfData.EquipmentData)
                {
                    ERFManagementEquipmentModel eqp = new ERFManagementEquipmentModel();
                    string eqpCategory = string.IsNullOrEmpty(dataEqp.Category) ? "" : dataEqp.Category.Replace('\"', ' ').Trim();
                    if (!string.IsNullOrEmpty(eqpCategory))
                    {
                        Contingent eqpCon = _context.Contingents.Where(c => c.ContingentName.ToLower() == eqpCategory.ToLower()).FirstOrDefault();
                        if (eqpCon != null)
                        {
                            eqp.Category = eqpCon.ContingentId;

                            string eqpBrand = string.IsNullOrEmpty(dataEqp.Brand) ? "" : dataEqp.Brand.Replace('\"', ' ').Trim();
                            if (!string.IsNullOrEmpty(eqpBrand))
                            {
                                ContingentDetail eqpConDtl = _context.ContingentDetails.Where(c => c.Name.ToLower() == eqpBrand.ToLower()).FirstOrDefault();
                                if (eqpConDtl != null)
                                {
                                    eqp.Brand = eqpConDtl.Id;

                                    eqp.Quantity = dataEqp.Quantity;
                                    eqp.Substitution = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(dataEqp.SubstitutionPossible.ToLower());
                                    eqp.TransactionType = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(dataEqp.TransType.ToLower());
                                    eqp.EquipmentType = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(dataEqp.EqpType.ToLower());
                                    eqp.Branch = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(dataEqp.UsingBranch.ToLower());

                                    if (dataEqp.EqpType.ToLower() == "new")
                                    {
                                        if (dataEqp.TransType.ToLower() == "case sale")
                                        {
                                            eqp.LaidInCost = Convert.ToDouble(eqpConDtl.LaidInCost);
                                            eqp.RentalCost = Convert.ToDouble(eqpConDtl.CashSale);
                                        }
                                        else if (dataEqp.TransType.ToLower() == "rental")
                                        {
                                            double laidCost = Convert.ToDouble(eqpConDtl.LaidInCost);
                                            eqp.LaidInCost = laidCost;
                                            eqp.RentalCost = (laidCost) / 24;
                                        }
                                        if (dataEqp.TransType.ToLower() == "loan")
                                        {
                                            eqp.LaidInCost = Convert.ToDouble(eqpConDtl.LaidInCost);
                                            eqp.RentalCost = 0;
                                        }
                                    }
                                    else if (dataEqp.EqpType.ToLower() == "refurb")
                                    {
                                        if (dataEqp.TransType.ToLower() == "case sale")
                                        {
                                            double laidCost = Convert.ToDouble(eqpConDtl.LaidInCost) * 0.75;
                                            eqp.LaidInCost = laidCost;
                                            eqp.RentalCost = laidCost + (0.3 * laidCost);
                                        }
                                        else if (dataEqp.TransType.ToLower() == "rental")
                                        {
                                            double laidCost = Convert.ToDouble(eqpConDtl.LaidInCost);
                                            eqp.LaidInCost = laidCost;
                                            eqp.RentalCost = (laidCost * 0.75) / 24;
                                        }
                                        if (dataEqp.TransType.ToLower() == "loan")
                                        {
                                            double laidCost = Convert.ToDouble(eqpConDtl.LaidInCost) * 0.75;
                                            eqp.LaidInCost = laidCost;
                                            eqp.RentalCost = 0;
                                        }
                                    }

                                    eqp.TotalCost = dataEqp.Quantity * eqp.LaidInCost;
                                    erfEqpList.Add(eqp);
                                }
                                else
                                {
                                    validFlag = false;
                                    message += " | Invalid Equipment Brand ";
                                }
                            }
                            else
                            {
                                validFlag = false;
                                message += " | Brand required";
                            }
                        }
                        else
                        {
                            validFlag = false;
                            message += " | Invalid Equipment Category ";
                        }
                    }
                    else
                    {
                        validFlag = false;
                        message += " | Category required";
                    }

                }
                foreach (ERFAccessoryModel dataExp in ErfData.ExpendableData)
                {
                    ERFManagementExpendableModel exp = new ERFManagementExpendableModel();
                    string expCategory = string.IsNullOrEmpty(dataExp.Category) ? "" : dataExp.Category.Replace('\"', ' ').Trim();
                    if (!string.IsNullOrEmpty(expCategory))
                    {
                        Contingent expCon = _context.Contingents.Where(c => c.ContingentName.ToLower() == expCategory.ToLower()).FirstOrDefault();
                        if (expCon != null)
                        {
                            exp.Category = expCon.ContingentId;

                            string expBrand = string.IsNullOrEmpty(dataExp.Brand) ? "" : dataExp.Brand.Replace('\"', ' ').Trim();
                            ContingentDetail expConDtl = _context.ContingentDetails.Where(c => c.Name.ToLower() == expBrand.ToLower()).FirstOrDefault();
                            if (expConDtl != null)
                            {
                                exp.Brand = expConDtl.Id;

                                exp.Quantity = dataExp.Quantity;
                                exp.Substitution = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(dataExp.SubstitutionPossible.ToLower());
                                exp.TransactionType = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(dataExp.TransType.ToLower());
                                exp.EquipmentType = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(dataExp.EqpType.ToLower());
                                exp.Branch = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(dataExp.UsingBranch.ToLower());


                                if (dataExp.TransType.ToLower() == "case sale")
                                {
                                    double laidCost = Convert.ToDouble(expConDtl.LaidInCost);
                                    exp.LaidInCost = laidCost;
                                    exp.RentalCost = Convert.ToDouble(expConDtl.CashSale);
                                }
                                else if (dataExp.TransType.ToLower() == "rental")
                                {
                                    double laidCost = Convert.ToDouble(expConDtl.LaidInCost);
                                    exp.LaidInCost = laidCost;
                                    exp.RentalCost = Convert.ToDouble(expConDtl.Rental);
                                }
                                if (dataExp.TransType.ToLower() == "loan")
                                {
                                    double laidCost = Convert.ToDouble(expConDtl.LaidInCost);
                                    exp.LaidInCost = laidCost;
                                    exp.RentalCost = 0;
                                }

                                exp.TotalCost = dataExp.Quantity * exp.LaidInCost;

                                erfExpList.Add(exp);
                            }
                            else
                            {
                                validFlag = false;
                                message += " | Invalid Expendable Brand ";
                            }
                        }
                        else
                        {
                            validFlag = false;
                            message += " | Invalid Expendable Category ";
                        }
                    }

                }



                erfMdl.ErfAssetsModel.EquipmentList = erfEqpList;
                erfMdl.ErfAssetsModel.ExpendableList = erfExpList;


                Erf erf = new Erf();
                int resultValue = ERFSave(erfMdl, userId, userName, out erf, out message);

                result.responseCode = 200;
                result.Data = new ERFResponseClass();
                result.Data.WorkorderId = Convert.ToInt32(erf.WorkorderId);
                result.Data.ERFId = Convert.ToInt32(erf.ErfId);
                result.Message = "";
                result.IsSuccess = true;

                return result;


            }
            else
            {
                result.responseCode = 500;
                result.Data = new ERFResponseClass();
                result.Data.WorkorderId = 0;
                result.Data.ERFId = 0;
                result.Message = "Save ERF Failed, Please check the Required data \n\r" + message;
                result.IsSuccess = false;

                return result;
            }
        }
        */
        public ResultResponse<ERFResponseClass> SaveERFData(ERFRequestModel ErfData, int userId, string userName)
        {
            using (var _context = new FBContext())
            {
                using (var transaction = _context.Database.BeginTransaction())
                {
                    ErfModel erfMdl = new ErfModel();
                    //CustomerModel custMdl = _customerRepository.GetCustomerDetails(ErfData.AccountNumber);
                    ResultResponse<ERFResponseClass> result = new ResultResponse<ERFResponseClass>();

                    Erf erfResult = new Erf();
                    //if (custMdl == null)
                    //{
                    //    result.responseCode = 500;
                    //    result.Message = ErfData.AccountNumber + " - " + ErfData.MainContactName + " : Invalid Customer";
                    //    result.IsSuccess = false;

                    //    return result;
                    //}

                    var existingErf = _context.Erves.Where(e => e.ErfId == ErfData.ErfId.ToString()).FirstOrDefault();
                    if (existingErf != null)
                    {
                        transaction.Rollback();

                        result.responseCode = 500;
                        result.Message = "ErfId already exists, Please provide a new erfid";
                        result.IsSuccess = false;

                        return result;
                    }

                    DateTime currentDate = DateTime.Now;
                    string message = "";
                    bool validFlag = isValidERFData(ErfData, _context, out message);

                    CustomerModel custMdl = new CustomerModel();
                    if (ErfData.AccountNumber != 0)
                    {
                        erfMdl.Customer = new CustomerModel();
                        custMdl = _customerRepository.GetCustomerDetails(ErfData.AccountNumber);

                        if (custMdl == null)
                        {
                            if (string.IsNullOrWhiteSpace(ErfData.CustomerName))
                            {
                                message += "| CustomerName Required";
                                validFlag = false;
                            }
                            if (string.IsNullOrWhiteSpace(ErfData.Address1))
                            {
                                message += "| Address1 Required";
                                validFlag = false;
                            }
                            if (string.IsNullOrWhiteSpace(ErfData.City))
                            {
                                message += "| City Required";
                                validFlag = false;
                            }
                            if (string.IsNullOrWhiteSpace(ErfData.State))
                            {
                                message += "| State Required";
                                validFlag = false;
                            }
                            if (string.IsNullOrWhiteSpace(ErfData.PostalCode))
                            {
                                message += "| PostalCode Required";
                                validFlag = false;
                            }

                            //if (!validFlag)
                            //{
                            //    result.responseCode = 500;
                            //    result.Message = message;
                            //    result.IsSuccess = false;

                            //    return result;
                            //}
                            //else
                            //{
                            //    workorderModel.Customer = new CustomerModel();
                            //    workorderModel.Customer.CustomerId = RequestData.AccountNumber.ToString();
                            //    workorderModel.Customer.Address = RequestData.Address1;
                            //    workorderModel.Customer.Address2 = RequestData.Address2;
                            //    workorderModel.Customer.City = RequestData.City;
                            //    workorderModel.Customer.State = RequestData.State;
                            //    workorderModel.Customer.ZipCode = RequestData.PostalCode;
                            //    workorderModel.Customer.MainContactName = RequestData.MainContactName;
                            //    workorderModel.Customer.PhoneNumber = RequestData.MainContactNum;

                            //    int custSaveResult = CustomerModel.saveCustomerDetails(workorderModel.Customer, _context);
                            //    if (custSaveResult == 0)
                            //    {                            
                            //        result.responseCode = 500;
                            //        result.Message = "Customer Details saving Failed";
                            //        result.IsSuccess = false;

                            //        return result;
                            //    }
                            //    else
                            //    {
                            //        custMdl = new CustomerModel();
                            //        custMdl.CustomerId = RequestData.AccountNumber.ToString();
                            //        custMdl.CustomerName = RequestData.CustomerName;
                            //        custMdl.Address = RequestData.Address1;
                            //        custMdl.Address2 = RequestData.Address2;
                            //        custMdl.City = RequestData.City;
                            //        custMdl.State = RequestData.State;
                            //        custMdl.ZipCode = RequestData.PostalCode;
                            //        custMdl.MainContactName = RequestData.MainContactName;
                            //        custMdl.PhoneNumber = RequestData.MainContactNum;
                            //    }
                            //}
                        }

                    }
                    else
                    {
                        message += "| Invalid Customer";
                        validFlag = false;
                    }

                    if (validFlag)
                    {

                        erfMdl.Customer = new CustomerModel();
                        erfMdl.Customer.CustomerId = ErfData.AccountNumber.ToString();
                        erfMdl.Customer.Address = ErfData.Address1;
                        erfMdl.Customer.Address2 = ErfData.Address2;
                        erfMdl.Customer.City = ErfData.City;
                        erfMdl.Customer.State = ErfData.State;
                        erfMdl.Customer.ZipCode = ErfData.PostalCode;
                        erfMdl.Customer.MainContactName = ErfData.MainContactName;
                        erfMdl.Customer.PhoneNumber = ErfData.MainContactNum;

                        if (custMdl == null)
                        {
                            int custSaveResult = CustomerModel.saveCustomerDetails(erfMdl.Customer, _context);
                            if (custSaveResult == 0)
                            {
                                transaction.Rollback();

                                result.responseCode = 500;
                                result.Message = "Customer Details saving Failed";
                                result.IsSuccess = false;

                                return result;
                            }
                            else
                            {
                                custMdl = new CustomerModel();
                                custMdl.CustomerId = ErfData.AccountNumber.ToString();
                                custMdl.CustomerName = ErfData.CustomerName;
                                custMdl.Address = ErfData.Address1;
                                custMdl.Address2 = ErfData.Address2;
                                custMdl.City = ErfData.City;
                                custMdl.State = ErfData.State;
                                custMdl.ZipCode = ErfData.PostalCode;
                                custMdl.MainContactName = ErfData.MainContactName;
                                custMdl.PhoneNumber = ErfData.MainContactNum;
                            }
                        }
                        erfMdl.ErfId = ErfData.ErfId;
                        erfMdl.Customer = custMdl;
                        erfMdl.Customer.CustomerId = ErfData.AccountNumber.ToString();
                        erfMdl.CreatedBy = userName;
                        erfMdl.CreatedByUserId = userId;
                        erfMdl.CrateWorkOrder = ErfData.CreateWorkorder == null ? false : Convert.ToBoolean(ErfData.CreateWorkorder);
                        erfMdl.ApprovalStatus = "Approved for Processing";
                        erfMdl.OrderType = ErfData.OrderType;
                        erfMdl.BranchName = ErfData.ShipToBranch;

                        NotesModel nm = new NotesModel();
                        nm.Notes = ErfData.ErfNotes;

                        erfMdl.Notes = nm;

                        erfMdl.ErfAssetsModel = new ErfAssetsModel();
                        erfMdl.ErfAssetsModel.Erf = new Erf();

                        erfMdl.ErfAssetsModel.Erf.CustomerMainContactName = ErfData.MainContactName;
                        erfMdl.Customer.MainContactName = ErfData.MainContactName;
                        erfMdl.Customer.PhoneNumber = ErfData.MainContactNum;
                        erfMdl.ErfAssetsModel.Erf.Phone = ErfData.MainContactNum;
                        erfMdl.ErfAssetsModel.Erf.DateErfreceived = ErfData.ERFReceivedDate;
                        erfMdl.ErfAssetsModel.Erf.DateErfprocessed = ErfData.ERFProcessedDate;
                        erfMdl.ErfAssetsModel.Erf.DateOnErf = ErfData.FormDate;
                        erfMdl.ErfAssetsModel.Erf.OriginalRequestedDate = ErfData.InstallDate == null ? currentDate : Convert.ToDateTime(ErfData.InstallDate);
                        erfMdl.ErfAssetsModel.Erf.HoursofOperation = ErfData.HoursofOperation;
                        erfMdl.ErfAssetsModel.Erf.InstallLocation = ErfData.InstallLocation;
                        erfMdl.ErfAssetsModel.Erf.SiteReady = ErfData.SiteReady;

                        List<Fbcbe> fbcbeList = Utility.GetFbcbeList(ErfData.AccountNumber, _context);//_context.Fbcbes.Where(cbe => cbe.CurrentCustomerId == ErfData.AccountNumber).ToList();
                        if (fbcbeList != null)
                        {
                            erfMdl.CurrentEquipmentTotal = fbcbeList.Sum(eq => eq.InitialValue).Value;
                        }
                        else
                        {
                            erfMdl.CurrentEquipmentTotal = 0;
                        }

                        erfMdl.CurrentNSV = erfMdl.Customer.NetSalesAmt;
                        erfMdl.ContributionMargin = string.IsNullOrEmpty(erfMdl.Customer.ContributionMargin) ? "" : (Convert.ToDouble(erfMdl.Customer.ContributionMargin) * 100) + "%";
                        erfMdl.TotalNSV = ErfData.AdditionalNSV == null ? 0 : Convert.ToDecimal(ErfData.AdditionalNSV);

                        List<ERFManagementEquipmentModel> erfEqpList = new List<ERFManagementEquipmentModel>();
                        List<ERFManagementExpendableModel> erfExpList = new List<ERFManagementExpendableModel>();
                        foreach (ERFAccessoryModel dataEqp in ErfData.EquipmentData)
                        {
                            ERFManagementEquipmentModel eqp = new ERFManagementEquipmentModel();
                            string eqpCategory = string.IsNullOrEmpty(dataEqp.Category) ? "" : dataEqp.Category.Replace('\"', ' ').Trim();
                            if (!string.IsNullOrEmpty(eqpCategory))
                            {
                                Contingent eqpCon = _context.Contingents.Where(c => c.ContingentName.ToLower() == eqpCategory.ToLower()).FirstOrDefault();
                                if (eqpCon != null)
                                {
                                    eqp.Category = eqpCon.ContingentId;

                                    string eqpBrand = string.IsNullOrEmpty(dataEqp.Brand) ? "" : dataEqp.Brand.Replace('\"', ' ').Trim();
                                    if (!string.IsNullOrEmpty(eqpBrand))
                                    {
                                        ContingentDetail eqpConDtl = _context.ContingentDetails.Where(c => c.Name.ToLower() == eqpBrand.ToLower()).FirstOrDefault();
                                        if (eqpConDtl != null)
                                        {
                                            eqp.Brand = eqpConDtl.Id;

                                            eqp.Quantity = dataEqp.Quantity;
                                            eqp.Substitution = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(dataEqp.SubstitutionPossible.ToLower());
                                            eqp.TransactionType = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(dataEqp.TransType.ToLower());
                                            eqp.EquipmentType = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(dataEqp.EqpType.ToLower());
                                            eqp.Branch = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(dataEqp.UsingBranch.ToLower());

                                            if (dataEqp.EqpType.ToLower() == "new")
                                            {
                                                if (dataEqp.TransType.ToLower() == "case sale")
                                                {
                                                    eqp.LaidInCost = Convert.ToDouble(eqpConDtl.LaidInCost);
                                                    eqp.RentalCost = Convert.ToDouble(eqpConDtl.CashSale);
                                                }
                                                else if (dataEqp.TransType.ToLower() == "rental")
                                                {
                                                    double laidCost = Convert.ToDouble(eqpConDtl.LaidInCost);
                                                    eqp.LaidInCost = laidCost;
                                                    eqp.RentalCost = (laidCost) / 24;
                                                }
                                                if (dataEqp.TransType.ToLower() == "loan")
                                                {
                                                    eqp.LaidInCost = Convert.ToDouble(eqpConDtl.LaidInCost);
                                                    eqp.RentalCost = 0;
                                                }
                                            }
                                            else if (dataEqp.EqpType.ToLower() == "refurb")
                                            {
                                                if (dataEqp.TransType.ToLower() == "case sale")
                                                {
                                                    double laidCost = Convert.ToDouble(eqpConDtl.LaidInCost) * 0.75;
                                                    eqp.LaidInCost = laidCost;
                                                    eqp.RentalCost = laidCost + (0.3 * laidCost);
                                                }
                                                else if (dataEqp.TransType.ToLower() == "rental")
                                                {
                                                    double laidCost = Convert.ToDouble(eqpConDtl.LaidInCost);
                                                    eqp.LaidInCost = laidCost;
                                                    eqp.RentalCost = (laidCost * 0.75) / 24;
                                                }
                                                if (dataEqp.TransType.ToLower() == "loan")
                                                {
                                                    double laidCost = Convert.ToDouble(eqpConDtl.LaidInCost) * 0.75;
                                                    eqp.LaidInCost = laidCost;
                                                    eqp.RentalCost = 0;
                                                }
                                            }

                                            eqp.TotalCost = dataEqp.Quantity * eqp.LaidInCost;
                                            erfEqpList.Add(eqp);
                                        }
                                        else
                                        {
                                            validFlag = false;
                                            message += " | Invalid Equipment Brand ";
                                        }
                                    }
                                    else
                                    {
                                        validFlag = false;
                                        message += " | Brand required";
                                    }
                                }
                                else
                                {
                                    validFlag = false;
                                    message += " | Invalid Equipment Category ";
                                }
                            }
                            else
                            {
                                validFlag = false;
                                message += " | Category required";
                            }

                        }
                        foreach (ERFAccessoryModel dataExp in ErfData.ExpendableData)
                        {
                            ERFManagementExpendableModel exp = new ERFManagementExpendableModel();
                            string expCategory = string.IsNullOrEmpty(dataExp.Category) ? "" : dataExp.Category.Replace('\"', ' ').Trim();
                            if (!string.IsNullOrEmpty(expCategory))
                            {
                                Contingent expCon = _context.Contingents.Where(c => c.ContingentName.ToLower() == expCategory.ToLower()).FirstOrDefault();
                                if (expCon != null)
                                {
                                    exp.Category = expCon.ContingentId;

                                    string expBrand = string.IsNullOrEmpty(dataExp.Brand) ? "" : dataExp.Brand.Replace('\"', ' ').Trim();
                                    ContingentDetail expConDtl = _context.ContingentDetails.Where(c => c.Name.ToLower() == expBrand.ToLower()).FirstOrDefault();
                                    if (expConDtl != null)
                                    {
                                        exp.Brand = expConDtl.Id;

                                        exp.Quantity = dataExp.Quantity;
                                        exp.Substitution = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(dataExp.SubstitutionPossible.ToLower());
                                        exp.TransactionType = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(dataExp.TransType.ToLower());
                                        exp.EquipmentType = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(dataExp.EqpType.ToLower());
                                        exp.Branch = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(dataExp.UsingBranch.ToLower());


                                        if (dataExp.TransType.ToLower() == "case sale")
                                        {
                                            double laidCost = Convert.ToDouble(expConDtl.LaidInCost);
                                            exp.LaidInCost = laidCost;
                                            exp.RentalCost = Convert.ToDouble(expConDtl.CashSale);
                                        }
                                        else if (dataExp.TransType.ToLower() == "rental")
                                        {
                                            double laidCost = Convert.ToDouble(expConDtl.LaidInCost);
                                            exp.LaidInCost = laidCost;
                                            exp.RentalCost = Convert.ToDouble(expConDtl.Rental);
                                        }
                                        if (dataExp.TransType.ToLower() == "loan")
                                        {
                                            double laidCost = Convert.ToDouble(expConDtl.LaidInCost);
                                            exp.LaidInCost = laidCost;
                                            exp.RentalCost = 0;
                                        }

                                        exp.TotalCost = dataExp.Quantity * exp.LaidInCost;

                                        erfExpList.Add(exp);
                                    }
                                    else
                                    {
                                        validFlag = false;
                                        message += " | Invalid Expendable Brand ";
                                    }
                                }
                                else
                                {
                                    validFlag = false;
                                    message += " | Invalid Expendable Category ";
                                }
                            }

                        }



                        erfMdl.ErfAssetsModel.EquipmentList = erfEqpList;
                        erfMdl.ErfAssetsModel.ExpendableList = erfExpList;

                        try
                        {
                            Erf erf = new Erf();
                            int resultValue = ERFSave(erfMdl, userId, userName, _context, out erf, out message);

                            transaction.Commit();

                            result.responseCode = 200;
                            result.Data = new ERFResponseClass();
                            result.Data.WorkorderId = Convert.ToInt32(erf.WorkorderId);
                            result.Data.ERFId = Convert.ToInt32(erf.ErfId);
                            result.Message = "";
                            result.IsSuccess = true;

                            return result;
                        }
                        catch(Exception ex)
                        {
                            transaction.Rollback();

                            result.responseCode = 500;
                            result.Data = new ERFResponseClass();
                            result.Data.WorkorderId = 0;
                            result.Data.ERFId = 0;
                            result.Message = ex.Message;
                            result.IsSuccess = false;

                            return result;
                        }
                    }
                    else
                    {
                        transaction.Rollback();

                        result.responseCode = 500;
                        result.Data = new ERFResponseClass();
                        result.Data.WorkorderId = 0;
                        result.Data.ERFId = 0;
                        result.Message = message;
                        result.IsSuccess = false;

                        return result;
                    }
                }
            }
        }

        public bool isValidERFData(ERFRequestModel ErfData, FBContext _context, out string message)
        {
            bool validFlag = true;
            message = "";
            if (ErfData.ErfId <= 0)
            {
                validFlag = false;
                message += " | ErfId required";
            }
            if (string.IsNullOrEmpty(ErfData.OrderType))
            {
                message += " | Order Type Required";
                validFlag = false;
            }
            if (string.IsNullOrEmpty(ErfData.ShipToBranch))
            {
                message += " | ShipTo Branch Required";
                validFlag = false;
            }
            if (ErfData.InstallDate == null)
            {
                message += " | InstallDate Date Required";
                validFlag = false;
            }
            if (ErfData.MainContactName == null)
            {
                message += " | MainContact Name Required";
                validFlag = false;
            }
            if (ErfData.MainContactNum == null)
            {
                message += " | MainContact Number Required";
                validFlag = false;
            }
            if (string.IsNullOrEmpty(ErfData.HoursofOperation))
            {
                message += " | Hours Of Operation Required";
                validFlag = false;
            }
            if (string.IsNullOrEmpty(ErfData.InstallLocation))
            {
                message += " | Install Location Required";
                validFlag = false;
            }
            if (string.IsNullOrEmpty(ErfData.SiteReady))
            {
                message += " | Site Ready Value Required";
                validFlag = false;
            }
            if (ErfData.AdditionalNSV == null)
            {
                message += " | Additional NSV Required";
                validFlag = false;
            }
            if (string.IsNullOrWhiteSpace(ErfData.ApprovalStatus))
            {
                message += " | Approval Status Required";
                validFlag = false;
            }
            
            if ((ErfData.EquipmentData == null || ErfData.EquipmentData.Count <= 0)
                           && (ErfData.ExpendableData == null || ErfData.ExpendableData.Count <= 0))
            {
                message += " | Equipments Or Expendables are Required";
                validFlag = false;
            }
            else
            {
                foreach (ERFAccessoryModel dataEqp in ErfData.EquipmentData)
                {
                    ERFManagementEquipmentModel eqp = new ERFManagementEquipmentModel();
                    string eqpCategory = string.IsNullOrEmpty(dataEqp.Category) ? "" : dataEqp.Category.Replace('\"', ' ').Trim();
                    if (!string.IsNullOrEmpty(eqpCategory))
                    {
                        Contingent eqpCon = _context.Contingents.Where(c => c.ContingentName.ToLower() == eqpCategory.ToLower()).FirstOrDefault();
                        if (eqpCon == null)
                        {
                            validFlag = false;
                            message += " | Invalid Equipment Category ";
                        }
                    }
                    else
                    {
                        validFlag = false;
                        message += " | Category required";
                    }

                    string eqpBrand = string.IsNullOrEmpty(dataEqp.Brand) ? "" : dataEqp.Brand.Replace('\"', ' ').Trim();
                    if (!string.IsNullOrEmpty(eqpBrand))
                    {
                        ContingentDetail eqpConDtl = _context.ContingentDetails.Where(c => c.Name.ToLower() == eqpBrand.ToLower()).FirstOrDefault();
                        if (eqpConDtl == null)
                        {
                            validFlag = false;
                            message += " | Invalid Equipment Brand ";
                        }
                    }
                    else
                    {
                        validFlag = false;
                        message += " | Brand required";
                    }

                    if (dataEqp.Quantity <= 0)
                    {
                        validFlag = false;
                        message += " | Quantity required";
                    }
                    if (string.IsNullOrWhiteSpace(dataEqp.UsingBranch))
                    {
                        validFlag = false;
                        message += " | UsingBranch required";
                    }
                    if (string.IsNullOrWhiteSpace(dataEqp.SubstitutionPossible))
                    {
                        validFlag = false;
                        message += " | SubstitutionPossible required";
                    }
                    if (string.IsNullOrWhiteSpace(dataEqp.TransType))
                    {
                        validFlag = false;
                        message += " | TransType required";
                    }
                    if (string.IsNullOrWhiteSpace(dataEqp.EqpType))
                    {
                        validFlag = false;
                        message += " | EqpType required";
                    }
                    //if (dataEqp.LaidInCost < 0)
                    //{
                    //    validFlag = false;
                    //    message += " | LaidInCost required";
                    //}

                }
                foreach (ERFAccessoryModel dataExp in ErfData.ExpendableData)
                {
                    ERFManagementExpendableModel exp = new ERFManagementExpendableModel();
                    string eqpCategory = string.IsNullOrEmpty(dataExp.Category) ? "" : dataExp.Category.Replace('\"', ' ').Trim();
                    if (!string.IsNullOrEmpty(eqpCategory))
                    {
                        Contingent eqpCon = _context.Contingents.Where(c => c.ContingentName.ToLower() == eqpCategory.ToLower()).FirstOrDefault();
                        if (eqpCon == null)
                        {
                            validFlag = false;
                            message += " | Invalid Expendable Category ";
                        }
                    }
                    else
                    {
                        validFlag = false;
                        message += " | Category required";
                    }

                    string eqpBrand = string.IsNullOrEmpty(dataExp.Brand) ? "" : dataExp.Brand.Replace('\"', ' ').Trim();
                    if (!string.IsNullOrEmpty(eqpBrand))
                    {
                        ContingentDetail eqpConDtl = _context.ContingentDetails.Where(c => c.Name.ToLower() == eqpBrand.ToLower()).FirstOrDefault();
                        if (eqpConDtl == null)
                        {
                            validFlag = false;
                            message += " | Invalid Expendable Brand ";
                        }
                    }
                    else
                    {
                        validFlag = false;
                        message += " | Brand required";
                    }

                    if (dataExp.Quantity <= 0)
                    {
                        validFlag = false;
                        message += " | Quantity required";
                    }
                    if (string.IsNullOrWhiteSpace(dataExp.UsingBranch))
                    {
                        validFlag = false;
                        message += " | UsingBranch required";
                    }
                    if (string.IsNullOrWhiteSpace(dataExp.SubstitutionPossible))
                    {
                        validFlag = false;
                        message += " | SubstitutionPossible required";
                    }
                    if (string.IsNullOrWhiteSpace(dataExp.TransType))
                    {
                        validFlag = false;
                        message += " | TransType required";
                    }
                    if (string.IsNullOrWhiteSpace(dataExp.EqpType))
                    {
                        validFlag = false;
                        message += " | EqpType required";
                    }
                    //if (dataExp.LaidInCost < 0)
                    //{
                    //    validFlag = false;
                    //    message += " | LaidInCost required";
                    //}
                }
            }

            return validFlag;
        }
        private int ERFSave(ErfModel erfModel, int userId, string userName, FBContext _context, out Erf erf, out string message)
        {
            int returnValue = 0;
            message = string.Empty;
            erf = null;

            //using (var _context = new FBContext())
            {
               // using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        WorkOrder workOrder = null;
                        /*IndexCounterModel counter = Utility.GetIndexCounter("ERFNO", 1);
                        counter.IndexValue++;

                        erfModel.ErfAssetsModel.Erf.ErfId = counter.IndexValue.Value.ToString();*/
                        erfModel.ErfAssetsModel.Erf.ErfId = erfModel.ErfId.ToString();

                        if (erfModel.Customer != null)
                        {
                            erf = new Erf();
                            erf.ErfId = erfModel.ErfAssetsModel.Erf.ErfId;
                            erf.CustomerAddress = erfModel.Customer.Address;
                            erf.CustomerCity = erfModel.Customer.City;
                            if (!string.IsNullOrWhiteSpace(erfModel.Customer.CustomerId))
                            {
                                erf.CustomerId = new Nullable<int>(Convert.ToInt32(erfModel.Customer.CustomerId));
                            }
                            erf.CustomerMainContactName = erfModel.Customer.MainContactName;
                            erf.CustomerMainEmail = erfModel.Customer.MainEmailAddress;
                            erf.CustomerName = erfModel.Customer.CustomerName;

                            if (erfModel.Customer.PhoneNumber != null)
                            {
                                erf.CustomerPhone = erfModel.Customer.PhoneNumber.Replace("(", "").Replace(")", "").Replace("-", "");
                            }
                            erf.CustomerPhoneExtn = erfModel.Customer.PhoneExtn;
                            erf.CustomerState = erfModel.Customer.State;
                            erf.CustomerZipCode = erfModel.Customer.ZipCode;

                            erf.EntryDate = DateTime.Now; //Utility.GetCurrentTime(erfModel.Customer.ZipCode);
                            erf.ModifiedDate = erf.EntryDate;

                            erf.ModifiedUserId = userId;
                            erf.EntryUserId = userId;


                            erf.DateOnErf = erfModel.ErfAssetsModel.Erf.DateOnErf;
                            erf.DateErfreceived = erfModel.ErfAssetsModel.Erf.DateErfreceived;
                            erf.DateErfprocessed = erfModel.ErfAssetsModel.Erf.DateErfprocessed;
                            erf.OriginalRequestedDate = erfModel.ErfAssetsModel.Erf.OriginalRequestedDate;
                            erf.HoursofOperation = erfModel.ErfAssetsModel.Erf.HoursofOperation;
                            erf.InstallLocation = erfModel.ErfAssetsModel.Erf.InstallLocation;
                            erf.UserName = erfModel.ErfAssetsModel.Erf.UserName;
                            erf.Phone = Utilities.Utility.FormatPhoneNumber(erfModel.ErfAssetsModel.Erf.Phone);
                            erf.TotalNsv = erfModel.TotalNSV;

                            /*decimal currentNSV = Convert.ToDecimal(erfModel.TotalNSV + erfModel.CurrentNSV);
                            erf.CurrentNSV = currentNSV;*/

                            erf.CurrentNsv = erfModel.CurrentNSV;
                            erf.ContributionMargin = string.IsNullOrEmpty(erfModel.ContributionMargin) ? "" : erfModel.ContributionMargin;

                            erf.CurrentEqp = erfModel.CurrentEquipmentTotal;
                            erf.AdditionalEqp = erfModel.AdditionalEquipmentTotal;
                            erf.ApprovalStatus = erfModel.ApprovalStatus == null ? "" : erfModel.ApprovalStatus;

                            erf.SiteReady = erfModel.ErfAssetsModel.Erf.SiteReady;

                            erf.OrderType = erfModel.OrderType;
                            erf.ShipToBranch = erfModel.BranchName;
                            erf.ShipToJde = erfModel.ShipToCustomer;

                            //erf.ERFStatus = "Pending";

                        }
                        _context.Erves.Add(erf);

                        DateTime CurrentTime = DateTime.Now;//Utility.GetCurrentTime(erfModel.Customer.ZipCode, _context);
                        int effectedRecords = 0;
                        try
                        {
                            if (erfModel.CrateWorkOrder && erf.ApprovalStatus.ToLower() == "approved for processing")
                            {
                                WorkorderManagementModel workorderModel = new WorkorderManagementModel();
                                //workorderModel.Closure = new WorkOrderClosureModel();
                                workorderModel.Customer = erfModel.Customer;
                                workorderModel.Customer.CustomerId = erfModel.Customer.CustomerId;
                                workorderModel.Notes = erfModel.Notes;
                                //workorderModel.Operation = WorkOrderManagementSubmitType.CREATEWORKORDER;
                                workorderModel.WorkOrder = new WorkOrder();
                                workorderModel.WorkOrder.CallerName = "N/A";
                                workorderModel.WorkOrder.WorkorderContactName = "N/A";
                                workorderModel.WorkOrder.HoursOfOperation = "N/A";
                                workorderModel.WorkOrder.WorkorderCalltypeid = 1300;
                                workorderModel.WorkOrder.WorkorderCalltypeDesc = "Installation";
                                workorderModel.WorkOrder.WorkorderErfid = erfModel.ErfAssetsModel.Erf.ErfId;
                                workorderModel.WorkOrder.PriorityCode = 54;
                                workorderModel.WorkOrder.WorkOrderBrands = new List<WorkOrderBrand>();
                                WorkOrderBrand brand = new WorkOrderBrand();
                                brand.BrandId = 997;
                                workorderModel.WorkOrder.WorkOrderBrands.Add(brand);
                                workorderModel.PriorityList = new List<AllFbstatus>();
                                AllFbstatus priority = new AllFbstatus();
                                priority.FbstatusId = 54;
                                priority.Fbstatus = "P3  - PLANNED";
                                workorderModel.PriorityList.Add(priority);
                                workorderModel.NewNotes = new List<NewNotesModel>();
                                workorderModel.NewNotes = erfModel.NewNotes;

                                workorderModel.WorkOrderEquipments = new List<WorkOrderManagementEquipmentModel>();
                                workorderModel.WorkOrderEquipmentsRequested = new List<WorkOrderManagementEquipmentModel>();
                                workorderModel.WorkOrderParts = new List<WorkOrderPartModel>();
                                workorderModel.Erf = erfModel.ErfAssetsModel.Erf;



                                //jsonResult = wc.SaveWorkOrder(workorderModel, null, string.Empty, false, true);                         
                                //ResultResponse<ERFRequestModel> woResult = _workorderRepository.SaveWorkorderData(workorderModel, WOFBEntity, out workOrder, out message);
                                ResultResponse<ERFResponseClass> woResult = _workorderRepository.SaveWorkorderData(workorderModel, userId, userName, _context);

                                //JavaScriptSerializer serializer = new JavaScriptSerializer();
                                //WorkOrderResults result = serializer.Deserialize<WorkOrderResults>(serializer.Serialize(jsonResult.Data));
                                if (woResult.responseCode == 200 && woResult.IsSuccess)
                                {
                                    erf.WorkorderId = Convert.ToInt32(woResult.Data.WorkorderId);
                                    ErfWorkorderLog erfWorkOrderLog = new ErfWorkorderLog();
                                    erfWorkOrderLog.ErfId = erf.ErfId;
                                    erfWorkOrderLog.WorkorderId = Convert.ToInt32(erf.WorkorderId);
                                    _context.ErfWorkorderLogs.Add(erfWorkOrderLog);

                                    NotesHistory notesHistory = new NotesHistory()
                                    {
                                        AutomaticNotes = 1,
                                        EntryDate = CurrentTime,
                                        Notes = @"Work Order created from ERF WO#: " + Convert.ToInt32(woResult.Data.WorkorderId) + @" in “MARS”!",
                                        Userid = userId,
                                        UserName = userName,
                                        ErfId = erf.ErfId,
                                        WorkorderId = erf.WorkorderId,
                                        IsDispatchNotes = 0
                                    };

                                    _context.NotesHistories.Add(notesHistory);
                                }
                            }


                        }
                        catch (DbEntityValidationException e)
                        {
                            string errormsg = string.Empty;
                            foreach (var eve in e.EntityValidationErrors)
                            {
                                errormsg += string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
                                    eve.Entry.Entity.GetType().Name, eve.Entry.State);
                                errormsg += Environment.NewLine;
                                foreach (var ve in eve.ValidationErrors)
                                {
                                    errormsg += string.Format("- Property: \"{0}\", Error: \"{1}\"",
                                        ve.PropertyName, ve.ErrorMessage);
                                    errormsg += Environment.NewLine;
                                }
                            }
                            throw;
                        }

                        SaveNotes(erfModel, Convert.ToInt32(erf.WorkorderId), userId, userName, _context);

                        string EqpNotes = "";
                        string ExpNotes = "";
                        if (erfModel.ErfAssetsModel.EquipmentList != null)
                        {
                            int eqpCount = 1;
                            foreach (ERFManagementEquipmentModel equipment in erfModel.ErfAssetsModel.EquipmentList)
                            {
                                Fberfequipment eq = new Fberfequipment()
                                {
                                    Erfid = erf.ErfId,
                                    WorkOrderId = erf.WorkorderId,
                                    ModelNo = equipment.ModelNo,
                                    Quantity = equipment.Quantity,
                                    ProdNo = equipment.ProdNo,
                                    EquipmentType = equipment.EquipmentType,
                                    UnitPrice = Convert.ToDecimal(equipment.UnitPrice),
                                    TransactionType = equipment.TransactionType,
                                    Substitution = equipment.Substitution,
                                    Extra = equipment.Extra,
                                    Description = equipment.Description,
                                    LaidInCost = Convert.ToDecimal(equipment.LaidInCost),
                                    RentalCost = Convert.ToDecimal(equipment.RentalCost),
                                    TotalCost = Convert.ToDecimal(equipment.TotalCost),
                                    ContingentCategoryId = equipment.Category,
                                    ContingentCategoryTypeId = equipment.Brand,
                                    UsingBranch = equipment.Branch

                                };

                                Contingent eqpCon = _context.Contingents.Where(c => c.ContingentId == equipment.Category).FirstOrDefault();
                                ContingentDetail eqpConDtl = _context.ContingentDetails.Where(c => c.Id == equipment.Brand).FirstOrDefault();
                                string eqpCategoryName = "";
                                string eqpBrandName = "";
                                if (eqpCon != null)
                                {
                                    eqpCategoryName = eqpCon.ContingentName;
                                }
                                if (eqpConDtl != null)
                                {
                                    eqpBrandName = eqpConDtl.Name;
                                }

                                EqpNotes += eqpCount + ") Category: " + eqpCategoryName + ", Brand: " + eqpBrandName + ", Quantity: " + equipment.Quantity + ", UsingBranch: " + equipment.Branch + "\n\r";
                                eqpCount++;

                                _context.Fberfequipments.Add(eq);
                            }
                        }

                        if (erfModel.ErfAssetsModel.ExpendableList != null)
                        {
                            int expCount = 1;
                            foreach (ERFManagementExpendableModel expItems in erfModel.ErfAssetsModel.ExpendableList)
                            {
                                Fberfexpendable eq = new Fberfexpendable()
                                {
                                    Erfid = erf.ErfId,
                                    WorkOrderId = erf.WorkorderId,
                                    ModelNo = expItems.ModelNo,
                                    Quantity = expItems.Quantity,
                                    ProdNo = expItems.ProdNo,
                                    UnitPrice = Convert.ToDecimal(expItems.UnitPrice),
                                    TransactionType = expItems.TransactionType,
                                    Substitution = expItems.Substitution,
                                    EquipmentType = expItems.EquipmentType,
                                    Extra = expItems.Extra,
                                    Description = expItems.Description,
                                    LaidInCost = Convert.ToDecimal(expItems.LaidInCost),
                                    RentalCost = Convert.ToDecimal(expItems.RentalCost),
                                    TotalCost = Convert.ToDecimal(expItems.TotalCost),
                                    ContingentCategoryId = expItems.Category,
                                    ContingentCategoryTypeId = expItems.Brand,
                                    UsingBranch = expItems.Branch

                                };


                                Contingent expCon = _context.Contingents.Where(c => c.ContingentId == expItems.Category).FirstOrDefault();
                                ContingentDetail expConDtl = _context.ContingentDetails.Where(c => c.Id == expItems.Brand).FirstOrDefault();
                                string expCategoryName = "";
                                string expBrandName = "";
                                if (expCon != null)
                                {
                                    expCategoryName = expCon.ContingentName;
                                }
                                if (expConDtl != null)
                                {
                                    expBrandName = expConDtl.Name;
                                }

                                ExpNotes += expCount + ") Category: " + expCategoryName + ", Brand: " + expBrandName + ", Quantity: " + expItems.Quantity + ", UsingBranch: " + expItems.Branch + "\n\r";
                                expCount++;

                                _context.Fberfexpendables.Add(eq);
                            }
                        }


                        NotesHistory eqpNotesHistory = new NotesHistory()
                        {
                            AutomaticNotes = 0,
                            EntryDate = CurrentTime,
                            Notes = "Equipments: " + EqpNotes + "Expendables: " + ExpNotes,
                            Userid = userId,
                            UserName = userName,
                            ErfId = erf.ErfId,
                            WorkorderId = erf.WorkorderId,
                            IsDispatchNotes = 0
                        };

                        _context.NotesHistories.Add(eqpNotesHistory);

                        decimal? eqpTotal = Convert.ToDecimal(erfModel.ErfAssetsModel.EquipmentList.Sum(x => x.TotalCost));
                        decimal? expTotal = Convert.ToDecimal(erfModel.ErfAssetsModel.ExpendableList.Sum(x => x.TotalCost));

                        decimal GrandTotal = 0;

                        if (eqpTotal != null && expTotal != null)
                        {
                            GrandTotal = Convert.ToDecimal(eqpTotal + expTotal);
                        }
                        else if (eqpTotal != null && expTotal == null)
                        {
                            GrandTotal = Convert.ToDecimal(eqpTotal);
                        }
                        else if (eqpTotal == null && expTotal != null)
                        {
                            GrandTotal = Convert.ToDecimal(expTotal);
                        }



                        if (GrandTotal >= 10000)
                        {
                            erf.Erfstatus = "Pending CapEx-FA";
                        }
                        else
                        {
                            erf.Erfstatus = "Pending";
                        }
                        erf.CashSaleStatus = "1"; // Default value for CashSaleStatus while ERF Creation

                        int woSaveEffectRecords = _context.SaveChanges();
                        int woReturnValue = woSaveEffectRecords > 0 ? 1 : 0;

                        if (((!erfModel.CrateWorkOrder && erf.ApprovalStatus.ToLower() == "approved for processing") && woReturnValue == 1)
                            || (erfModel.CrateWorkOrder || erf.ApprovalStatus.ToLower() != "approved for processing"))
                        {
                            effectedRecords = _context.SaveChanges();
                            returnValue = effectedRecords > 0 ? 1 : 0;
                        }
                       // transaction.Commit();
                    }
                    catch(Exception ex)
                    {
                        //transaction.Rollback();
                    }
                }
            }

            return returnValue;
        }

        public void SaveNotes(ErfModel erfManagement, int erfWorkorderId , int userId, string userName, FBContext _context)
        {
            if (erfManagement.NewNotes != null)
            {

                TimeZoneInfo newTimeZoneInfo = null;
                Utility.GetCustomerTimeZone(erfManagement.Customer.ZipCode, _context);

                DateTime CurrentTime = DateTime.Now; //Utility.GetCurrentTime(erfManagement.Customer.ZipCode);

                foreach (NewNotesModel newNotesModel in erfManagement.NewNotes)
                {
                    NotesHistory notesHistory = new NotesHistory()
                    {
                        AutomaticNotes = 0,
                        EntryDate = CurrentTime,
                        Notes = newNotesModel.Text,
                        Userid = userId,
                        UserName = userName,
                        ErfId = erfManagement.ErfAssetsModel.Erf.ErfId,
                        WorkorderId = erfWorkorderId == 0 ? null : (int?)erfWorkorderId,// erfManagement.ErfAssetsModel.Erf.WorkorderID,
                        IsDispatchNotes = 0
                    };
                    _context.NotesHistories.Add(notesHistory);
                }
            }
        }

        public ResultResponse<ERFResponseClass> ERFStatusUpdate(ERFStatusChangeRequestModel ErfData, int userId, string userName)
        {
            ResultResponse<ERFResponseClass> result = new ResultResponse<ERFResponseClass>();
            using (var _context = new FBContext())
            {
                List<string> erfStatusList = new List<string>() { "pending", "shipped", "processed", "cancel", "complete", "sourcing 3rd party" };
                string message = "";
                Erf erf = _context.Erves.Where(er => er.ErfId == ErfData.ERFId.ToString()).FirstOrDefault();

                if (erf != null && !string.IsNullOrEmpty(ErfData.Status))
                {
                    if (!erfStatusList.Contains(ErfData.Status.ToLower()))
                    {
                        message = "| ERF Status not valid";

                        result.responseCode = 500;
                        result.Data = new ERFResponseClass();
                        result.Data.ERFId = Convert.ToInt32(ErfData.ERFId);
                        result.Message = message;
                        result.IsSuccess = false;
                    }
                    else
                    {
                        string tempStatus = erf.Erfstatus;

                        erf.Erfstatus = ErfData.Status;
                        DateTime CurrentTime = DateTime.Now;//Utility.GetCurrentTime(erf.CustomerZipCode, _context);

                        /*Contact customer = FarmerBrothersEntitites.Contacts.Where(c => c.ContactID == CustomerID).FirstOrDefault();
                        string customerBranch = string.Empty;
                        string customerZipCode = string.Empty;

                        int ESMId = 0;
                        string ESMEmail = string.Empty;

                        if (customer != null)
                        {
                            customerBranch = customer.Branch == null ? "0" : customer.Branch.ToString();
                            customerZipCode = customer.PostalCode == null ? "0" : customer.PostalCode.ToString();

                            ESMId = customer.FSMJDE == null ? 0 : Convert.ToInt32(customer.FSMJDE);
                            ESMEmail = customer.ESMEmail == null ? "" : customer.ESMEmail;
                        }


                        int esmId = Convert.ToInt32(ESMId);

                        ESMCCMRSMEscalation esmdsmrsmView = FarmerBrothersEntitites.ESMCCMRSMEscalations.FirstOrDefault(x => x.EDSMID == esmId);*/

                        NotesHistory notesHistory = new NotesHistory()
                        {
                            AutomaticNotes = 1,
                            EntryDate = CurrentTime,
                            Notes = "[ERF]:  Status Updated from " + tempStatus + " to " + ErfData.Status,
                            Userid = userId,
                            UserName = userName,
                            ErfId = erf.ErfId,
                            WorkorderId = erf.WorkorderId,
                            IsDispatchNotes = 1
                        };
                        _context.NotesHistories.Add(notesHistory);



                        int returnValue = _context.SaveChanges();

                        if (ErfData.Status.ToLower() == "cancel")
                        {
                            //ERFNewController enc = new ERFNewController();
                            //enc.ERFEmail(erf.ErfID, erf.WorkorderID, false, erf.ApprovalStatus, true);
                        }



                        message = "| ERF Status Update Success !";

                        result.responseCode = 200;
                        result.Data = new ERFResponseClass();
                        result.Data.ERFId = Convert.ToInt32(ErfData.ERFId);
                        result.Message = message;
                        result.IsSuccess = true;
                    }

                }
                else
                {
                    if (erf == null)
                    {
                        message = "| ErfId not valid";
                    }
                    else if (string.IsNullOrEmpty(ErfData.Status))
                    {
                        message = "Status cannot be null";
                    }

                    result.responseCode = 500;
                    result.Data = new ERFResponseClass();
                    result.Data.ERFId = Convert.ToInt32(ErfData.ERFId);
                    result.Message = message;
                    result.IsSuccess = false;
                }
            }
            return result;
        }

        public ResultResponse<ErfMaintenanceResponse> ERFMaintenanceDataUpsert(ERFMaintenanceDataModel RequestData, int userId, string userName)
        {
            ResultResponse<ErfMaintenanceResponse> result = new ResultResponse<ErfMaintenanceResponse>();

            bool isValid = validateErfMaintenanceData(RequestData, out List<string> message);
            if (!isValid)
            {
                result.responseCode = 500;
                result.IsSuccess = false;
                result.Data = new ErfMaintenanceResponse() { Message = message };
            }
            else
            {
                List<string> FailedDataList = new List<string>();
                using (var _context = new FBContext())
                {
                    List<ERFCategory> categoryList = RequestData.CategoryList != null ? RequestData.CategoryList : new List<ERFCategory>();
                    List<ERFBrand> brandList = RequestData.BrandList != null ? RequestData.BrandList : new List<ERFBrand>();

                    foreach (ERFCategory ctg in categoryList)
                    {
                        using (var transaction = _context.Database.BeginTransaction())
                        {
                            try
                            {
                                Contingent existingCategory = _context.Contingents.Where(c => c.ContingentName.Equals(ctg.CategoryName)).FirstOrDefault();
                                string type = "";
                                if (ctg.CategoryType.ToLower() != "eqp" && ctg.CategoryType.ToLower() != "equipment")
                                {
                                    type = "eqp";
                                }
                                else if (ctg.CategoryType.ToLower() != "exp" && ctg.CategoryType.ToLower() != "expendable")
                                {
                                    type = "exp";
                                }

                                if (existingCategory != null)
                                {
                                    existingCategory.ContingentName = ctg.CategoryName;
                                    existingCategory.ContingentType = type;
                                    existingCategory.IsActive = ctg.IsActive;
                                }
                                else
                                {
                                    Contingent newContingent = new Contingent();
                                    newContingent.ContingentName = ctg.CategoryName;
                                    newContingent.ContingentType = type;
                                    newContingent.IsActive = ctg.IsActive;

                                    _context.Contingents.Add(newContingent);
                                }
                                _context.SaveChanges();
                                transaction.Commit();

                            }
                            catch (Exception ex)
                            {
                                FailedDataList.Add("Error Updating Category : " + ctg.CategoryName);
                                transaction.Rollback();
                            }
                        }
                    }


                    foreach (ERFBrand brnd in brandList)
                    {
                        using (var transaction = _context.Database.BeginTransaction())
                        {
                            try
                            {
                                ContingentDetail existingBrand = _context.ContingentDetails.Where(c => c.Name.Equals(brnd.BrandName)).FirstOrDefault();
                                Contingent con = _context.Contingents.Where(c => c.ContingentName == brnd.CategoryName).FirstOrDefault();

                                if (con == null)
                                {
                                    FailedDataList.Add("Category Name invalid for the given brandt : " + brnd.BrandName);
                                    continue;
                                }

                                if (existingBrand != null)
                                {
                                    existingBrand.Name = brnd.BrandName ?? "";
                                    existingBrand.ContingentId = con.ContingentId;
                                    existingBrand.LaidInCost = brnd.LaidInCost ?? 0;
                                    existingBrand.Rental = brnd.RentalCost ?? 0;
                                    existingBrand.CashSale = brnd.CashSale ?? 0;
                                    existingBrand.IsActive = brnd.IsActive;
                                }
                                else
                                {
                                    ContingentDetail newBrand = new ContingentDetail();
                                    newBrand.Name = brnd.BrandName ?? "";
                                    newBrand.ContingentId = con.ContingentId;
                                    newBrand.LaidInCost = brnd.LaidInCost ?? 0;
                                    newBrand.Rental = brnd.RentalCost ?? 0;
                                    newBrand.CashSale = brnd.CashSale ?? 0;
                                    newBrand.IsActive = brnd.IsActive;

                                    _context.ContingentDetails.Add(newBrand);
                                }
                                _context.SaveChanges();
                                transaction.Commit();

                            }
                            catch (Exception ex)
                            {
                                FailedDataList.Add("Error Updating Brand : " + brnd.BrandName);
                                transaction.Rollback();
                            }
                        }
                    }
                }

                result.responseCode = 200;
                result.IsSuccess = true;
                result.Data = new ErfMaintenanceResponse() { Message = FailedDataList };
            }

            return result;
        }

        public bool validateErfMaintenanceData(ERFMaintenanceDataModel RequestData, out List<string> message)
        {
            bool validFlag = true;
            message = new List<string>();
            if ((RequestData.CategoryList == null || RequestData.CategoryList.Count <= 0)
                           && (RequestData.BrandList == null || RequestData.BrandList.Count <= 0))
            {
                message.Add("Category Or Brand List Required");
                validFlag = false;
            }
            else
            {
                List<ERFCategory> categoryList = RequestData.CategoryList != null ? RequestData.CategoryList : new List<ERFCategory>();
                List<ERFBrand> brandList = RequestData.BrandList != null ? RequestData.BrandList : new List<ERFBrand>();

                int index = 0;
                foreach (ERFCategory ctg in categoryList)
                {
                    string CategoryName = string.IsNullOrEmpty(ctg.CategoryName) ? "" : ctg.CategoryName.Replace('\"', ' ').Trim();
                    if (string.IsNullOrEmpty(CategoryName))
                    {
                        validFlag = false;
                        message.Add("CategoryName required at : " + index);
                    }
                    if (string.IsNullOrEmpty(ctg.CategoryType))
                    {
                        validFlag = false;
                        message.Add("Category Type required at : " + index);
                    }
                    else if((ctg.CategoryType.ToLower() != "eqp" && ctg.CategoryType.ToLower() != "equipment") && (ctg.CategoryType.ToLower() != "exp" && ctg.CategoryType.ToLower() != "expendable"))
                    {
                        validFlag = false;
                        message.Add("Category Type invalid at : " + index);
                    }

                    index++;
                }

                index = 0;
                foreach (ERFBrand brnd in brandList)
                {
                    string BrandName = string.IsNullOrEmpty(brnd.BrandName) ? "" : brnd.BrandName.Replace('\"', ' ').Trim();
                    if (string.IsNullOrEmpty(BrandName))
                    {
                        validFlag = false;
                        message.Add("BrandName required at : " + index);
                    }
                    if (string.IsNullOrEmpty(brnd.CategoryName))
                    {
                        validFlag = false;
                        message.Add("Brand CategoryName required at : " + index);
                    }
                }
            }

            return validFlag;
        }
    }
}
