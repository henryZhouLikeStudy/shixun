// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
namespace Shixun {
public sealed partial class ReaderApp {
 ReleaseUpdate updateInfo=new ReleaseUpdate();bool checkingUpdate,installingUpdate;
 async void BeginUpdateCheck(){try{await CheckUpdates(false);}catch{}}
 async Task<object> CheckUpdates(bool manual){
  if(checkingUpdate||installingUpdate)return updateInfo;checkingUpdate=true;updateInfo.status="checking";updateInfo.message="正在检查更新…";Push();
  try{updateInfo=await Task.Run(()=>UpdateService.Check());if(!manual&&updateInfo.available){var popup=new StartupPopup("发现新版本 "+updateInfo.version,"更新已就绪，可在设置中下载并重启。",10,"查看更新",()=>{ShowReader();if(ready)web.CoreWebView2.PostWebMessageAsJson("{\"type\":\"updateAvailable\"}");});var area=Screen.FromPoint(Cursor.Position).WorkingArea;popup.Location=new System.Drawing.Point(area.Right-popup.Width-20,area.Bottom-popup.Height-20);popup.Show();}}
  catch{updateInfo=new ReleaseUpdate{status="error",message="更新检查失败，请检查网络后重试。"};}
  finally{checkingUpdate=false;Push();}return updateInfo;
 }
 async Task<object> InstallUpdate(){
  if(installingUpdate||!updateInfo.available)throw new SafeError("请先检查更新。");
  string helper=Path.Combine(root,"UpdateInstaller.exe");if(!File.Exists(helper))throw new SafeError("缺少更新组件，请从 GitHub 下载完整便携包。");
  installingUpdate=true;updateInfo.status="downloading";updateInfo.message="正在下载并校验，完成后将重启…";Push();
  try{string stage=await Task.Run(()=>UpdateService.Prepare(updateInfo,store.Folder));string runner=Path.Combine(store.Folder,"updates","UpdateInstaller-"+Guid.NewGuid().ToString("N")+".exe");File.Copy(helper,runner);string args=Process.GetCurrentProcess().Id+" "+Quote(root)+" "+Quote(stage)+" "+Quote(store.Folder);Process.Start(new ProcessStartInfo(runner,args){UseShellExecute=false,CreateNoWindow=true,WorkingDirectory=store.Folder});ExitThread();return true;}
  catch(Exception ex){updateInfo.status="error";updateInfo.message=ex is SafeError?ex.Message:"更新失败，当前程序和本机数据已保留。";throw new SafeError(updateInfo.message);}
  finally{installingUpdate=false;Push();}
 }
 static string Quote(string value){return "\""+value.Replace("\"","").TrimEnd('\\')+"\"";}
}
}
