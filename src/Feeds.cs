// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
namespace Shixun {
public sealed class FeedResult {public string title="",format="";public List<Article> articles=new List<Article>();}
public static class Feeds {
 public static string ShortTitle(string title){string text=Plain(title);text=Regex.Replace(text,"\\[图片\\]|网页链接","");text=Regex.Replace(text,"\\s+"," ").Trim();return text.Length>44?text.Substring(0,44)+"…":text.Length>0?text:"无标题内容";}
 public static Uri Base(XElement element,Uri fallback){var result=fallback;foreach(var ancestor in element.AncestorsAndSelf().Reverse()){var attribute=ancestor.Attribute(XNamespace.Xml+"base");Uri resolved;if(attribute!=null&&Uri.TryCreate(result,attribute.Value,out resolved))result=resolved;}return result;}
 public static XDocument Xml(string text){using(var reader=XmlReader.Create(new StringReader(text.TrimStart('\uFEFF')),new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=5*1024*1024}))return XDocument.Load(reader);}
 static string Child(XElement e,string name){var v=e.Elements().FirstOrDefault(x=>x.Name.LocalName.Equals(name,StringComparison.OrdinalIgnoreCase));return v==null?"":v.Value;}
 public static string Plain(string html){return Regex.Replace(WebUtility.HtmlDecode(Regex.Replace(html??"","<[^>]*>"," ")),"\\s+"," ").Trim();}
 public static string Resolve(string value,Uri baseUri){Uri uri;if(Uri.TryCreate(baseUri,WebUtility.HtmlDecode(value??""),out uri)&&(uri.Scheme=="http"||uri.Scheme=="https")&&String.IsNullOrEmpty(uri.UserInfo))return uri.AbsoluteUri;return "";}
 public static string Hash(string value){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-","").ToLowerInvariant();}
 public static FeedResult Parse(string text,Source source,Uri address){XDocument doc;try{doc=Xml(text);}catch{throw new SafeError("订阅 XML 无效或包含被禁止的 DTD。");}if(doc.Root==null)throw new SafeError("订阅内容为空。");bool atom=doc.Root.Name.LocalName=="feed";bool rss=doc.Root.Name.LocalName=="rss"||doc.Root.Name.LocalName=="RDF";if(!atom&&!rss){if(doc.Root.Name.LocalName=="html")throw new SafeError("这不是订阅文件；请添加 RSS/Atom 地址，或使用网站发现。");throw new SafeError("目前支持 RSS 2.0、RSS 1.0 和 Atom 1.0。");}var root=atom?doc.Root:doc.Root.Elements().FirstOrDefault(x=>x.Name.LocalName=="channel")??doc.Root;var result=new FeedResult{title=Plain(Child(root,"title")),format=atom?"Atom":"RSS"};var items=doc.Descendants().Where(x=>x.Name.LocalName==(atom?"entry":"item")).Take(1000);
 foreach(var item in items){Uri basis=Base(item,address);string link=atom?item.Elements().Where(x=>x.Name.LocalName=="link"&&((string)x.Attribute("rel")==null||(string)x.Attribute("rel")=="alternate")).Select(x=>(string)x.Attribute("href")).FirstOrDefault()??"":Child(item,"link");link=Resolve(link,basis);string html=Child(item,atom?"content":"encoded");if(String.IsNullOrWhiteSpace(html))html=Child(item,atom?"summary":"description");if(atom){var content=item.Elements().FirstOrDefault(x=>x.Name.LocalName=="content");if(content!=null&&(string)content.Attribute("type")=="xhtml")html=String.Join("",content.Nodes().Select(n=>n.ToString()));}string plain=Plain(html),title=Plain(Child(item,"title")),original=title;if(String.IsNullOrWhiteSpace(title)||title==result.title)title=plain.Length>100?plain.Substring(0,100)+"…":plain;if(title.Length==0)title="无标题内容";string guid=Child(item,atom?"id":"guid");if(guid.Length==0)guid=link.Length>0?link:Hash(title+plain);double now=Data.Now(),time=now;string published=Child(item,atom?"published":"pubDate");if(published.Length==0)published=Child(item,atom?"updated":"date");DateTimeOffset date;bool valid=DateTimeOffset.TryParse(published,System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.AssumeUniversal,out date);if(valid){time=(date-new DateTimeOffset(1970,1,1,0,0,0,TimeSpan.Zero)).TotalMilliseconds;if(time>now+300000){time=now;valid=false;}}
 var article=new Article{id=Hash(source.id+"\n"+guid),guid=Data.Limit(guid,4000),source=source.id,title=Data.Limit(title,1000),originalTitle=Data.Limit(original,1000),html=Data.Limit(html,500000),summary=Data.Limit(plain,3000),link=link,author=Data.Limit(Child(item,atom?"author":"creator"),300),time=time,firstSeen=now,dateOrigin=valid?"published":"firstSeen"};
 foreach(var media in item.Descendants().Where(x=>x.Name.LocalName=="thumbnail"||x.Name.LocalName=="enclosure"||(x.Name.LocalName=="content"&&x.Attribute("url")!=null))){string type=(string)media.Attribute("type")??"";if(type.Length>0&&!type.StartsWith("image/"))continue;string image=Resolve((string)media.Attribute("url"),basis);if(image.Length>0)article.images.Add(image);}
 foreach(Match match in Regex.Matches(html,"<img\\b[^>]*?\\bsrc\\s*=\\s*[\"']([^\"']+)[\"']",RegexOptions.IgnoreCase)){string image=Resolve(match.Groups[1].Value,basis);if(image.Length>0&&!Regex.IsMatch(image,"timeline_card|emoticon|url-icon|default_avatar|/upload/|/dpool/",RegexOptions.IgnoreCase))article.images.Add(image);}
 article.images=article.images.Distinct().Take(20).ToList();result.articles.Add(article);}
 return result;
 }
 public static List<string> Discover(string html,Uri address){var urls=new List<string>();foreach(Match tag in Regex.Matches(html,"<link\\b[^>]+>",RegexOptions.IgnoreCase)){if(!Regex.IsMatch(tag.Value,"(?:application/(?:rss|atom)\\+xml)",RegexOptions.IgnoreCase))continue;var href=Regex.Match(tag.Value,"href\\s*=\\s*[\"']([^\"']+)[\"']",RegexOptions.IgnoreCase);string url=Resolve(href.Groups[1].Value,address);if(url.Length>0)urls.Add(url);}return urls.Distinct().Take(15).ToList();}
}
}
