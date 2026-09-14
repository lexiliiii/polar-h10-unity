import test from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { transform } from '../../PolarH10Visualizer/node_modules/esbuild/lib/main.js';
import fili from '../../PolarH10Visualizer/node_modules/fili/dist/fili.js';
import { Adapter } from '../protocol.mjs';

test('actual upstream callback: 26-sample warmup, rolling RMS and exact adapter output',async()=>{
  const source=await readFile(new URL('../../PolarH10Visualizer/src/PolarH10VisualizerRow.ts',import.meta.url),'utf8');
  const fragment=source.slice(source.indexOf('  newECGCallback ='),source.indexOf('  newACCCallback ='));
  const js=await transform(fragment.replace('newECGCallback = (data: PolarH10Data) =>','function callback(data: PolarH10Data)').replace(/;\s*$/,''),{loader:'ts'});
  const out=[];
  const callback=new Function('window','setTimeout',js.code+';return callback;')({lslBridge:{streamData:(_,p)=>out.push(p)}},fn=>fn());
  const filter=new fili.IirFilter(new fili.CalcCascades().highpass({order:4,characteristic:'butterworth',Fs:130,Fc:25,preGain:false}));
  const ts={append:()=>{}};
  const state={ecg_ts:ts,ecg_rms_ts:ts,ecg_filter_ts:ts,ecg_filter_iir:filter,ecg_rms_window_size:26,ecg_mss_win:new Float64Array(200),ecg_mss:0,ecg_mss_win_i:0,ecg_buf_head:0,getEcgSchema:()=>null};
  const samples=Int32Array.from({length:100},(_,i)=>Math.round(50*Math.sin(i*1.9)+200));
  callback.call(state,{samples,epoch_timestamps_ms:Float64Array.from(samples,(_,i)=>Date.now()+i*1000/130),prev_sample_timestamp_ms:1,event_time_offset_ms:0});
  assert.equal(out.length,100);
  for(let i=0;i<25;i++) assert.ok(Number.isNaN(out[i].rms));
  for(let i=25;i<100;i++) {
    const expected=Math.sqrt(out.slice(i-25,i+1).reduce((sum,p)=>sum+p.filtered*p.filtered,0)/26);
    assert.ok(Math.abs(out[i].rms-expected)<1e-10);
  }
  const a=new Adapter('test'); const sensor_name='Polar H10 ABC-LShoulder',channels=['raw','filtered','rms'];
  a.accept({type:'setup',sensor_name,modality:'EXG',channels,metadata:{unit:'microvolts',filter_type:'highpass',filter_Fs:'130',rms_win_size:'26'}});
  for(const p of out.slice(25)) assert.equal(a.accept({sensor_name,modality:'EXG',channels,sample:[p.raw,p.filtered,p.rms],timestamp_ms:p.epoch_timestamp_ms}).rmsUv,p.rms);
});
