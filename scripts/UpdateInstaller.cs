// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Windows.Forms;
class UpdateInstaller {
 [STAThread]static int Main(string[] args){string root="",data="";try{
  if(args.Length!=4)throw new Exception();int pid=int.Parse(args[0]);root=Path.GetFullPath(args[1]);string stage=Path.GetFullPath(args[2]);data=Path.GetFullPath(args[3]);
  if(root==stage||!File.Exists(Path.Combine(root,"Shixun.exe"))||!stage.StartsWith(Path.Combine(data,"updates")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new Exception();
  try{var process=Process.GetProcessById(pid);if(!process.MainModule.FileName.Equals(Path.Combine(root,"Shixun.exe"),StringComparison.OrdinalIgnoreCase)||!process.WaitForExit(30000))throw new Exception();}catch(ArgumentException){}
  Apply(root,stage,Path.Combine(root,".update-backup-"+DateTime.UtcNow.ToString("yyyyMMddHHmmss")));
  Restart(root,data);return 0;
 }catch{if(root.Length>0&&data.Length>0)try{Restart(root,data);}catch{}MessageBox.Show("更新未完成，已尝试恢复原版本。本机订阅和凭据未改动。","拾讯");return 1;}}
 public static void Apply(string root,string stage,string backup){var copied=new List<string>();var originals=new HashSet<string>();Directory.CreateDirectory(backup);try{
  foreach(string file in Directory.GetFiles(stage,"*",SearchOption.AllDirectories)){
   string name=file.Substring(stage.Length+1);if(name.Contains("..")||name.Contains(":")||name.StartsWith("\\")||!(name.StartsWith("ui\\")||new[]{"Shixun.exe","Shixun.exe.config","UpdateInstaller.exe","Microsoft.Web.WebView2.Core.dll","Microsoft.Web.WebView2.WinForms.dll","WebView2Loader.dll","LICENSE","LICENSE-WebView2.txt","NOTICE-WebView2.txt","README.md","THIRD-PARTY-NOTICES.md","CHANGELOG.md"}.Contains(name)||System.Text.RegularExpressions.Regex.IsMatch(name,"^Shixun-[0-9.]+-source\\.zip$")))throw new Exception();
   string target=Path.Combine(root,name),saved=Path.Combine(backup,name);Directory.CreateDirectory(Path.GetDirectoryName(target));if(File.Exists(target)){Directory.CreateDirectory(Path.GetDirectoryName(saved));File.Copy(target,saved);originals.Add(name);}copied.Add(name);File.Copy(file,target,true);
  }
 }catch{foreach(string name in copied)try{if(originals.Contains(name))File.Copy(Path.Combine(backup,name),Path.Combine(root,name),true);else File.Delete(Path.Combine(root,name));}catch{}throw;}}
 static void Restart(string root,string data){Process.Start(new ProcessStartInfo(Path.Combine(root,"Shixun.exe"),"--data-dir \""+data+"\""){WorkingDirectory=root,UseShellExecute=true});}
}
