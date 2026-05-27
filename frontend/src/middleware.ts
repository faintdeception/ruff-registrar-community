import type { NextRequest } from 'next/server';
import { NextResponse } from 'next/server';
import { extractTenantSlugFromPath, stripTenantSlugFromPath } from './lib/tenant-routing';

export function middleware(request: NextRequest) {
  const tenantSlug = extractTenantSlugFromPath(request.nextUrl.pathname);
  if (!tenantSlug) {
    return NextResponse.next();
  }

  const rewrittenUrl = request.nextUrl.clone();
  rewrittenUrl.pathname = stripTenantSlugFromPath(request.nextUrl.pathname);

  const requestHeaders = new Headers(request.headers);
  requestHeaders.set('x-tenant-slug', tenantSlug);

  return NextResponse.rewrite(rewrittenUrl, {
    request: {
      headers: requestHeaders,
    },
  });
}

export const config = {
  matcher: ['/((?!_next/static|_next/image|favicon.ico).*)'],
};
