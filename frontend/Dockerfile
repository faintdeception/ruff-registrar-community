# Build the application
FROM node:18-alpine AS builder
WORKDIR /app
ENV NPM_CONFIG_FETCH_RETRIES=5 \
	NPM_CONFIG_FETCH_RETRY_FACTOR=2 \
	NPM_CONFIG_FETCH_RETRY_MINTIMEOUT=20000 \
	NPM_CONFIG_FETCH_RETRY_MAXTIMEOUT=120000 \
	NPM_CONFIG_LOGLEVEL=warn
COPY package.json package-lock.json* ./
RUN npm ci --no-audit --no-fund --prefer-offline
COPY . .
RUN npm run build

# Production image
FROM node:18-alpine AS runner
WORKDIR /app

ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1

RUN addgroup --system --gid 1001 nodejs
RUN adduser --system --uid 1001 nextjs

# Copy the built application
COPY --from=builder /app/public ./public
COPY --from=builder /app/.next/standalone ./
COPY --from=builder /app/.next/static ./.next/static
COPY core/frontend/docker-entrypoint.sh /app/docker-entrypoint.sh

RUN chmod +x /app/docker-entrypoint.sh
RUN chown -R nextjs:nodejs /app/public /app/.next

USER nextjs

EXPOSE 3000

ENV PORT=3000

ENTRYPOINT ["/app/docker-entrypoint.sh"]
CMD ["node", "server.js"]
