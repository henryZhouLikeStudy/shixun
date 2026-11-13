// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace Shixun {
public sealed partial class ReaderApp {
 AiService aiService;
 readonly HashSet<string> aiQueued=new HashSet<string>();
 void QueueAi(System.Collections.Generic.List<string> ids){if(aiService==null)return;lock(store.Gate){if(store.State.ai==null||!store.State.ai.enabled||!store.State.ai.autoNew)return;}var batch=new List<string>();foreach(string id in ids.Where(x=>!pendingAiNotices.Contains(x)))lock(aiQueued){if(aiQueued.Add(id))batch.Add(id);}Task.Run(async()=>{foreach(string id in batch){try{await aiService.Analyze(id,false,false,"auto").ConfigureAwait(false);}catch{store.Log("AI automatic task not completed");}finally{lock(aiQueued)aiQueued.Remove(id);UI(Push);}}});}
 async Task<object> AiAction(string action,Dictionary<string,object> data){string id=S(data,"id");
 if(action=="saveAiNotice"){lock(store.Gate){store.State.ai.notifyAfter=S(data,"enabled").Equals("true",StringComparison.OrdinalIgnoreCase);store.Save();}return true;}
 if(action=="saveAi"){object raw;var values=data.TryGetValue("settings",out raw)?raw:data;var value=ConvertData<AiSettings>(values);string token=S(data,"apiKey");if(token.Length>32000||token.IndexOfAny(new[]{'\r','\n'})>=0)throw new SafeError("API Key 格式无效。");AiService.Validate(value);AiSettings old;lock(store.Gate)old=store.State.ai??new AiSettings();if(value.enabled&&(!old.enabled||old.endpoint!=value.endpoint||!old.allowPrivate&&value.allowPrivate)){string text="启用后会把文章标题与截断正文发送到你填写的AI接口，可能产生服务商费用。不会发送订阅访问码、Cookie、图片或私有订阅地址。\n\n"+(value.allowPrivate?"当前选择：允许发送私有订阅正文。":"当前选择：只处理公开订阅正文。")+"\n\n推荐度是模型主观评估，不是价格真实性验证。是否保存并启用？";if(MessageBox.Show(window,text,"AI发送范围与费用",MessageBoxButtons.OKCancel,MessageBoxIcon.Information)!=DialogResult.OK)return false;}lock(store.Gate){RequestProfile profile=null,saved;if(token.Length>0){profile=old.endpoint==value.endpoint&&store.State.credentials.TryGetValue(AiService.CredentialKey,out saved)?Data.Clone(saved):new RequestProfile();profile.method="POST";profile.mode="bearer";profile.token=token;profile.body="";profile.fields.RemoveAll(f=>f.place=="header"&&f.name.Equals("Authorization",StringComparison.OrdinalIgnoreCase));Transport.ValidateProfile(profile);}store.State.ai=value;if(old.endpoint!=value.endpoint)store.State.credentials.Remove(AiService.CredentialKey);if(profile!=null)store.State.credentials[AiService.CredentialKey]=profile;store.Save();}return true;}
 if(action=="aiCredentials"){lock(store.Gate){RequestProfile saved;store.State.credentials.TryGetValue(AiService.CredentialKey,out saved);var profile=saved==null?new RequestProfile{method="POST",mode="bearer"}:Data.Clone(saved);profile.body="";using(var dialog=new CredentialDialog(profile,"AI接口（请求体由应用生成，仅配置认证参数）")){if(dialog.ShowDialog(window)==DialogResult.OK){dialog.Result.body="";dialog.Result.method="POST";if(dialog.Result.fields.Any(f=>f.place=="form"))throw new SafeError("AI接口使用JSON请求体，认证请使用请求头、查询或路径参数。");if(dialog.Result.HasSecrets())store.State.credentials[AiService.CredentialKey]=dialog.Result;else store.State.credentials.Remove(AiService.CredentialKey);store.Save();}}return true;}}
 if(action=="analyze"){return await aiService.Analyze(id,S(data,"force").Equals("true",StringComparison.OrdinalIgnoreCase));}
 if(action=="testAi"){return await aiService.Analyze("",true,true);}
 if(action=="clearAi"){lock(store.Gate){var a=store.State.articles.FirstOrDefault(x=>x.id==id);if(a!=null){a.ai=null;a.aiError="";if(!a.locked)a.category=Rules.Classify(store.State,a);store.Save();}}return true;}
 throw new SafeError("AI操作无效。");}
}
}
