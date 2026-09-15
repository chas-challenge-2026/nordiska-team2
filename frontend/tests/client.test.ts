import { describe, it, expect, beforeEach, vi } from 'vitest';
import { http, HttpResponse } from 'msw';
import { server } from './mswServer';
import { apiClient, setAuthToken, setRedirectHandler } from '../src/client';

const BASE_URL = 'http://localhost:8080';

beforeEach(() => {
  apiClient.defaults.baseURL = BASE_URL;
  setAuthToken('expired-token');
});

describe('refresh-on-401 interceptor', () => {
    // Happy path, expired token gets refreshed and the original request retries silently
  it('refreshes an expired token and transparently retries the original request', async () => {
    let callCount = 0;

    server.use(
      http.get(`${BASE_URL}/protected-test`, () => {
        callCount++;
        if (callCount === 1) return new HttpResponse(null, { status: 401 });
        return HttpResponse.json({ message: 'success' });
      }),
      http.post(`${BASE_URL}/auth/refresh`, () =>
        HttpResponse.json({ accessToken: 'new-valid-token' })
      )
    );

    const response = await apiClient.get('/protected-test');

    expect(response.data).toEqual({ message: 'success' });
    expect(callCount).toBe(2);
    expect(apiClient.defaults.headers.common['Authorization']).toBe('Bearer new-valid-token');
  });

  // Prevents the self-referential loop, a failed refresh call must not trigger another refresh
  it('does NOT try to refresh when the failure is from an auth endpoint itself', async () => {
    let refreshCallCount = 0;

    server.use(
      http.post(`${BASE_URL}/auth/refresh`, () => {
        refreshCallCount++;
        return new HttpResponse(null, { status: 401 });
      })
    );

    await expect(apiClient.post('/auth/refresh')).rejects.toBeTruthy();
    expect(refreshCallCount).toBe(1);
  });

  // Wrong password is a login failure (not expired-token situation)
  it('does NOT try to refresh on a failed login (wrong password)', async () => {
    let refreshCallCount = 0;

    server.use(
      http.post(`${BASE_URL}/auth/login`, () => new HttpResponse(null, { status: 401 })),
      http.post(`${BASE_URL}/auth/refresh`, () => {
        refreshCallCount++;
        return HttpResponse.json({ accessToken: 'irrelevant' });
      })
    );

    await expect(
      apiClient.post('/auth/login', { email: 'a@b.com', password: 'wrong' })
    ).rejects.toBeTruthy();

    expect(refreshCallCount).toBe(0);
  });

  // Multiple simultaneous 401s must share one refresh attempt and not race each other
  it('deduplicates concurrent refresh attempts into a single network call', async () => {
    let refreshCallCount = 0;

    server.use(
      http.get(`${BASE_URL}/protected-a`, ({ request }) => {
        const auth = request.headers.get('Authorization');
        return auth === 'Bearer new-valid-token'
          ? HttpResponse.json({ data: 'a' })
          : new HttpResponse(null, { status: 401 });
      }),
      http.get(`${BASE_URL}/protected-b`, ({ request }) => {
        const auth = request.headers.get('Authorization');
        return auth === 'Bearer new-valid-token'
          ? HttpResponse.json({ data: 'b' })
          : new HttpResponse(null, { status: 401 });
      }),
      http.post(`${BASE_URL}/auth/refresh`, () => {
        refreshCallCount++;
        return HttpResponse.json({ accessToken: 'new-valid-token' });
      })
    );

    const [resA, resB] = await Promise.all([
      apiClient.get('/protected-a'),
      apiClient.get('/protected-b'),
    ]);

    expect(resA.data).toEqual({ data: 'a' });
    expect(resB.data).toEqual({ data: 'b' });
    expect(refreshCallCount).toBe(1);
  });

  // A dead session should clear local state and redirect and not fail silently.
  it('clears the token and redirects when refresh itself fails', async () => {
    const redirectSpy = vi.fn();
    setRedirectHandler(redirectSpy);

    server.use(
      http.get(`${BASE_URL}/protected-test`, () => new HttpResponse(null, { status: 401 })),
      http.post(`${BASE_URL}/auth/refresh`, () => new HttpResponse(null, { status: 401 }))
    );

    await expect(apiClient.get('/protected-test')).rejects.toBeTruthy();

    expect(apiClient.defaults.headers.common['Authorization']).toBeUndefined();
    expect(redirectSpy).toHaveBeenCalledOnce();
  });

  // 403 means forbidden and not expired, refreshing the token wouldn't fix it.
  it('does not attempt a refresh for a 403 (authenticated but forbidden)', async () => {
    let refreshCallCount = 0;

    server.use(
      http.get(`${BASE_URL}/someone-elses-account`, () => new HttpResponse(null, { status: 403 })),
      http.post(`${BASE_URL}/auth/refresh`, () => {
        refreshCallCount++;
        return HttpResponse.json({ accessToken: 'irrelevant' });
      })
    );

    await expect(apiClient.get('/someone-elses-account')).rejects.toBeTruthy();
    expect(refreshCallCount).toBe(0);
  });
});