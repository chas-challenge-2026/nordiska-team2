# Stage 1: build the static files with Node
FROM node:22-alpine AS build
WORKDIR /app

# Copy only the package files first, so `npm ci` is cached
# until dependencies actually change.
COPY package.json package-lock.json ./
RUN npm ci

COPY . .

# Vite bakes these into the JavaScript AT BUILD TIME.
# Changing them later means rebuilding the image, not restarting it.
ARG VITE_API_BASE_URL
ARG VITE_ASSISTANT_BASE_URL
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL \
    VITE_ASSISTANT_BASE_URL=$VITE_ASSISTANT_BASE_URL
RUN npm run build

# Stage 2: serve the built files with nginx (no Node in the final image)
FROM nginx:1.27-alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80