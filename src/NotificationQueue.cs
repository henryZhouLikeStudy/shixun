// SPDX-License-Identifier: GPL-3.0-only
using System.Collections.Generic;
namespace Shixun {
public sealed class NotificationQueue {
 readonly Queue<string> waiting=new Queue<string>();
 readonly HashSet<string> active=new HashSet<string>();
 public int Count{get{return waiting.Count;}}
 public void Add(IEnumerable<Article> articles){foreach(var article in articles)if(!string.IsNullOrEmpty(article.id)&&active.Add(article.id))waiting.Enqueue(article.id);}
 public string Take(){return waiting.Dequeue();}
 public void Complete(string id){active.Remove(id);}
}
}
