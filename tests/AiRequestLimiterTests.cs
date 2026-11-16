// SPDX-License-Identifier: GPL-3.0-only
using System;using System.Linq;using System.Collections.Generic;using System.Threading.Tasks;using Shixun;
class AiRequestLimiterTests {
 static void Check(bool value,string label){if(!value)throw new Exception(label);Console.WriteLine("PASS "+label);}
 static int Main(){try{Run().GetAwaiter().GetResult();return 0;}catch(Exception error){Console.Error.WriteLine(error.Message);return 1;}}
 static async Task VerifyCommentRefresh(){
  string folder=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"ShixunComment-"+Guid.NewGuid().ToString("N"));
  var listener=new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback,0);listener.Start();
  try{
   string url="http://127.0.0.1:"+((System.Net.IPEndPoint)listener.LocalEndpoint).Port+"/feed";
   var server=Task.Run(()=>{for(int index=0;index<2;index++)using(var client=listener.AcceptTcpClient())using(var stream=client.GetStream()){
    var reader=new System.IO.StreamReader(stream);string line;while(!String.IsNullOrEmpty(line=reader.ReadLine())){}
    string html="<p>正文</p>"+(index==1?"<!-- rss-comments --><hr><h3>评论（采集时）</h3><p>新增评论</p>":"");
    byte[] body=System.Text.Encoding.UTF8.GetBytes("<rss><channel><title>Fixture</title><item><guid>same</guid><title>文章标题</title><pubDate>"+DateTime.UtcNow.ToString("r")+"</pubDate><description><![CDATA["+html+"]]></description></item></channel></rss>");
    byte[] head=System.Text.Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Type: application/xml; charset=utf-8\r\nContent-Length: "+body.Length+"\r\nConnection: close\r\n\r\n");stream.Write(head,0,head.Length);stream.Write(body,0,body.Length);
   }});
   var store=new Store(folder);store.State.sources.Add(new Source{id="fixture",url=url});var engine=new Engine(store);int notices=0;engine.Arrived+=rows=>notices+=rows.Count;
   await engine.Run(true);var article=store.State.articles.Single();article.read=true;article.star=true;article.ai=new AiResult{title="保留AI",rating=4};await engine.Run(true);await server;
   var updated=store.State.articles.Single();Check(updated.legacyCommentsHtml.Contains("新增评论")&&!updated.html.Contains("新增评论")&&updated.read&&updated.star&&updated.ai!=null&&updated.ai.title=="保留AI"&&notices==0,"real RSS comment refresh separates comments and preserves read, favorite and AI without another notification");
  }finally{listener.Stop();if(System.IO.Directory.Exists(folder))System.IO.Directory.Delete(folder,true);}
 }
 static async Task VerifyServiceConcurrency(){
  string folder=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"ShixunConcurrent-"+Guid.NewGuid().ToString("N"));var listener=new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback,0);listener.Start();var release=new TaskCompletionSource<bool>();int received=0;
  try{
   string endpoint="http://127.0.0.1:"+((System.Net.IPEndPoint)listener.LocalEndpoint).Port+"/chat/completions";
   string result=Data.Json().Serialize(new{title="独立标题",articleType="other",price="",categoryId="",categoryName="",rating=4,reason="测试"});string response=Data.Json().Serialize(new{choices=new[]{new{message=new{content=result}}}});
   var server=Task.Run(async()=>{var handlers=new List<Task>();for(int i=0;i<2;i++){var client=listener.AcceptTcpClient();handlers.Add(Task.Run(async()=>{using(client)using(var stream=client.GetStream()){var raw=new List<byte>();int value;while((value=stream.ReadByte())>=0){raw.Add((byte)value);int n=raw.Count;if(n>=4&&raw[n-4]==13&&raw[n-3]==10&&raw[n-2]==13&&raw[n-1]==10)break;}int length=0;foreach(string line in System.Text.Encoding.ASCII.GetString(raw.ToArray()).Split('\n'))if(line.StartsWith("Content-Length:",StringComparison.OrdinalIgnoreCase))int.TryParse(line.Substring(15).Trim(),out length);for(int index=0;index<length;index++)stream.ReadByte();System.Threading.Interlocked.Increment(ref received);await release.Task;byte[] body=System.Text.Encoding.UTF8.GetBytes(response),header=System.Text.Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: "+System.Text.Encoding.UTF8.GetByteCount(response)+"\r\nConnection: close\r\n\r\n");stream.Write(header,0,header.Length);stream.Write(body,0,body.Length);}}));}await Task.WhenAll(handlers);});
   var store=new Store(folder);store.State.ai=new AiSettings{enabled=true,endpoint=endpoint,model="fixture",requestIntervalSeconds=1,maxConcurrentRequests=2};store.State.sources.Add(new Source{id="s",url="https://example.com/feed"});store.State.articles.Add(new Article{id="a",source="s",title="Article A"});store.State.articles.Add(new Article{id="b",source="s",title="Article B"});
   var service=new AiService(store);var tasks=Task.WhenAll(service.Analyze("a"),service.Analyze("b"),service.Analyze("a"));var watch=System.Diagnostics.Stopwatch.StartNew();while(received<2&&watch.ElapsedMilliseconds<6000)await Task.Delay(20);Check(received==2,"actual AI service sends two simultaneous local HTTP requests");release.TrySetResult(true);Check(await Task.WhenAny(tasks,Task.Delay(8000))==tasks,"parallel AI tasks complete without a duplicate paid request");await tasks;await server;Check(store.State.aiRequestsToday==2&&store.State.aiHistory.Count(r=>r.status=="cached")==1,"same-article concurrent analysis reuses the first paid result");
  }finally{release.TrySetResult(true);listener.Stop();if(System.IO.Directory.Exists(folder))System.IO.Directory.Delete(folder,true);}
 }
 static async Task Run(){
  AiService.Validate(new AiSettings{maxConcurrentRequests=2500,requestIntervalSeconds=0});
  var many=new AiRequestLimiter();int opened=0;var finish=new TaskCompletionSource<bool>();var high=Enumerable.Range(0,2500).Select(_=>many.Run(0,async()=>{System.Threading.Interlocked.Increment(ref opened);await finish.Task;return 1;},concurrency:2500)).ToArray();Check(opened==2500,"configured 2500 concurrency is not capped at six or sixteen");finish.SetResult(true);await Task.WhenAll(high);
  var favoriteState=new ClientState{ai=new AiSettings{useHistory=true}};var favoriteArticle=new Article{title="工具",ai=new AiResult()};favoriteArticle.ai.fingerprint=AiService.Fingerprint(favoriteArticle,favoriteState.ai,favoriteState.categories,AiService.FavoritesKey(favoriteState,favoriteState.ai));Check(AiService.IsCurrentResult(favoriteState,favoriteArticle),"notification result validation includes favorite preference fingerprint");
  await VerifyServiceConcurrency();
  await VerifyCommentRefresh();
  string rss="<rss xmlns:sx='urn:shixun:rss:comments:v1'><channel><title>Fixture</title><item><guid>structured</guid><title>文章</title><description>只有正文</description><sx:comments state='ready' round='1'><sx:comment id='c'><sx:author>作者</sx:author><sx:text>&lt;script&gt;评论&lt;/script&gt;</sx:text><sx:likes>2</sx:likes></sx:comment></sx:comments></item></channel></rss>";
  var parsed=Feeds.Parse(rss,new Source{id="s"},new Uri("https://example.com/feed")).articles.Single();Check(parsed.html=="只有正文"&&parsed.summary=="只有正文"&&parsed.comments.entries.Single().text=="<script>评论</script>"&&parsed.comments.round==1,"structured RSS comment namespace parses independently from article body and summary");
  var fifth=Feeds.Parse(rss.Replace("round='1'","round='5' totalRounds='5'"),new Source{id="s"},new Uri("https://example.com/feed")).articles.Single();Check(fifth.comments.round==5&&fifth.comments.totalRounds==5,"fifth comment round is preserved without former two-round truncation");
  var legacy=Feeds.Parse(rss.Replace("round='1'","round='2' totalRounds='2'"),new Source{id="s"},new Uri("https://example.com/feed")).articles.Single();Check(legacy.comments.round==2&&legacy.comments.totalRounds==2,"legacy comment round count remains compatible");
  var concurrent=new AiRequestLimiter();int started=0,active=0,peak=0;var release=new TaskCompletionSource<bool>();
  Func<Task<int>> concurrentRequest=async()=>{int current=System.Threading.Interlocked.Increment(ref active);peak=Math.Max(peak,current);System.Threading.Interlocked.Increment(ref started);await release.Task;System.Threading.Interlocked.Decrement(ref active);return 1;};
  var concurrentTasks=Enumerable.Range(0,3).Select(i=>concurrent.Run(1,concurrentRequest,concurrency:2)).ToArray();var watch=System.Diagnostics.Stopwatch.StartNew();while(started<2&&watch.ElapsedMilliseconds<4000)await Task.Delay(20);Check(started==2&&peak==2,"AI requests overlap with configured concurrency two while third waits");release.SetResult(true);await Task.WhenAll(concurrentTasks);Check(peak==2&&started==3,"AI concurrency ceiling holds while remaining work drains");
  double clock=0;var starts=new List<double>();var limiter=new AiRequestLimiter(()=>clock,milliseconds=>{clock+=milliseconds;return Task.FromResult(0);});
  Func<Task<int>> request=()=>{starts.Add(clock);return Task.FromResult(1);};
  await Task.WhenAll(Enumerable.Range(0,6).Select(i=>limiter.Run(10,request)));
  Check(starts.SequenceEqual(new double[]{0,10000,20000,30000,40000,50000}),"six simultaneous AI tasks share spaced serial request slots");
  int calls=0,retries=0;double first=clock;
  await limiter.Run(10,()=>{starts.Add(clock);if(calls++==0)throw new SafeError("limited",120,429);return Task.FromResult(1);},()=>retries++);
  Check(calls==2&&retries==1&&starts.Last()-starts[starts.Count-2]>=120000,"429 honors Retry-After before one retry");
  calls=0;try{await limiter.Run(10,()=>{calls++;return Task.FromException<int>(new SafeError("limited",0,429));});throw new Exception("expected failure");}catch(SafeError){}
  Check(calls==2,"persistent 429 is bounded to one retry");
  double previous=clock;await limiter.Run(10,request);Check(clock-previous>=120000,"cooldown also protects the next article");
  calls=0;try{await limiter.Run(10,()=>{calls++;return Task.FromException<int>(new SafeError("auth",0,401));});}catch(SafeError){}
  Check(calls==1,"authentication errors are never retried");
  Check(new AiSettings().requestIntervalSeconds==10,"default AI request interval is ten seconds with no daily quota");
  var title=new AiResult{title="华味亨脱油花生420g两袋麻辣或椒盐口味",price="13.90元（需浏览商品页面掉落2元限时立减，非Plus会员也包邮）",rating=4};
  var before=new Article{title="文章",html="<p>正文</p>"};var after=new Article{title="文章",html="<p>正文</p><!-- rss-comments --><hr><h3>评论（采集时）</h3><p>评论</p>"};
  Check(Feeds.SamePrimaryContent(before,after),"comment-only updates preserve primary article identity");
  Check(AiService.Fingerprint(before,new AiSettings(),new List<Category>())==AiService.Fingerprint(after,new AiSettings(),new List<Category>()),"comment updates preserve AI fingerprint and avoid another paid analysis");
  string display=AiService.DisplayTitle(title,true);Check(display.Length<=44&&display.Contains("13.90元")&&display.EndsWith("推荐 4/5")&&display.Contains("条件见正文"),"compact title retains complete price marker and rating without half a condition");
 }
}
