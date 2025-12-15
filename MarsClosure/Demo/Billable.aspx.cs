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

public partial class Billable : System.Web.UI.Page
{
    SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["ConnectionStringDemo"].ToString());
    protected void Page_Load(object sender, EventArgs e)
    {

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
    public static string GetItems(string SID)
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
    protected void Button1_Click(object sender, EventArgs e)
    {
        String rcount = trcount.Value;
        int SCF = int.Parse(Request.QueryString["SCF"].ToString());
        int chk;
        if (BillableCheckBox.Checked) { chk = 1; }else { chk = 0;  }
        Double stot = 0.00, sum;
        String stotal = "";
        SqlTransaction tran = null;
        try
        {
            con.Open();
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
                        sum = Convert.ToDouble(Request.Form["Price" + i].Replace("'", "''").ToString());
                        stot = stot + sum;
                        SqlCommand cmd1 = new SqlCommand("INSERT INTO FbWorkOrderSKU(WorkorderID,SKU,Qty) values (" + SCF + ", '" + Sku + "'," + Qty + ")", con);
                        cmd1.Transaction = tran;
                        cmd1.ExecuteNonQuery();

                    }
                }
                stotal = Math.Round(Convert.ToDecimal(stot), 2).ToString("0.00");
            }
            SqlCommand cmd = new SqlCommand("update WorkOrder set IsBillable="+ chk + ",TotalUnitPrice='"+ stotal + "'  where WorkorderID=" + SCF, con);
            cmd.Transaction = tran;
            cmd.ExecuteNonQuery();
            tran.Commit();
            //errmsg.Text = "<font color='green'><b>Data Inserted Successfully.</b></font>";
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
            ScriptManager.RegisterClientScriptBlock(this, this.GetType(), "alertMessage", "alert('SKUs Inserted Successfully.')", true);
        }

    }

    public void linkClose_Click(object sender, EventArgs e)
    {
        String SCF = Request.QueryString["SCF"].ToString();
        String TECHID = Request.QueryString["TECHID"].ToString();
        Response.Redirect("Default.aspx?Status=CLOSED&TECHID=" + TECHID + "&SCF=" + SCF);
    }
}