import { useAuth } from '../lib/auth';
import Terminal from '../components/Terminal';

export default function Home() {
  const { user, logout } = useAuth();
  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', background: '#111' }}>
      <div style={{
        display: 'flex', alignItems: 'center', justifyContent: 'space-between',
        padding: '8px 16px', background: '#181818', borderBottom: '1px solid #2a2a2a',
        color: '#ddd', fontFamily: 'system-ui, sans-serif', fontSize: 13,
      }}>
        <span><strong>Shelly</strong> — {user?.displayName}</span>
        <button onClick={logout} style={{
          background: 'transparent', border: '1px solid #333', color: '#ccc',
          padding: '4px 10px', borderRadius: 4, cursor: 'pointer', fontSize: 12,
        }}>Sign out</button>
      </div>
      <Terminal room="default" />
    </div>
  );
}