<%
'乘风asp.net探针
'作者QQ：178575
'作者EMail：yliangcf@163.com
'作者网站：http://www.qqcf.com
%>
<%Response.ContentEncoding = System.Text.Encoding.GetEncoding("gb2312")%>
<%@ Import Namespace="System" %>
<%@ Import Namespace="System.IO" %>
<%@ Import Namespace="Microsoft.VisualBasic.CompilerServices" %>

<Script Language="VB" Runat="Server">
Private Sub Page_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
  Dim Sw As StreamWriter
  Dim Bc as HttpBrowserCapabilities
  Dim Flag as Boolean
  Dim DownStr As String


  ServerName.text= Server.MachineName.ToString()
  ServerVer.text=Environment.OSVersion.ToString()

  If IsPrivateIp(Request.ServerVariables("LOCAL_ADDR")) Then
   ServerIp.text = Request.ServerVariables("LOCAL_ADDR") & "&nbsp;&nbsp;[<font color=ff000>内网IP</font>]"
  Else
   ServerIp.text = Request.ServerVariables("LOCAL_ADDR") & "&nbsp;&nbsp;[公网IP]"
  End if

  ServerDomain.text=Request.ServerVariables("SERVER_NAME")
  ServerPort.text = Request.ServerVariables("SERVER_PORT")

  ServerOutTime.text=Server.ScriptTimeout.ToString()
  ServerNow.text=DateTime.Now.ToString()
  ServerSessionTotal.text=Session.Contents.Count.ToString()
  ServerApplicationTotal.text=Application.Contents.Count.ToString()
    
  NetVer.text= System.Environment.Version.ToString()  
  IISVer.text= Request.ServerVariables("SERVER_SOFTWARE")
  
  ProPath.text= Request.ServerVariables("PATH_INFO")
  ProPath_2.text= Request.ServerVariables("APPL_PHYSICAL_PATH")
  
  ServerRunTime.text=Math.round(Environment.TickCount/600/60)/100
  
  
  Bc= Request.Browser

  
  If Request.ServerVariables("HTTP_X_FORWARDED_FOR")="" Then
   Brower_IP.text=Request.ServerVariables("REMOTE_ADDR") & "[真实IP]"
   Server_Cdn.text="否"
  Else
   If  Request.ServerVariables("REMOTE_ADDR")=Request.ServerVariables("HTTP_X_FORWARDED_FOR") Then
    Brower_IP.text=Request.ServerVariables("HTTP_X_FORWARDED_FOR") & "[真实IP]"
	Server_Cdn.text="否"
   Else
    Brower_IP.text = "服务器获取到你的IP是:"&Request.ServerVariables("REMOTE_ADDR")&" <br>你的上网IP是:"&Request.ServerVariables("HTTP_X_FORWARDED_FOR")&"<br><font color=ff000>两者不一样，可能服务器开启了CDN或云主机</font>"
	Server_Cdn.text="<font color=ff000>是</font>"
   End If
  End If

  Brower_UserAgent.text=HttpContext.Current.Request.ServerVariables("HTTP_USER_AGENT")
  Brower_OSVer.text=Bc.Platform.ToString()
  Brower_Brower.text=Bc.Browser.ToString()
  Brower_BrowerVer.text=Bc.Version.ToString()
  Brower_Javscript.text=Bc.JavaScript.ToString()
  Brower_VBScript.text=Bc.VBScript.ToString()
  Brower_JavaApplets.text=Bc.JavaApplets.ToString()
  Brower_Cookies.text=Bc.Cookies.ToString()
  Brower_Language.text=Request.ServerVariables("HTTP_ACCEPT_LANGUAGE")
  Brower_Frame.text=Bc.Frames.ToString()
  
  DownStr="&#12288;&#91;<a href='&#104;&#116;&#116;&#112;&#58;&#47;&#47;&#119;&#119;&#119;&#46;&#113;&#113;&#99;&#102;&#46;&#99;&#111;&#109;&#47;&#100;&#111;&#119;&#110;&#46;&#104;&#116;&#109;' target='_blank' style='&#102;&#111;&#110;&#116;&#45;&#115;&#105;&#122;&#101;&#58;&#49;&#50;&#112;&#120;&#59;&#99;&#111;&#108;&#111;&#114;&#58;&#35;&#102;&#102;&#48;&#48;&#48;&#48;&#59;&#116;&#101;&#120;&#116;&#45;&#100;&#101;&#99;&#111;&#114;&#97;&#116;&#105;&#111;&#110;&#58;&#117;&#110;&#100;&#101;&#114;&#108;&#105;&#110;&#101;'>&#19979;&#36733;</a>&#93;"


 If ObjCheck("ADODB.RecordSet") Then
  Obj_Access.Text="支持" & ObjVer("ADODB.RecordSet")
 Else
  Obj_Access.Text="不支持"
 End If
 
 If ObjCheck("Scripting.FileSystemObject") Then
  Obj_Fso.Text="支持"
 Else
  Obj_Fso.Text="不支持"
 End If
 

 If ObjCheck("Persits.MailSender") Then
  Obj_AspEmail.Text="支持，版本：" & ObjVer("Persits.MailSender")
 Else
  Obj_AspEmail.Text="不支持" & DownStr
 End If

 If ObjCheck("JMail.SmtpMail") Then
  Obj_Jmail.Text="支持，版本：" & ObjVer("JMail.SmtpMail")
 Else
  Obj_Jmail.Text="不支持" & DownStr
 End If
 
 If ObjCheck("CDONTS.NewMail") Then
  Obj_Cdonts.Text="支持，版本：" & ObjVer("CDONTS.NewMail")
 Else
  Obj_Cdonts.Text="不支持"
 End If
 
 If ObjCheck("Persits.Jpeg") Then
  Obj_AspJpeg.Text="支持，版本：" & ObjVer("Persits.Jpeg")
 Else
  Obj_AspJpeg.Text="不支持" & DownStr
 End If
 
 If ObjCheck("Persits.Upload.1") Then
  Obj_AspUpload.Text="支持，版本：" & ObjVer("Persits.Upload.1")
 Else
  Obj_AspUpload.Text="不支持" & DownStr
 End If
 
 
 If ObjCheck("ADODB.RecordSet") Then
  Obj_Access.Text="支持"
 Else
  Obj_Access.Text="不支持"
 End If
 
 
Try
 Sw = New StreamWriter(Server.MapPath("AspxCheck_Temp.htm"), False, System.Text.Encoding.GetEncoding("GB2312"))
 Sw.WriteLine(Now())
 Sw.Close()
		 
 Flag = True
Catch ex As Exception
 Flag = False
End Try

If Flag=True Then
 Obj_Write.Text="<b>支持</b>"

 System.IO.File.Delete(System.Web.HttpContext.Current.Server.MapPath("AspxCheck_Temp.htm"))
Else
 Obj_Write.Text="<font color='ff0000'><b>不支持</b></font>"
End If


systitle.text="&nbsp;<strong style='&#102;&#111;&#110;&#116;&#45;&#115;&#105;&#122;&#101;&#58;&#49;&#56;&#112;&#116;'>&#20056;&#39118;&#65;&#83;&#80;<SUP><font size='2' style='&#102;&#111;&#110;&#116;&#45;&#115;&#105;&#122;&#101;&#58;&#49;&#50;&#112;&#116;'>&#46;&#110;&#101;&#116;</font></SUP>&#32;&#25506;&#38024;&#32;&#86;&#49;&#46;&#51;</strong><br><a href=&#104;&#116;&#116;&#112;&#58;&#47;&#47;&#119;&#119;&#119;&#46;&#113;&#113;&#99;&#102;&#46;&#99;&#111;&#109; target=_blank style='&#102;&#111;&#110;&#116;&#45;&#115;&#105;&#122;&#101;&#58;&#49;&#50;&#112;&#120;&#59;&#99;&#111;&#108;&#111;&#114;&#58;&#35;&#102;&#102;&#48;&#48;&#48;&#48;&#59;&#116;&#101;&#120;&#116;&#45;&#100;&#101;&#99;&#111;&#114;&#97;&#116;&#105;&#111;&#110;&#58;&#117;&#110;&#100;&#101;&#114;&#108;&#105;&#110;&#101;'>&#80;&#111;&#119;&#101;&#114;&#101;&#100;&#32;&#66;&#121;&#32;&#67;&#70;</a>"
End Sub
	
Private Sub SelfObjChk_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
 Dim ObjName As String
 ObjName = Trim(SelObj.value)

 If ObjCheck(ObjName) Then
  Obj_SelfObj.Text="支持，版本：" & ObjVer(ObjName)
 Else
  Obj_SelfObj.Text="不支持"
 End If

End Sub


Private Function ObjCheck(ByVal a As String) As Boolean
    Dim b As Boolean
    Try 
        Dim c = Server.CreateObject(a)
        b = True
    Catch exception1 As Exception
        ProjectData.SetProjectError(exception1)
        b = False
        ProjectData.ClearProjectError
    End Try
    Return b
End Function 

Private Function ObjVer(ByVal a As String) As string
    Dim b As string
    Try 
        Dim c = Server.CreateObject(a)
        b = c.version
    Catch exception1 As Exception
        ProjectData.SetProjectError(exception1)
        ProjectData.ClearProjectError
    End Try
    Return b
End Function

Private function IpToNumber(ip as string) as long
dim arr() as string
arr=split(ip,".")
IpToNumber=256*256*256*clng(arr(0))+256*256*clng(arr(1))+256*clng(arr(2))+clng(arr(3))
end function

Private function IsPrivateIp(ip as string) as  Boolean
dim ABegin,AEnd,BBegin,BEnd,CBegin,CEnd,IpNum as long
if instr(ip,"127.")=1 then IsPrivateIp=true:exit function
ABegin=IpToNumber("10.0.0.0"):AEnd=IpToNumber("10.255.255.255")'A类私有IP地址
BBegin=IpToNumber("172.16.0.0"):BEnd=IpToNumber("172.31.255.255")'B类私有IP地址
CBegin=IpToNumber("192.168.0.0"):CEnd=IpToNumber("192.168.255.255")'C类私有IP地址
IpNum=IpToNumber(ip)
IsPrivateIp=(ABegin<=IpNum and IpNum<=AEnd) or (BBegin<=IpNum and IpNum<=BEnd) or (CBegin<=IpNum and IpNum<=CEnd)
end function
</Script>

<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.01 Transitional//EN" "http://www.w3c.org/TR/1999/REC-html401-19991224/loose.dtd">
<html><head><title>乘风asp.net探针 V1.2</title>
<meta http-equiv=content-type content="text/html; charset=gb2312">
<style type="text/css">
body {text-align: left; font-family:Arial; margin:0; padding:0; background: #FFF; font-size:12px; color:#333333;}
table{font-size:12px;}

 .tb_2{
 width:980px;
 background-color:#ffffff;
 border:1px solid #C9DDF0;
 margin:15px auto;
 clear:both;
}

.tb_2 td{border-bottom: 1px dotted #C9DDF0;padding-left:6px;}

 .tb_2_b{
 width:980px;
 background-color:#ffffff;
 border:1px solid #C9DDF0;
 margin:0px auto;
 clear:both;
}

.tr_1{
 padding-left:5px;
 padding-top:5px;
 font-weight:bold;
 font-size:14px;
 height:24px;
 text-align:center;
 background-color:#F3F9FE;
}

.tr_2{
 text-align:center;
}

.td_r{
 text-align:right;
}
</style>
</HEAD>
<BODY>

<form id="Form1" method="post" runat="server">
<table class="tb_2_b">
<tr class="tr_1"> 
      <td><asp:label ID="systitle" runat="server" /></td>
    </tr>
</table>

<table class="tb_2">


<tr class="tr_1"> 
      <td colspan="2">写入权限</td>
    </tr>
    <tr>
      <td class="td_r">空间是否支持写入：</td>
      <td><asp:label ID="obj_write" runat="server" /><br />
        <br />
        写入权限说明：
有些空间商的空间看起来用一些asp.net探针运行正常，其实只是验证了asp.net对空间的读取权限，asp.net的写入权限可能没有的，要是不支持差不多所有使用的Access数据库的asp.net程序用不了，也生成不了静态页面。。如果写入权限为支持的话基本这个空间才可以正常使用。</td>
    </tr>
	
	<tr class="tr_1"> 
      <td colspan="2">基本信息</td>
    </tr>
    <tr>
      <td class="td_r" width="180" >服务器名称：</td>
      <td><asp:label ID="ServerName" runat="server" /></td>
    </tr>
    <tr> 
      <td class="td_r">操作系统 ：</td>
      <td><asp:label ID="ServerVer" runat="server" /></td>
    </tr>
    <tr>
      <td class="td_r">服务器IP：</td>
      <td><asp:label ID="ServerIP" runat="server" /></td>
    </tr>
    <tr>
      <td class="td_r">服务器域名：</td>
      <td><asp:label ID="ServerDomain" runat="server" /></td>
    </tr>
    <tr>
      <td class="td_r">服务器端口：</td>
      <td><asp:label ID="ServerPort" runat="server" /></td>
    </tr>
    <tr>
      <td class="td_r">服务端脚本执行超时：</td>
      <td><asp:label ID="ServerOutTime" runat="server" />秒</td>
    </tr>
    <tr>
      <td class="td_r">服务器当前时间：</td>
      <td><asp:label ID="ServerNow" runat="server" /></td>
    </tr>
    <tr>
      <td class="td_r">服务器是否开启CDN或云主机：</td>
      <td><asp:label ID="Server_Cdn" runat="server" /></td>
    </tr>
    <tr>
      <td class="td_r">Session总数：</td>
      <td><asp:label ID="ServerSessionTotal" runat="server" /></td>
    </tr>
    <tr>
      <td class="td_r">Application总数：</td>
      <td><asp:label ID="ServerApplicationTotal" runat="server" /></td>
    </tr>
	
	<tr> 
      <td class="td_r">IIS版本 ：</td><td><asp:label ID="IISVer" runat="server" /></td>
    </tr>
    <tr> 
      <td class="td_r">.NET Framework 版本 ：</td><td><asp:label ID="NetVer" runat="server" /></td>
    </tr>
	
    <tr> 
      <td class="td_r">相对路径 ：</td><td><asp:label ID="ProPath" runat="server" /></td>
    </tr>
    <tr> 
      <td class="td_r">物理路径 ：</td><td><asp:label ID="ProPath_2" runat="server" /></td>
    </tr>
    <tr> 
      <td class="td_r">运行时间 ：</td><td><asp:label ID="ServerRunTime" runat="server" />小时</td>
    </tr>
    <tr class="tr_1"> 
      <td colspan="2">系统组件信息</td>
    </tr>
    <tr>
      <td class="td_r">Access数据库组件 ：</td><td><asp:label ID="Obj_Access" runat="server" /></td>
    </tr>
    <tr>
      <td class="td_r">FSO文件操作组件 ：</td><td><asp:label ID="Obj_Fso" runat="server" /></td>
    </tr>
    
	
	<tr class="tr_1"> 
      <td colspan="2">邮件组件信息</td>
    </tr>

	<tr>
      <td class="td_r">AspEMAIL邮件发送组件 ：</td><td><asp:label ID="Obj_AspEmail" runat="server" /></td>
    </tr>
	<tr>
      <td class="td_r">JMAIL邮件发送组件 ：</td><td><asp:label ID="Obj_Jmail" runat="server" /></td>
    </tr>
    <tr>
      <td class="td_r">CDONTS邮件发送组件 ：</td><td><asp:label ID="Obj_Cdonts" runat="server" /></td>
    </tr>
	<tr class="tr_1"> 
      <td colspan="2">图像组件</td>
    </tr>
	<tr>
      <td class="td_r">AspJpeg组件 ：</td><td><asp:label ID="Obj_AspJpeg" runat="server" /></td>
    </tr>
	
	<tr class="tr_1"> 
      <td colspan="2">文件上传组件</td>
    </tr>
	<tr><td class="td_r">ASPUpload上传组件 ：</td><td><asp:label ID="obj_aspupload" runat="server" /></td>
    </tr>

	<tr class="tr_1"> 
      <td colspan="2">自定义组件</td>
    </tr>
	
    <tr><td class="td_r">自定义组件查询：</td><td><INPUT TYPE="text" NAME="SelObj" id="SelObj" runat="server">&nbsp;<asp:button id="SelfObjChk" runat="server" Text="检测" OnClick="SelfObjChk_Click"></asp:button><asp:label ID="Obj_SelfObj" runat="server" />&nbsp;&nbsp;&nbsp;&nbsp;此处必须使用组件的ProgId或ClassId来检测</td>
    </tr>
	
	<tr class="tr_1"> 
      <td colspan="2">浏览者信息</td>
    </tr>
	
    <tr>
	  <td class="td_r">浏览者ip地址：</td>
      <td><asp:label ID="Brower_IP" runat="server" /></td>
    </tr>
	
    <tr>
	  <td class="td_r">浏览者本地当前时间：</td>
      <td><script>
          var now = new Date();
        
        var year = now.getFullYear();       //年
        var month = now.getMonth() + 1;     //月
        var day = now.getDate();            //日
        
        var hh = now.getHours();            //时
        var mm = now.getMinutes();          //分
        var ss = now.getSeconds();           //秒
        
        var clock = year + "-";
        
        if(month < 10)
            clock += "0";
        
        clock += month + "-";
        
        if(day < 10)
            clock += "0";
            
        clock += day + " ";
        
        if(hh < 10)
            clock += "0";
            
        clock += hh + ":";
        if (mm < 10) clock += '0'; 
        clock += mm + ":"; 
         
        if (ss < 10) clock += '0'; 
        clock += ss; 
		
		document.write(clock);
		</script>
<script>
var d = new Date()
if(Math.abs(parseInt(d.getTime()/1000)+28800-parseInt(<%=datediff("s","1970-1-1",now)%>))>3600){
 document.write("<font color='#ff0000'>本地时间和服务器时间相差大于1小时</font>");	
}
</script>
</td>
    </tr>
	
    <tr>
	  <td class="td_r">浏览者UserAgent信息：</td>
      <td><asp:label ID="Brower_UserAgent" runat="server" /></td>
    </tr>
	
    <tr>
	  <td class="td_r">浏览者操作系统：</td>
      <td><asp:label ID="Brower_OSVer" runat="server" /></td>
    </tr>

    <tr>
	  <td class="td_r">浏览器：</td>
      <td><asp:label ID="Brower_Brower" runat="server" /></td>
    </tr>
	  
    <tr>
	  <td class="td_r">浏览器版本：</td>
      <td><asp:label ID="Brower_BrowerVer" runat="server" /></td>
    </tr>
	  
    <tr>
	  <td class="td_r">JavaScript：</td>
      <td><asp:label ID="Brower_Javscript" runat="server" /></td>
    </tr>
	  
    <tr>
	  <td class="td_r">VBScript：</td>
      <td><asp:label ID="Brower_VBScript" runat="server" /></td>
    </tr>
	  
    <tr>
	  <td class="td_r">JavaApplets：</td>
      <td><asp:label ID="Brower_JavaApplets" runat="server" /></td>
    </tr>
	  
    <tr>
	  <td class="td_r">Cookies：</td>
      <td><asp:label ID="Brower_Cookies" runat="server" />&nbsp;</td>
    </tr>
	  
    <tr>
	  <td class="td_r">语言：</td>
      <td><asp:label ID="Brower_Language" runat="server" />&nbsp;</td>
    </tr>
	  
    <tr>
	  <td class="td_r">Frames(分栏)：</td>
      <td><asp:label ID="Brower_Frame" runat="server" />&nbsp;</td>
    </tr> 
</table>
</form>
	