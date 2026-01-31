import { NextApiRequest, NextApiResponse } from 'next';
import { getApiBaseUrl } from '@/lib/runtime-env';

export default async function handler(req: NextApiRequest, res: NextApiResponse) {
  const API_BASE_URL = getApiBaseUrl();
  
  // Get token from Authorization header
  const authHeader = req.headers.authorization;
  if (!authHeader || !authHeader.startsWith('Bearer ')) {
    return res.status(401).json({ message: 'No token provided' });
  }

  try {
    // Forward the request to the actual backend API
    const fetchOptions: RequestInit = {
      method: req.method,
      headers: {
        'Authorization': authHeader,
        'Content-Type': 'application/json',
      }
    };

    // Only include body for non-GET requests
    if (req.method !== 'GET' && req.body) {
      fetchOptions.body = JSON.stringify(req.body);
    }

    const response = await fetch(`${API_BASE_URL}/api/AccountHolders/me`, fetchOptions);

    // Check if response is JSON
    const contentType = response.headers.get('content-type');
    if (contentType && contentType.includes('application/json')) {
      const data = await response.json();
      
      if (!response.ok) {
        return res.status(response.status).json(data);
      }

      return res.status(200).json(data);
    } else {
      // Non-JSON response, likely an error page
      const text = await response.text();
      console.error('Backend returned non-JSON response:', {
        status: response.status,
        statusText: response.statusText,
        contentType,
        body: text.substring(0, 200) // First 200 chars for debugging
      });
      
      return res.status(response.status).json({ 
        message: `Backend error: ${response.status} ${response.statusText}`,
        debug: text.substring(0, 200)
      });
    }
  } catch (error) {
    console.error('Error proxying request to backend:', error);
    return res.status(500).json({ message: 'Internal server error' });
  }
}
