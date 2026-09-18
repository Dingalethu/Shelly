import { useEffect, useRef } from 'react';
import { Terminal as XTerm } from '@xterm/xterm';
import { FitAddon } from '@xterm/addon-fit';
import '@xterm/xterm/css/xterm.css';

const MSG_DATA = 0x00;
const MSG_RESIZE = 0x01;

const WS_BASE = import.meta.env.VITE_WS_BASE ?? 'ws://localhost:5000';

type Props = { room: string };

export default function Terminal({ room }: Props) {
  const containerRef = useRef<HTMLDivElement>(null);
  const termRef = useRef<XTerm | null>(null);

  useEffect(() => {
    if (!containerRef.current) return;

    const term = new XTerm({
      cursorBlink: true,
      fontFamily: 'Menlo, Monaco, "Courier New", monospace',
      fontSize: 14,
      theme: { background: '#111111' },
    });
    const fit = new FitAddon();
    term.loadAddon(fit);
    term.open(containerRef.current);
    fit.fit();
    termRef.current = term;

    const ws = new WebSocket(`${WS_BASE}/ws/terminal/${room}`);
    ws.binaryType = 'arraybuffer';

    const sendResize = () => {
      if (ws.readyState !== WebSocket.OPEN) return;
      const cols = term.cols;
      const rows = term.rows;
      const buf = new ArrayBuffer(5);
      const view = new DataView(buf);
      view.setUint8(0, MSG_RESIZE);
      view.setUint16(1, cols);
      view.setUint16(3, rows);
      ws.send(buf);
    };

    ws.onopen = () => {
      term.writeln(`\x1b[90mconnected to room "${room}"\x1b[0m`);
      sendResize();
    };

    ws.onmessage = (ev) => {
      const buf = ev.data as ArrayBuffer;
      const bytes = new Uint8Array(buf);
      if (bytes.length === 0) return;
      if (bytes[0] === MSG_DATA) {
        term.write(bytes.subarray(1));
      }
      // ignore other message types in Phase 0
    };

    ws.onclose = () => {
      term.writeln('\r\n\x1b[31m[disconnected]\x1b[0m');
    };

    const dataSub = term.onData((data) => {
      if (ws.readyState !== WebSocket.OPEN) return;
      const payload = new TextEncoder().encode(data);
      const buf = new Uint8Array(1 + payload.length);
      buf[0] = MSG_DATA;
      buf.set(payload, 1);
      ws.send(buf);
    });

    const resizeSub = term.onResize(() => sendResize());

    const ro = new ResizeObserver(() => {
      try { fit.fit(); } catch { /* ignore */ }
    });
    ro.observe(containerRef.current);

    return () => {
      ro.disconnect();
      dataSub.dispose();
      resizeSub.dispose();
      ws.close();
      term.dispose();
    };
  }, [room]);

  return <div ref={containerRef} style={{ flex: 1, padding: 8 }} />;
}