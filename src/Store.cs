// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
namespace Shixun {
public sealed class Store {
 public readonly object Gate=new object();
 public readonly string Folder;
 public ClientState State;
 public string Recovery="";
 public Store(string folder){Folder=folder;Directory.CreateDirectory(folder);State=Load();}
 ClientState Decode(string path){return Data.Json().Deserialize<ClientState>(Encoding.UTF8.GetString(ProtectedData.Unprotect(File.ReadAllBytes(path),null,DataProtectionScope.CurrentUser)));}
 ClientState Load(){string path=Path.Combine(Folder,"state.dpapi");if(!File.Exists(path))return new ClientState();try{return Decode(path);}catch{try{var recovered=Decode(path+".bak");Recovery="主数据损坏，已恢复上一份加密备份。";return recovered;}catch{throw new SafeError("本机加密数据无法读取，请保留数据目录并从备份恢复；未覆盖原文件。");}}}
 public void Save(){lock(Gate){var raw=Encoding.UTF8.GetBytes(Data.Json().Serialize(State));var bytes=ProtectedData.Protect(raw,null,DataProtectionScope.CurrentUser);Array.Clear(raw,0,raw.Length);string path=Path.Combine(Folder,"state.dpapi"),temp=path+".tmp";File.WriteAllBytes(temp,bytes);if(File.Exists(path))File.Replace(temp,path,path+".bak");else File.Move(temp,path);}}
 public void Clean(){lock(Gate){double now=Data.Now(),cut=now-Math.Max(1,State.settings.retention)*86400000d;State.articles.RemoveAll(a=>a.time<cut&&a.remindAt<=now&&!(a.star&&State.settings.keepStars));State.articles=State.articles.OrderByDescending(a=>a.time).Take(20000).ToList();State.deleted=State.deleted.Where(m=>m.time>now-90*86400000d).OrderByDescending(m=>m.time).Take(100000).ToList();State.seen=State.seen.Where(m=>m.time>now-90*86400000d).OrderByDescending(m=>m.time).Take(100000).ToList();var ids=State.articles.Select(a=>a.id).ToHashSetCompat();State.aiHistory=(State.aiHistory??new System.Collections.Generic.List<AiRun>()).Where(x=>x.at>now-30*86400000d).OrderByDescending(x=>x.at).Take(500).ToList();State.notices=State.notices.Where(n=>ids.Contains(n.article)).OrderByDescending(n=>n.time).Take(500).ToList();Save();}}
 public void Log(string message){try{string path=Path.Combine(Folder,"events.log");if(File.Exists(path)&&new FileInfo(path).Length>256000)File.WriteAllText(path,"");File.AppendAllText(path,DateTimeOffset.Now.ToString("o")+" "+message+Environment.NewLine);}catch{}}
}
static class CollectionExtensions {public static System.Collections.Generic.HashSet<T> ToHashSetCompat<T>(this System.Collections.Generic.IEnumerable<T> list){return new System.Collections.Generic.HashSet<T>(list);}}
}
