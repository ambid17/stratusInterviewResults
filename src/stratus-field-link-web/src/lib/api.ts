import type { Dashboard, SketchSummary } from './types';

async function getJson<T>(path: string): Promise<T> {
  const response = await fetch(path, { headers: { Accept: 'application/json' } });
  if (!response.ok) {
    throw new Error(`${response.status} ${response.statusText} from ${path}`);
  }
  return (await response.json()) as T;
}

export function fetchDashboard(): Promise<Dashboard> {
  return getJson<Dashboard>('/api/dashboard');
}

export function fetchSketches(params: Record<string, string | undefined> = {}): Promise<SketchSummary[]> {
  const query = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value) query.set(key, value);
  }
  const suffix = query.toString() ? `?${query.toString()}` : '';
  return getJson<SketchSummary[]>(`/api/sketches${suffix}`);
}

// TODO(candidate): there is no throughput call here yet, because there is no
// throughput endpoint yet. See DashboardEndpoints.cs.
