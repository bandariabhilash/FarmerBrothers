<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="CallClosure.aspx.cs" Inherits="FBClosure.CallClosure" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Farmer Brothers Call Closure Application</title>
    <style type="text/css">
        .required {
            color:red;
        }
.select{
    width:200px;
    padding:2px 4px;
}
input[type=text]{
    width:190px;
    padding:2px 4px;
}
       .ui-autocomplete {
            max-height: 200px;
            overflow-y: auto;
            /* prevent horizontal scrollbar */
            overflow-x: hidden;
            /* add padding to account for vertical scrollbar */
            padding-right: 20px;
        }
.style11 {font-family: Arial, Helvetica, sans-serif; font-size: 15px; }
.style12 {font-size: 12px}
.txtglow {font-family: Arial, Helvetica, sans-serif; font-size: 25px;font-weight: bold; color:#FFFF00 }
.labelTxt {font-family: Arial, Helvetica, sans-serif; font-size: 14px;font-weight: bold; }
.ErrTxt {font-family: Arial, Helvetica, sans-serif; font-size: 10;font-weight: bold; color:#FF0000 }
.style15 {font-family: Arial, Helvetica, sans-serif; font-weight: bold; font-size:35px }
.top-bar1 a.button {
	float:left;
	display:block;
	height:15px;
	text-align:center;
	color:#fff;
	text-transform:uppercase;
	font-weight:bold;
	padding:10px;
	background: #400000;
	background: -webkit-gradient(
			linear,
			left top,
			left bottom,
			color-stop(0.2, rgb(255,154,76)),
			color-stop(0.8, rgb(240,96,0))
		);
	background:	-moz-linear-gradient(
			center top,
			rgb(255,154,76) 20%,
			rgb(240,96,0) 80%
		);
	border-radius: 7px;
	-moz-border-radius: 7px;
	-webkit-border-radius: 7px;
	}
    #OParts {
    width: 426px;
    }
    #OParts thead {
        background-color:#000000;
        color:#fff;
        font-weight:bold;  
    }
    .btn{
        padding: 5px;
    background-color: #071b63;
    color: #fff;
    border: 0px;
    margin-top: 10px;
    cursor:pointer;
}
</style>
<script language="javascript">

function CheckForm(frmship)
{
		//updated on Mar 10th by hareesh
		if (frmship.CallType.value==""){
		alert("Please Select Service Code");
		frmship.CallType.focus();
		return false;
		}
		
		if (frmship.model.value==""){
		alert("Please enter Model Number");
		frmship.model.select;
		frmship.model.focus();
		return false;
		}
				return true;
}
</script>

    <script src="js/jquery-1.10.0.min.js" type="text/javascript"></script>
<script src="js/jquery-ui.min.js" type="text/javascript"></script>
<link href="css/jquery-ui.css" rel="Stylesheet" type="text/css" />
		<script type="text/javascript" src="js/jquery-calendar.js"></script>
    <link rel="stylesheet" type="text/css" href="css/jquery-calendar.css" />
    <script type="text/javascript">
    $(function () {
        $("[id$=txtSearch]").autocomplete({
            source: function (request, response) {
                $.ajax({
                    url: '<%=ResolveUrl("~/CallClosure.aspx/GetParts") %>',
                    data: "{ 'prefix': '" + request.term + "'}",
                    dataType: "json",
                    type: "POST",
                    contentType: "application/json; charset=utf-8",
                    success: function (data) {
                        response($.map(data.d, function (item) {
                            return {
                                label: item.split('-')[0],
                                val: item.split('-')[1]
                            }
                        }))
                    },
                    error: function (response) {
                        alert(response.responseText);
                    },
                    failure: function (response) {
                        alert(response.responseText);
                    }
                });
            },
            select: function (e, i) {
                //alert(i.item.val);
                //$("[id$=hfPartsId]").val(i.item.val);
            },
            minLength: 1
        });
        var c =1;
        $('#search').click(function () {
            var sid = $("#txtSearch").val();
            //alert(sid);
            $.ajax({
                url: '<%=ResolveUrl("~/CallClosure.aspx/GetItems") %>',
                data: "{ 'SID': '" + sid + "'}",
                dataType: "json",
                type: "POST",
                contentType: "application/json; charset=utf-8",
                success: function (data) {
                    var str1 = data.d;
                    var res = str1.split("^");
                    // alert(res[1]);
                    if (str1 != "") {
                        $("#OParts tbody").append('<tr><td><input type="text" name="EN'+c+'" value="' + res[0] + '" style="width: 75px;" readonly/></td><td><input type="text" name="IN'+c+'" value="' + res[1] + '" style="width: 75px;" readonly/></td><td><input type="text" name="VN'+c+'" value="' + res[2] + '" style="width: 75px;" readonly/></td><td><input type="text" name="Des'+c+'" value="' + res[3] + '" style="width: 175px;" readonly/></td><td><input type="text" name="Qty'+c+'" value="1" style="width: 20px;"/></td><td><a href="javascript:void(0);" class="remCF">Del</a></td></tr>');
                        c++;
                        //var rowCount = $('#OParts tbody tr').length;
                        $("#trcount").val(c);
                    } else {
                        alert("No Record Found.");
                    }
                }
            });
        });
        $("#OParts tbody").on('click', '.remCF', function () {
            $(this).parent().parent().remove();
            //var rowCount = $('#OParts tbody tr').length;
            //$("#trcount").val(rowCount);
        });
        $('#Button1').click(function () {
            if ($('#CallType').val() == '0') {
                alert('Please Enter Call Type');
                return false;
            }
            if ($('#CompletionCode').val() == '0') {
                alert('Please Enter Completion Code');
                return false;
            }
            if ($('#EquipType').val() == '0') {
                alert('Please Enter Equip Type');
                return false;
            }
            if ($('#Vendor').val() == '0') {
                alert('Please Enter Vendor');
                return false;
            }
            if ($('#model').val() == '') {
                alert('Please Enter Model');
                return false;
            }
            if ($('#serial').val() == '') {
                alert('Please Enter Serial');
                return false;
            }
        });
        
        $('#Button2').click(function () {
            if ($('#SignedBy').val() == '') {
                alert('Please Enter Signed By');
                return false;
            }
            if ($('#SDate').val() == '') {
                alert('Please Enter Start Date/Time');
                return false;
            }
            if ($('#ADate').val() == '') {
                alert('Please Enter Arrival Date/Time');
                return false;
            }
            if ($('#CDate').val() == '') {
                alert('Please Enter Completion Date/Time');
                return false;
            }
        });
        $("#SDate,#ADate,#CDate").calendar();
    });  
</script>
</head>
<body>
    <form id="form1" runat="server">        
        <table border="0">
<tr>
    <td colspan="2"><span class="style15">FB# <asp:Label runat="server" ID="fb" /></span></td>
  </tr>
  <div runat="server" id="Ttype">
  <tr>
  	<td colspan="2">
	<table width="100%">
		<tr>
			<td width="100%" bgcolor="#400000" align="center" height="30"><a href="#" class="txtglow" >No Service Required</a></td>
		</tr>
	</table>
	</td>
  </tr>
  <tr>
  	<td colspan="2" class="labelTxt">No Service Required means customer site was not visited.  <br>If anyone has visited the 
	customer site to attempt service, this tab is not to be used. <BR>Events closed NSR do not count toward the Technician’s Productivity.</td>
  </tr>
  </div>
  <tr>
  	<td colspan="2"><font color="#FF0000"><b><asp:Label runat="server" ID="errmsg" ></asp:Label></b></font></td>
  </tr>
  <tr>
    <td colspan="2">
        <span class="style11">Service Code<span class="required">*</span></span><br />
        <asp:DropDownList ID="CallType" runat="server" Class="select" style="font-size:14px;" >
      </asp:DropDownList>
       </td>
      </tr>
      <tr>
    <td colspan="2">
        <span class="style11"> Completion Code<span class="required">*</span> </span><br />
              <asp:DropDownList ID="CompletionCode" runat="server" Class="select" style="font-size:14px;" >
      </asp:DropDownList>
       </td>
      </tr>
      <tr>
    <td colspan="2">
        <span class="style11">Equipment Type<span class="required">*</span>  </span><br />        
      <asp:DropDownList ID="EquipType" runat="server" Class="select" style="font-size:14px;" >
      </asp:DropDownList>
       </td>
      </tr>
      <tr>
    <td colspan="2">
        <span class="style11">Manufacturer<span class="required">*</span> </span><br />
      <asp:DropDownList ID="Vendor" runat="server" Class="select" style="font-size:14px;" >
      </asp:DropDownList>
       </td>
      </tr>
      <tr>
    <td colspan="2">
        <span class="style11">Model<span class="required">*</span> </span><br />        
        <asp:TextBox ID="model" runat="server" style=" font-size:14px"></asp:TextBox>
    
       </td>
      </tr>
      <tr>
    <td colspan="2">
        <span class="style11">Serial # <span class="required">*</span></span><br />
        <asp:TextBox ID="serial" runat="server" style=" font-size:14px"></asp:TextBox>
    </td>
  </tr>
  <tr>
    <td colspan="2"><span class="style11">Comments: </span><br />        
        <asp:TextBox ID="EventNotes" runat="server" TextMode="MultiLine" style="font-size:14px; width:200px;"></asp:TextBox>
    </td>
  </tr>
   <tr>
     <td colspan="2"><hr /></td></tr>
   <tr>
       <td colspan="2">
           <table>
               <tr><td>Part/Entry/Vendor:<br /><asp:TextBox ID="txtSearch" runat="server" /></td><td></td>
                   <td>                       
                    <%--<asp:HiddenField ID="hfPartsId" runat="server" />
                    <asp:Button ID="Button1" Text="Search/Add" runat="server" OnClick="Submit" />--%>
                       <asp:HiddenField ID="trcount" runat="server" Value="0" />
                       <input type="button" value="Search/Add" id="search" style="margin-top: 17px; cursor:pointer;" />
                   </td>
               </tr>
           </table>

       </td>
   </tr>
            <tr>
                <td>
                    <table id="OParts" border="1">
                        <thead>
                        <tr><td>Entry No</td><td>Item No</td><td>Vendor No</td><td>Description</td><td>Qty</td><td>Remove</td></tr>
                        </thead>
                        <tbody>
                        </tbody>
                    </table>
                </td>
            </tr>
            <tr>
              <td colspan="2">
                  <asp:Button ID="Button1" Text="Add Machin & Parts" runat="server" Class="btn" OnClick="Button1_Click" />
                </td>
            </tr>
            <tr>
                <td>
                    <table style="margin-bottom:60px;">
                        <tr>
                            <td>Signed By:<span class="required">*</span><br />
                                <asp:TextBox ID="SignedBy" runat="server" style=" font-size:14px"></asp:TextBox>
                            </td>
                        </tr>
                        <tr>
                            <td>Signature:<br />
                                <asp:TextBox ID="Signature" runat="server" TextMode="MultiLine" style="width: 224px; height: 99px; background-color: #f1f0f0;"></asp:TextBox>
                            </td>
                        </tr>
                        <tr>
                            <td>Start Date/Time:<span class="required">*</span><br />
                                <asp:TextBox ID="SDate" runat="server" style=" font-size:14px" ></asp:TextBox>
                            </td>
                        </tr>
                        <tr>
                            <td>Arrival Date/Time:<span class="required">*</span><br />
                                <asp:TextBox ID="ADate" runat="server" style=" font-size:14px" ></asp:TextBox>
                            </td>
                        </tr>
                        <tr>
                            <td>Completion Date/Time:<span class="required">*</span><br />
                                <asp:TextBox ID="CDate" runat="server" style=" font-size:14px" ></asp:TextBox>
                            </td>
                        </tr>
                        <tr>
                          <td>
                                <asp:Button ID="Button2" Text="CLOSE CALL" runat="server" Class="btn" OnClick="Button2_Click" style="background-color:green;"/>
                          </td>
                        </tr>
                    </table>
                </td>
                <td></td>
            </tr>
      </table>

    </form>
</body>
</html>
