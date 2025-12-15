using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.SqlClient;
using System.IO;
using System.Data;
using System.Web.Services;
using System.Configuration;
using System.Text;
using System.Net.Mail;
using System.Web.Services;
using System.Net.Mail;
using System.Net.Mime;

public partial class _Default : System.Web.UI.Page
{
    SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["ConnectionStringDemo"].ToString());
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            String SCF = "", TechID = "", ISBill = "", TECHType ="";
            int EventErr = 0;
            Ttype.Visible = true;
            errmsg.Text = "";
            maintbl.Visible = true;
            callclo.Visible = false;
            succmsg.Visible = false;
            Div1.Visible = false;
            NSR.Visible = false;
	        serialNumDD.Visible = true;
            serial.Visible = false;
            if (Request.QueryString["SCF"] != null && Request.QueryString["TechID"] != null && Request.QueryString["IsBillable"] != null)
            {
                GetMachins();
                SCF = Request.QueryString["SCF"].ToString();
                TechID = Request.QueryString["TechID"].ToString();
                ISBill = Request.QueryString["IsBillable"].ToString();
                if(ISBill == "True") { BillableCheckBox.Checked = true; }else { BillableCheckBox.Checked = false; }
                //sGenPwd(5);
                //String sNewPass = SCF.Substring(0, 3) + sGenPwd(5) + SCF.Substring(SCF.Length - 6);

                fb.Text = SCF;
                con.Open();
                using (SqlCommand cmd = new SqlCommand("Select count(*) from workorder where workordercallstatus = 'Accepted' and [WorkorderID] =" + SCF, con))
                {
                    SqlDataReader dr = cmd.ExecuteReader();
                    if (dr.Read())
                    {

                    }else
                    {
                        EventErr = 1;
                        errmsg.Text = "<font color='Red'><b>FB# Not In DISPATCHED Status</b></font>";
                    }
                    dr.Close();
                }

		        string CustomerId = string.Empty;
                using (SqlCommand woQry = new SqlCommand("Select * from workorder where WorkorderID = " + SCF, con))
                {
                    SqlDataReader wodr = woQry.ExecuteReader();
                    if (wodr.Read())
                    {
                        if (wodr["CustomerID"] != DBNull.Value)
                        {
                            CustomerId = wodr["CustomerID"].ToString();
                        }
                    }
                    wodr.Close();
                }

                using (SqlCommand cmd2 = new SqlCommand("Select count(*) from [dbo].[WorkorderSchedule] where techID= " + TechID + " and [WorkorderID] = " + SCF, con))
                {
                    SqlDataReader dr2 = cmd2.ExecuteReader();
                    if (dr2.Read())
                    {
                    }else
                    {
                        EventErr = 1;
                        errmsg.Text = "<font color='Red'><b>FB# DISPATCHED To Another Tech</b></font>";
                    }
                    dr2.Close();
                }
                SqlCommand cmd3 = new SqlCommand("Select * from [WorkorderType] where active = 1 order by CalltypeID", con);
                SqlDataReader sdr3 = cmd3.ExecuteReader();
                CallType.DataSource = sdr3;
                CallType.DataTextField = "Description";
                CallType.DataValueField = "calltypeID";
                CallType.DataBind();
                CallType.Items.Insert(0, new ListItem("", "0"));
                sdr3.Close();

                SqlCommand cmd4 = new SqlCommand("Select * from Solution where active = 1", con);
                SqlDataReader sdr4 = cmd4.ExecuteReader();
                CompletionCode.DataSource = sdr4;
                CompletionCode.DataTextField = "Description";
                CompletionCode.DataValueField = "solutionId";
                CompletionCode.DataBind();
                CompletionCode.Items.Insert(0, new ListItem("", "0"));
                sdr4.Close();

		        using (SqlCommand cmd1 = new SqlCommand("Select * from TECH_HIERARCHY where Dealerid =" + TechID, con))
                {
                    SqlDataReader dr1 = cmd1.ExecuteReader();
                    if (dr1.Read())
                    {
                        if (dr1["FamilyAff"].ToString() == "SPT")
                        {
                            Ttype.Visible = false;
			    CompletionCode.Items.Remove(CompletionCode.Items.FindByValue("9999"));
                        }
                    }
                    dr1.Close();
                }

                SqlCommand cmd5 = new SqlCommand("Select CategoryId,(CategoryCode+' - '+CategoryDesc) as CatDes from [dbo].[Category] where active = 1", con);
                SqlDataReader sdr5 = cmd5.ExecuteReader();
                EquipType.DataSource = sdr5;
                EquipType.DataTextField = "CatDes";
                EquipType.DataValueField = "CategoryId";
                EquipType.DataBind();
                EquipType.Items.Insert(0, new ListItem("", "0"));
                sdr5.Close();

                SqlCommand cmd6 = new SqlCommand("Select * from Vendor order by VendorDescription asc", con);
                SqlDataReader sdr6 = cmd6.ExecuteReader();
                Vendor.DataSource = sdr6;
                Vendor.DataTextField = "VendorDescription";
                Vendor.DataValueField = "VendorCode";
                Vendor.DataBind();
                Vendor.Items.Insert(0, new ListItem("", "0"));
                sdr6.Close();

                SqlCommand cmd7 = new SqlCommand("select * from FBCBE where CurrentCustomerId = " + CustomerId, con);
                SqlDataReader sdr7 = cmd7.ExecuteReader();
                serialNumDD.DataSource = sdr7;
                serialNumDD.DataTextField = "SerialNumber";
                serialNumDD.DataValueField = "SerialNumber";
                serialNumDD.DataBind();
                int SerialNumberListCount = serialNumDD.Items.Count;
                serialNumDD.Items.Insert(0, new ListItem("", "0"));
                serialNumDD.Items.Add(new ListItem("Other", ""));
                sdr7.Close();

                serialNumDD.SelectedIndex = -1;

                if (SerialNumberListCount > 0)
                {
                    serialNumDD.Visible = true;
                    serial.Visible = false;
                }
                else
                {
                    serialNumDD.Visible = false;
                    serial.Visible = true;
                }

                SqlCommand cmd8 = new SqlCommand("select FBStatusID,FBStatus from AllFBStatus where StatusFor='NSR Reason' order by FBStatus asc", con);
                SqlDataReader sdr8 = cmd8.ExecuteReader();
                Reasions.DataSource = sdr8;
                Reasions.DataTextField = "FBStatus";
                Reasions.DataValueField = "FBStatusID";
                Reasions.DataBind();
                Reasions.Items.Insert(0, new ListItem("", "0"));
                sdr8.Close();


                using (SqlCommand cmd9 = new SqlCommand("Select * from WorkorderDetails where WorkorderID =" + SCF, con))
                {
                    SqlDataReader dr9 = cmd9.ExecuteReader();
                    if (dr9.Read())
                    {
                        if(dr9["StartDateTime"].ToString() != "")
                        {
                            DateTime Sdate = Convert.ToDateTime(dr9["StartDateTime"].ToString());
                            SDate.Text = Sdate.ToString("MM/dd/yyyy hh:mm tt");
                        }
                        if (dr9["ArrivalDateTime"].ToString() != "")
                        {
                            DateTime Adate = Convert.ToDateTime(dr9["ArrivalDateTime"].ToString());
                            ADate.Text = Adate.ToString("MM/dd/yyyy hh:mm tt");
                        }
                        if (dr9["CompletionDateTime"].ToString() != "")
                        {
                            DateTime Cdate = Convert.ToDateTime(dr9["CompletionDateTime"].ToString());
                            CDate.Text = Cdate.ToString("MM/dd/yyyy hh:mm tt");
                        }
                    }
                    dr9.Close();
                }

                con.Close();
            }
        }
    }
    [WebMethod]
    public static string[] GetParts(string prefix)
    {
        List<string> customers = new List<string>();
        using (SqlConnection conn = new SqlConnection())
        {
            conn.ConnectionString = ConfigurationManager.ConnectionStrings["ConnectionStringDemo"].ConnectionString;
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.CommandText = "Select SKUID, EntryNumber, ItemNo, VendorNo, Description, Supplier from FBClosureParts where SKUactive = 1 and SKUID > 0 and (EntryNumber like @SearchText + '%'  or ItemNo like @SearchText + '%'  or VendorNo like @SearchText + '%')";
                cmd.Parameters.AddWithValue("@SearchText", prefix);
                cmd.Connection = conn;
                conn.Open();
                using (SqlDataReader sdr = cmd.ExecuteReader())
                {
                    while (sdr.Read())
                    {
                        customers.Add(string.Format("{0}-{1}", sdr["EntryNumber"], sdr["SKUID"]));
                    }
                    sdr.Close();
                }
                conn.Close();
            }
        }
        return customers.ToArray();
    }

    [WebMethod]
    public static string GetItems(string SID)
    {
        string one = "";
        using (SqlConnection conn = new SqlConnection())
        {
            conn.ConnectionString = ConfigurationManager.ConnectionStrings["ConnectionStringDemo"].ConnectionString;
            using (SqlCommand cmd1 = new SqlCommand())
            {
                cmd1.CommandText = "Select SKUID, EntryNumber, ItemNo, VendorNo, Description, Supplier from FBClosureParts where SKUactive = 1 and SKUID > 0 and (EntryNumber like @SearchText + '%'  or ItemNo like @SearchText + '%'  or VendorNo like @SearchText + '%')";
                cmd1.Parameters.AddWithValue("@SearchText", SID);
                cmd1.Connection = conn;
                conn.Open();
                using (SqlDataReader sdr1 = cmd1.ExecuteReader())
                {
                    if (sdr1.Read())
                    {
                        one = sdr1["EntryNumber"].ToString() + "^" + sdr1["ItemNo"].ToString() + "^" + sdr1["VendorNo"].ToString() + "^" + sdr1["Description"].ToString() + "^" + sdr1["SKUID"].ToString();
                    }
                    sdr1.Close();
                }
                conn.Close();
            }
        }
        return one;
    }
    protected void Button1_Click(object sender, EventArgs e)
    {
        String Eventid = Request.QueryString["SCF"].ToString();
        String Techid = Request.QueryString["TechID"].ToString();
        String CallType1 = CallType.Text.Replace("'", "''").ToString();
        String CompletionCode1 = CompletionCode.Text.Replace("'", "''").ToString();
        String EquipType1 = EquipType.Text.Replace("'", "''").ToString();
        String Vendor1 = Vendor.Text.Replace("'", "''").ToString();
        String model1 = model.Text.Replace("'", "''").ToString();
        String weight = WeightTxt.Text.Replace("'", "''").ToString();
        String ratio = RatioTxt.Text.Replace("'", "''").ToString();

        String serial1 = string.Empty;
        if (serial.Visible)
        {
            serial1 = serial.Text.Replace("'", "''").ToString();
        }
        else if(serialNumDD.Visible)
        {
            serial1 = serialNumDD.Text.Replace("'", "''").ToString();
        }

        String EventNotes1 = EventNotes.Text.Replace("'", "''").ToString();
	//Session["Comments"] = EventNotes1;
        String rcount = trcount.Value;
        SqlTransaction tran = null;
        try
        {
            con.Open();
            tran = con.BeginTransaction();
            String sNewPass = Eventid.Substring(0, 3) + sGenPwd(2) + Eventid.Substring(Eventid.Length - 6);
            SqlCommand cmd = new SqlCommand("INSERT INTO TMP_BlackBerry_SCFAssetInfo(WorkorderID, TechID, ServiceCode, CompletionCode, EquipTypeCode, VendorCode, Model, SerialNo, Notes, CloseCall, ClosureConfirmationNo, Weight, Ratio) values ('" + Eventid + "', '" + Techid + "', '" + CallType1 + "', '" + CompletionCode1 + "', '" + EquipType1 + "', '" + Vendor1 + "', '" + model1 + "', '" + serial1 + "', '" + EventNotes1 + "',0, '" + sNewPass + "', '"+ weight + "','"+ratio+"')", con);
            cmd.Transaction = tran;
            int rowsAffected = cmd.ExecuteNonQuery();
            string query1 = "Select @@Identity";
            cmd.CommandText = query1;
            int idx1 = Convert.ToInt32(cmd.ExecuteScalar());
            if (rowsAffected == 1)
            {
                if (rcount != "0" || rcount != "")
                {
                    int r = Int32.Parse(rcount);
                    for (int i = 1; i <= r; i++)
                    {
                        if (Request.Form["EN" + i] != null)
                        {
                            string EN = Request.Form["EN" + i].Replace("'", "''").ToString();
                            string IN = Request.Form["IN" + i].Replace("'", "''").ToString();
                            string VN = Request.Form["VN" + i].Replace("'", "''").ToString();
                            string Des = Request.Form["Des" + i].Replace("'", "''").ToString();
                            string Qty = Request.Form["Qty" + i].Replace("'", "''").ToString();
                            string Sku = Request.Form["Sku" + i].Replace("'", "''").ToString();
                            SqlCommand cmd1 = new SqlCommand("INSERT INTO TMP_BlackBerry_SCFPartsInfo(WorkorderID,TechID,EntryNo,ItemNo,VendorNo,Description,SKUID,Quantity,Pid) values ('" + Eventid + "', '" + Techid + "','" + EN + "', '" + IN + "', '" + VN + "', '" + Des + "','" + Sku + "', '" + Qty + "', '" + idx1 + "')", con);
                            cmd1.Transaction = tran;
                            cmd1.ExecuteNonQuery();
                        }
                    }
                }
            }
            tran.Commit();
            //errmsg.Text = "<font color='green'><b>Data Inserted Successfully.</b></font>";
        }
        catch (Exception exp)
        {
            if (tran != null)
                tran.Rollback();
            ScriptManager.RegisterClientScriptBlock(this, this.GetType(), "alertMessage", "alert('Transaction Rolledback, Please Try Again.')", true);
            // errmsg.Text = "<font color='Red'><b>"+ exp.Message.ToString() + " \nTransaction Rolledback, Please Try Again.</b></font>";
        }
        finally
        {
            con.Close();
            ClearData();
            //ScriptManager.RegisterClientScriptBlock(this, this.GetType(), "alertMessage", "alert('Machine and Parts Inserted Successfully.')", true);
	    msg1.Text = "<font color='green' style='font-size:20px;'><b>Machine and Parts Inserted Successfully</b></font>";
            succmsg.Visible = true;
            callclo.Visible = true;
            GetMachins();
        }
    }
    protected void GetMachins()
    {
        Div1.Visible = true;
        StringBuilder str1 = new StringBuilder();
        String SCF = Request.QueryString["SCF"].ToString();
        String TechID = Request.QueryString["TechID"].ToString();
        con.Open();
        using (SqlCommand cmd7 = new SqlCommand("Select * from TMP_BlackBerry_SCFAssetInfo where techID= " + TechID + " and [WorkorderID] = " + SCF, con))
        {
            str1.Append("<h4 style='margin: 0px;'>Added Machines:</h4>");
            str1.Append("<table border='1'><tr><th>Model</th><th>Serial</th><th>Completion Code</th></tr>");   
            SqlDataReader dr7 = cmd7.ExecuteReader();
            while (dr7.Read())
            {
                str1.Append("<tr><td>"+dr7["Model"].ToString()+ "</td><td>" + dr7["SerialNo"].ToString() + "</td><td>" + dr7["CompletionCode"].ToString() + "</td></tr>");
            }
            if(!dr7.HasRows)
            {
                str1.Append("<tr><td colspan='3'>No Machines Added.</td></tr>");
            }
            else
            {
                callclo.Visible = true;
            }
            str1.Append("</table>");
            dr7.Close();
            Div1.InnerHtml = str1.ToString();
            con.Close();
        }
    }
    protected void Button2_Click(object sender, EventArgs e)
    {
        BillableData();
        
        //string imgurl = ImgInformation.Value;
        String Eventid = Request.QueryString["SCF"].Replace("'", "''").ToString();
        String Techid = Request.QueryString["TechID"].Replace("'", "''").ToString();
        DateTime Sdate = Convert.ToDateTime(SDate.Text.ToString());
        DateTime Adate = Convert.ToDateTime(ADate.Text.ToString());
        DateTime Cdate = Convert.ToDateTime(CDate.Text.ToString());
        String SignedBy1 = SignedBy.Text.Replace("'", "''");
        String InvNo1 = InvNo.Text.Replace("'", "''");

        ClosureFilters(Eventid);

        bool WaterTested = WaterTestedChk.Checked;
        string waterHardness = HardnessRatingDropDown.Text.Replace("'", "''").ToString();

        con.Open();
        //SqlCommand cmd2 = new SqlCommand("INSERT INTO TMP_BlackBerry_SCFInvoiceInfo(WorkorderID, TechID, StartDateTime, ArrivalDateTime, CompletionDateTime,SignedBy,Signature) values('" + Eventid + "', '" + Techid + "', '" + Sdate.ToString("yyyy-MM-dd HH:mm:ss") + "', '" + Adate.ToString("yyyy-MM-dd HH:mm:ss") + "', '" + Cdate.ToString("yyyy-MM-dd HH:mm:ss") + "','" + SignedBy1 + "', '" + imgurl + "')", con);
        SqlCommand cmd2 = new SqlCommand("INSERT INTO TMP_BlackBerry_SCFInvoiceInfo(WorkorderID, TechID, StartDateTime, ArrivalDateTime, CompletionDateTime,SignedBy,InvoiceNo,WaterTested,HardnessRating) values('" + Eventid + "', '" + Techid + "', '" + Sdate.ToString("yyyy-MM-dd HH:mm:ss") + "', '" + Adate.ToString("yyyy-MM-dd HH:mm:ss") + "', '" + Cdate.ToString("yyyy-MM-dd HH:mm:ss") + "','" + SignedBy1 + "','" + InvNo1 + "','"+WaterTested+"','"+waterHardness+"')", con);
        cmd2.ExecuteNonQuery();
        SqlCommand cmd9 = new SqlCommand("update TMP_BlackBerry_SCFAssetInfo set CloseCall=1 where WorkorderID='" + Eventid + "'", con);
        cmd9.ExecuteNonQuery();
        ClearData();
        //ScriptManager.RegisterClientScriptBlock(this, this.GetType(), "alertMessage", "alert('Data Inserted Successfully.')", true);
        fb1.Text = Eventid;

        //To get Closure Conf No:
        string CloseConfNo="";
        using (SqlCommand cmdClosConfNo = new SqlCommand("Select top 1 ClosureConfirmationNo from TMP_BlackBerry_SCFAssetInfo where WorkorderID =" + Eventid, con))
        {
            SqlDataReader drClosConfNo = cmdClosConfNo.ExecuteReader();
            if (drClosConfNo.Read())
            {
                
                CloseConfNo = drClosConfNo["ClosureConfirmationNo"].ToString(); 
            }
            drClosConfNo.Close();
        }
        msg1.Text = "<font color='green' style='font-size:20px;'><b>Data Inserted Successfully, Conf No:" + CloseConfNo + "</b></font>";
        con.Close();
        maintbl.Visible = false;
        callclo.Visible = true;
        succmsg.Visible = true;

        DeleteInvoice(Eventid);
        //sendElectronicMail(Convert.ToInt32(Eventid));	

    }
    protected void ClearData()
    {
        CallType.SelectedIndex = -1;
        CompletionCode.SelectedIndex = -1;
        EquipType.SelectedIndex = -1;
        Vendor.SelectedIndex = -1;
        model.Text = "";
        serial.Text = "";
        serialNumDD.SelectedIndex = -1;
        EventNotes.Text = "";
        WeightTxt.Text = "";
        RatioTxt.Text = "";
    }
    public void linkGoSomewhere_Click(object sender, EventArgs e)
    {
        maintbl.Visible = false;
        succmsg.Visible = false;
        NSR.Visible = true;
        fb2.Text= Request.QueryString["SCF"].ToString();
    }
    protected void Button3_Click(object sender, EventArgs e)
    {
            //int chk;
            //if (BillableCheckBox.Checked) { chk = 1; } else { chk = 0; }
            String WorkorderID = Request.QueryString["SCF"].Replace("'", "''").ToString();
            String Techid = Request.QueryString["TechID"].Replace("'", "''").ToString();
            String msrmsg1 = Reasions.SelectedItem.Text.Replace("'", "''").ToString()  +" - "+ nsrmsg.Text.Replace("'", "''").ToString();
            con.Open();
            String sNewPass = WorkorderID.Substring(0, 3) + sGenPwd(2) + WorkorderID.Substring(WorkorderID.Length - 6);
            SqlCommand cmd4 = new SqlCommand("USP_MAFBC_CloseNSR_ViaBlackBerry", con);
            cmd4.CommandType = CommandType.StoredProcedure;
            cmd4.Parameters.Add(new SqlParameter("@WorkorderID", WorkorderID));
            cmd4.Parameters.Add(new SqlParameter("@ClosureConf", sNewPass));
            cmd4.Parameters.Add(new SqlParameter("@Notes", msrmsg1));
            //cmd4.Transaction = tran1;
            cmd4.ExecuteNonQuery();
            ClearData();
            fb1.Text = WorkorderID;
            string CloseConfNo = "";
            using (SqlCommand cmdClosConfNo = new SqlCommand("Select top 1 WorkorderClosureConfirmationNo from WorkOrder where WorkorderID =" + WorkorderID, con))
            {
                SqlDataReader drClosConfNo = cmdClosConfNo.ExecuteReader();
                if (drClosConfNo.Read())
                {
                    CloseConfNo = drClosConfNo["WorkorderClosureConfirmationNo"].ToString();
                }
                drClosConfNo.Close();
            }
            //SqlCommand cmd5 = new SqlCommand("update WorkOrder set IsBillable=" + chk + "  where WorkorderID=" + WorkorderID, con);
            //cmd5.ExecuteNonQuery();
            msg1.Text = "<font color='green' style='font-size:20px;'><b>Data Inserted Successfully, Conf No:" + CloseConfNo + "</b></font>";
            con.Close();
            maintbl.Visible = false;
            callclo.Visible = true;
            succmsg.Visible = true;
            NSR.Visible = false;
    }
    [WebMethod]
    public static string[] GetSKUs(string prefix)
    {
        List<string> customers = new List<string>();
        using (SqlConnection conn = new SqlConnection())
        {
            conn.ConnectionString = ConfigurationManager.ConnectionStrings["ConnectionStringDemo"].ConnectionString;
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.CommandText = "Select SKU from FbBillableSKU where SKU like @SearchText + '%'";
                cmd.Parameters.AddWithValue("@SearchText", prefix);
                cmd.Connection = conn;
                conn.Open();
                using (SqlDataReader sdr = cmd.ExecuteReader())
                {
                    while (sdr.Read())
                    {
                        customers.Add(string.Format("{0}-{1}", sdr["SKU"], sdr["SKU"]));
                    }
                    sdr.Close();
                }
                conn.Close();
            }
        }
        return customers.ToArray();
    }
    [WebMethod]
    public static string GetItemsList(string SID)
    {
        string one = "";
        using (SqlConnection conn = new SqlConnection())
        {
            conn.ConnectionString = ConfigurationManager.ConnectionStrings["ConnectionStringDemo"].ConnectionString;
            using (SqlCommand cmd1 = new SqlCommand())
            {
                cmd1.CommandText = "Select * from FbBillableSKU where SKU like @SearchText + '%'";
                cmd1.Parameters.AddWithValue("@SearchText", SID);
                cmd1.Connection = conn;
                conn.Open();
                using (SqlDataReader sdr1 = cmd1.ExecuteReader())
                {
                    if (sdr1.Read())
                    {
                        one = sdr1["SKU"].ToString() + "^" + sdr1["SKUDescription"].ToString() + "^" + sdr1["UnitPrice"].ToString();
                    }
                    sdr1.Close();
                }
                conn.Close();
            }
        }
        return one;
    }
    protected void BillableData()
    {
        String rcount = SKUtrcount.Value;
        int SCF = int.Parse(Request.QueryString["SCF"].ToString());
        int chk;
        if (BillableCheckBox.Checked) { chk = 1; } else { chk = 0; }
        Double stot = 0.00, sum;
        String stotal = "";
        SqlTransaction tran = null;
        try
        {
            con.Open();
            if (chk == 1)
            {
                tran = con.BeginTransaction();

                if (rcount != "0" || rcount != "")
                {
                    int r = Int32.Parse(rcount);
                    for (int i = 1; i <= r; i++)
                    {
                        if (Request.Form["Sku" + i] != null)
                        {
                            string Sku = Request.Form["Sku" + i].Replace("'", "''").ToString();
                            int Qty = int.Parse(Request.Form["Qty" + i].Replace("'", "''").ToString());
                            sum = Convert.ToDouble(Request.Form["UnitPrice" + i].Replace("'", "''").Replace("$", "").ToString()) * Qty;
                            // sum = Convert.ToDouble(Request.Form["Price" + i].Replace("'", "''").Replace("$", "").ToString());

                            stot = stot + sum;
                            SqlCommand cmd1 = new SqlCommand("INSERT INTO FbWorkOrderSKU(WorkorderID,SKU,Qty) values (" + SCF + ", '" + Sku + "'," + Qty + ")", con);
                            cmd1.Transaction = tran;
                            cmd1.ExecuteNonQuery();

                        }
                    }
                    stotal = Math.Round(Convert.ToDecimal(stot), 2).ToString("0.00");
                }
                SqlCommand cmd = new SqlCommand("update WorkOrder set IsBillable=" + chk + ",TotalUnitPrice='" + stotal + "'  where WorkorderID=" + SCF, con);
                cmd.Transaction = tran;
                cmd.ExecuteNonQuery();
                tran.Commit();
                //errmsg.Text = "<font color='green'><b>Data Inserted Successfully.</b></font>";
            }
        }
        catch (Exception exp)
        {
            if (tran != null)
                tran.Rollback();
            BillableCheckBox.Checked = false;
            ScriptManager.RegisterClientScriptBlock(this, this.GetType(), "alertMessage", "alert('Transaction Rolledback, Please Try Again.')", true);
            // errmsg.Text = "<font color='Red'><b>"+ exp.Message.ToString() + " \nTransaction Rolledback, Please Try Again.</b></font>";
        }
        finally
        {
            con.Close();
            BillableCheckBox.Checked = false;
            //ScriptManager.RegisterClientScriptBlock(this, this.GetType(), "alertMessage", "alert('SKUs Inserted Successfully.')", true);
        }

    }
    public object sGenPwd(int nLength)
    {
        String sPassword = "";
        String sChars = "abcdefghijkmnopqrstuvwxyzABCDEFGHIJKLMNPQRSTUVWXYZ23456789";
        int nSize = sChars.Length;
        var random = new Random();
        for (int x = 1; x <= nLength; x++)
        {
            sPassword += sChars[random.Next(sChars.Length)];
        }
        return sPassword;
    }

    protected void ClosureFilters(string EventId)
    {
        string customerid = GetContact(EventId, "id");
        string customerZip = GetContact(EventId, "zip");

        SqlTransaction tran = null;
        try
        {
            con.Open();
            tran = con.BeginTransaction();

            DateTime CurrentDateTime = DateTime.Now;
            DateTime NextReplaceDateTime = CurrentDateTime.AddMonths(6);

            bool filterReplaced = FilterReplacedChk.Checked;

            SqlCommand cmd1 = new SqlCommand("UPDATE Contact Set FilterReplaced='" + filterReplaced + "', FilterReplacedDate='" + CurrentDateTime + "',NextFilterReplacementDate='" + NextReplaceDateTime+"' Where ContactId="+ customerid, con);
            cmd1.Transaction = tran;
            cmd1.ExecuteNonQuery();
            tran.Commit();

        }
        catch (Exception exp)
        {
            if (tran != null)
                tran.Rollback();
            ScriptManager.RegisterClientScriptBlock(this, this.GetType(), "alertMessage", "alert('Closure Filter Saving Error')", true);
        }
        finally
        {
            con.Close();
            //BillableCheckBox.Checked = false;
            //ScriptManager.RegisterClientScriptBlock(this, this.GetType(), "alertMessage", "alert('SKUs Inserted Successfully.')", true);
        }
    }

    public static DateTime GetCurrentTime(string zipCode)
    {
        if (zipCode.Length > 5)
        {
            zipCode = zipCode.Substring(0, 5);
        }

        using (SqlConnection conn = new SqlConnection())
        {
            conn.ConnectionString = ConfigurationManager.ConnectionStrings["ConnectionStringDemo"].ConnectionString;
            using (SqlCommand cmd1 = new SqlCommand())
            {
                cmd1.CommandText =@"Select dbo.getCustDateTime('" + zipCode + "')";                
                cmd1.Connection = conn;
                conn.Open();
                using (SqlDataReader sdr1 = cmd1.ExecuteReader())
                {
                    if (sdr1.Read())
                    {
                       // one = sdr1["SKU"].ToString() + "^" + sdr1["SKUDescription"].ToString() + "^" + sdr1["UnitPrice"].ToString();
                    }
                    sdr1.Close();
                }
                conn.Close();
            }
        }

        return new DateTime();
    }

    public static string GetContact(string EventId, string property)
    {
        string one = "";
        using (SqlConnection conn = new SqlConnection())
        {
            conn.ConnectionString = ConfigurationManager.ConnectionStrings["ConnectionStringDemo"].ConnectionString;
            using (SqlCommand cmd1 = new SqlCommand())
            {
                cmd1.CommandText = "Select CustomerId, CustomerZipCode from Workorder where workorderid="+EventId;
                cmd1.Connection = conn;
                conn.Open();
                using (SqlDataReader sdr1 = cmd1.ExecuteReader())
                {
                    if (sdr1.Read())
                    {
                        if(property.ToLower() == "id")
                        {
                            one = sdr1["CustomerId"].ToString();
                        }
                        else if(property.ToLower() == "zIp")
                        {
                            one = sdr1["CustomerZipCode"].ToString();
                        }
                        else
                        {
                            one = "";
                        }

                       
                    }
                    sdr1.Close();
                }
                conn.Close();
            }
        }
        return one;
    }

	private void sendElectronicMail(int WorkorderId)
    {
        string workorderQry = "select * from workorder where workorderid = " + WorkorderId;
        DataTable woSdr = GetQueryResults(workorderQry);
        string CustomerEmail = "";
        foreach (DataRow dr in woSdr.Rows)
        {
            CustomerEmail = dr["CustomerMainEmail"] != DBNull.Value ? dr["CustomerMainEmail"].ToString() : "";
        }

        if (!string.IsNullOrEmpty(CustomerEmail))
        {
            CustomerEmail = "abhilash@teqdatum.com;kavitabalusu@gmail.com;";

            MailMessage MyMailMessage = new MailMessage();
            MyMailMessage.From = new MailAddress("FarmerBrothers@mktalt.com");

            string[] addresses = CustomerEmail.Split(';');
            foreach (string address in addresses)
            {
                if (!string.IsNullOrWhiteSpace(address))
                {
                    MyMailMessage.To.Add(new MailAddress(address));
                }
            }

            //MyMailMessage.To.Add(CustomerEmail);
            MyMailMessage.Subject = "Electronic Invoice " + WorkorderId;
            MyMailMessage.IsBodyHtml = true;

            string htmlContent = GetEmailString(WorkorderId);

            StringBuilder salesEmailBody = new StringBuilder();
            salesEmailBody.Append(@"<img src='cid:logo' width='15%' height='15%' style='float: right;margin-right: 100px;margin-bottom: 10px;'>");
            salesEmailBody.Append(htmlContent);

            string contentId = Guid.NewGuid().ToString();
            string logoPath = string.Empty;
            if (Server == null)
            {
                logoPath = Path.Combine(HttpRuntime.AppDomainAppPath, "Img/mainlogo.jpg");
            }
            else
            {
                logoPath = Server.MapPath("~/Img/mainlogo.jpg");
            }

            salesEmailBody = salesEmailBody.Replace("cid:logo", "cid:" + contentId);
            AlternateView avHtml = AlternateView.CreateAlternateViewFromString
                   (salesEmailBody.ToString(), null, MediaTypeNames.Text.Html);

            LinkedResource inline = new LinkedResource(logoPath, MediaTypeNames.Image.Jpeg);
            inline.ContentId = contentId;
            avHtml.LinkedResources.Add(inline);

            MyMailMessage.AlternateViews.Add(avHtml);



            using (var smtp = new SmtpClient())
            {
                smtp.Host = "127.0.0.1";//ConfigurationManager.AppSettings["MailServer"];
                smtp.Port = 25;

                try
                {
                    smtp.Send(MyMailMessage);
                }
                catch (Exception ex)
                {
                    // result = false;
                }
            }
        }

    }

    private string GetEmailString(int workorderid)
    {
        string htmlString = string.Empty;

        string workorderQry = "select * from workorder where workorderid = " + workorderid;
        DataTable woSdr = GetQueryResults(workorderQry);
        string CustomerAcNo = "", WOEntryDate = "", CustomerName = "", CustomerAddress = "", CustomerCity = "";
        string CustomerState = "", CustomerZipCode = "", WOContactNm = "", WOContactPh = "", WorkorderId = "", TotalUnitPrice = "";
        foreach (DataRow dr in woSdr.Rows)
        {           
            CustomerAcNo = dr["CustomerID"] != DBNull.Value ? dr["CustomerID"].ToString() : "";
            WOEntryDate = dr["WorkorderEntryDate"] != DBNull.Value ? dr["WorkorderEntryDate"].ToString() : "";
            CustomerName = dr["CustomerName"] != DBNull.Value ? dr["CustomerName"].ToString() : "";
            CustomerAddress = dr["CustomerAddress"] != DBNull.Value ? dr["CustomerAddress"].ToString() : "";
            CustomerCity = dr["CustomerCity"] != DBNull.Value ? dr["CustomerCity"].ToString() : "";
            CustomerState = dr["CustomerState"] != DBNull.Value ? dr["CustomerState"].ToString() : "";
            CustomerZipCode = dr["CustomerZipCode"] != DBNull.Value ? dr["CustomerZipCode"].ToString() : "";
            WOContactNm = dr["CustomerMainContactName"] != DBNull.Value ? dr["CustomerMainContactName"].ToString() : "";
            WOContactPh = dr["CustomerPhone"] != DBNull.Value ? dr["CustomerPhone"].ToString() : "";
            WorkorderId = dr["WorkorderID"] != DBNull.Value ? dr["WorkorderID"].ToString() : "";
            TotalUnitPrice = dr["TotalUnitPrice"] != DBNull.Value ? dr["TotalUnitPrice"].ToString() : "";
        }

        string workorderScheduleQry = "select * from workorderschedule where workorderid = " + workorderid + " and AssignedStatus != 'Redirected' and AssignedStatus != 'Declined'";
        DataTable woSchSdr = GetQueryResults(workorderScheduleQry);
        string WOScheduleTechName = "";
        foreach (DataRow dr in woSchSdr.Rows)
        {
            WOScheduleTechName = dr["TechName"] != DBNull.Value ? dr["TechName"].ToString() : "";
        }
        
        string WorkorderDetailsQry = "select * from TMP_BlackBerry_SCFInvoiceInfo where workorderid = " + workorderid;
        DataTable woDtlSdr = GetQueryResults(WorkorderDetailsQry);
        string StartTime = "", ArrivalTime = "", CompletionTime = "";
        foreach (DataRow dr in woDtlSdr.Rows)
        {
            //TravelTime = dr["TravelTime"] != DBNull.Value ? dr["TravelTime"].ToString() : "";
            StartTime = dr["StartDateTime"] != DBNull.Value ? dr["StartDateTime"].ToString() : "";
            ArrivalTime = dr["ArrivalDateTime"] != DBNull.Value ? dr["ArrivalDateTime"].ToString() : "";
            CompletionTime = dr["CompletionDateTime"] != DBNull.Value ? dr["CompletionDateTime"].ToString() : "";
        }

        string woEqpReqQry = @"select wer.Assetid, wt.Description as WorkTypeDesc, wer.Temperature, wer.WorkPerformedCounter,
                                wer.Category, wer.Manufacturer, wer.Model, wer.Location, wer.SerialNumber, wer.QualityIssue,
                                wer.Email, wer.WorkDescription, sy.Description as SymptomDesc from WorkorderEquipmentRequested wer
                                inner join WorkorderType wt on wer.CallTypeid = wt.CallTypeID
                                inner join Symptom sy on wer.Symptomid = sy.SymptomID
                                where wer.WorkorderID = " + workorderid;
        DataTable woEqpReqSdr = GetQueryResults(woEqpReqQry);
        List<eqpModel> eqpReqObjList = new List<eqpModel>();
        foreach (DataRow dr in woEqpReqSdr.Rows)
        {
            eqpModel obj = new eqpModel();

            obj.Assetid = dr["Assetid"] != DBNull.Value ? dr["Assetid"].ToString() : "";
            obj.WorkOrderType = dr["WorkTypeDesc"] != DBNull.Value ? dr["WorkTypeDesc"].ToString() : "";
            obj.Temperature = dr["Temperature"] != DBNull.Value ? dr["Temperature"].ToString() : "";
            obj.WorkPerformedCounter = dr["WorkPerformedCounter"] != DBNull.Value ? dr["WorkPerformedCounter"].ToString() : "";
            obj.WorkDescription = dr["WorkDescription"] != DBNull.Value ? dr["WorkDescription"].ToString() : "";
            obj.Category = dr["Category"] != DBNull.Value ? dr["Category"].ToString() : "";
            obj.Manufacturer = dr["Manufacturer"] != DBNull.Value ? dr["Manufacturer"].ToString() : "";
            obj.Model = dr["Model"] != DBNull.Value ? dr["Model"].ToString() : "";
            obj.Location = dr["Location"] != DBNull.Value ? dr["Location"].ToString() : "";
            obj.SerialNumber = dr["SerialNumber"] != DBNull.Value ? dr["SerialNumber"].ToString() : "";
            obj.QualityIssue = dr["QualityIssue"] != DBNull.Value ? dr["QualityIssue"].ToString() : "";
            obj.Email = dr["Email"] != DBNull.Value ? dr["Email"].ToString() : "";
            obj.SymptomDesc = dr["SymptomDesc"] != DBNull.Value ? dr["SymptomDesc"].ToString() : "";
            

            eqpReqObjList.Add(obj);
        }



        //string woEqpQry = @"select* from WorkorderEquipment we
        //                                        inner join WorkorderType wt on we.CallTypeid = wt.CallTypeID
        //                                        inner join Solution so on we.Solutionid = so.SolutionId
        //                                        where we.WorkorderID = " + workorderid;
        string woEqpQry = @"select we.AssetKey as Assetid, wt.Description as WorkorderDesc, we.CategoryDesc as Category, we.Model as Model, we.ManufacturerDesc as Manufacturer,
                                        we.SerialNo as SerialNumber, so.Description as SolutionDesc from TMP_BlackBerry_SCFAssetInfo we
                                        inner join WorkorderType wt on we.ServiceCode = wt.CallTypeID
                                        inner join Solution so on we.CompletionCode = so.SolutionId
                                        where we.WorkorderID = " + workorderid;
        DataTable woEqpSdr = GetQueryResults(woEqpQry);
        List<eqpModel> eqpObjList = new List<eqpModel>();
        foreach (DataRow dr in woEqpSdr.Rows)
        {
            eqpModel obj = new eqpModel();

            string assetId = dr["Assetid"] != DBNull.Value ? dr["Assetid"].ToString() : "";

            obj.Assetid = assetId;
            obj.WorkOrderType = dr["WorkorderDesc"] != DBNull.Value ? dr["WorkorderDesc"].ToString() : "";
            //obj.Temperature = dr["Temperature"] != DBNull.Value ? dr["Temperature"].ToString() : "";
            //obj.WorkPerformedCounter = dr["WorkPerformedCounter"] != DBNull.Value ? dr["WorkPerformedCounter"].ToString() : "";
            //obj.WorkDescription = dr["WorkDescription"] != DBNull.Value ? dr["WorkDescription"].ToString() : "";
            obj.Category = dr["Category"] != DBNull.Value ? dr["Category"].ToString() : "";
            obj.Manufacturer = dr["Manufacturer"] != DBNull.Value ? dr["Manufacturer"].ToString() : "";
            obj.Model = dr["Model"] != DBNull.Value ? dr["Model"].ToString() : "";
            //obj.Location = dr["Location"] != DBNull.Value ? dr["Location"].ToString() : "";
            obj.SerialNumber = dr["SerialNumber"] != DBNull.Value ? dr["SerialNumber"].ToString() : "";
            //obj.QualityIssue = dr["QualityIssue"] != DBNull.Value ? dr["QualityIssue"].ToString() : "";
            //obj.Email = dr["Email"] != DBNull.Value ? dr["Email"].ToString() : "";
            obj.SolutionDesc = dr["SolutionDesc"] != DBNull.Value ? dr["SolutionDesc"].ToString() : "";

            eqpObjList.Add(obj);
        }

        //===================================================================================================
        IList<WOParts> ClosureEquipmentParts = new List<WOParts>();
        decimal partsTotal = 0;
        foreach (eqpModel em in eqpObjList)
        {
            ClosureEquipmentParts = new List<WOParts>();
            if (!string.IsNullOrEmpty(em.Assetid))
            {
                string woPartsQry = @"select * from TMP_BlackBerry_SCFAssetInfo where AssetKey = " + em.Assetid;
                DataTable woPartsSdr = GetQueryResults(woPartsQry);

                foreach (DataRow prt in woPartsSdr.Rows)
                {
                    WOParts wp = new WOParts();
                    wp.PartsIssueid = prt["PartsIssueid"] != DBNull.Value ? Convert.ToInt32(prt["PartsIssueid"].ToString()) : 0;
                    wp.Quantity = prt["Quantity"] != DBNull.Value ? Convert.ToInt32(prt["Quantity"].ToString()) : 0;
                    wp.Manufacturer = prt["Manufacturer"] != DBNull.Value ? prt["Manufacturer"].ToString() : "";
                    wp.Sku = prt["SKU"] != DBNull.Value ? prt["SKU"].ToString() : "";
                    wp.Description = prt["Description"] != DBNull.Value ? prt["Description"].ToString() : "";

                    if (!string.IsNullOrEmpty(wp.Sku))
                    {
                        string skuCostQry = @"select * from Sku where Sku = '" + wp.Sku + "'";
                        DataTable skuCostSdr = GetQueryResults(skuCostQry);
                        foreach (DataRow cst in skuCostSdr.Rows)
                        {
                            wp.skuCost = cst["SKUCost"] != DBNull.Value ? Convert.ToDecimal(cst["SKUCost"].ToString()) : 0;
                        }
                    }
                    else
                    {
                        wp.skuCost = 0;
                    }

                    wp.partsTotal = Convert.ToDecimal(wp.skuCost * wp.Quantity);
                    partsTotal += wp.partsTotal;

                    ClosureEquipmentParts.Add(wp);
                }
            }
            em.Parts = ClosureEquipmentParts;
        }

        string PartsTotal = partsTotal.ToString();
        string LaborCost = TotalUnitPrice == "" ? "0" : TotalUnitPrice;
        string Total = (Convert.ToDecimal(PartsTotal) + Convert.ToDecimal(LaborCost)).ToString() ;



        //===================================================================================================

        string taxQry = @"select * from State Where StateCode = '" + CustomerState +"'";
        DataTable taxSdr = GetQueryResults(taxQry);
        string taxValue = "";
        foreach (DataRow dr in taxSdr.Rows)
        {
            taxValue = dr["TaxPercent"] != DBNull.Value ? dr["TaxPercent"].ToString() : "";
        }
        string machineNotes = Session["Comments"] != null ? Session["Comments"].ToString() : "";

        htmlString += "<body>";
        htmlString += " <div id='mainBodyDiv' style='background-color:#8080801c; border:#a9a9a9b5 solid 1px;float:left; padding:5px;'>";
        htmlString += "<div class='invoice-form'>";
        htmlString += "<table width = '100%' border='0' cellspacing='0' cellpadding='0' class='table border-less'>";
        htmlString += " <tr>";
        htmlString += "<td style='padding: 2px; text-align: left; '>";
        htmlString += "<div class='invoice-header' style='margin-bottom: 20px;overflow: hidden; '>";
        htmlString += "<div class='work-order' style='padding:10px;'>";
        htmlString += "<table>";
        htmlString += "<tr>";
        htmlString += "<td width = '80%' style='padding: 2px; text-align: left; '>";
        htmlString += "<table width='100%' border='0' cellspacing='0' cellpadding='0'>";
        htmlString += @"<tr>
             <td style='padding: 2px; text-align: left; '>
                                                        <label>Customer Account Number&nbsp;:</label>
                                                    </td>
                                                    <td style='padding: 2px; text-align: left; '>
                                                        <label style = 'color:#0000ff;'>" + CustomerAcNo + @"</label>
                                                    </td>
                                                    <td style='padding: 2px; text-align: left; '>
                                                        <label> Service Date&nbsp;:</label>
                                                    </td>
                                                    <td style='padding: 2px; text-align: left; '>
                                                        <label>" + WOEntryDate + @"</label>
                                                    </td>
                                                </tr>
                                                <tr>
                                                    <td style='padding: 2px; text-align: left; '>
                                                        <label>Customer Location&nbsp;:</label>
                                                    </td>
                                                    <td style='padding: 2px; text-align: left; '>
                                                        <label>" + CustomerName+@"</label></br>
                                                        <label>"+CustomerAddress + @"</label></br>
                                                        <label>"+CustomerCity+@" </label>
                                                        <label>"+CustomerState + @"</label>
                                                        <label>"+CustomerZipCode+ @" </label>
                                                    </td>
                                                    <td style='padding: 2px; text-align: left; '>
                                                        <label>Technician Name&nbsp;:</label>
                                                    </td>
                                                    <td style='padding: 2px; text-align: left; '>
                                                        <label>" + WOScheduleTechName + @"</label>
                                                    </td>
                                                </tr>
                                                <tr>
                                                    <td style='padding: 2px; text-align: left; '><label>Service Contact&nbsp;:</label></td>
                                                    <td style='padding: 2px; text-align: left; '>
                                                        <label>" + WOContactNm + @"</label>

                                                    </td>
                                                    <td style='padding: 2px; text-align: left; '><label>Phone Number&nbsp;:</label></td>
                                                    <td style='padding: 2px; text-align: left; '>
                                                        <label>" + WOContactPh+ @"</label>
                                                    </td>
                                                </tr>
                                                <tr>
                                                    <td style='padding: 2px; text-align: left; '><label>Workorder ID&nbsp;:</label></td>
                                                    <td style='padding: 2px; text-align: left; '><label>" + WorkorderId + @"</label></td>
                                                </tr>
                                            </table>
                                        </td>
                                    </tr>
                                </table>
                            </div>
                        </div>
                    </td>
                </tr>";

        htmlString += @"<tr>
                    <td style='padding: 2px; text-align: left; '>
                        <div class='reqEquipmentBlock'>

                            <table width = '100%' border='0' cellspacing='0' cellpadding='0' class='table'>
                                <tr>
                                    <td style='padding: 2px; text-align: left; '>

                                        <div class='panel panel-default margin0'>
                                            <div class='panel-heading wrkReqHeading'>
                                                <h4 class='panel-title'>Equipment</h4>
                                            </div>
                                            <div class='panel-body'>

                                                <div id = 'eqpReqMain' style='background-color: white;padding:10px;' class='stripe-borders-table-grid reqEqpSec'>
                                                    
                                                    <table id = 'gridWrkReq' width='100%' data-swhgajax='true' data-swhgcontainer='eqpReqMain' data-swhgcallback=''>
                                                        <thead>
                                                            <tr>
                                                                <th scope = 'col' style='background-color: rgb(241,237,237); padding: 2px; text-align: left;     word-wrap: break-word;'>
                                                                    Equipment Type
                                                                </th>
                                                                <th scope = 'col' style='background-color: rgb(241,237,237); padding: 2px; text-align: left;     word-wrap: break-word;'>
                                                                    Service Code
                                                                </th>
                                                                <th scope = 'col' style='background-color: rgb(241,237,237); padding: 2px; text-align: left;     word-wrap: break-word;'>
                                                                    Symptom
                                                                </th>
                                                                <th scope='col' style='background-color: rgb(241,237,237); padding: 2px; text-align: left;     word-wrap: break-word;'>
                                                                    Equipment Location
                                                                </th>
                                                                <th scope = 'col' style='background-color: rgb(241,237,237); padding: 2px; text-align: left;     word-wrap: break-word;'>
                                                                    Serial Number
                                                                </th>
                                                            </tr>
                                                        </thead>";

        htmlString += "<tbody>";
        foreach(eqpModel epreq in eqpReqObjList)
        {
           

            htmlString += @"<tr>
                                            <td style='padding: 2px; text-align: left; '>" + epreq.Category + @"</td>
                                            <td style='padding: 2px; text-align: left; '> " + epreq.WorkOrderType + @"</td>
                                            <td style='padding: 2px; text-align: left; '> " + epreq.SymptomDesc + @"</td>
                                            <td style='padding: 2px; text-align: left; '> " + epreq.Location + @"</td>
                                            <td style='padding: 2px; text-align: left; '> " + epreq.SerialNumber + @"</td>
                                        </tr>";
        }


        htmlString += @"</tbody>
                                                    </table>

                                                </div>

                                            </div>
                                        </div>

                                    </td>
                                </tr>
                            </table>

                        </div>
                    </td>
                </tr>
               <tr>
                    <td style='padding: 2px; text-align: left; '>
                        <div class='costDetailsBlock' style='padding: 30px; margin-top:25px'>
                            <table align = 'right' border='0' cellspacing='0' cellpadding='0' class='table' style='width: 50%;border: black solid 1px;'>
                               
                                <tr style = 'background:#cacbe6;'>
                                    <td style='padding: 2px; text-align: left; '><label> Labor </label></td>
                                    <td style='padding: 2px; text-align: left; '><label>$" + LaborCost + @"</label></td>
                                </tr>
                                <tr style = 'background:#cacbe6;'>
                                    <td style='padding: 2px; text-align: left; '><label> Parts </label></td>
                                    <td style='padding: 2px; text-align: left; '><label>$" + partsTotal + @"</label></td>
                                </tr>
                                <tr style = 'background:#cacbe6;'>
                                    <td style='padding: 2px; text-align: left; '><label> Total </label></td>
                                    <td style='padding: 2px; text-align: left; '><label>$" +Total + @"</label></td>
                                </tr>

                            </table>
                        </div>
                    </td>
                </tr>


                <tr>
                    <td style='padding: 2px; text-align: left; '>
                        <div class='equipmentBlock'>
                            <table width = '100%' border='0' cellspacing='0' cellpadding='0' class='table'>
                                <tr>
                                    <td style='padding: 2px; text-align: left; '>

                                        <div class='panel panel-default margin0'>
                                            <div class='panel-heading closureEqpHeading'>
                                                <h4 class='panel-title'>Asset Details</h4>
                                            </div>

                                            <div class='panel-body' style='background:#ffffff;'>
                                                <div id = 'main' style='background-color:#fffffff; padding: 0;' class='stripe-borders-table-grid eqpSec'>
                                                    <table id = 'closureAssetsTbl' style='background:#ffffff; width:100%;'>
                                                        <thead>
                                                        <th style='background-color: rgb(241,237,237); padding: 2px; text-align: left; word-wrap: break-word;'>service Code</th>
                                                        <th style='background-color: rgb(241,237,237); padding: 2px; text-align: left; word-wrap: break-word;'>Equipment Type</th>
                                                        <th style='background-color: rgb(241,237,237); padding: 2px; text-align: left; word-wrap: break-word;'>Manufacturer</th>
                                                        <th style='background-color: rgb(241,237,237); padding: 2px; text-align: left; word-wrap: break-word;'>Asset ID</th>
                                                        <th style='background-color: rgb(241,237,237); padding: 2px; text-align: left; word-wrap: break-word;'>Model</th>
                                                        <th style='background-color: rgb(241,237,237); padding: 2px; text-align: left; word-wrap: break-word;'>Serial Number</th>
                                                        <th style='background-color: rgb(241,237,237); padding: 2px; text-align: left; word-wrap: break-word;'>Completion Code</th>
                                                        <th style='background-color: rgb(241,237,237); padding: 2px; text-align: left; word-wrap: break-word;'>Temperature</th>
                                                        <th style='background-color: rgb(241,237,237); padding: 2px; text-align: left; word-wrap: break-word;'>Describe Work Performed</th>
                                                        </thead>
                                                        <tbody>";
        foreach (eqpModel ep in eqpObjList) {
            htmlString += @" <tr>
                                                                <td style='padding: 2px; text-align: left;border: 0.1px solid; border-color: #e3e1e1eb;word-wrap: break-word; '>" + ep.WorkOrderType + @"</td>
                                                                <td style='padding: 2px; text-align: left;border: 0.1px solid; border-color: #e3e1e1eb;word-wrap: break-word; '>" + ep.Category + @"</td>
                                                                <td style='padding: 2px; text-align: left;border: 0.1px solid; border-color: #e3e1e1eb;word-wrap: break-word; '>" + ep.Manufacturer + @"</td>
                                                                <td style='padding: 2px; text-align: left;border: 0.1px solid; border-color: #e3e1e1eb;word-wrap: break-word; '>" + ep.Assetid + @"</td>
                                                                <td style='padding: 2px; text-align: left;border: 0.1px solid; border-color: #e3e1e1eb;word-wrap: break-word; '>" + ep.Model + @"</td>
                                                                <td style='padding: 2px; text-align: left;border: 0.1px solid; border-color: #e3e1e1eb;word-wrap: break-word; '>" + ep.SerialNumber + @"</td>
                                                                <td style='padding: 2px; text-align: left;border: 0.1px solid; border-color: #e3e1e1eb;word-wrap: break-word; '>" + ep.SolutionDesc + @"</td>
                                                                <td style='padding: 2px; text-align: left;border: 0.1px solid; border-color: #e3e1e1eb;word-wrap: break-word; '>" + ep.Temperature + @"</td>
                                                                <td style='padding: 2px; text-align: left;border: 0.1px solid; border-color: #e3e1e1eb;word-wrap: break-word; '>" + ep.WorkDescription + @"</td>                                                                
                                                            </tr>
                                                            <tr>";
            htmlString += @"<tr>
                                                                <td style='padding: 2px; text-align: left;border: 0.1px solid;border-color: #e3e1e1eb; word-wrap: break-word; '></td>
                                                                <td style = 'padding:5px; margin:0px;border: 0.1px solid;border-color: #e3e1e1eb; word-wrap: break-word;' colspan= '9'>
                                                                    <table id= 'partsTbl' style='width: 100%;'>
                                                                        <thead>
                                                                            <tr>
                                                                                <th style='background-color: rgb(241,237,237); padding: 2px; text-align: left;border:0.1px solid;border-color:#e3e1e1eb; word-wrap:break-word;'> Quantity </th>
                                                                                <th style='background-color: rgb(241,237,237); padding: 2px; text-align: left;border:0.1px solid;border-color:#e3e1e1eb; word-wrap:break-word;'> Manufacturer </th>
                                                                                <th style='background-color: rgb(241,237,237); padding: 2px; text-align: left;border:0.1px solid;border-color:#e3e1e1eb; word-wrap:break-word;'> SKU </th>
                                                                                <th style='background-color: rgb(241,237,237); padding: 2px; text-align: left;border:0.1px solid;border-color:#e3e1e1eb; word-wrap:break-word;'> Description </th>
                                                                                <th style='background-color: rgb(241,237,237); padding: 2px; text-align: left;border:0.1px solid;border-color:#e3e1e1eb; word-wrap:break-word;'> Parts Total</th>
                                                                            </tr>
                                                                        </thead>
                                                                        <tbody>";
            foreach (WOParts wop in ep.Parts)
            {

                htmlString += @"  <tr>
                                                <td style='padding:2px; text-align:left;border: 0.1px solid;border-color: #e3e1e1eb; word-wrap: break-word; '>"+wop.Quantity+ @"</td>
                                                <td style='padding:2px; text-align:left;border: 0.1px solid;border-color: #e3e1e1eb; word-wrap: break-word; '>" + wop.Manufacturer + @"</td>
                                                <td style='padding:2px; text-align:left;border: 0.1px solid;border-color: #e3e1e1eb; word-wrap: break-word; '>" + wop.Sku + @"</td>
                                                <td style='padding:2px; text-align:left;border: 0.1px solid;border-color: #e3e1e1eb; word-wrap: break-word; '>" + wop.Description + @"</td>
                                                <td style='padding:2px; text-align:left;border: 0.1px solid;border-color: #e3e1e1eb; word-wrap: break-word; '>" + wop.partsTotal + @"</td>
                                            </tr>";
            }
                                                                         htmlString += @" </tbody>
                                                                    </table>
                                                                </td>
                                                            </tr>
                                </tr>";
        }
        htmlString += @" </tbody>
            </table>
        </div>



    </div>
    </div>

    </td>
    </tr>

    
    </table>

    </div>
    </td>
    </tr>

    <tr></tr>

    <tr>
        <td style='padding: 2px; text-align: left; '>
            <div class='timeDetailsBlock' style='padding: 25px; margin-top:25px'>
                <table width = '100%'>
                    <tr style='background:#fed5c5;'>
                        <td style='padding: 2px; text-align: left; '><label>Start Time</label></td>
                        <td style='padding: 2px; text-align: left; '><label>" + StartTime+ @"</label></td>
                        <td style='padding: 2px; text-align: left; '><label>Arrival Time</label></td>
                        <td style='padding: 2px; text-align: left; '><label>" + ArrivalTime+ @"</label></td>
                    </tr>
                    <tr style = 'background:#fed5c5;'>
                        <td style='padding: 2px; text-align: left; '><label>Completion Time</label></td>
                        <td style='padding: 2px; text-align: left; '><label>" + CompletionTime + @"</label></td>
                        <td style='padding: 2px; text-align: left; '><label></label></td>
                        <td style='padding: 2px; text-align: left; '><label></label></td>
                </tr>

                </table>
            </div>
        </td>
    </tr>
    <tr>
        <td style='padding: 2px; text-align: left; '>
            <div class='panel panel-default margin0'>

                <div class='panel-body'>
                    <table style='float: right;' >
                        <tr><td>Machine Notes: </td><td style='padding: 2px; text-align: left; '>" + machineNotes + @"</td></tr>
                        <tr><td>Tax: </td><td style='padding: 2px; text-align: left; '>" + taxValue + @"%</td></tr>
                        <tr><td></td><td style='padding: 2px; text-align: left; '><a href = 'https://goo.gl/forms/KgvSAgobIEee3kEz2' target='_blank'>Click here for Customer Satisfaction Survey</a> </td></tr>
                    </table>

                </div>
            </div>
        </td>
    </tr>
    </table>
    </div>
    <table align= 'right' class='signatureBlock'></table> </div></body> ";


        return htmlString;
    }


    private DataTable GetQueryResults(string Query)
    {
        string connectionString = ConfigurationManager.ConnectionStrings["ConnectionStringDemo"].ConnectionString;
        DataSet ds = new DataSet();
        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            SqlCommand sqlComm = new SqlCommand(Query, conn);
            sqlComm.CommandType = CommandType.Text;
            SqlDataAdapter da = new SqlDataAdapter();
            da.SelectCommand = sqlComm;
            da.Fill(ds);
        }
        return ds.Tables[0];
    }

    protected void serialNumDD_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (serialNumDD.SelectedItem.Text.ToLower() == "other")
        {
            serial.Visible = true;
            model.Text = "";
        }
        else
        {
            serial.Visible = false;
            string message = serialNumDD.SelectedItem.Text + " - " + serialNumDD.SelectedItem.Value;

            string modelNo = "";
            using (SqlConnection conn = new SqlConnection())
            {
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["ConnectionStringDemo"].ConnectionString;
                using (SqlCommand cmd1 = new SqlCommand())
                {
                    cmd1.CommandText = "Select * from FBCBE where SerialNumber = @SNO";
                    cmd1.Parameters.AddWithValue("@SNO", serialNumDD.SelectedItem.Value);
                    cmd1.Connection = conn;
                    conn.Open();
                    using (SqlDataReader sdr1 = cmd1.ExecuteReader())
                    {
                        if (sdr1.Read())
                        {
                            modelNo = sdr1["ItemNumber"] != DBNull.Value ? sdr1["ItemNumber"].ToString() : "";
                        }
                        sdr1.Close();
                    }
                    conn.Close();
                }
            }

            model.Text = modelNo;
        }
    }

    protected void InvoiceGenerateBtn_Click(object sender, EventArgs e)
    {
        string Eventid = Request.QueryString["SCF"].Replace("'", "''").ToString();
        try
        {
            string InvoiceNo = "";
            using (SqlConnection conn = new SqlConnection())
            {
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["ConnectionStringDemo"].ConnectionString;
                using (SqlCommand cmd1 = new SqlCommand())
                {
                    cmd1.CommandText = "Select * from Invoices where workorderid = @EventId";
                    cmd1.Parameters.AddWithValue("@EventId", Eventid);
                    cmd1.Connection = conn;
                    conn.Open();
                    using (SqlDataReader sdr1 = cmd1.ExecuteReader())
                    {
                        if (sdr1.Read())
                        {
                            InvoiceNo = sdr1["Invoiceid"] != DBNull.Value ? sdr1["Invoiceid"].ToString() : "";
                        }
                        sdr1.Close();
                    }

                    if (string.IsNullOrEmpty(InvoiceNo))
                    {
                        int IndexValue = 0;
                        cmd1.CommandText = "Select * from IndexCounter  WHERE IndexName = 'InvoiceID'";
                        cmd1.Connection = conn;
                        using (SqlDataReader sdr2 = cmd1.ExecuteReader())
                        {
                            if (sdr2.Read())
                            {
                                IndexValue = sdr2["IndexValue"] != DBNull.Value ? Convert.ToInt32(sdr2["IndexValue"].ToString()) : 0;
                            }
                            sdr2.Close();
                        }

                        IndexValue++;
                        SqlCommand cmd2 = new SqlCommand("Update IndexCounter Set IndexValue = " + (IndexValue) + " WHERE  IndexName = 'InvoiceID'", conn);
                        cmd2.ExecuteNonQuery();

                        InvoiceNo = "FB" + IndexValue;

                        SqlCommand cmd3 = new SqlCommand("Insert into Invoices (InvoiceId, WorkorderId) Values('" + InvoiceNo + "' , " + Eventid + ")", conn);
                        cmd3.ExecuteNonQuery();

                    }

                    InvNo.Text = InvoiceNo;

                    conn.Close();
                }
            }
        }
        catch (Exception ex)
        {
            InvNo.Text = "";
        }
    }

    [WebMethod]
    public static string GenerateInvoiceNumber(string Eventid)
    {
        //string Eventid = Request.QueryString["SCF"].Replace("'", "''").ToString();
        try
        {
            string InvoiceNo = "";
            using (SqlConnection conn = new SqlConnection())
            {
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["ConnectionStringDemo"].ConnectionString;
                using (SqlCommand cmd1 = new SqlCommand())
                {
                    cmd1.CommandText = "Select * from Invoices where workorderid = @EventId";
                    cmd1.Parameters.AddWithValue("@EventId", Eventid);
                    cmd1.Connection = conn;
                    conn.Open();
                    using (SqlDataReader sdr1 = cmd1.ExecuteReader())
                    {
                        if (sdr1.Read())
                        {
                            InvoiceNo = sdr1["Invoiceid"] != DBNull.Value ? sdr1["Invoiceid"].ToString() : "";
                        }
                        sdr1.Close();
                    }

                    if (string.IsNullOrEmpty(InvoiceNo))
                    {
                        int IndexValue = 0;
                        cmd1.CommandText = "Select * from IndexCounter  WHERE IndexName = 'InvoiceID'";
                        cmd1.Connection = conn;
                        using (SqlDataReader sdr2 = cmd1.ExecuteReader())
                        {
                            if (sdr2.Read())
                            {
                                IndexValue = sdr2["IndexValue"] != DBNull.Value ? Convert.ToInt32(sdr2["IndexValue"].ToString()) : 0;
                            }
                            sdr2.Close();
                        }

                        IndexValue++;
                        SqlCommand cmd2 = new SqlCommand("Update IndexCounter Set IndexValue = " + (IndexValue) + " WHERE  IndexName = 'InvoiceID'", conn);
                        cmd2.ExecuteNonQuery();

                        InvoiceNo = "FB" + IndexValue;

                        SqlCommand cmd3 = new SqlCommand("Insert into Invoices (InvoiceId, WorkorderId) Values('" + InvoiceNo + "' , " + Eventid + ")", conn);
                        cmd3.ExecuteNonQuery();

                    }

                    //InvNo.Text = InvoiceNo;

                    conn.Close();
                }
            }

            return InvoiceNo;
        }
        catch (Exception ex)
        {
            return "";
        }
    }


    private void DeleteInvoice(string EventId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection())
            {
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["ConnectionStringDemo"].ConnectionString;
                using (SqlCommand cmd1 = new SqlCommand())
                {
                    cmd1.CommandText = "Delete from Invoices where workorderid = @EventId";
                    cmd1.Parameters.AddWithValue("@EventId", EventId);
                    cmd1.Connection = conn;
                    conn.Open();
                    cmd1.ExecuteNonQuery();
                }
                conn.Close();
            }
        }
        catch (Exception ex)
        {

        }
    }
}


class eqpModel
{
    public string Assetid {get;set;}
    public string WorkOrderType {get;set;}
    public string Temperature { get; set; }
    public string WorkPerformedCounter { get; set; }
    public string WorkDescription { get; set; }
    public string Category { get; set; }
    public string Manufacturer { get; set; }
    public string Model { get; set; }
    public string Location { get; set; }
    public string SerialNumber { get; set; }
    public string QualityIssue { get; set; }
    public string Email { get; set; }
    public string SymptomDesc { get; set; }
    public string SolutionDesc { get; set; }

    public IList<WOParts> Parts;
}

class WOParts
{
    public int? PartsIssueid { get; set; }
    public int? Quantity { get; set; }
    public string Manufacturer { get; set; }
    public string Sku { get; set; }
    public string Description { get; set; }
    public bool? Issue { get; set; }
    public decimal skuCost { get; set; }
    public decimal partsTotal { get; set; }
}