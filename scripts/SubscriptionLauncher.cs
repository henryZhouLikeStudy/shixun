// SPDX-License-Identifier: GPL-3.0-only
// Optional launcher for privately distributed subscription bundles. No default sources or keys.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using Shixun;
public sealed class SubscriptionEntry { public Source source; public RequestProfile profile; }
public sealed class SubscriptionSeed { public string id=""; public List<SubscriptionEntry> entries=new List<SubscriptionEntry>(); public List<Category> categories=new List<Category>(); }
class SubscriptionLauncher {
 [STAThread]static int Main(string[] args){try{
  string root=AppDomain.CurrentDomain.BaseDirectory,folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Shixun");
  int index=Array.IndexOf(args,"--data-dir");if(index>=0&&index+1<args.Length)folder=Path.GetFullPath(args[index+1]);
  bool verify=args.Contains("--verify");Directory.CreateDirectory(folder);
  using(var mutex=new Mutex(false,"Local\\Shixun-"+Feeds.Hash(folder).Substring(0,16))){
   if(mutex.WaitOne(0)){try{
    string marker=Path.Combine(folder,"subscription-pack-imported.txt");
    if(!File.Exists(marker)){
     string path=Path.Combine(root,"subscriptions.private.json");if(new FileInfo(path).Length>1048576)throw new Exception();
     var seed=Data.Json().Deserialize<SubscriptionSeed>(File.ReadAllText(path));if(seed.entries==null||seed.entries.Count>100||seed.categories==null)throw new Exception();
     var store=new Store(folder);
     foreach(var entry in seed.entries){if(entry.source==null)throw new Exception();Transport.ValidateUrl(entry.source.url);if(store.State.sources.Any(s=>s.url==entry.source.url))continue;
      var source=entry.source;source.id=Data.Id();source.privateSource=true;source.enabled=true;source.initialized=false;source.nextRun=0;source.etag="";source.modified="";source.lastSync=0;source.error="";
      store.State.sources.Add(source);if(entry.profile!=null)store.State.credentials[source.id]=entry.profile;
     }
     foreach(var c in seed.categories)if(!store.State.categories.Any(x=>x.name==c.name)){c.id=Data.Id();store.State.categories.Add(c);}
     store.Save();File.WriteAllText(marker,"Imported subscription bundle. Existing user preferences are preserved.");
    }
    if(verify){var state=new Store(folder).State;Console.WriteLine(Data.Json().Serialize(new{sources=state.sources.Count,credentials=state.credentials.Count,articles=state.articles.Count,aiEnabled=state.ai.enabled}));}
   }finally{mutex.ReleaseMutex();}}
   else if(verify)throw new Exception();
   else if(!File.Exists(Path.Combine(folder,"subscription-pack-imported.txt"))){MessageBox.Show("请先从托盘退出已运行的拾讯，再运行启动器导入订阅。","拾讯");return 1;}
  }
  if(!verify){var start=new ProcessStartInfo(Path.Combine(root,"Shixun.exe")){WorkingDirectory=root};if(index>=0)start.Arguments="--data-dir \""+folder+"\"";Process.Start(start);}
  return 0;
 }catch{if(!args.Contains("--verify"))MessageBox.Show("初始化失败，请完整解压分发包，并检查数据目录权限。已有数据未清除。","拾讯");return 1;}}
}
