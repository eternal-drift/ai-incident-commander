FROM node:22-alpine AS build
WORKDIR /src
COPY package.json package-lock.json* ./
RUN npm install
COPY . .
ARG VITE_API_BASE=http://localhost:5288
ENV VITE_API_BASE=$VITE_API_BASE
RUN npm run build

FROM nginx:1.27-alpine AS runtime
COPY --from=build /src/dist /usr/share/nginx/html
EXPOSE 80
