import test from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { transform } from '../../PolarH10Visualizer/node_modules/esbuild/lib/main.js';

test('upstream transport cache stays bounded across fresh per-sample Arrow schemas and re-registers metadata changes',async()=>{
  const source=await readFile(new URL('../../PolarH10Visualizer/src/LslBridge.ts',import.meta.url),'utf8');
  const js=await transform(source.replace('export class','class'),{loader:'ts'});
  class Socket {static OPEN=1;readyState=1;sent=[];send(s){this.sent.push(JSON.parse(s));}close(){}}
  const Bridge=new Function('WebSocket','console','setTimeout',js.code+';return LslBridge;')(Socket,{log(){},debug(){}},()=>0);
  const bridge=new Bridge(); bridge.ws.onopen();
  const schema=(filter='highpass')=>({fields:['epoch_timestamp_ms','raw','filtered','rms'].map(name=>({name})),metadata:new Map([['sensor_name','H10-LShoulder'],['modality','EXG'],['filter_type',filter]])});
  for(let i=0;i<10000;i++)bridge.streamData(schema(),{epoch_timestamp_ms:Date.now(),raw:100,filtered:-2,rms:42.3});
  assert.equal(bridge.activeSchemas.size,1);
  assert.equal(bridge.ws.sent.filter(p=>p.type==='setup').length,1);
  bridge.streamData(schema('lowpass'),{rms:1});assert.equal(bridge.ws.sent.filter(p=>p.type==='setup').length,2);
  bridge.connect();bridge.ws.onopen();assert.equal(bridge.ws.sent[0].type,'setup');
});
