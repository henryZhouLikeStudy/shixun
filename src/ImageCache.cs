// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace Shixun {
public static class ImageCache {
 sealed class Item {public byte[] bytes;public double at;}
 static readonly object gate=new object();
 static readonly SemaphoreSlim slots=new SemaphoreSlim(4);
 static readonly Dictionary<string,Item> cache=new Dictionary<string,Item>();
 static readonly Dictionary<string,Task<byte[]>> jobs=new Dictionary<string,Task<byte[]>>();
 public static Task<byte[]> Get(string key,Func<Task<byte[]>> fetch){lock(gate){Item cached;if(cache.TryGetValue(key,out cached)&&cached.at>Data.Now()-60000)return Task.FromResult(cached.bytes);Task<byte[]> pending;if(jobs.TryGetValue(key,out pending))return pending;var job=Task.Run(()=>Load(key,fetch));jobs[key]=job;return job;}}
 public static void ClearExpired(){lock(gate)foreach(var old in cache.Where(x=>x.Value.at<Data.Now()-60000).Select(x=>x.Key).ToArray())cache.Remove(old);}
 static async Task<byte[]> Load(string key,Func<Task<byte[]>> fetch){await slots.WaitAsync().ConfigureAwait(false);try{byte[] bytes=await fetch().ConfigureAwait(false);lock(gate){foreach(var old in cache.Where(x=>x.Value.at<Data.Now()-60000).Select(x=>x.Key).ToArray())cache.Remove(old);cache[key]=new Item{bytes=bytes,at=Data.Now()};while(cache.Values.Sum(x=>(long)x.bytes.Length)>32*1024*1024&&cache.Count>1){var oldest=cache.OrderBy(x=>x.Value.at).First().Key;cache.Remove(oldest);}}return bytes;}finally{lock(gate)jobs.Remove(key);slots.Release();}}
}
}
