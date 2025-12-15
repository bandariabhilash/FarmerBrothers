<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Default.aspx.cs" Inherits="_Default" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
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
       table {font-family: Arial, Helvetica, sans-serif; font-size: 15px; }
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
    .clear{
        background-color: #c70a0a;
    border: 0;
    padding: 3px 10px;
    color: #fff;
    cursor: pointer;
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
<link rel="stylesheet" href="https://code.jquery.com/ui/1.12.1/themes/flick/jquery-ui.css">
    <link rel="stylesheet" href="css/jquery.datetimepicker.css" />
		<%--<script type="text/javascript" src="js/jquery-calendar.js"></script>
    <link rel="stylesheet" type="text/css" href="css/jquery-calendar.css" />--%>
    <script src="js/jquery-1.10.0.min.js" type="text/javascript"></script>
  <link rel="stylesheet" href="https://maxcdn.bootstrapcdn.com/bootstrap/4.4.1/css/bootstrap.min.css">
  <script src="https://cdnjs.cloudflare.com/ajax/libs/popper.js/1.16.0/umd/popper.min.js"></script>
  <script src="https://maxcdn.bootstrapcdn.com/bootstrap/4.4.1/js/bootstrap.min.js"></script>
   <%--<script src="js/bootstrap.js" type="text/javascript"></script>--%>
<script src="js/jquery-ui.min.js" type="text/javascript"></script>
                <%--<script src="js/signature_pad.js"></script>
                <script src="js/app.js"></script>--%>
    <%--<script src="js/jquery.datetimepicker.full.js" type="text/javascript"></script>--%>
    
    <script type="text/javascript" src="js/jquery-ui-timepicker-addon.js"></script>
    <script type="text/javascript" src="js/jquery-ui-sliderAccess.js"></script>
    <link rel="stylesheet" href="css/jquery-ui-timepicker-addon.css" />
        <script>
            $(document).ready(function () {
                'use strict';               
                Date.prototype.addHours = function (h) {
                    this.setHours(this.getHours() + h);
                    return this;
                }
                //$('#ADate,#CDate').datetimepicker();
                var today = new Date();
                var WOEntryDat = new Date($('#WOrdEntryDate').val());
                var today = new Date();
                $('#SDate').datetimepicker({
                    minDateTime: WOEntryDat,
                    maxDateTime: null,
                    controlType: 'select',
                    timeFormat: "hh:mm TT"
                }).attr('readonly', 'readonly');
                //var ex13 = $('#ADate');
                
                $('body').on('focus',"#ADate", function(){ 
                            if ($('#SDate').val() == "") {
                                alert("Please Select Start Date/Time");
                                $("#SDate").focus();
                                return false;
                            }
                    if (false == $(this).hasClass('hasDatepicker')) {
                                var SDat = new Date($('#SDate').val());
                                var Addhr = new Date($('#SDate').val());
                                Addhr = Addhr.addHours(4);
                                Addhr = new Date(Addhr);
                        $(this).datetimepicker({
                            minDateTime: SDat,
                            maxDateTime: Addhr,
                            //addSliderAccess: true,
                            //sliderAccessArgs: { touchonly: false },
                            controlType: 'select',
                            timeFormat: "hh:mm TT"
                        }).attr('readonly', 'readonly');
                    }
                });
                $('body').on('focus', "#CDate", function () {
                    if ($('#ADate').val() == "") {
                        alert("Please Select Arrival Date/Time");
                        $("#CDate").datetimepicker('Hide');
                        return false;
                    }
                    if (false == $(this).hasClass('hasDatepicker')) {
                        var ADat = new Date($('#ADate').val());
                        var AddChr = new Date($('#ADate').val());
                        AddChr = AddChr.addHours(12);
                        AddChr = new Date(AddChr);
                        $(this).datetimepicker({
                            minDateTime: ADat,
                            maxDateTime: AddChr,
                            controlType: 'select',
                            timeFormat: "hh:mm TT"
                        }).attr('readonly', 'readonly');
                    }
                });
            });
        </script>

    <script type="text/javascript">
    $(function () {
        $("[id$=txtSearch]").autocomplete({
            source: function (request, response) {
                $.ajax({
                    url: '<%=ResolveUrl("~/Default.aspx/GetParts") %>',
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
                url: '<%=ResolveUrl("~/Default.aspx/GetItems") %>',
                data: "{ 'SID': '" + sid + "'}",
                dataType: "json",
                type: "POST",
                contentType: "application/json; charset=utf-8",
                success: function (data) {
                    var str1 = data.d;
                    var res = str1.split("^");
                    // alert(res[1]);
                    if (str1 != "") {
                        $("#OParts tbody").append('<tr><td><input type="text" name="EN' + c + '" value="' + res[0] + '" style="width: 75px;" readonly/></td><td><input type="text" name="IN' + c + '" value="' + res[1] + '" style="width: 75px;" readonly/></td><td><input type="text" name="VN' + c + '" value="' + res[2] + '" style="width: 75px;" readonly/></td><td><input type="text" name="Des' + c + '" value="' + res[3] + '" style="width: 175px;" readonly/></td><td><input type="hidden" name="Sku' + c + '" value="' + res[4] + '" readonly/><input type="text" name="Qty' + c + '" value="1" style="width: 20px;"/></td><td><a href="javascript:void(0);" class="remCF">Del</a></td></tr>');
                        c++;
                        //var rowCount = $('#OParts tbody tr').length;
                        $("#trcount").val(c);
                        $("#txtSearch").val("");
                    } else {
                        alert("No Record Found.");
                        $("#txtSearch").val("");
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
                alert('Please Select Service Code');
                return false;
            }
            if ($('#CompletionCode').val() == '0') {
                alert('Please Select Completion Code');
                return false;
            }
            if ($('#EquipType').val() == '0') {
                alert('Please Select Equip Type');
                return false;
            }
            if ($('#Vendor').val() == '0') {
                alert('Please Select Manufacturer');
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
            //var signaturePad = new SignaturePad(canvas);

            //var dataURL = signaturePad.toDataURL();
           // alert(dataURL);
            //if (signaturePad.isEmpty()) {
            //    alert("Please provide a signature first.");
            //    return false;
            //} else {
            //    var dataURL = signaturePad.toDataURL();
            //    alert(dataURL);
            //    $("#ImgInformation").val(signaturePad.toDataURL());
            //}
        });

        $('#Button2').click(function () {
                if ($('#BillableCheckBox').is(":checked")) {
                    var trow = $("#SKURows > tbody > tr").length;
                    if (trow <= '0') {
                        alert("Please Add Minimum one SKU");
                        return false;
                    }
                }

            if ($('#CallType').val() != '0' || $('#CompletionCode').val() != '0' || $('#EquipType').val() != '0' || $('#Vendor').val() != '0' || $('#model').val() != '') {
                alert('Please Submit Machine Details First.');
                return false;
            }
            if ($('#InvNo').val() == '') {
                alert('Please Enter Invoice Number');
                return false;
            }
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
            ////            Added By hareesh on 2 Mar 2018  //
            //var SDate = new Date($('#SDate').val().replace("AM", " AM").replace("PM", " PM"));
            //var ADate = new Date($('#ADate').val().replace("AM", " AM").replace("PM", " PM"));
            //var CDate = new Date($('#CDate').val().replace("AM", " AM").replace("PM", " PM"));

            //var todayDate = new Date();
            //if (SDate > todayDate) {
            //    alert('Please Enter Start Date/Time less than today');
            //    return false;
            //}

            //if (ADate > todayDate) {
            //    alert('Please Enter Arrival Date/Time less than today');
            //    return false;
            //}

            //if (CDate > todayDate) {
            //    alert('Please Enter Completion Date/Time less than today');
            //    return false;
            //}
            ////            Added By hareesh on 2 Mar 2018  //
        });
        //$("#SDate,#ADate,#CDate").calendar().attr('readonly', 'readonly');

        
        
        $('#Button3').click(function () {
            if ($('#Reasions').val() == '0') {
                alert('Please Select Reasion');
                return false;
            }
        });
    });  
</script>
<script type="text/javascript">
    $(function () {
        //$(".billablechk").hide();
        if ($('#BillableCheckBox').is(":checked")) {
            $(".billablechk").show();
        } else {
            $(".billablechk").hide();
        }
            $("[id$=SkuSearch]").autocomplete({
                source: function (request, response) {
                    $.ajax({
                        url: '<%=ResolveUrl("~/Default.aspx/GetSKUs") %>',
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
            var j = 1;
            $('#skusearchbtn').click(function () {
                var sid = $("#SkuSearch").val();
            //alert(sid);
            $.ajax({
                url: '<%=ResolveUrl("~/Default.aspx/GetItemsList") %>',
                data: "{ 'SID': '" + sid + "'}",
                dataType: "json",
                type: "POST",
                contentType: "application/json; charset=utf-8",
                success: function (data) {
                    var str2 = data.d;
                    var res = str2.split("^");
                    //alert(res[1]);
                    if (str2 != "") {
                        var pri = res[2]*1;
                        //alert(pri.toFixed(2));
                        $("#SKURows tbody").append('<tr><td><input type="text" name="Sku' + j + '" value="' + res[0] + '" style="width: 75px;border: 0;    background-color: inherit;" readonly/></td><td><input type="text" name="SKUDes' + j + '" value="' + res[1] + '" style="width: 175px;border: 0; background-color: inherit;" readonly/></td><td><input type="text" id="UnitPrice' + j + '" name="UnitPrice' + j + '" value="$' + pri + '" style="width:76px;border: 0;background-color: inherit;text-align:right;" readonly/></td><td><input type="text" class="qty" id="Qty' + j + '" name="Qty' + j + '" value="1" style="width: 45px;text-align: center;"/></td><td><input type="text" id="Price' + j + '" name="Price' + j + '" value="$' + pri + '" style="width: 75px;border: 0;background-color: inherit;text-align:right;" readonly/></td><td><a href="javascript:void(0);" class="remCF">Del</a></td></tr>');
                        j++;
                        //var rowCount = $('#OParts tbody tr').length;
                        $("#SKUtrcount").val(j);
                        $("#SkuSearch").val("");
                    } else {
                        alert("No Record Found.");
                        $("#SkuSearch").val("");
                    }
                }
            });
        });
            $("#SKURows tbody").on('click', '.remCF', function () {
                $(this).parent().parent().remove();
                //var rowCount = $('#OParts tbody tr').length;
                //$("#trcount").val(rowCount);
            });


            $("#SKURows tbody").on('keypress keyup blur', '.qty', function (e) {
            //alert("hi");
            $(this).val($(this).val().replace(/[^\d].+/, ""));
            var val = $(this).val();
            //alert(val);
            if ((e.which < 48 || e.which > 57)) {
                e.preventDefault();
            } else {
                var tid = $(this).attr("id");
                //alert(tid);
                tid = tid.replace("Qty", "");
                var UPrice = $("#UnitPrice" + tid).val().replace("$", "");
                var total = UPrice * val
                //alert(total);
                $("#Price" + tid).val("$"+total.toFixed(2));
            }
        });
            

        //    $('#Button4').click(function () {
        //    var trow = $("#SKURows > tbody > tr").length;
        //    if (trow <= '0') {
        //        alert("Please Add Minimum one SKU");
        //        return false;
        //    }
        //});
            $('#BillableCheckBox').change(function () {
                if ($(this).is(":checked")) {
                    //alert("hi");
                    $(".billablechk").show();
                } else {
                    $(".billablechk").hide();
                }
            });
        });

        </script>
     <style>
                #Button1 {
            margin-top: 11px;
            background-color: #0008f7cc;
            color: #ffffff;
            border: 0px;
            padding: 5px 8px;
        }
        #search {
                margin-top: 17px;
    cursor: pointer;
    background-color: #001823;
    color: #fff;
    border: 1px solid #3533ad;
    padding: 3px 5px;
        }
 #SKURows {
    font-family: "Trebuchet MS", Arial, Helvetica, sans-serif;
    border-collapse: collapse;
}
 #SKURows thead tr {
 background-color: #0ba8cc;
    color: #fff;
}
 #SKURows td {
    border: 1px solid #ddd;
}
#customers thead tr td {
    border: 1px solid #ddd;
    padding: 8px;
}

#SKURows tr:nth-child(even){background-color: #f2f2f2;}

#SKURows  thead tr td {
        padding: 8px 8px;
    text-align: left;
    background-color: #0ba8cc;
    color: white;
}
/*.txtglow {
    color: #f7f7f7 !important;
    font-size: 16px !important;
    background-color: #000;
    padding: 2px 4px;
    }*/
    </style>
</head>
<body>
    <form id="form1" runat="server">    
        <%--<ej:Signature ID="Signature1" Height="400px" StrokeWidth="3" IsResponsive="true" runat="server"></ej:Signature>--%>
        <div  runat="server" id="maintbl">    
        <table border="0">
<tr>
    <td colspan="2"><span class="style15">FB# <asp:Label runat="server" ID="fb" /></span></td>
  </tr>
  <div runat="server" id="Ttype">
  <tr>
  	<td colspan="2">
	<table width="100%">
		<tr>
			<td width="100%" bgcolor="#400000" align="center" height="30">
                <asp:LinkButton ID="linkGoSomewhere" runat="server" Class="txtglow" onClick="linkGoSomewhere_Click" Text="No Service Required" />
			</td>
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
        <span class="style11">Serial # <span class="required">*</span></span><br />
        <asp:DropDownList ID="serialNumDD" runat="server" Class="select" style="font-size:14px;" AutoPostBack="True"  OnSelectedIndexChanged="serialNumDD_SelectedIndexChanged" >
	    </asp:DropDownList>
        <asp:TextBox ID="serial" runat="server" style=" font-size:14px"></asp:TextBox>
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
            <span class="style11">Weight </span><br />  
            <asp:TextBox ID="WeightTxt" runat="server" style=" font-size:14px" ></asp:TextBox>
        </td>
   </tr>
   <tr>
        <td colspan="2">
            <span class="style11">Ratio </span><br />  
            <asp:TextBox ID="RatioTxt" runat="server" style=" font-size:14px" ></asp:TextBox>
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
                   <%-- <button class="button clear">Clear</button>--%>
               <asp:Button ID="Button1" Text="Add Machine & Parts" runat="server" Class="btn" OnClick="Button1_Click"/>
                </td>
            </tr>
            <tr>
                <td>
                    <div id="Div1" runat="server"></div>
                </td>
            </tr>
            <tr>
                <td>
                    <div id="callclo" runat="server">
                    <table style="margin-top: 15px;">
                        <tr>
                            <td>
                                <asp:CheckBox ID="BillableCheckBox" runat="server" Text="Is Billable" style="font-weight:bold;"/></td>
                        </tr>
                    </table>
                    <div class="billablechk">
                    <table style="margin-top: 7px;">
                           <tr>
                               <td>Sku Search:<br /><asp:TextBox ID="SkuSearch" runat="server" /></td>
                              <td><input type="button" value="Search/Add" id="skusearchbtn" style="margin-top: 17px; cursor:pointer;" />
                                   <asp:HiddenField ID="SKUtrcount" runat="server" Value="0" /></td>
                           </tr>
                    </table>
                    
                    <table id="SKURows" border="1">
                        <thead>
                            <tr><td>SKU</td><td>Description</td><td>Unit Price</td><td>Qty</td><td>Price</td><td>Remove</td></tr>
                        </thead>
                        <tbody></tbody>
                    </table>
                    </div>
                    <table style="margin-bottom:60px;">
                        <tr>
                            <td>Invoice Number:<span class="required">*</span><br />
                                <asp:TextBox ID="InvNo" runat="server" style=" font-size:14px"></asp:TextBox>
                                <asp:Button ID="InvoiceGenerateBtn" Text="AutoGen Invoice" OnClick="InvoiceGenerateBtn_Click" runat="server"></asp:Button>

                                <asp:HiddenField ID="WOrdEntryDate" runat="server" />
                            </td>
                        </tr>  
                        <tr>
                            <td>Signed By:<span class="required">*</span><br />
                                <asp:TextBox ID="SignedBy" runat="server" style=" font-size:14px"></asp:TextBox>
                            </td>
                        </tr>                        
                         <%--<tr>
                              <td>Signature:<br />
                                <div id="signature-pad" class="m-signature-pad" style="width: 300px; height: 162px; position:relative;">
                                    <div class="m-signature-pad--body" style="height: 150px;">
                                        <canvas style="margin-top: -25px; background-color: #e6e4e4;"></canvas>
                                    </div>
                                    <div class="m-signature-pad--footer" style="border: 0;    position: absolute;    top: 9px;    left: -53px;">
                                        <button class="button clear" data-action="clear">Clear</button>
                                    </div>
                                    <asp:HiddenField ID="ImgInformation" runat="server" />
                                </div>
                             </td>
                          </tr>--%>
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
                                <asp:CheckBox ID="WaterTestedChk" runat="server" Text="Water Tested"/>
                           </td>
                        <%--</tr>

                        <tr>--%>
                            <td>
                                <asp:CheckBox ID="FilterReplacedChk" runat="server" Text="Filter Replaced"/>
                            </td>
                        </tr>

                        <tr>
                            <td colspan="2">
                                <span class="style11">Hardness Rating</span><br />        
                                <asp:DropDownList ID="HardnessRatingDropDown" runat="server" Class="select" style="font-size:14px;" >
                                    <asp:ListItem Text=" " Value=" " />
                                    <asp:ListItem Text="1" Value="1" />
                                    <asp:ListItem Text="2" Value="2" />
                                    <asp:ListItem Text="3" Value="3" />
                                    <asp:ListItem Text="4" Value="4" />
                                    <asp:ListItem Text="5" Value="5" />
                                     <asp:ListItem Text="6" Value="6" />
                                    <asp:ListItem Text="7" Value="7" />
                                    <asp:ListItem Text="8" Value="8" />
                                    <asp:ListItem Text="9" Value="9" />
                                    <asp:ListItem Text="Over 10" Value="Over 10" />
                                </asp:DropDownList>
                            </td>
                        </tr>



                        <tr>
                          <td>
                                <asp:Button ID="Button2" Text="CLOSE CALL" runat="server" Class="btn" OnClick="Button2_Click" data-action="save-png" style="background-color:green;"/>
                          </td>
                        </tr>
                    </table>
                        </div>
                </td>
                <td></td>
            </tr>
      </table>
            </div>
        <div  runat="server" id="succmsg"> 
        <table>
            <tr>
                <td colspan="2"><span class="style15">FB# <asp:Label runat="server" ID="fb1" /></span></td>
              </tr>
              <tr>
  	            <td colspan="2"><font color="#FF0000"><b><asp:Label runat="server" ID="msg1" ></asp:Label></b></font></td>
              </tr>
        </table>
            </div>
        <div runat="server" id="NSR">
            <table>
                <tr>
                    <td colspan="2"><span class="style15">FB# <asp:Label runat="server" ID="fb2" /></span></td>
                  </tr>
                <tr>
                    <td><h3 style="font-size:15px; color:#b33838;margin: 0px;">No Service Required</h3></td>
                </tr>
                <tr>
                    <td>                        
                        <span class="style11">Reasions<span class="required">*</span></span><br />
                        <asp:DropDownList ID="Reasions" runat="server" Class="select" style="font-size:14px;" >
                      </asp:DropDownList>
                    </td>
                </tr>
                <tr>
                    <td>
                        <span class="style11">Comments</span><br />
                        <asp:TextBox ID="nsrmsg" runat="server" TextMode="MultiLine" style="font-size:14px; width: 258px; height: 107px;"></asp:TextBox>
                    </td>
                </tr>
                        <tr>
                          <td>
                                <asp:Button ID="Button3" Text="SUBMIT" runat="server" Class="btn" OnClick="Button3_Click" style="background-color:orange;"/>
                          </td>
                    </tr>
            </table>
        </div>
    </form>
</body>
    
</html>
