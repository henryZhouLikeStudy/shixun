// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace Shixun {
public sealed partial class ReaderApp {
 readonly HashSet<string> pendingAiNotices=new HashSet<string>();
 bool WaitForAiNotices(List<Article> articles){
  lock(store.Gate){if(store.State.ai==null||!store.State.ai.enabled||!store.State.ai.notifyAfter)return false;}
  var ids=articles.Select(a=>a.id).Where(id=>pendingAiNotices.Add(id)).ToList();
  Task.Run(async()=>{foreach(string id in ids){try{await aiService.Analyze(id,false,false,"notification").ConfigureAwait(false);}catch{store.Log("AI notification fallback");}finally{UI(()=>{pendingAiNotices.Remove(id);Article live;lock(store.Gate){var a=store.State.articles.FirstOrDefault(x=>x.id==id);live=a==null?null:Data.Clone(a);}if(live!=null&&!live.read)notificationQueue.Add(new[]{live});Push();DrainNotifications();});}}});
  return true;
 }
}
}
