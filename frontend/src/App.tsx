import Terminal from './components/Terminal';

export default function App() {
  // Phase 0: hardcoded room.
  const room = 'default';
  return <Terminal room={room} />;
}