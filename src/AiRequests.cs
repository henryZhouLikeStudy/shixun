// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Threading;
using System.Threading.Tasks;
namespace Shixun {
public sealed class AiRequestLimiter {
 readonly object sync=new object();int active;TaskCompletionSource<bool> changed=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
 readonly Func<double> now;readonly Func<int,Task> delay;double nextAt,blockedUntil,lastLimitedAt=-1;int failures;
 public AiRequestLimiter(Func<double> clock=null,Func<int,Task> wait=null){var watch=System.Diagnostics.Stopwatch.StartNew();now=clock??(()=>watch.Elapsed.TotalMilliseconds);delay=wait??(milliseconds=>Task.Delay(milliseconds));}
 async Task Enter(int seconds,int concurrency,Action waiting){for(;;){Task signal;int milliseconds;lock(sync){double until=Math.Max(nextAt,blockedUntil);if(active<concurrency&&until<=now()){active++;nextAt=now()+seconds*1000d;return;}signal=changed.Task;milliseconds=active>=concurrency?60000:(int)Math.Max(1,Math.Min(60000,Math.Ceiling(until-now())));}if(waiting!=null)waiting();await Task.WhenAny(signal,delay(milliseconds)).ConfigureAwait(false);}}
 void Leave(){lock(sync){active--;var previous=changed;changed=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);previous.TrySetResult(true);}}
 public async Task<T> Run<T>(int seconds,Func<Task<T>> request,Action retry=null,Action waiting=null,int concurrency=1){
  if(concurrency<1||concurrency>10000||seconds<0||seconds>300)throw new SafeError("AI请求间隔或并发数无效。");
  for(int attempt=0;;attempt++){
   await Enter(seconds,concurrency,waiting).ConfigureAwait(false);double started=now();
   try{if(attempt>0&&retry!=null)retry();var result=await request().ConfigureAwait(false);lock(sync){if(started>lastLimitedAt)failures=0;}return result;}
   catch(SafeError error){if(error.statusCode!=429)throw;lock(sync){failures=Math.Min(5,failures+1);lastLimitedAt=now();blockedUntil=Math.Max(blockedUntil,now()+Math.Max(error.delay,Math.Min(900,60*Math.Pow(2,failures-1)))*1000d);}if(attempt>=1)throw;}
   finally{Leave();}
  }
 }
}
public sealed partial class AiService {
 public static bool IsCurrentResult(ClientState state,Article article){var settings=state.ai??new AiSettings();return article!=null&&article.ai!=null&&article.aiError.Length==0&&article.ai.fingerprint==Fingerprint(article,settings,state.categories,FavoritesKey(state,settings));}
 readonly AiRequestLimiter requests;
 Task<FetchResult> RequestAi(AiSettings settings,RequestProfile profile,AiRun run){
  return requests.Run(settings.requestIntervalSeconds,async()=>{
   lock(store.Gate){if(!store.State.ai.enabled||store.State.ai.endpoint!=settings.endpoint||store.State.ai.model!=settings.model)throw new SafeError("AI配置已变更，已取消排队请求。");run.status="running";}Announce();
   var endpoint=System.Net.ServicePointManager.FindServicePoint(Transport.ValidateUrl(Transport.RequestUrl(settings.endpoint,profile)));endpoint.ConnectionLimit=Math.Max(endpoint.ConnectionLimit,Math.Max(16,settings.maxConcurrentRequests));
   return await Task.Run(()=>Transport.Fetch(settings.endpoint,profile,"","",2*1024*1024,settings.timeoutSeconds*1000)).ConfigureAwait(false);
  },()=>{lock(store.Gate){string day=DateTime.Now.ToString("yyyy-MM-dd");if(store.State.aiDay!=day){store.State.aiDay=day;store.State.aiRequestsToday=0;}if(settings.dailyLimit>0&&store.State.aiRequestsToday>=settings.dailyLimit)throw new SafeError("已达到今日AI调用上限。");store.State.aiRequestsToday++;store.Save();}},()=>{if(run.status!="queued"){run.status="queued";Announce();}},settings.maxConcurrentRequests);
 }
}
}
