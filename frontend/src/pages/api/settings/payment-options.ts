import { NextApiRequest, NextApiResponse } from 'next';
import { getApiBaseUrl } from '@/lib/runtime-env';

export default async function handler(req: NextApiRequest, res: NextApiResponse) {
  const API_BASE_URL = getApiBaseUrl();
  const authHeader = req.headers.authorization;

  if (!authHeader || !authHeader.startsWith('Bearer ')) {
    return res.status(401).json({ message: 'No token provided' });
  }

  if (req.method !== 'GET' && req.method !== 'PUT') {
    return res.status(405).json({ message: 'Method not allowed' });
  }

  const forwardedHost = Array.isArray(req.headers.host) ? req.headers.host[0] : req.headers.host;

  try {
    const response = await fetch(`${API_BASE_URL}/api/settings/payment-options`, {
      method: req.method,
      headers: {
        Authorization: authHeader,
        'Content-Type': 'application/json',
        'X-Forwarded-Host': forwardedHost ?? '',
      },
      body: req.method === 'PUT' ? JSON.stringify(req.body) : undefined,
    });

    const contentType = response.headers.get('content-type') ?? '';
    if (contentType.includes('application/json')) {
      const payload = await response.json();
      return res.status(response.status).json(payload);
    }

    const text = await response.text();
    return res.status(response.status).json({
      message: `Backend error: ${response.status} ${response.statusText}`,
      debug: text.substring(0, 200),
    });
  } catch (error) {
    console.error('Error proxying payment-options request:', error);
    return res.status(500).json({ message: 'Internal server error' });
  }
}