// Adapts upstream LslBridge packets. No filtering, RMS calculation or normalization.
export const regions = ['LShoulder', 'RShoulder', 'MiddleSpine', 'LowerSpine'];
export function regionFor(name) {
  return typeof name === 'string' ? regions.find(r => name.endsWith('-' + r)) : undefined;
}
export class Adapter {
  constructor(sessionId) { this.sessionId = sessionId; this.streams = new Map(); this.sequence = 0; }
  accept(p) {
    if (!p || p.modality !== 'EXG' || typeof p.sensor_name !== 'string') return null;
    const region = regionFor(p.sensor_name);
    if (!region) return null;
    if (p.type === 'setup') {
      const m = p.metadata || {};
      const valid = m.unit === 'microvolts' && m.filter_type === 'highpass'
        && Number(m.filter_Fs) === 130 && Number(m.rms_win_size) === 26
        && Array.isArray(p.channels) && p.channels.filter(c => c === 'rms').length === 1;
      if (valid) this.streams.set(p.sensor_name, { channels: p.channels, device: m.device_name || p.sensor_name });
      else this.streams.delete(p.sensor_name);
      return null;
    }
    const stream = this.streams.get(p.sensor_name);
    if (!stream || !Array.isArray(p.channels) || !Array.isArray(p.sample)
        || JSON.stringify(p.channels) !== JSON.stringify(stream.channels)
        || p.sample.length !== p.channels.length) return null;
    const rms = p.sample[p.channels.indexOf('rms')];
    if (typeof rms !== 'number' || !Number.isFinite(rms) || rms < 0
        || !Number.isFinite(p.timestamp_ms) || p.timestamp_ms <= 0) return null;
    return { version: 1, type: 'polar_rms', sessionId: this.sessionId,
      sequence: ++this.sequence, region, sensorName: p.sensor_name, deviceName: stream.device,
      rmsUv: rms, windowMs: 200, unit: 'uV', timestampMs: p.timestamp_ms };
  }
}
