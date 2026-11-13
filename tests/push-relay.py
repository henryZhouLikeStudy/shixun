import importlib.util,os,tempfile,threading,json,urllib.request,urllib.error,time,unittest
from pathlib import Path
from datetime import datetime,timezone
os.environ['RELAY_UIDS']='100'
spec=importlib.util.spec_from_file_location('relay',Path(__file__).parents[1]/'scripts/push-relay.py');r=importlib.util.module_from_spec(spec);spec.loader.exec_module(r)
class Tests(unittest.TestCase):
 def test_wakeup(self):
  with tempfile.TemporaryDirectory() as folder:
   r.DATA=Path(folder);os.environ['READER_TOKEN']='fixture-reader';os.environ['RELAY_PUSH_TOKEN']='fixture-push'
   server=r.ThreadingHTTPServer(('127.0.0.1',0),r.Handler);threading.Thread(target=server.serve_forever,daemon=True).start()
   def call(path,token='fixture-reader',data=None):
    req=urllib.request.Request('http://127.0.0.1:'+str(server.server_port)+path,headers={'Authorization':'Bearer '+token},data=json.dumps(data).encode() if data else None)
    with urllib.request.urlopen(req,timeout=5) as res:return res.status,res.read()
   def push(title):
    now=datetime.now(timezone.utc).isoformat();return call('/push','fixture-push',{'updatedAt':now,'feeds':{'100':{'updatedAt':now,'xml':'<rss><channel><item><title>'+title+'</title></item></channel></rss>'}}})
   try:
    push('first');version=json.loads(call('/events/weibo/user/100')[1])['version'];self.assertEqual(len(version),64)
    results=[]
    waiter=threading.Thread(target=lambda:results.append(json.loads(call('/events/weibo/user/100?since='+version)[1])['version']));waiter.start();time.sleep(.15);self.assertTrue(waiter.is_alive())
    push('first');time.sleep(.15);self.assertTrue(waiter.is_alive())
    push('second');waiter.join(3);self.assertFalse(waiter.is_alive());self.assertNotEqual(results[0],version)
    with self.assertRaises(urllib.error.HTTPError) as error:call('/events/weibo/user/100','invalid')
    self.assertEqual(error.exception.code,401)
   finally:server.shutdown();server.server_close()
if __name__=='__main__':unittest.main()
