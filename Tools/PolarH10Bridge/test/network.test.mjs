import test from 'node:test';
import assert from 'node:assert/strict';
import { spawn } from 'node:child_process';
import { once } from 'node:events';
import dgram from 'node:dgram';
import { WebSocket } from 'ws';

test('real browser-format WebSocket -> localhost UDP, remap clearing and HTTP page', {timeout:15000}, async()=>{
  const udp=dgram.createSocket('udp4');
  udp.bind(0,'127.0.0.1'); await once(udp,'listening');
  const server=spawn(process.execPath,[new URL('../server.mjs',import.meta.url).pathname.replace(/^\/([A-Za-z]:)/,'$1')],{env:{...process.env,POLAR_UDP_PORT:String(udp.address().port),POLAR_WS_PORT:'18765',POLAR_HTTP_PORT:'18080'},stdio:['ignore','pipe','pipe']});
  const incoming=[]; udp.on('message',b=>incoming.push(JSON.parse(b.toString())));
  let ws;
  try {
    await new Promise((resolve,reject)=>{
      let log=''; const timer=setTimeout(()=>reject(new Error('Server did not start: '+log)),7000);
      server.stdout.on('data',d=>{log+=d;if(log.includes('Open http')) {clearTimeout(timer);resolve();}});
      server.stderr.on('data',d=>{log+=d;});
      server.on('exit',code=>{clearTimeout(timer);reject(new Error('Server exit '+code+': '+log));});
    });
    ws=new WebSocket('ws://127.0.0.1:18765'); await once(ws,'open');
    const channels=['raw','filtered','rms'];
    for(const [region,value] of [['LShoulder',42.3],['RShoulder',31.7],['MiddleSpine',57.8],['LowerSpine',23.4]]) {
      const sensor_name='Polar H10 '+region+'-'+region;
      ws.send(JSON.stringify({type:'setup',modality:'EXG',sensor_name,channels,metadata:{unit:'microvolts',filter_type:'highpass',filter_Fs:'130',rms_win_size:'26',device_name:'Polar H10 '+region}}));
      ws.send(JSON.stringify({modality:'EXG',sensor_name,channels,sample:[100,-50,value],timestamp_ms:Date.now()}));
    }
    await new Promise(resolve=>setTimeout(resolve,150));
    assert.deepEqual(incoming.map(p=>p.rmsUv),[42.3,31.7,57.8,23.4]);
    const health=await (await fetch('http://127.0.0.1:18080/health')).json(); assert.equal(health.forwarded,4);
    const page=await fetch('http://127.0.0.1:18080/'); assert.equal(page.status,200); assert.match(await page.text(),/html/i);
    await new Promise(resolve=>setTimeout(resolve,2100)); // Let the previous target owner expire.
    const sensor_name='Polar H10 LShoulder-LowerSpine';
    ws.send(JSON.stringify({type:'setup',modality:'EXG',sensor_name,channels,metadata:{unit:'microvolts',filter_type:'highpass',filter_Fs:'130',rms_win_size:'26',device_name:'Polar H10 LShoulder'}}));
    ws.send(JSON.stringify({modality:'EXG',sensor_name,channels,sample:[100,-50,19.1],timestamp_ms:Date.now()}));
    await new Promise(resolve=>setTimeout(resolve,100));
    assert.equal(incoming[4].type,'polar_clear'); assert.equal(incoming[4].region,'LShoulder');
    assert.equal(incoming[5].region,'LowerSpine'); assert.equal(incoming[5].rmsUv,19.1);
  } finally { if(ws) ws.terminate(); server.kill(); udp.close(); }
});
