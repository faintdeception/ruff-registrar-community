import { NextApiRequest, NextApiResponse } from 'next';
import { getApiBaseUrl } from '@/lib/runtime-env';

export default async function handler(req: NextApiRequest, res: NextApiResponse) {
  const API_BASE_URL = getApiBaseUrl();
  const { studentId } = req.query;
  
  // Get token from Authorization header
  const authHeader = req.headers.authorization;
  if (!authHeader || !authHeader.startsWith('Bearer ')) {
    return res.status(401).json({ message: 'No token provided' });
  }

  try {
    // Forward the request to the actual backend API
    const response = await fetch(`${API_BASE_URL}/api/AccountHolders/me/students/${studentId}`, {
      method: req.method,
      headers: {
        'Authorization': authHeader,
        'Content-Type': 'application/json',
      },
      ...(req.body && { body: JSON.stringify(req.body) })
    });

    if (req.method === 'DELETE' && response.ok) {
      return res.status(204).end();
    }

    const data = await response.json();
    
    if (!response.ok) {
      return res.status(response.status).json(data);
    }

    return res.status(response.status).json(data);
  } catch (error) {
    console.error('Error proxying request to backend:', error);
    return res.status(500).json({ message: 'Internal server error' });
  }
}
