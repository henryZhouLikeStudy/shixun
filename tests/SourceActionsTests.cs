// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shixun;
class SourceActionsTests {
 static object Call(ReaderApp app,string id,object enabled){var data=new Dictionary<string,object>{{"id",id},{"enabled",enabled}};return ((Task<object>)typeof(ReaderApp).GetMethod("Dispatch",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(app,new object[]{"setSourceEnabled",data})).GetAwaiter().GetResult();}
 static void Check(bool value){if(!value)throw new Exception("source action check failed");}
 static int Main(){var folder=Path.Combine(Path.GetTempPath(),"ShixunSourceTests-"+Guid.NewGuid().ToString("N"));try{
  var store=new Store(folder);store.State.sources.Add(new Source{id="fixture",enabled=true,nextRun=123});store.Save();
  var app=(ReaderApp)FormatterServices.GetUninitializedObject(typeof(ReaderApp));typeof(ReaderApp).GetField("store",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(app,store);
  Check((bool)Call(app,"fixture",false)==false);Check(!new Store(folder).State.sources[0].enabled);
  Check((bool)Call(app,"fixture",false)==false);Check(!store.State.sources[0].enabled);
  Check((bool)Call(app,"fixture",true));Check(store.State.sources[0].nextRun==0);Check(new Store(folder).State.sources[0].enabled);
  bool rejected=false;try{Call(app,"fixture","false");}catch(SafeError){rejected=true;}Check(rejected&&store.State.sources[0].enabled);
  rejected=false;try{Call(app,"missing",true);}catch(SafeError){rejected=true;}Check(rejected);
  store.State.articles.Add(new Article{id="notice",read=false,remindAt=Data.Now()+900000});store.Save();
  typeof(ReaderApp).GetMethod("MarkNoticeRead",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(app,new object[]{"notice"});
  var readBack=new Store(folder).State.articles[0];Check(readBack.read&&readBack.remindAt==0);
  Console.WriteLine("PASS native source actions: persisted pause/resume, repeated intent, immediate schedule, invalid inputs");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}finally{if(Directory.Exists(folder))Directory.Delete(folder,true);}}
}
