// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Linq;
using System.Collections.Generic;
namespace Shixun {
public sealed class ModelPrice {
 public string provider="",providerName="",model="",api="";
 public double input,output,cacheRead,at;
 public bool cacheReadKnown;
}
public static class ModelPricing {
 static readonly object sync=new object();
 static string catalog="";static double fetched;
 public static List<ModelPrice> Parse(string json,string model){
  var root=Data.Json().Deserialize<Dictionary<string,object>>(json);var result=new List<ModelPrice>();
  if(root==null||String.IsNullOrWhiteSpace(model))return result;
  foreach(var pair in root){var provider=pair.Value as Dictionary<string,object>;object raw;if(provider==null||!provider.TryGetValue("models",out raw))continue;var models=raw as Dictionary<string,object>;if(models==null||!models.TryGetValue(model,out raw))continue;
   var row=raw as Dictionary<string,object>;if(row==null||!row.TryGetValue("cost",out raw))continue;var cost=raw as Dictionary<string,object>;double input,output,cache;if(!Rate(cost,"input",out input)||!Rate(cost,"output",out output))continue;bool known=Rate(cost,"cache_read",out cache);
   // Tiered or multimodal price bands cannot be reduced to one token rate accurately.
   if(cost.ContainsKey("tiers")||cost.ContainsKey("input_audio")||cost.ContainsKey("output_audio"))continue;
   result.Add(new ModelPrice{provider=pair.Key,providerName=provider.ContainsKey("name")?Convert.ToString(provider["name"]):pair.Key,api=provider.ContainsKey("api")?Convert.ToString(provider["api"]):"",model=model,input=input,output=output,cacheRead=cache,cacheReadKnown=known,at=Data.Now()});
  }return result.OrderBy(p=>p.provider,StringComparer.Ordinal).ToList();
 }
 static bool Rate(Dictionary<string,object> value,string key,out double rate){rate=0;object raw;return value!=null&&value.TryGetValue(key,out raw)&&Double.TryParse(Convert.ToString(raw,System.Globalization.CultureInfo.InvariantCulture),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out rate)&&!Double.IsNaN(rate)&&!Double.IsInfinity(rate)&&rate>=0&&rate<=1000000;}
 public static List<ModelPrice> Find(string model,bool refresh=true){if(model==null||model.Length>200)throw new SafeError("请填写确切的目录模型ID。");lock(sync){if(catalog.Length==0||refresh||Data.Now()-fetched>86400000){catalog=Transport.Fetch("https://models.dev/api.json",new RequestProfile(),"","",32*1024*1024,30000).text;fetched=Data.Now();}return Parse(catalog,model);}}
 public static bool Bound(AiSettings s){return s.priceUpdated>0&&s.priceForModel==s.model&&s.priceEndpoint==s.endpoint&&s.priceProvider.Length>0&&s.currency=="USD";}
 public static void Apply(AiSettings s,ModelPrice p){s.inputCost=p.input;s.outputCost=p.output;s.cacheReadCost=p.cacheRead;s.cacheReadKnown=p.cacheReadKnown;s.currency="USD";s.priceUpdated=p.at;s.priceProvider=p.provider;s.priceModel=p.model;s.priceForModel=s.model;s.priceEndpoint=s.endpoint;s.estimateCost=true;}
}
public sealed partial class AiService {
 public static void RecordCost(AiRun run,AiSettings s){if(!run.usageKnown||!s.estimateCost||s.priceUpdated>0&&!ModelPricing.Bound(s))return;run.costKnown=true;run.costCurrency=s.currency;run.costSource=s.priceUpdated>0?"models.dev/"+s.priceProvider+"/"+s.priceModel:"manual";long cached=s.cacheReadKnown?Math.Min(run.inputTokens,run.cachedTokens):0;run.estimatedCost=((run.inputTokens-cached)*s.inputCost+cached*s.cacheReadCost+run.outputTokens*s.outputCost)/1000000d;}
 async System.Threading.Tasks.Task RefreshPricing(){AiSettings snapshot;lock(store.Gate)snapshot=Data.Clone(store.State.ai);if(snapshot==null||!snapshot.estimateCost||!ModelPricing.Bound(snapshot)||Data.Now()-snapshot.priceUpdated<86400000)return;try{var prices=await System.Threading.Tasks.Task.Run(()=>ModelPricing.Find(snapshot.priceModel,false)).ConfigureAwait(false);var price=prices.FirstOrDefault(p=>p.provider==snapshot.priceProvider);if(price==null)return;lock(store.Gate){var live=store.State.ai;if(live.estimateCost&&live.priceUpdated==snapshot.priceUpdated&&live.endpoint==snapshot.endpoint&&live.model==snapshot.model&&live.priceProvider==snapshot.priceProvider&&live.priceModel==snapshot.priceModel){ModelPricing.Apply(live,price);store.Save();}}}catch{/* Offline catalog never blocks reading or AI. Keep timestamp of last successful lookup. */}}
}
}
