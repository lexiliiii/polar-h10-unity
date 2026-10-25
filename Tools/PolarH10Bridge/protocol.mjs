// Preserve the upstream 200 ms RMS display while deriving calibration/decision
// windows directly from filtered 130 Hz samples. Never average 200 ms RMS values.
export const documentRegions = ['LShoulder', 'RShoulder', 'LLowerSpine', 'RLowerSpine',
  'LUpperBack', 'RUpperBack', 'LFrontShoulder', 'RFrontShoulder', 'LLowerArm', 'RLowerArm', 'LThigh', 'RThigh'];
export const regions = ['LShoulder', 'RShoulder', 'MiddleSpine', 'LowerSpine', ...documentRegions.slice(2)];
export const sampleRateHz = 130;
export const maximumSampleGapMs = 1000 / sampleRateHz + 1; // 1 ms timestamp rounding allowance, not an effort threshold.
export function regionFor(name) {
  return typeof name === 'string' ? regions.find(r => name.endsWith('-' + r)) : undefined;
}
const finite = value => typeof value === 'number' && Number.isFinite(value);
const stableMetadata = metadata => JSON.stringify(Object.entries(metadata).sort(([a], [b]) => a.localeCompare(b)));
class RmsWindow {
  constructor(size) { this.size = size; this.values = new Float64Array(size); this.count = this.index = this.sum = 0; }
  push(value) {
    const square = (value / this.size) * value;
    if (!Number.isFinite(square)) return null;
    this.sum += square;
    if (this.count === this.size) this.sum -= this.values[this.index]; else this.count++;
    this.values[this.index] = square; this.index = (this.index + 1) % this.size;
    return this.count === this.size ? Math.sqrt(Math.max(0, this.sum)) : null;
  }
}
export class Adapter {
  constructor(sessionId) {
    this.sessionId = sessionId; this.streams = new Map(); this.sequence = 0;
    this.nextSegment = 0; this.deviceLocations = new Map();
  }
  reset(stream) {
    stream.one = new RmsWindow(130); stream.two = new RmsWindow(260);
    stream.lastAt = null; stream.count = 0; stream.segment = ++this.nextSegment;
    stream.pendingGap = true; stream.lastBlanked = null;
  }
  accept(p) {
    if (!p || p.modality !== 'EXG' || typeof p.sensor_name !== 'string') return null;
    const region = regionFor(p.sensor_name);
    if (!region) return null;
    if (p.type === 'setup') {
      const m = p.metadata || {};
      const valid = m.unit === 'microvolts' && m.filter_type === 'highpass'
        && Number(m.filter_Fs) === 130 && Number(m.rms_win_size) === 26
        && Array.isArray(p.channels) && p.channels.filter(c => c === 'rms').length === 1
        && p.channels.every(c => typeof c === 'string') && new Set(p.channels).size === p.channels.length;
      if (!valid) { this.streams.delete(p.sensor_name); return null; }
      const signature = stableMetadata(m) + JSON.stringify(p.channels);
      if (this.streams.get(p.sensor_name)?.signature === signature) return null;
      const stream = { channels: [...p.channels], device: m.device_name || p.sensor_name, signature,
        // Only an upstream producer that actually performed this processing may
        // assert the hook. The included browser supplies no R-wave detector.
        ecgBlanked: (m.ecg_blanked === true || m.ecg_blanked === 'true') && Number(m.ecg_blanking_window_ms) === 100 };
      this.reset(stream); this.streams.set(p.sensor_name, stream);
      return null;
    }
    const stream = this.streams.get(p.sensor_name);
    if (!stream || !Array.isArray(p.channels) || !Array.isArray(p.sample)
        || JSON.stringify(p.channels) !== JSON.stringify(stream.channels)
        || p.sample.length !== p.channels.length) {
      if (stream) this.reset(stream);
      return null;
    }
    const rms = p.sample[p.channels.indexOf('rms')];
    if (!finite(rms) || rms < 0 || !finite(p.timestamp_ms) || p.timestamp_ms <= 0) {
      this.reset(stream); return null;
    }
    if (stream.lastAt !== null && p.timestamp_ms <= stream.lastAt) return null;
    const previousLocation = this.deviceLocations.get(stream.device);
    if (previousLocation && previousLocation !== p.sensor_name) {
      const previousStream = this.streams.get(previousLocation);
      if (previousStream) this.reset(previousStream);
      this.reset(stream);
    }
    this.deviceLocations.set(stream.device, p.sensor_name);
    const ecgBlanked = Object.hasOwn(p, 'ecgBlanked')
      ? p.ecgBlanked === true && p.ecgBlankingWindowMs === 100 : stream.ecgBlanked;
    if ((stream.lastAt !== null && p.timestamp_ms - stream.lastAt > maximumSampleGapMs)
        || (stream.lastBlanked !== null && stream.lastBlanked !== ecgBlanked)) this.reset(stream);
    const filtered = p.sample[p.channels.indexOf('filtered')];
    const hasFilteredSample = finite(filtered) && Number.isFinite(filtered * filtered);
    let rms1s = null, rms2s = null;
    if (hasFilteredSample) {
      stream.count++;
      rms1s = stream.one.push(filtered); rms2s = stream.two.push(filtered);
    } else this.reset(stream);
    stream.lastAt = p.timestamp_ms; stream.lastBlanked = ecgBlanked;
    const mapped = documentRegions.includes(region);
    const packet = { version: 1, type: 'polar_rms', sessionId: this.sessionId,
      sequence: ++this.sequence, region, sensorName: p.sensor_name, deviceName: stream.device,
      rmsUv: rms, windowMs: 200, unit: 'uV', timestampMs: p.timestamp_ms,
      filteredUv: hasFilteredSample ? filtered : 0, hasFilteredSample,
      rms1sUv: rms1s ?? 0, rms2sUv: rms2s ?? 0,
      hasCalibrationRms: mapped && rms1s !== null, hasDecisionRms: mapped && rms2s !== null,
      calibrationWindowMs: 1000, decisionWindowMs: 2000, sampleRateHz, windowSampleCount: Math.min(stream.count, 260),
      ecgBlanked, ecgBlankingWindowMs: ecgBlanked ? 100 : 0,
      signalSegment: stream.segment, signalGap: stream.pendingGap };
    stream.pendingGap = false;
    return packet;
  }
}
