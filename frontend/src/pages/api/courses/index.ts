import { NextApiRequest, NextApiResponse } from 'next';

export default async function handler(req: NextApiRequest, res: NextApiResponse) {
  const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';
  
  // Get token from Authorization header
  const authHeader = req.headers.authorization;
  var requestHeaders;
  if (!authHeader || !authHeader.startsWith('Bearer ')) {
    requestHeaders = {
        'Content-Type': 'application/json',
    };
  }
  else {
    requestHeaders = {
        'Authorization': authHeader,
        'Content-Type': 'application/json',
    };
  }

  try {
    // Build the request options
    const fetchOptions: RequestInit = {
      method: req.method,
      headers: requestHeaders
    };

    // Only include body for non-GET requests
    if (req.method !== 'GET' && req.body) {
      fetchOptions.body = JSON.stringify(req.body);
    }

    // Build the URL based on method and query parameters
    let url = `${API_BASE_URL}/api/courses`;
    if (req.method === 'GET' && req.query.semesterId) {
      url = `${API_BASE_URL}/api/courses/semester/${req.query.semesterId}`;
    }

    // Forward the request to the actual backend API
    const response = await fetch(url, fetchOptions);

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
        body: text.substring(0, 200)
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
