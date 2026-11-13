// SPDX-License-Identifier: GPL-3.0-only
const path=require('node:path'),assert=require('node:assert/strict');
const {chromium}=require(process.env.SHIXUN_PLAYWRIGHT||path.join(require('node:os').homedir(),'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
(async()=>{const browser=await chromium.launch({headless:true,executablePath:process.env.SHIXUN_CHROMIUM});try{
 const page=await browser.newPage();await page.addInitScript(()=>{window.messages=[];window.calls=[];window.chrome={webview:{addEventListener:(_,fn)=>messages.push(fn),postMessage:m=>{calls.push(m);if(m.request)setTimeout(()=>messages.forEach(fn=>fn({data:{type:'reply',request:m.request,ok:true,result:true}})),0)}}};window.send=m=>messages.forEach(fn=>fn({data:m}));});
 await page.goto(require('node:url').pathToFileURL(path.join(__dirname,'../ui/index.html')).href);
 await page.evaluate(()=>{send({type:'state',settings:{retention:1},sources:[],articles:[{id:'notice',source:'s',title:'通知测试文章',summary:'正文',html:'',images:[],read:false,time:Date.now()}],categories:[],notices:[],ruleSources:[]});active='stars';document.querySelector('#search').value='不匹配';send({type:'select',id:'notice'});});
 await page.waitForFunction(()=>calls.some(m=>m.action==='article'&&m.payload.id==='notice'&&m.payload.operation==='read'));
 assert.equal(await page.evaluate(()=>active),'all');assert.equal(await page.locator('.article.selected').count(),1);
 assert.match(await page.locator('#detail').textContent(),/通知测试文章/);
 await page.evaluate(()=>{state.articles[0].read=true;calls.length=0;send({type:'select',id:'notice'});});
 assert.equal(await page.evaluate(()=>calls.filter(m=>m.action==='article').length),0);
 await page.evaluate(()=>{state.articles[0].read=false;active='unread';unreadSession=new Set(['notice']);send({type:'select',id:'notice'});state.articles[0].read=true;render();});
 assert.equal(await page.locator('.article.selected').count(),1);
 console.log('PASS notification selection marks unread article, reveals filtered article, retains unread session and skips already-read writes');
}finally{await browser.close()}})().catch(e=>{console.error(e);process.exitCode=1});
