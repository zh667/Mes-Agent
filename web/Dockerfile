FROM node:20-alpine AS deps
WORKDIR /app
RUN corepack enable
COPY package.json pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile

FROM node:20-alpine AS build
WORKDIR /app
RUN corepack enable
ARG NEXT_PUBLIC_API_URL=http://localhost:5000/api
ENV NEXT_PUBLIC_API_URL=$NEXT_PUBLIC_API_URL
COPY --from=deps /app/node_modules ./node_modules
COPY . .
RUN pnpm build

FROM node:20-alpine AS final
WORKDIR /app
ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1
RUN corepack enable
COPY --from=build /app/package.json /app/pnpm-lock.yaml ./
COPY --from=build /app/next.config.mjs ./
COPY --from=build /app/.next ./.next
COPY --from=build /app/node_modules ./node_modules
EXPOSE 3000
CMD ["pnpm", "start", "-H", "0.0.0.0"]
