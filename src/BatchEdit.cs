// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Linq;
using System.Collections.Generic;
namespace Shixun {
public static class BatchEdit {
 public static List<Article> Resolve(ClientState state,List<string> ids,string operation,string category){
  if(ids==null||ids.Count==0||ids.Count>20000)throw new SafeError("请选择1—20000篇内容。");
  if(!new[]{"read","unread","star","unstar","remind","classify","delete"}.Contains(operation))throw new SafeError("不支持的批量操作。");
  if(operation=="classify"&&category.Length>0&&!state.categories.Any(c=>c.id==category))throw new SafeError("分类不存在。");
  var keys=new HashSet<string>(ids);return state.articles.Where(a=>keys.Contains(a.id)).ToList();
 }
 public static void Apply(ClientState state,List<Article> articles,string operation,string category){
  double now=Data.Now();foreach(var a in articles){if(operation=="read")a.read=true;else if(operation=="unread")a.read=false;else if(operation=="star")a.star=true;else if(operation=="unstar")a.star=false;else if(operation=="remind")a.remindAt=now+900000;else if(operation=="classify"){a.category=category;a.locked=true;}else if(operation=="delete"){state.deleted.RemoveAll(m=>m.id==a.id);state.deleted.Add(new Mark{id=a.id,time=now});state.articles.Remove(a);}}
 }
 public static int Restore(ClientState state,List<Article> articles){int count=0;foreach(var a in articles){if(state.articles.Any(x=>x.id==a.id))continue;state.deleted.RemoveAll(m=>m.id==a.id);state.articles.Add(Data.Clone(a));count++;}return count;}
}
}
