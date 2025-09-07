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
    // Forward the request to the actual backend API
    const response = await fetch(`${API_BASE_URL}/api/semesters/active`, {
      method: 'GET',
      headers: requestHeaders
    });

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
