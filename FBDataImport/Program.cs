using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FBDataImport
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            //Program.SaveFile(Encoding.ASCII);
            string FilePath = ConfigurationManager.AppSettings["FullPath"];
            DirectoryInfo destinationDir = new DirectoryInfo(FilePath);
            FileInfo[] Files = destinationDir.GetFiles("*.txt");

            foreach (FileInfo file in Files)
            {
                int resultFlag = Program.ProcessFile(file.FullName);

                if (resultFlag == 1)
                {
                    Program.UpdateContacts();

                    DirectoryInfo FileToBeMoved = new DirectoryInfo(FilePath + file.Name);
                    string CompletedDir = FilePath + @"Completed\" + file.Name;
                    FileToBeMoved.MoveTo(CompletedDir);

                }
                else
                {
                    continue;
                }
            }
        }

        private static void SaveFile(Encoding encoding)
        {

            string FilePath = ConfigurationManager.AppSettings["FullPath"];
            string filename = string.Concat(FilePath, "SampleFBDataASCII", ".txt");

            string[] lines = System.IO.File.ReadAllLines(FilePath + "E1MAI.txt");
            StreamWriter streamWriter = new StreamWriter(filename, false, encoding);

            foreach (string line in lines)
            {
                streamWriter.WriteLine(line);
            }
            streamWriter.Close();
        }

        private static int ProcessFile(string filePath)
        {
            string[] lines = System.IO.File.ReadAllLines(filePath);

            string constr = ConfigurationManager.ConnectionStrings["FB_Connection"].ConnectionString;
            try
            {
                using (SqlConnection con = new SqlConnection(constr))
                {
                    con.Open();

                    string TruncateQuery = @"TRUNCATE Table FBDailyContacts";
                    SqlCommand Truncatecmd = new SqlCommand(TruncateQuery, con);
                    Truncatecmd.ExecuteNonQuery();

                    foreach (string line in lines)
                    {
                        if (string.IsNullOrEmpty(line)) continue;

                        string Account = line.Substring(0, 8).Trim();
                        string[] AccountStr = null;
                        if (Account.Contains("'"))
                        {
                            AccountStr = Account.Split('\'');
                            Account = AccountStr[0] + "''" + AccountStr[1];
                        }

                        string LongAddress = line.Substring(8, 20).Trim();
                        string[] LongAddressStr = null;
                        if (LongAddress.Contains("'"))
                        {
                            LongAddressStr = LongAddress.Split('\'');
                            LongAddress = LongAddressStr[0] + "''" + LongAddressStr[1];
                        }

                        string company = line.Substring(28, 40).Trim();
                        string[] companyStr = null;
                        if (company.Contains("'"))
                        {
                            companyStr = company.Split('\'');
                            company = companyStr[0] + "''" + companyStr[1];
                        }

                        string BusinessUnit = line.Substring(68, 12).Trim();
                        string[] BusinessUnitStr = null;
                        if (BusinessUnit.Contains("'"))
                        {
                            BusinessUnitStr = BusinessUnit.Split('\'');
                            BusinessUnit = BusinessUnitStr[0] + "''" + BusinessUnitStr[1];
                        }

                        string SearchUnit = line.Substring(80, 3).Trim();
                        string[] SearchUnitStr = null;
                        if (SearchUnit.Contains("'"))
                        {
                            SearchUnitStr = SearchUnit.Split('\'');
                            SearchUnit = SearchUnitStr[0] + "''" + SearchUnitStr[1];
                        }

                        string Categorycode01 = line.Substring(83, 3).Trim();
                        string[] Categorycode01Str = null;
                        if (Categorycode01.Contains("'"))
                        {
                            Categorycode01Str = Categorycode01.Split('\'');
                            Categorycode01 = Categorycode01Str[0] + "''" + Categorycode01Str[1];
                        }

                        string OperatingUnit = line.Substring(86, 3).Trim();
                        string[] OperatingUnitStr = null;
                        if (OperatingUnit.Contains("'"))
                        {
                            OperatingUnitStr = OperatingUnit.Split('\'');
                            OperatingUnit = OperatingUnitStr[0] + "''" + OperatingUnitStr[1];
                        }

                        string Route = line.Substring(89, 3).Trim();
                        string[] RouteStr = null;
                        if (Route.Contains("'"))
                        {
                            RouteStr = Route.Split('\'');
                            Route = RouteStr[0] + "''" + RouteStr[1];
                        }

                        string Branch = line.Substring(92, 3).Trim();
                        string[] BranchStr = null;
                        if (Branch.Contains("'"))
                        {
                            BranchStr = Branch.Split('\'');
                            Branch = BranchStr[0] + "''" + BranchStr[1];
                        }

                        string District = line.Substring(95, 3).Trim();
                        string[] DistrictStr = null;
                        if (District.Contains("'"))
                        {
                            DistrictStr = District.Split('\'');
                            District = DistrictStr[0] + "''" + DistrictStr[1];
                        }

                        string Division = line.Substring(98, 3).Trim();
                        string[] DivisionStr = null;
                        if (Division.Contains("'"))
                        {
                            DivisionStr = Division.Split('\'');
                            Division = DivisionStr[0] + "''" + DivisionStr[1];
                        }

                        string Reporting1099 = line.Substring(101, 3).Trim();
                        string[] Reporting1099Str = null;
                        if (Reporting1099.Contains("'"))
                        {
                            Reporting1099Str = Reporting1099.Split('\'');
                            Reporting1099 = Reporting1099Str[0] + "''" + Reporting1099Str[1];
                        }

                        string Chain = line.Substring(104, 3).Trim();
                        string[] ChainStr = null;
                        if (Chain.Contains("'"))
                        {
                            ChainStr = Chain.Split('\'');
                            Chain = ChainStr[0] + "''" + ChainStr[1];
                        }

                        string BrewmaticAgentCode = line.Substring(107, 3).Trim();
                        string[] BrewmaticAgentCodeStr = null;
                        if (BrewmaticAgentCode.Contains("'"))
                        {
                            BrewmaticAgentCodeStr = BrewmaticAgentCode.Split('\'');
                            BrewmaticAgentCode = BrewmaticAgentCodeStr[0] + "''" + BrewmaticAgentCodeStr[1];
                        }

                        string NTR = line.Substring(110, 3).Trim();
                        string[] NTRStr = null;
                        if (NTR.Contains("'"))
                        {
                            NTRStr = NTR.Split('\'');
                            NTR = NTRStr[0] + "''" + NTRStr[1];
                        }

                        string DelDayFOB = line.Substring(113, 3).Trim();
                        string[] DelDayFOBStr = null;
                        if (DelDayFOB.Contains("'"))
                        {
                            DelDayFOBStr = DelDayFOB.Split('\'');
                            DelDayFOB = DelDayFOBStr[0] + "''" + DelDayFOBStr[1];
                        }

                        string RouteCode = line.Substring(116, 3).Trim();
                        string[] RouteCodeStr = null;
                        if (RouteCode.Contains("'"))
                        {
                            RouteCodeStr = RouteCode.Split('\'');
                            RouteCode = RouteCodeStr[0] + "''" + RouteCodeStr[1];
                        }

                        string ZoneNumber = line.Substring(119, 3).Trim();
                        string[] ZoneNumberStr = null;
                        if (ZoneNumber.Contains("'"))
                        {
                            ZoneNumberStr = ZoneNumber.Split('\'');
                            ZoneNumber = ZoneNumberStr[0] + "''" + ZoneNumberStr[1];
                        }

                        string AddressLine1 = line.Substring(122, 40).Trim();
                        string[] AddressLine1Str = null;
                        if (AddressLine1.Contains("'"))
                        {
                            AddressLine1Str = AddressLine1.Split('\'');
                            AddressLine1 = AddressLine1Str[0] + "''" + AddressLine1Str[1];
                        }

                        string AddressLine2 = line.Substring(162, 40).Trim();
                        string[] AddressLine2Str = null;
                        if (AddressLine2.Contains("'"))
                        {
                            AddressLine2Str = AddressLine2.Split('\'');
                            AddressLine2 = AddressLine2Str[0] + "''" + AddressLine2Str[1];
                        }

                        string AddressLine3 = line.Substring(202, 40).Trim();
                        string[] AddressLine3Str = null;
                        if (AddressLine3.Contains("'"))
                        {
                            AddressLine3Str = AddressLine3.Split('\'');
                            AddressLine3 = AddressLine3Str[0] + "''" + AddressLine3Str[1];
                        }

                        string City = line.Substring(242, 25).Trim();
                        string[] CityStr = null;
                        if (City.Contains("'"))
                        {
                            CityStr = City.Split('\'');
                            City = CityStr[0] + "''" + CityStr[1];
                        }

                        string State = line.Substring(267, 3).Trim();
                        string[] StateStr = null;
                        if (State.Contains("'"))
                        {
                            StateStr = State.Split('\'');
                            State = StateStr[0] + "''" + StateStr[1];
                        }

                        string PostalCode = line.Substring(270, 12).Trim();
                        string[] PostalCodeStr = null;
                        if (PostalCode.Contains("'"))
                        {
                            PostalCodeStr = PostalCode.Split('\'');
                            PostalCode = PostalCodeStr[0] + "''" + PostalCodeStr[1];
                        }
                        if (PostalCode.Length == 4)
                        {
                            PostalCode = "0" + PostalCode;
                        }

                        string Country = line.Substring(282, 25).Trim();
                        string[] CountryStr = null;
                        if (Country.Contains("'"))
                        {
                            CountryStr = Country.Split('\'');
                            Country = CountryStr[0] + "''" + CountryStr[1];
                        }

                        string Prefix = line.Substring(307, 6).Trim();
                        string[] PrefixStr = null;
                        if (Prefix.Contains("'"))
                        {
                            PrefixStr = Prefix.Split('\'');
                            Prefix = PrefixStr[0] + "''" + PrefixStr[1];
                        }

                        string PhoneNumber = line.Substring(313, 20).Trim();
                        string[] PhoneNumberStr = null;
                        if (PhoneNumber.Contains("'"))
                        {
                            PhoneNumberStr = PhoneNumber.Split('\'');
                            PhoneNumber = PhoneNumberStr[0] + "''" + PhoneNumberStr[1];
                        }

                        string MailingName = line.Substring(333, 40).Trim();
                        string[] MailingNameStr = null;
                        if (MailingName.Contains("'"))
                        {
                            MailingNameStr = MailingName.Split('\'');
                            MailingName = MailingNameStr[0] + "''" + MailingNameStr[1];
                        }

                        string[] mailingStr = null;
                        if (MailingName.Contains("'"))
                        {
                            mailingStr = MailingName.Split('\'');
                            MailingName = mailingStr[0] + "''" + mailingStr[1];
                        }


                        string GivenName = line.Substring(373, 25).Trim();
                        string[] GivenNameStr = null;
                        if (GivenName.Contains("'"))
                        {
                            GivenNameStr = GivenName.Split('\'');
                            GivenName = GivenNameStr[0] + "''" + GivenNameStr[1];
                        }

                        string MiddleName = line.Substring(398, 25).Trim();
                        string[] MiddleNameStr = null;
                        if (MiddleName.Contains("'"))
                        {
                            MiddleNameStr = MiddleName.Split('\'');
                            MiddleName = MiddleNameStr[0] + "''" + MiddleNameStr[1];
                        }

                        string SurName = line.Substring(423, 25).Trim();
                        string[] SurNameStr = null;
                        if (SurName.Contains("'"))
                        {
                            SurNameStr = SurName.Split('\'');
                            SurName = SurNameStr[0] + "''" + SurNameStr[1];
                        }

                        string PPID = line.Substring(448, 8).Trim();
                        string[] PPIDStr = null;
                        if (PPID.Contains("'"))
                        {
                            PPIDStr = PPID.Split('\'');
                            PPID = PPIDStr[0] + "''" + PPIDStr[1];
                        }

                        string PParent = line.Substring(456, 40).Trim();
                        string[] PParentStr = null;
                        if (PParent.Contains("'"))
                        {
                            PParentStr = PParent.Split('\'');
                            PParent = PParentStr[0] + "''" + PParentStr[1];
                        }

                        string SaleDate = line.Substring(496, 10).Trim();
                        string[] SaleDateStr = null;
                        if (SaleDate.Contains("'"))
                        {
                            SaleDateStr = SaleDate.Split('\'');
                            SaleDate = SaleDateStr[0] + "''" + SaleDateStr[1];
                        }

                        string BillingCode = line.Substring(506, 3).Trim();
                        string[] BillingCodeStr = null;
                        if (BillingCode.Contains("'"))
                        {
                            BillingCodeStr = BillingCode.Split('\'');
                            BillingCode = BillingCodeStr[0] + "''" + BillingCodeStr[1];
                        }


                        //***************NEW COLUMNS*********************

                        string BCClassificationCode = line.Substring(509, 3).Trim();
                        string[] BCClassificationCodeStr = null;
                        if (BCClassificationCode.Contains("'"))
                        {
                            BCClassificationCodeStr = BCClassificationCode.Split('\'');
                            BCClassificationCode = BCClassificationCodeStr[0] + "''" + BCClassificationCodeStr[1];
                        }

                        string AaddNum = line.Substring(512, 8).Trim();
                        string[] AaddNumStr = null;
                        if (AaddNum.Contains("'"))
                        {
                            AaddNumStr = AaddNum.Split('\'');
                            AaddNum = AaddNumStr[0] + "''" + AaddNumStr[1];
                        }

                        string TaxExemption = line.Substring(520, 20).Trim();
                        string[] TaxExemptionStr = null;
                        if (TaxExemption.Contains("'"))
                        {
                            TaxExemptionStr = TaxExemption.Split('\'');
                            TaxExemption = TaxExemptionStr[0] + "''" + TaxExemptionStr[1];
                        }

                        string TaxGrp = line.Substring(540, 3).Trim();
                        string[] TaxGrpStr = null;
                        if (TaxGrp.Contains("'"))
                        {
                            TaxGrpStr = TaxGrp.Split('\'');
                            TaxGrp = TaxGrpStr[0] + "''" + TaxGrpStr[1];
                        }


                        string AddrBook12 = line.Substring(543, 3).Trim();
                        string[] AddrBook12Str = null;
                        if (AddrBook12.Contains("'"))
                        {
                            AddrBook12Str = AddrBook12.Split('\'');
                            AddrBook12 = AddrBook12Str[0] + "''" + AddrBook12Str[1];
                        }

                        string AddrBook14 = line.Substring(546, 3).Trim();
                        string[] AddrBook14Str = null;
                        if (AddrBook14.Contains("'"))
                        {
                            AddrBook12Str = AddrBook14.Split('\'');
                            AddrBook14 = AddrBook14Str[0] + "''" + AddrBook14Str[1];
                        }

                        string AddrBook15 = line.Substring(549, 3).Trim();
                        string[] AddrBook15Str = null;
                        if (AddrBook15.Contains("'"))
                        {
                            AddrBook15Str = AddrBook15.Split('\'');
                            AddrBook15 = AddrBook15Str[0] + "''" + AddrBook15Str[1];
                        }

                        string AddrBook16 = line.Substring(552, 3).Trim();
                        string[] AddrBook16Str = null;
                        if (AddrBook16.Contains("'"))
                        {
                            AddrBook16Str = AddrBook16.Split('\'');
                            AddrBook16 = AddrBook16Str[0] + "''" + AddrBook16Str[1];
                        }

                        string AddrBook17 = line.Substring(555, 3).Trim();
                        string[] AddrBook17Str = null;
                        if (AddrBook17.Contains("'"))
                        {
                            AddrBook17Str = AddrBook17.Split('\'');
                            AddrBook17 = AddrBook17Str[0] + "''" + AddrBook17Str[1];
                        }

                        string AddrBook18 = line.Substring(558, 3).Trim();
                        string[] AddrBook18Str = null;
                        if (AddrBook18.Contains("'"))
                        {
                            AddrBook18Str = AddrBook18.Split('\'');
                            AddrBook18 = AddrBook18Str[0] + "''" + AddrBook18Str[1];
                        }

                        string AddrBook19 = line.Substring(561, 3).Trim();
                        string[] AddrBook19Str = null;
                        if (AddrBook19.Contains("'"))
                        {
                            AddrBook19Str = AddrBook19.Split('\'');
                            AddrBook19 = AddrBook19Str[0] + "''" + AddrBook19Str[1];
                        }

                        string AddrBook20 = line.Substring(564, 3).Trim();
                        string[] AddrBook20Str = null;
                        if (AddrBook20.Contains("'"))
                        {
                            AddrBook20Str = AddrBook20.Split('\'');
                            AddrBook20 = AddrBook20Str[0] + "''" + AddrBook20Str[1];
                        }

                        string AddrBook21 = line.Substring(567, 3).Trim();
                        string[] AddrBook21Str = null;
                        if (AddrBook21.Contains("'"))
                        {
                            AddrBook21Str = AddrBook21.Split('\'');
                            AddrBook21 = AddrBook21Str[0] + "''" + AddrBook21Str[1];
                        }

                        string AddrBook22 = line.Substring(570, 3).Trim();
                        string[] AddrBook22Str = null;
                        if (AddrBook22.Contains("'"))
                        {
                            AddrBook22Str = AddrBook22.Split('\'');
                            AddrBook22 = AddrBook22Str[0] + "''" + AddrBook22Str[1];
                        }

                        string SplEqp = line.Substring(573, 3).Trim();
                        string[] SplEqpStr = null;
                        if (SplEqp.Contains("'"))
                        {
                            SplEqpStr = SplEqp.Split('\'');
                            SplEqp = SplEqpStr[0] + "''" + SplEqpStr[1];
                        }

                        string CAProtection = line.Substring(576, 3).Trim();
                        string[] CAProtectionStr = null;
                        if (CAProtection.Contains("'"))
                        {
                            CAProtectionStr = CAProtection.Split('\'');
                            CAProtection = CAProtectionStr[0] + "''" + CAProtectionStr[1];
                        }

                        string CustGrp = line.Substring(579, 3).Trim();
                        string[] CustGrpStr = null;
                        if (CustGrp.Contains("'"))
                        {
                            CustGrpStr = CustGrp.Split('\'');
                            CustGrp = CustGrpStr[0] + "''" + CustGrpStr[1];
                        }

                        string SPCommis = line.Substring(582, 3).Trim(); //FB Type
                        string[] SPCommisStr = null;
                        if (SPCommis.Contains("'"))
                        {
                            SPCommisStr = SPCommis.Split('\'');
                            SPCommis = SPCommisStr[0] + "''" + SPCommisStr[1];
                        }

                        string AlliedDisct = line.Substring(585, 3).Trim();
                        string[] AlliedDisctStr = null;
                        if (AlliedDisct.Contains("'"))
                        {
                            AlliedDisctStr = AlliedDisct.Split('\'');
                            AlliedDisct = AlliedDisctStr[0] + "''" + AlliedDisctStr[1];
                        }

                        string CoffeeVol = line.Substring(588, 3).Trim();
                        string[] CoffeeVolStr = null;
                        if (CoffeeVol.Contains("'"))
                        {
                            CoffeeVolStr = CoffeeVol.Split('\'');
                            CoffeeVol = CoffeeVolStr[0] + "''" + CoffeeVolStr[1];
                        }

                        string EqpProg = line.Substring(591, 3).Trim();
                        string[] EqpProgStr = null;
                        if (EqpProg.Contains("'"))
                        {
                            EqpProgStr = EqpProg.Split('\'');
                            EqpProg = EqpProgStr[0] + "''" + EqpProgStr[1];
                        }

                        string Specilal = line.Substring(594, 3).Trim();
                        string[] SpecilalStr = null;
                        if (Specilal.Contains("'"))
                        {
                            SpecilalStr = Specilal.Split('\'');
                            Specilal = SpecilalStr[0] + "''" + SpecilalStr[1];
                        }

                        string BillAddrType = line.Substring(597, 1).Trim();
                        string[] BillAddrTypeStr = null;
                        if (BillAddrType.Contains("'"))
                        {
                            BillAddrTypeStr = BillAddrType.Split('\'');
                            BillAddrType = BillAddrTypeStr[0] + "''" + BillAddrTypeStr[1];
                        }

                        string PriceAdjustSchedule = line.Substring(598, 8).Trim();
                        string[] PriceAdjustScheduleStr = null;
                        if (PriceAdjustSchedule.Contains("'"))
                        {
                            PriceAdjustScheduleStr = PriceAdjustSchedule.Split('\'');
                            PriceAdjustSchedule = PriceAdjustScheduleStr[0] + "''" + PriceAdjustScheduleStr[1];
                        }

                        string CustStatus = line.Substring(606, 1).Trim();
                        string[] CustStatusStr = null;
                        if (CustStatus.Contains("'"))
                        {
                            CustStatusStr = CustStatus.Split('\'');
                            CustStatus = CustStatusStr[0] + "''" + CustStatusStr[1];
                        }

                        string FreightHandlingCode = line.Substring(607, 3).Trim();
                        string[] FreightHandlingCodeStr = null;
                        if (FreightHandlingCode.Contains("'"))
                        {
                            FreightHandlingCodeStr = FreightHandlingCode.Split('\'');
                            FreightHandlingCode = FreightHandlingCodeStr[0] + "''" + FreightHandlingCodeStr[1];
                        }

                        string BranchType = line.Substring(610, 2).Trim();
                        string[] BranchTypeStr = null;
                        if (BranchType.Contains("'"))
                        {
                            BranchTypeStr = BranchType.Split('\'');
                            BranchType = BranchTypeStr[0] + "''" + BranchTypeStr[1];
                        }

                        //***************END OF NEW COLUMNS*********************

                        string sqlQuery = @"Insert Into FBDailyContacts([Address Number],[Long Address Number],[Alpha Name],[Business Unit],[Search Type]
                                                    ,[Category Code 01],[Operating Unit],Route,Branch,District,Division,[1099 Reporting],Chain,[Brewmatic Agent Code],NTR
                                                    ,[Del Day / FOB],[Route Code],[Zone Number],[Address Line 1],[Address Line 2],[Address Line 3],City,State,[Postal Code],Country
                                                    ,Prefix,[Phone Number],[Mailing Name],[Given Name],[Middle Name],Surname,BillingCode,PricingParentId,PricingParentDesc,LastSaleDate
 
                                                    ,[BCClassificationCode] ,[addressNumber]  ,[TaxExemption] , [TaxGrp] ,[AddressBook12], [AddressBook14], [AddressBook15] ,[AddressBook16] 
                                                    ,[AddressBook17] ,[AddressBook18] ,[AddressBook19] ,[AddressBook20] ,[AddressBook21] ,[AddressBook22] ,[SpecialEquipment] ,[CAProtection] 
                                                    ,[CustomerGroup] ,[SPCommisFBType] ,[AlliedDiscount] ,[CoffeeVolume] ,[EquipmentProg]  ,[Special], [BillAddrType], [PriceAdjustSchedule]
                                                    , [CustomerStatus], [FreightHandlingCode], [BranchType]

                                                    ) 
                                                    VALUES('" + Account + "','" + LongAddress + "','" + company + "','" + BusinessUnit + "','" + SearchUnit +
                                                        "','" + Categorycode01 + "','" + OperatingUnit + "','" + Route + "','" + Branch + "','" + District + "','" + Division + "','" + Reporting1099 + "','" + Chain + "','" + BrewmaticAgentCode + "','" + NTR +
                                                        "','" + DelDayFOB + "','" + RouteCode + "','" + ZoneNumber + "','" + AddressLine1 + "','" + AddressLine2 + "','" + AddressLine3 + "','" + City + "','" + State + "','" + PostalCode + "','" + Country +
                                                        "','" + Prefix + "','" + PhoneNumber + "','" + MailingName + "','" + GivenName + "','" + MiddleName + "','" + SurName + "','" + BillingCode + "','" + PPID + "','" + PParent + "','" + SaleDate +


                                                        "','" + BCClassificationCode + "','" + AaddNum + "','" + TaxExemption + "','" + TaxGrp + "','" + AddrBook12 + "','" + AddrBook14 + "','" + AddrBook15 + "','" + AddrBook16 +
                                                        "','" + AddrBook17 + "','" + AddrBook18 + "','" + AddrBook19 + "','" + AddrBook20 + "','" + AddrBook21 + "','" + AddrBook22 + "','" + SplEqp + "','" + CAProtection + 
                                                        "','" + CustGrp + "','" + SPCommis + "','" + AlliedDisct + "','" + CoffeeVol + "','" + EqpProg + "','" + Specilal + "','" + BillAddrType + "','" + PriceAdjustSchedule + 
                                                        "','" + CustStatus + "','" + FreightHandlingCode + "','"+ BranchType + "')";

                        SqlCommand cmd = new SqlCommand(sqlQuery, con);

                        cmd.ExecuteNonQuery();

                    }
                    con.Close();
                }
                return 1;
            }
            catch (Exception ex)
            {
                return 0;
            }
            //streamWriter.Close();
        }

        private static void UpdateContacts()
        {
            try
            {
                string constr = ConfigurationManager.ConnectionStrings["FB_Connection"].ConnectionString;
                using (SqlConnection con = new SqlConnection(constr))
                {
                    con.Open();

                    SqlCommand cmnd = new SqlCommand("USP_MAFBC_UpdateDaily_Contacts", con);
                    cmnd.CommandTimeout = 3000;
                    cmnd.CommandType = CommandType.StoredProcedure;

                    SqlDataAdapter da = new SqlDataAdapter(cmnd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    con.Close();
                }
            }
            catch (Exception ex)
            {

            }
        }
    }
}
