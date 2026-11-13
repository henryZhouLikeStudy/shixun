// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace Shixun {
public sealed partial class ReaderApp {
 readonly HashSet<string> pushRunning=new HashSet<string>();
 readonly Dictionary<string,string> pushVersions=new Dictionary<string,string>();
 readonly Dictionary<string,double> pushRetry=new Dictionary<string,double>();
 void CheckPush(){
  if(exiting)return;
  List<Source> sources;lock(store.Gate){if(!store.State.settings.pushEnabled)return;sources=store.State.sources.Where(s=>s.enabled).Select(Data.Clone).ToList();}
  foreach(var source in sources){
   Uri uri=Transport.ValidateUrl(source.url);if(!uri.AbsolutePath.Contains("/rss/weibo/user/"))continue;
   if(pushRunning.Contains(source.id)||pushRetry.ContainsKey(source.id)&&pushRetry[source.id]>Data.Now())continue;
   RequestProfile profile;lock(store.Gate){RequestProfile saved;profile=store.State.credentials.TryGetValue(source.id,out saved)?Data.Clone(saved):new RequestProfile();}
   // Only the same origin receives the source's credentials. No redirect is permitted by Transport.
   if(profile.method!="GET"||profile.fields.Any(f=>f.place=="path"))continue;
   var endpoint=new UriBuilder(uri);endpoint.Path=uri.AbsolutePath.Replace("/rss/weibo/user/","/events/weibo/user/");
   string version;pushVersions.TryGetValue(source.id,out version);endpoint.Query="since="+Uri.EscapeDataString(version??"");
   pushRunning.Add(source.id);
   Task.Run(()=>{try{var result=Transport.Fetch(endpoint.Uri.AbsoluteUri,profile,"","",4096,55000);var data=Data.Json().Deserialize<Dictionary<string,object>>(result.text);string next=S(data,"version");if(next.Length!=64||!System.Text.RegularExpressions.Regex.IsMatch(next,"^[a-f0-9]{64}$"))throw new SafeError("更新信号无效。");UI(()=>{pushVersions[source.id]=next;if(next!=version)lock(store.Gate){var live=store.State.sources.FirstOrDefault(s=>s.id==source.id);if(live!=null&&live.enabled&&store.State.settings.pushEnabled)live.nextRun=0;}});}catch{UI(()=>pushRetry[source.id]=Data.Now()+600000);}finally{UI(()=>pushRunning.Remove(source.id));}});
  }
 }
}
}
