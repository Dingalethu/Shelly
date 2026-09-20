import { FormEvent, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../lib/auth';
import { ApiError } from '../lib/api';

export default function Login() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      await login(email, password);
      navigate('/');
    } catch (err) {
      if (err instanceof ApiError && err.status === 401) setError('Invalid email or password.');
      else setError('Login failed. Is the backend running?');
    } finally {
      setBusy(false);
    }
  }

  return (
    <div style={styles.wrap}>
      <form onSubmit={onSubmit} style={styles.card}>
        <h1 style={styles.h1}>Shelly</h1>
        <p style={styles.sub}>Sign in to your account</p>

        <label style={styles.label}>Email</label>
        <input
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
          style={styles.input}
          autoComplete="email"
        />

        <label style={styles.label}>Password</label>
        <input
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          required
          style={styles.input}
          autoComplete="current-password"
        />

        {error && <div style={styles.error}>{error}</div>}

        <button type="submit" disabled={busy} style={styles.button}>
          {busy ? 'Signing in…' : 'Sign in'}
        </button>

        <p style={styles.footer}>
          No account? <Link to="/register" style={styles.link}>Create one</Link>
        </p>
      </form>
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  wrap: {
    flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center',
    background: '#111', color: '#ddd', fontFamily: 'system-ui, sans-serif',
  },
  card: {
    width: 340, padding: 32, background: '#181818', borderRadius: 8,
    border: '1px solid #2a2a2a', display: 'flex', flexDirection: 'column',
  },
  h1: { margin: 0, fontSize: 24, color: '#fff' },
  sub: { margin: '4px 0 24px', color: '#888', fontSize: 14 },
  label: { fontSize: 12, color: '#999', marginBottom: 4, marginTop: 12 },
  input: {
    padding: '8px 10px', background: '#0f0f0f', border: '1px solid #333',
    borderRadius: 4, color: '#eee', fontSize: 14, outline: 'none',
  },
  button: {
    marginTop: 20, padding: '10px 12px', background: '#4f7cff', color: '#fff',
    border: 'none', borderRadius: 4, fontSize: 14, cursor: 'pointer',
  },
  error: {
    marginTop: 12, padding: '8px 10px', background: '#3a1414',
    border: '1px solid #5a1c1c', borderRadius: 4, color: '#f88', fontSize: 13,
  },
  footer: { marginTop: 20, fontSize: 13, color: '#888', textAlign: 'center' },
  link: { color: '#4f7cff', textDecoration: 'none' },
};