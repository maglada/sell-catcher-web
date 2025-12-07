# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["SellCatcher.Blazor/SellCatcher.Blazor.csproj", "SellCatcher.Blazor/"]
RUN dotnet restore "SellCatcher.Blazor/SellCatcher.Blazor.csproj"

# Copy source code
COPY . .
WORKDIR "/src/SellCatcher.Blazor"
RUN dotnet publish "SellCatcher.Blazor.csproj" -c Release -o /app/publish

# Debug: List what was published
RUN echo "=== Published files ===" && \
    ls -la /app/publish/ && \
    echo "=== Checking for wwwroot ===" && \
    ls -la /app/publish/wwwroot/ 2>/dev/null || echo "No wwwroot found" && \
    echo "=== Checking for _framework ===" && \
    find /app/publish -name "_framework" -type d

# Runtime stage - nginx for serving static files
FROM nginx:alpine
WORKDIR /usr/share/nginx/html

# Remove default nginx content
RUN rm -rf /usr/share/nginx/html/*

# Copy published files (handles both wwwroot and root publish scenarios)
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html/

# Debug: Show what we copied
RUN echo "=== Files in nginx html ===" && ls -la /usr/share/nginx/html/

# Create nginx config with better error handling
RUN echo 'server { \
    listen 3000; \
    server_name localhost; \
    root /usr/share/nginx/html; \
    index index.html; \
    \
    location / { \
        try_files $uri $uri/ /index.html =404; \
    } \
    \
    location ~* \.(js|css|wasm|json|png|jpg|jpeg|gif|ico|svg)$ { \
        add_header Cache-Control "public, max-age=31536000, immutable"; \
    } \
    \
    error_log /var/log/nginx/error.log debug; \
    access_log /var/log/nginx/access.log; \
}' > /etc/nginx/conf.d/default.conf

EXPOSE 3000
CMD ["nginx", "-g", "daemon off;"]