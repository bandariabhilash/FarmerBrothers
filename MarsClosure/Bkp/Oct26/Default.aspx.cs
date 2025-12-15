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
using System.Web.Mail;
public partial class _Default : System.Web.UI.Page
{
    SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["ConnectionString"].ToString());
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            String SCF = "", TechID = "", TECHType="";
            int EventErr = 0;
            Ttype.Visible = true;
            errmsg.Text = "";
            maintbl.Visible = true;
            callclo.Visible = false;
            succmsg.Visible = false;
            Div1.Visible = false;
            NSR.Visible = false;
            if (Request.QueryString["SCF"] != null && Request.QueryString["TechID"] != null)
            {
                GetMachins();
                SCF = Request.QueryString["SCF"].ToString();
                TechID = Request.QueryString["TechID"].ToString();

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
                using (SqlCommand cmd1 = new SqlCommand("Select * from TECH_HIERARCHY where Dealerid =" + TechID, con))
                {
                    SqlDataReader dr1 = cmd1.ExecuteReader();
                    if (dr1.Read())
                    {
                        if(dr1["FamilyAff"].ToString() == "SPT")
                        {
                            Ttype.Visible = false;
                        }
                    }
                    dr1.Close();
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
                SqlCommand cmd3 = new SqlCommand("Select * from [WorkorderType] where calltypeID IN (1100,1200,1300,1400,1700) order by CalltypeID", con);
                SqlDataReader sdr3 = cmd3.ExecuteReader();
                CallType.DataSource = sdr3;
                CallType.DataTextField = "Description";
                CallType.DataValueField = "calltypeID";
                CallType.DataBind();
                CallType.Items.Insert(0, new ListItem("", "0"));
                sdr3.Close();

                SqlCommand cmd4 = new SqlCommand("Select * from Solution where solutionID IN (4100,4200,5191)", con);
                SqlDataReader sdr4 = cmd4.ExecuteReader();
                CompletionCode.DataSource = sdr4;
                CompletionCode.DataTextField = "Description";
                CompletionCode.DataValueField = "solutionId";
                CompletionCode.DataBind();
                CompletionCode.Items.Insert(0, new ListItem("", "0"));
                sdr4.Close();

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
            conn.ConnectionString = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
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
            conn.ConnectionString = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
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
        String serial1 = serial.Text.Replace("'", "''").ToString();
        String EventNotes1 = EventNotes.Text.Replace("'", "''").ToString();
        String rcount = trcount.Value;
        SqlTransaction tran = null;
        try
        {
            con.Open();
            tran = con.BeginTransaction();
            String sNewPass = Eventid.Substring(0, 3) + sGenPwd(2) + Eventid.Substring(Eventid.Length - 6);
            SqlCommand cmd = new SqlCommand("INSERT INTO TMP_BlackBerry_SCFAssetInfo(WorkorderID, TechID, ServiceCode, CompletionCode, EquipTypeCode, VendorCode, Model, SerialNo, Notes, CloseCall, ClosureConfirmationNo) values ('" + Eventid + "', '" + Techid + "', '" + CallType1 + "', '" + CompletionCode1 + "', '" + EquipType1 + "', '" + Vendor1 + "', '" + model1 + "', '" + serial1 + "', '" + EventNotes1 + "',1, '" + sNewPass + "')", con);
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
                            string EN = Request.Form["EN" + i].ToString();
                            string IN = Request.Form["IN" + i].ToString();
                            string VN = Request.Form["VN" + i].ToString();
                            string Des = Request.Form["Des" + i].ToString();
                            string Qty = Request.Form["Qty" + i].ToString();
                            string Sku = Request.Form["Sku" + i].ToString();
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
            ScriptManager.RegisterClientScriptBlock(this, this.GetType(), "alertMessage", "alert('Machine and Parts Inserted Successfully.')", true);
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
            str1.Append("</table>");
            dr7.Close();
            Div1.InnerHtml = str1.ToString();
            con.Close();
        }
    }
    protected void Button2_Click(object sender, EventArgs e)
    {
        //string imgurl = ImgInformation.Value;
        String Eventid = Request.QueryString["SCF"].ToString();
        String Techid = Request.QueryString["TechID"].ToString();
        DateTime Sdate = Convert.ToDateTime(SDate.Text.ToString());
        DateTime Adate = Convert.ToDateTime(ADate.Text.ToString());
        DateTime Cdate = Convert.ToDateTime(CDate.Text.ToString());
        String SignedBy1 = SignedBy.Text;
        con.Open();
        //SqlCommand cmd2 = new SqlCommand("INSERT INTO TMP_BlackBerry_SCFInvoiceInfo(WorkorderID, TechID, StartDateTime, ArrivalDateTime, CompletionDateTime,SignedBy,Signature) values('" + Eventid + "', '" + Techid + "', '" + Sdate.ToString("yyyy-MM-dd HH:mm:ss") + "', '" + Adate.ToString("yyyy-MM-dd HH:mm:ss") + "', '" + Cdate.ToString("yyyy-MM-dd HH:mm:ss") + "','" + SignedBy1 + "', '" + imgurl + "')", con);
        SqlCommand cmd2 = new SqlCommand("INSERT INTO TMP_BlackBerry_SCFInvoiceInfo(WorkorderID, TechID, StartDateTime, ArrivalDateTime, CompletionDateTime,SignedBy) values('" + Eventid + "', '" + Techid + "', '" + Sdate.ToString("yyyy-MM-dd HH:mm:ss") + "', '" + Adate.ToString("yyyy-MM-dd HH:mm:ss") + "', '" + Cdate.ToString("yyyy-MM-dd HH:mm:ss") + "','" + SignedBy1 + "')", con);
        cmd2.ExecuteNonQuery();
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

    }
    protected void ClearData()
    {
        CallType.SelectedIndex = -1;
        CompletionCode.SelectedIndex = -1;
        EquipType.SelectedIndex = -1;
        Vendor.SelectedIndex = -1;
        model.Text = "";
        serial.Text = "";
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
        String WorkorderID = Request.QueryString["SCF"].ToString();
        String Techid = Request.QueryString["TechID"].ToString();
        //String Reasions1 = Reasions.Text.Replace("'", "''").ToString();
        String msrmsg1 = Reasions.SelectedItem.Text.Replace("'", "''").ToString()  +" - "+ nsrmsg.Text.Replace("'", "''").ToString();
        con.Open();
        //sGenPwd(5);
        String sNewPass = WorkorderID.Substring(0, 3) + sGenPwd(2) + WorkorderID.Substring(WorkorderID.Length - 6);
        SqlCommand cmd4 = new SqlCommand("USP_MAFBC_CloseNSR_ViaBlackBerry", con);
        cmd4.CommandType = CommandType.StoredProcedure;
        cmd4.Parameters.Add(new SqlParameter("@WorkorderID", WorkorderID));
        cmd4.Parameters.Add(new SqlParameter("@ClosureConf", sNewPass));
        cmd4.Parameters.Add(new SqlParameter("@Notes", msrmsg1));
        cmd4.ExecuteNonQuery();
        //SqlCommand cmd2 = new SqlCommand("INSERT INTO TMP_BlackBerry_NSR(WorkorderID, TechID, Reasion, Comments) values('" + WorkorderID + "', '" + Techid + "', '" + Reasions1 + "', '" + msrmsg1 + "')", con);
        //cmd2.ExecuteNonQuery();
        ClearData();
        //ScriptManager.RegisterClientScriptBlock(this, this.GetType(), "alertMessage", "alert('Data Inserted Successfully.')", true);
        fb1.Text = WorkorderID;
        msg1.Text = "<font color='green' style='font-size:20px;'><b>Data Inserted Successfully.</b></font>";
        con.Close();
        maintbl.Visible = false;
        callclo.Visible = true;
        succmsg.Visible = true;
        NSR.Visible = false;
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
}