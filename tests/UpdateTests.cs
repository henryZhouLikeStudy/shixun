// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Text;
using System.Security.Cryptography;
using Shixun;
class UpdateTests {
 static int checks;
 static void Check(bool value,string message){if(!value)throw new Exception(message);checks++;Console.WriteLine("PASS "+message);}
 static int Main(){try{
  string prefix="https://github.com/henryZhouLikeStudy/shixun/releases/download/v1.2.3/";
  string json=Data.Json().Serialize(new{tag_name="v1.2.3",draft=false,prerelease=false,assets=new[]{new{name="Shixun-1.2.3-win-x64.zip",browser_download_url=prefix+"Shixun-1.2.3-win-x64.zip"},new{name="SHA256SUMS.txt",browser_download_url=prefix+"SHA256SUMS.txt"}}});
  Check(UpdateService.Parse(json,"1.2.2").available,"stable newer release is available");Check(!UpdateService.Parse(json,"1.2.3").available,"same release is not an upgrade");Check(!UpdateService.Parse(json,"1.3.0").available,"downgrade rejected");Check(!UpdateService.Parse(json.Replace("\"draft\":false","\"draft\":true"),"1.2.1").available,"draft rejected");Check(!UpdateService.Parse(json.Replace("\"prerelease\":false","\"prerelease\":true"),"1.2.1").available,"prerelease rejected");Check(!UpdateService.Parse(json.Replace(prefix,"https://evil.example/"),"1.2.1").available,"foreign asset origin rejected");
  byte[] bytes=Encoding.UTF8.GetBytes("fixture-update");string hash;using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","");UpdateService.Verify(bytes,hash+"  Shixun-1.2.3-win-x64.zip","Shixun-1.2.3-win-x64.zip");Check(true,"exact SHA256 manifest accepted");bool rejected=false;try{UpdateService.Verify(new byte[]{1},hash+"  Shixun-1.2.3-win-x64.zip","Shixun-1.2.3-win-x64.zip");}catch(SafeError){rejected=true;}Check(rejected,"corrupt payload rejected");
  Check(!UpdateService.Allowed("../state.dpapi","1.2.3")&&!UpdateService.Allowed("C:/escape.exe","1.2.3")&&!UpdateService.Allowed("subscriptions.private.json","1.2.3"),"path traversal and private data overwrite rejected");Check(UpdateService.Allowed("ui/client.js","1.2.3")&&UpdateService.Allowed("UpdateInstaller.exe","1.2.3"),"explicit update file allowlist");Check(UpdateService.Current=="1.2.3","assembly version 1.2.3");
  var result=new AiResult{title="运动鞋",price="到手¥129",rating=4};AiService.ValidatePrice(result,new Article{title="运动鞋到手129元"});Check(AiService.DisplayTitle(result,true)=="运动鞋｜到手¥129｜推荐 4/5","concise product price rating title");result.price="到手¥19";AiService.ValidatePrice(result,new Article{title="运动鞋129元"});Check(result.price=="价格未说明","unsupported price not displayed");
  Console.WriteLine("RESULT: "+checks+" release checks passed");return 0;
 }catch(Exception ex){Console.Error.WriteLine(ex.Message);return 1;}}
}
