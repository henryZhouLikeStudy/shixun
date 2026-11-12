// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Script.Serialization;
namespace Shixun {
public static class Data {
 public static JavaScriptSerializer Json(){return new JavaScriptSerializer{MaxJsonLength=64*1024*1024,RecursionLimit=80};}
 public static T Clone<T>(T value){return Json().Deserialize<T>(Json().Serialize(value));}
 public static double Now(){return (DateTimeOffset.UtcNow-new DateTimeOffset(1970,1,1,0,0,0,TimeSpan.Zero)).TotalMilliseconds;}
 public static string Id(){return Guid.NewGuid().ToString("N");}
 public static string Limit(string value,int max){return (value??"").Length>max?value.Substring(0,max):value??"";}
}
public sealed class Preferences {
 public int interval=5,retention=1,duration=8,cacheMB=200;
 public bool autostart=false,hiddenStart=true,confirmDelete=true,images=true,sound=false,quiet=true,keepStars=false;
 public string theme="system",font="normal",density="comfortable",notification="custom",position="right",notifyScope="all",notifyJoin="any",notifyKeywords="",quietStart="22:00",quietEnd="08:00";
 public List<string> notifyCategories=new List<string>();
}
public sealed class RequestField { public string place="header",name="",value=""; }
public sealed class RequestProfile {
 public string method="GET",mode="none",token="",username="",password="",body="",contentType="application/json";
 public List<RequestField> fields=new List<RequestField>();
 public bool HasSecrets(){return mode!="none"||fields.Count>0||body.Length>0;}
}
public sealed class Source {
 public string id=Data.Id(),name="",url="",group="我的订阅",etag="",modified="",error="",imageReferer="",format="",description="";
 public bool enabled=true,privateSource=false,initialized=false;
 public int interval=0,failures=0;
 public double nextRun=0,lastSync=0;
}
public sealed class Category {
 public string id=Data.Id(),name="",color="#729178",keywords="",exclude="",pattern="",ruleSource="",ruleKey="";
}
public sealed class RuleSource {
 public string id=Data.Id(),name="",url="",error="";
 public bool autoUpdate=false;
 public double nextRun=0,lastSync=0;
}
public sealed class Article {
 public string id="",guid="",source="",title="",originalTitle="",summary="",html="",link="",author="",category="",dateOrigin="published";
 public double time=0,firstSeen=0,remindAt=0;
 public bool read=false,star=false,locked=false;
 public List<string> images=new List<string>();
}
public sealed class Mark {public string id="";public double time=0;}
public sealed class Notice {public string id="",article="";public double time=0;}
public sealed class ClientState {
 public int version=1;
 public Preferences settings=new Preferences();
 public List<Source> sources=new List<Source>();
 public List<RuleSource> ruleSources=new List<RuleSource>();
 public List<Article> articles=new List<Article>();
 public List<Category> categories=new List<Category>();
 public List<Mark> deleted=new List<Mark>(),seen=new List<Mark>();
 public List<Notice> notices=new List<Notice>();
 public Dictionary<string,RequestProfile> credentials=new Dictionary<string,RequestProfile>();
}
public sealed class RuleDocument {public int version;public List<RuleCategory> categories=new List<RuleCategory>();}
public sealed class RuleCategory {public string key="",name="",color="#729178";public List<string> keywords=new List<string>(),exclude=new List<string>();}
public sealed class SafeError:Exception {public readonly int delay;public SafeError(string text,int retry=0):base(text){delay=retry;}}
}
