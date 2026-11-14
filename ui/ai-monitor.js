// SPDX-License-Identifier: GPL-3.0-only
function refreshAiMonitor(){
 const root=document.querySelector('#ai-monitor');if(!root)return;const scroller=document.querySelector('#settingsContent'),scroll=scroller.scrollTop;
 const history=state.aiHistory||[],usage=state.aiUsage||{},labels={queued:'排队中',running:'处理中',success:'成功',failed:'失败',blocked:'已拦截',cached:'缓存命中',superseded:'结果已过期'};
 const rows=history.filter(x=>new Date(x.at).toDateString()===new Date().toDateString()),known=rows.filter(x=>x.usageKnown);
 root.replaceChildren();renderAiMetrics(root,history);root.append(heading('处理记录'));
 if(state.ai?.enabled&&!state.ai.allowPrivate&&state.sources.some(s=>s.privateSource||s.configured))root.append(note('私有订阅正文尚未授权：这些来源不会自动处理。请按需开启“允许发送私有订阅正文”，再处理所需文章。'));
 root.append(note('今日记录 '+rows.length+' 条 · 成功 '+rows.filter(x=>x.status==='success').length+' · 失败/拦截 '+rows.filter(x=>['failed','blocked'].includes(x.status)).length+' · 已知 Token '+known.reduce((n,x)=>n+x.totalTokens,0)),note('累计服务商返回用量：输入 '+(usage.input||0)+' / 输出 '+(usage.output||0)+' / 总计 '+(usage.total||0)+' Token。未返回 usage 的请求显示“未知”；缓存与推理 Token 为子项，不重复累加。费用仅在返回用量且价格可匹配时估算，与账单可能不同。'));
 const filter=el('select');filter.setAttribute('aria-label','AI记录筛选');for(const [value,text]of [['','全部记录'],['failed','失败'],['blocked','已拦截'],['success','成功'],['cached','缓存命中']]){const o=el('option','',text);o.value=value;filter.append(o)}
 filter.value=window.aiHistoryFilter||'';filter.onchange=()=>{window.aiHistoryFilter=filter.value;refreshAiMonitor()};root.append(filter);
 const list=el('div','ai-history');for(const run of history.filter(x=>!filter.value||x.status===filter.value).slice(0,100)){
  const row=el('div','ai-history-row');row.append(el('strong','',(labels[run.status]||run.status)+' · '+(run.title||'AI请求')),el('small','',date(run.at)+' · '+(run.kind==='digest'?'今日总结':run.kind==='test'?'连接测试':run.kind==='notification'?'通知处理':run.kind==='auto'?'自动处理':'手动处理')+' · '+run.model+' · '+((run.elapsedMs||0)/1000).toFixed(1)+'秒'),el('div','',run.usageKnown?'输入 '+run.inputTokens+' / 输出 '+run.outputTokens+' / 总计 '+run.totalTokens+' · 缓存 '+run.cachedTokens+' · 推理 '+run.reasoningTokens:'Token：'+(['cached','blocked','queued'].includes(run.status)?'未调用接口':'未知')));
  if(run.contextCount)row.append(el('small','','参考相关旧文 '+run.contextCount+' 篇 · 历史输入 '+run.contextChars+' 字符（含结构与说明）'));
  if(run.error)row.append(el('p','ai-error',run.error));if(run.article&&state.articles.some(a=>a.id===run.article))row.append(btn('查看文章','subtle',()=>{selected=run.article;document.querySelector('#settings').close();render();showDetail();document.querySelector('#detail').classList.add('mobile-open')}));list.append(row)
 }if(!list.children.length)list.append(note('暂无处理记录。连接测试只处理虚构示例，历史文章需手动处理。'));root.append(list);scroller.scrollTop=scroll
}
