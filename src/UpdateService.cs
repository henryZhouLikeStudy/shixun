// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Text.RegularExpressions;
namespace Shixun {
public sealed class ReleaseUpdate {
 public string version="",url="",asset="",checksums="",status="idle",message="";
 public bool available;
}
public static class UpdateService {
 public const string Repository="henryZhouLikeStudy/shixun";
 public static string Current{get{return typeof(UpdateService).Assembly.GetName().Version.ToString(3);}}
 static string Value(Dictionary<string,object> d,string key){object v;return d.TryGetValue(key,out v)&&v!=null?Convert.ToString(v):"";}
 public static ReleaseUpdate Parse(string json,string current){
  var d=Data.Json().Deserialize<Dictionary<string,object>>(json);if(d==null)throw new SafeError("更新信息无效。");
  object raw;bool draft=d.TryGetValue("draft",out raw)&&Convert.ToBoolean(raw),pre=d.TryGetValue("prerelease",out raw)&&Convert.ToBoolean(raw);
  string tag=Value(d,"tag_name");if(!Regex.IsMatch(tag,"^v?\\d+\\.\\d+\\.\\d+$"))throw new SafeError("版本格式无效。");
  string version=tag.TrimStart('v');Version next,old;if(!Version.TryParse(version,out next)||!Version.TryParse(current,out old))throw new SafeError("版本格式无效。");
  var result=new ReleaseUpdate{version=version,url="https://github.com/"+Repository+"/releases/tag/"+tag,status="current",message="已是最新版本。"};
  if(draft||pre||next<=old)return result;
  object assets;if(d.TryGetValue("assets",out assets)&&assets is System.Collections.IEnumerable)foreach(var item in (System.Collections.IEnumerable)assets){var a=item as Dictionary<string,object>;if(a==null)continue;string name=Value(a,"name"),url=Value(a,"browser_download_url");if(url!="https://github.com/"+Repository+"/releases/download/"+tag+"/"+name)continue;if(name=="Shixun-"+version+"-win-x64.zip")result.asset=url;if(name=="SHA256SUMS.txt")result.checksums=url;}
  if(result.asset.Length==0||result.checksums.Length==0){result.status="pending";result.message="新版本发布附件尚未就绪，请稍后检查。";return result;}
  result.available=true;result.status="available";result.message="发现新版本 "+version;return result;
 }
 public static byte[] Read(string url,int limit){
  ServicePointManager.SecurityProtocol=SecurityProtocolType.Tls12;
  for(int hop=0;hop<5;hop++){
   var uri=new Uri(url);if(uri.Scheme!="https"||!new[]{"api.github.com","github.com","release-assets.githubusercontent.com","objects.githubusercontent.com"}.Contains(uri.Host)||uri.UserInfo.Length>0)throw new SafeError("已阻止不受信任的更新地址。");
   var req=(HttpWebRequest)WebRequest.Create(uri);req.UserAgent="Shixun/"+Current;req.Accept="application/vnd.github+json";req.Timeout=20000;req.ReadWriteTimeout=20000;req.AllowAutoRedirect=false;
   try{using(var res=(HttpWebResponse)req.GetResponse()){
    if((int)res.StatusCode>=300&&(int)res.StatusCode<400){url=new Uri(uri,res.Headers["Location"]).AbsoluteUri;continue;}
    if(res.ContentLength>limit)throw new SafeError("更新文件过大。");using(var stream=res.GetResponseStream())using(var output=new MemoryStream()){var buffer=new byte[16384];int n;while((n=stream.Read(buffer,0,buffer.Length))>0){if(output.Length+n>limit)throw new SafeError("更新文件过大。");output.Write(buffer,0,n);}return output.ToArray();}
   }}catch(WebException){throw new SafeError("无法连接 GitHub 更新服务，请稍后重试。");}
  }throw new SafeError("更新重定向过多。");
 }
 public static ReleaseUpdate Check(){return Parse(System.Text.Encoding.UTF8.GetString(Read("https://api.github.com/repos/"+Repository+"/releases/latest",1024*1024)),Current);}
 public static void Verify(byte[] zip,string manifest,string filename){var line=manifest.Split('\n').FirstOrDefault(x=>Regex.IsMatch(x.Trim(),"^[a-fA-F0-9]{64}\\s+\\*?"+Regex.Escape(filename)+"$"));if(line==null)throw new SafeError("更新缺少校验信息。");using(var sha=SHA256.Create()){string hash=BitConverter.ToString(sha.ComputeHash(zip)).Replace("-","").ToLowerInvariant();if(!hash.Equals(line.Trim().Substring(0,64),StringComparison.OrdinalIgnoreCase))throw new SafeError("更新文件校验失败，已停止安装。");}}
 public static string Prepare(ReleaseUpdate release,string folder){
  if(!release.available||!Regex.IsMatch(release.version,"^\\d+\\.\\d+\\.\\d+$"))throw new SafeError("请先检查可用更新。");
  var bytes=Read(release.asset,100*1024*1024);string name="Shixun-"+release.version+"-win-x64.zip";Verify(bytes,System.Text.Encoding.UTF8.GetString(Read(release.checksums,65536)),name);
  string stage=Path.Combine(folder,"updates",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(stage);
  using(var zip=new ZipArchive(new MemoryStream(bytes),ZipArchiveMode.Read)){long expanded=0;var paths=new HashSet<string>(StringComparer.OrdinalIgnoreCase);foreach(var entry in zip.Entries){string path=entry.FullName.Replace('\\','/');if(path.EndsWith("/"))continue;if(!Allowed(path,release.version)||!paths.Add(path)||entry.Length>100*1024*1024||(expanded+=entry.Length)>200*1024*1024)throw new SafeError("更新包内容无效。");string target=Path.GetFullPath(Path.Combine(stage,path));if(!target.StartsWith(stage+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new SafeError("更新路径无效。");Directory.CreateDirectory(Path.GetDirectoryName(target));using(var input=entry.Open())using(var output=File.Create(target))input.CopyTo(output);}}
  if(!File.Exists(Path.Combine(stage,"Shixun.exe"))||!File.Exists(Path.Combine(stage,"ui","index.html"))||System.Diagnostics.FileVersionInfo.GetVersionInfo(Path.Combine(stage,"Shixun.exe")).FileVersion!=release.version+".0")throw new SafeError("更新包版本不匹配。");return stage;
 }
 public static bool Allowed(string path,string version){
  if(path.Length==0||path.Contains("..")||path.Contains(":")||path.StartsWith("/")||path.Contains("\\"))return false;
  if(path.StartsWith("ui/"))return path.Split('/').Length==2&&Regex.IsMatch(path.Substring(3),"^[A-Za-z0-9_-]+\\.(html|css|js|svg)$");
  return new[]{"Shixun.exe","Shixun.exe.config","UpdateInstaller.exe","Microsoft.Web.WebView2.Core.dll","Microsoft.Web.WebView2.WinForms.dll","WebView2Loader.dll","LICENSE","LICENSE-WebView2.txt","NOTICE-WebView2.txt","README.md","THIRD-PARTY-NOTICES.md","CHANGELOG.md","Shixun-"+version+"-source.zip"}.Contains(path);
 }
}
}
