import { NextRequest, NextResponse } from 'next/server';

const RESERVED_PATH_SEGMENTS = new Set([
  'account-holder',
  'admin',
  'api',
  'auth',
  'backend',
  'courses',
  'educators',
  'env.js',
  'favicon.ico',
  'login',
  'members',
  'rooms',
  'semesters',
  'settings',
  'silent-check-sso.html',
  'students',
  'unauthorized',
  '_next',
]);

export function middleware(request: NextRequest) {
  const segments = request.nextUrl.pathname.split('/').filter(Boolean);
  if (segments.length === 0) {
    return NextResponse.next();
  }

  const [firstSegment, ...remainingSegments] = segments;
  const normalizedFirstSegment = firstSegment.toLowerCase();

  if (RESERVED_PATH_SEGMENTS.has(normalizedFirstSegment)) {
    return NextResponse.next();
  }

  const rewrittenPath = remainingSegments.length > 0
    ? `/${remainingSegments.join('/')}`
    : '/';
  const rewrittenUrl = request.nextUrl.clone();
  rewrittenUrl.pathname = rewrittenPath;

  return NextResponse.rewrite(rewrittenUrl);
}

export const config = {
  matcher: ['/((?!_next/static|_next/image).*)'],
};