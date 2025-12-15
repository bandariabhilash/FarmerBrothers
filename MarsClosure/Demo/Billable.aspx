<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Billable.aspx.cs" Inherits="Billable" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    <script src="js/jquery-1.10.0.min.js" type="text/javascript"></script>
<script src="js/jquery-ui.min.js" type="text/javascript"></script>
<link href="css/jquery-ui.css" rel="Stylesheet" type="text/css" />
    <script type="text/javascript">
        $(function () {
            $("[id$=txtSearch]").autocomplete({
                source: function (request, response) {
                    $.ajax({
                        url: '<%=ResolveUrl("~/Billable.aspx/GetSKUs") %>',
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
            var c = 1;
            $('#search').click(function () {
            var sid = $("#txtSearch").val();
            //alert(sid);
            $.ajax({
                url: '<%=ResolveUrl("~/Billable.aspx/GetItems") %>',
                data: "{ 'SID': '" + sid + "'}",
                dataType: "json",
                type: "POST",
                contentType: "application/json; charset=utf-8",
                success: function (data) {
                    var str1 = data.d;
                    var res = str1.split("^");
                    // alert(res[1]);
                    if (str1 != "") {
                        $("#OParts tbody").append('<tr><td><input type="text" name="Sku' + c + '" value="' + res[0] + '" style="width: 75px;border: 0;    background-color: inherit;" readonly/></td><td><input type="text" name="SKUDes' + c + '" value="' + res[1] + '" style="width: 175px;border: 0; background-color: inherit;" readonly/></td><td><input type="text" id="UnitPrice' + c + '" name="UnitPrice' + c + '" value="' + res[2] + '" style="width:76px;border: 0;background-color: inherit;" readonly/></td><td><input type="text" class="qty" id="Qty' + c + '" name="Qty' + c + '" value="1" style="width: 45px;text-align: center;"/></td><td><input type="text" id="Price' + c + '" name="Price' + c + '" value="' + res[2] + '" style="width: 75px;border: 0;background-color: inherit;" readonly/></td><td><a href="javascript:void(0);" class="remCF">Del</a></td></tr>');
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


        $("#OParts tbody").on('keypress keyup blur', '.qty', function (e) {
            //alert("hi");
            $(this).val($(this).val().replace(/[^\d].+/, ""));
            var val = $(this).val();
            if ((e.which < 48 || e.which > 57)) {
                e.preventDefault();
            } else {
                var tid = $(this).attr("id");
                tid = tid.replace("Qty", "");
                var UPrice = $("#UnitPrice" + tid).val();
                var total = UPrice * val
                $("#Price" + tid).val(total.toFixed(2));
            }
        });
            

        $('#Button1').click(function () {
            var trow = $("#OParts > tbody > tr").length;
            if (trow <= '0') {
                alert("Please Add Minimum one SKU");
                return false;
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
 #OParts {
    font-family: "Trebuchet MS", Arial, Helvetica, sans-serif;
    border-collapse: collapse;
}
 #OParts thead tr {
 background-color: #0ba8cc;
    color: #fff;
}
 #OParts td {
    border: 1px solid #ddd;
}
#customers thead tr td {
    border: 1px solid #ddd;
    padding: 8px;
}

#OParts tr:nth-child(even){background-color: #f2f2f2;}

#OParts  thead tr td {
        padding: 8px 8px;
    text-align: left;
    background-color: #0ba8cc;
    color: white;
}
.txtglow {
    color: #f7f7f7 !important;
    font-size: 16px !important;
    background-color: #000;
    padding: 2px 4px;
    }
    </style>
</head>
<body>
    <form id="form1" runat="server">
    <div>
      <asp:LinkButton ID="linkbillable" runat="server" Class="txtglow" style="color:#2d46f9; font-size: 21px;" onClick="linkClose_Click" Text="< Back" />
    <table style="margin-top: 15px;">
        <tr>
            <td>
                <asp:CheckBox ID="BillableCheckBox" runat="server" Text="Is Billable" /></td>
        </tr>
    </table>
        <table style="margin-top: 7px;">
               <tr>
                   <td>Sku Search:<br /><asp:TextBox ID="txtSearch" runat="server" /></td>
                  <td><input type="button" value="Search/Add" id="search" style="margin-top: 17px; cursor:pointer;" />
                       <asp:HiddenField ID="trcount" runat="server" Value="0" /></td>
               </tr>
        </table>
        <table id="OParts" border="1">
            <thead>
                <tr><td>SKU</td><td>Description</td><td>Unit Price</td><td>Qty</td><td>Price</td><td>Remove</td></tr>
            </thead>
            <tbody></tbody>
        </table>

        <asp:Button ID="Button1" Text="Submit" runat="server" Class="btn" OnClick="Button1_Click"/>
    </div>
    </form>
</body>
</html>
