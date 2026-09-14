import { WebSocketServer } from 'ws';
import dgram from 'node:dgram';
import http from 'node:http';
import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { randomUUID } from 'node:crypto';
import { Adapter } from './protocol.mjs';

const here = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(here, '../PolarH10Visualizer/build');
const udpPort = Number(process.env.POLAR_UDP_PORT || 9050);
const wsPort = Number(process.env.POLAR_WS_PORT || 8765);
const httpPort = Number(process.env.POLAR_HTTP_PORT || 8080);
const udp = dgram.createSocket('udp4');
udp.on('error', e => console.error('UDP:', e.message));
const wss = new WebSocketServer({ host: '127.0.0.1', port: wsPort, maxPayload: 16384 });
// Bind to one producer while connected: avoid accidentally combining browser sessions.
let producer = null;
let forwarded = 0;
wss.on('connection', ws => {
  if (producer) { ws.close(1013, 'One acquisition browser at a time'); return; }
  producer = ws;
  const adapter = new Adapter(randomUUID());
  const owners = new Map();
  console.log('Acquisition browser connected. Waiting for high-pass, 200 ms EXG.');
  ws.on('message', data => {
    try {
      const packet = adapter.accept(JSON.parse(data.toString()));
      if (!packet) return;
      const now = Date.now();
      const owner = owners.get(packet.region);
      if (owner && owner.device !== packet.deviceName && now - owner.seen < 2000) return;
      // Moving a physical device invalidates its old location immediately.
      for (const [region, prior] of owners) {
        if (prior.device === packet.deviceName && region !== packet.region) {
          udp.send(Buffer.from(JSON.stringify({ ...packet, type: 'polar_clear', region })), udpPort, '127.0.0.1');
          owners.delete(region);
        }
      }
      owners.set(packet.region, { device: packet.deviceName, seen: now });
      udp.send(Buffer.from(JSON.stringify(packet)), udpPort, '127.0.0.1');
      forwarded++;
    } catch { /* Invalid input never reaches Unity. */ }
  });
  ws.on('close', () => { producer = null; console.log('Browser disconnected; Unity readings will expire.'); });
  ws.on('error', e => console.error('Browser socket:', e.message));
});
wss.on('listening', () => console.log(`Polar bridge ws://127.0.0.1:${wsPort} -> UDP 127.0.0.1:${udpPort}`));
wss.on('error', e => { console.error(`WebSocket: ${e.message}. Stop the upstream LSL bridge if it occupies port ${wsPort}.`); shutdown(1); });
const mime = { '.html': 'text/html', '.js': 'text/javascript', '.css': 'text/css', '.woff2': 'font/woff2', '.ico': 'image/x-icon' };
const server = http.createServer(async (req, res) => {
  try {
    if (req.url === '/health') { res.setHeader('Content-Type', 'application/json'); res.end(JSON.stringify({ connected: !!producer, forwarded })); return; }
    const requested = decodeURIComponent(new URL(req.url, 'http://localhost').pathname);
    const file = path.resolve(root, '.' + (requested === '/' ? '/index.html' : requested));
    if (!file.startsWith(root + path.sep)) { res.writeHead(403).end(); return; }
    const content = await readFile(file);
    res.setHeader('Content-Type', mime[path.extname(file)] || 'application/octet-stream');
    res.setHeader('Cross-Origin-Opener-Policy', 'same-origin');
    res.setHeader('Cross-Origin-Embedder-Policy', 'require-corp');
    res.end(content);
  } catch { res.writeHead(404).end('Build the visualizer first with Start-PolarDashboard.ps1.'); }
});
server.on('error', e => { console.error('HTTP:', e.message); shutdown(1); });
server.listen(httpPort, '127.0.0.1', () => console.log(`Open http://localhost:${httpPort} in Chrome or Edge. Ctrl+C stops the bridge.`));
function shutdown(code = 0) { for (const ws of wss.clients) ws.terminate(); wss.close(); server.close(); udp.close(); process.exitCode = code; }
process.on('SIGINT', () => shutdown());
process.on('SIGTERM', () => shutdown());
