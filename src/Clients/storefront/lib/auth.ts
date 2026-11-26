export interface PkceConfig {
  url: string;
  realm: string;
  clientId: string;
}

export function getKeycloakPkceConfig(): PkceConfig {
  return {
    url: process.env.NEXT_PUBLIC_KEYCLOAK_URL ?? 'http://localhost:8080',
    realm: 'palantir',
    clientId: 'palantir-shell',
  };
}

export async function createPkceChallenge(verifier: string): Promise<string> {
  const data = new TextEncoder().encode(verifier);
  const digest = await crypto.subtle.digest('SHA-256', data);
  return btoa(String.fromCharCode(...new Uint8Array(digest)))
    .replace(/\+/g, '-')
    .replace(/\//g, '_')
    .replace(/=+$/, '');
}
