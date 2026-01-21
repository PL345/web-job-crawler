FROM node:18-alpine AS builder
WORKDIR /build
COPY package*.json ./
RUN npm install
COPY . .
RUN npm run build

FROM node:18-alpine
WORKDIR /app
RUN npm install -g serve
COPY --from=builder /build/dist ./dist
EXPOSE 3000
CMD ["serve", "-s", "dist", "-l", "3000"]
