FROM node:24-alpine AS deps
WORKDIR /app
COPY package.json package-lock.json* ./
RUN npm ci

FROM node:24-alpine AS build
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .
RUN npm run build

FROM node:24-alpine AS runtime
WORKDIR /app
# HOSTNAME=0.0.0.0 is required: Docker auto-sets HOSTNAME to the container id, and Next.js's
# standalone server.js binds to process.env.HOSTNAME — without this override it listens on the
# container hostname instead of all interfaces, so the published port never actually connects.
ENV NODE_ENV=production PORT=3000 HOSTNAME=0.0.0.0

COPY --from=build /app/public ./public
COPY --from=build /app/.next/standalone ./
COPY --from=build /app/.next/static ./.next/static

USER node
EXPOSE 3000

HEALTHCHECK --interval=10s --timeout=3s --start-period=10s --retries=5 \
    CMD wget -q -O- http://127.0.0.1:3000/apply || exit 1

CMD ["node", "server.js"]
