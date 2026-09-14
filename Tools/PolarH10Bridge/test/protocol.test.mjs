import test from 'node:test';
import assert from 'node:assert/strict';
import { Adapter, regions } from '../protocol.mjs';
const setup = (region='LShoulder', extra={}) => ({ type:'setup', modality:'EXG', sensor_name:'Polar H10 ABC-'+region,
  channels:['raw','filtered','rms'], metadata:{ unit:'microvolts',filter_type:'highpass',filter_Fs:'130',rms_win_size:'26',device_name:'Polar H10 ABC',...extra} });
const sample = (region='LShoulder', rms=42.3) => ({modality:'EXG',sensor_name:'Polar H10 ABC-'+region,channels:['raw','filtered','rms'],sample:[100,-20,rms],timestamp_ms:Date.now()});
test('four regions forward exact RMS and epoch time, independently of raw/filter amplitudes',()=>{
  const a=new Adapter('test');
  for(const region of regions) { a.accept(setup(region)); const input=sample(region); const p=a.accept(input); assert.equal(p.region,region); assert.equal(p.rmsUv,42.3); assert.equal(p.timestampMs,input.timestamp_ms); assert.equal(p.windowMs,200); }
});
test('startup null, negative and nonfinite RMS, missing schema and unsupported locations are rejected',()=>{
  const a=new Adapter('test'); assert.equal(a.accept(sample()),null); a.accept(setup());
  for(const v of [null,undefined,NaN,Infinity,-1,'42.3']) { const p=sample(); p.sample[2]=v; assert.equal(a.accept(p),null); }
  assert.equal(a.accept(setup('Chest')),null); assert.equal(a.accept(sample('Chest')),null);
});
test('metadata changes immediately invalidate wrong window, units or filter',()=>{
  for(const extra of [{rms_win_size:'52'},{filter_Fs:'260'},{filter_type:'lowpass'},{unit:'mV'}]) {
    const a=new Adapter('test'); a.accept(setup()); assert.ok(a.accept(sample())); a.accept(setup('LShoulder',extra)); assert.equal(a.accept(sample()),null);
  }
});
test('RMS selected by channel name, never hardcoded column',()=>{
  const a=new Adapter('test'), s=setup(); s.channels=['rms','raw','filtered']; a.accept(s);
  assert.equal(a.accept({...sample(),channels:s.channels,sample:[57.8,1,2]}).rmsUv,57.8);
  assert.equal(a.accept(sample()),null);
});
