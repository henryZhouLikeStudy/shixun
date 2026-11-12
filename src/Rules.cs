// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
namespace Shixun {
public static class Rules {
 public static RuleDocument Parse(string text){if(text.Length>1024*1024)throw new SafeError("规则不可超过 1 MB。");RuleDocument doc;try{doc=Data.Json().Deserialize<RuleDocument>(text);}catch{throw new SafeError("规则 JSON 格式错误。");}if(doc==null||doc.version!=1||doc.categories==null||doc.categories.Count<1||doc.categories.Count>100)throw new SafeError("需要 version: 1 和 1—100 条 categories。");var keys=new HashSet<string>();foreach(var c in doc.categories){if(c==null||!Regex.IsMatch(c.key??"","^[A-Za-z0-9_-]{1,60}$")||!keys.Add(c.key)||String.IsNullOrWhiteSpace(c.name)||c.name.Length>50||!Regex.IsMatch(c.color??"","^#[0-9a-fA-F]{6}$")||c.keywords==null||c.exclude==null||c.keywords.Count>100||c.exclude.Count>100||c.keywords.Concat(c.exclude).Any(k=>k==null||k.Length>120))throw new SafeError("规则字段、颜色或关键词无效。");}return doc;}
 public static void Apply(ClientState state,RuleDocument document,string owner){foreach(var c in document.categories){var existing=state.categories.FirstOrDefault(x=>x.ruleSource==owner&&x.ruleKey==c.key);if(existing==null){existing=new Category{ruleSource=owner,ruleKey=c.key};state.categories.Add(existing);}existing.name=c.name.Trim();existing.color=c.color;existing.keywords=String.Join(",",c.keywords);existing.exclude=String.Join(",",c.exclude);}}
 public static string[] Words(string text){return (text??"").Split(new[]{',','，','\n'},StringSplitOptions.RemoveEmptyEntries).Select(x=>x.Trim()).Where(x=>x.Length>0).ToArray();}
 public static bool Contains(string text,string word){return text.IndexOf(word,StringComparison.OrdinalIgnoreCase)>=0;}
 public static string Classify(ClientState state,Article article){if(article.locked)return article.category;string text=article.title+" "+article.summary;foreach(var c in state.categories){if(Words(c.exclude).Any(w=>Contains(text,w)))continue;bool match=Words(c.keywords).Any(w=>Contains(text,w));if(!match&&c.pattern.Length>0)try{match=Regex.IsMatch(text,c.pattern,RegexOptions.IgnoreCase,TimeSpan.FromMilliseconds(100));}catch{}if(match)return c.id;}return "";}
 public static bool Notify(Preferences p,Article a){if(p.notifyScope!="filtered")return true;var words=Words(p.notifyKeywords);bool word=words.Any(w=>Contains(a.title+" "+a.summary,w)),cat=p.notifyCategories.Contains(a.category);if(words.Length==0&&p.notifyCategories.Count==0)return false;return p.notifyJoin=="all"?(words.Length==0||word)&&(p.notifyCategories.Count==0||cat):word||cat;}
 public static bool Quiet(Preferences p,DateTime local){if(!p.quiet)return false;TimeSpan start,end;if(!TimeSpan.TryParse(p.quietStart,out start)||!TimeSpan.TryParse(p.quietEnd,out end))return true;var now=local.TimeOfDay;return start<=end?now>=start&&now<end:now>=start||now<end;}
}
}
