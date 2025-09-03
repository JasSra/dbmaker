FROM nginx:alpine

# Copy nginx configuration
COPY nginx.conf /etc/nginx/nginx.conf
COPY conf.d/ /etc/nginx/conf.d/

# Create directories for dynamic configuration
RUN mkdir -p /etc/nginx/conf.d

# Add script to reload nginx when configuration changes
RUN apk add --no-cache inotify-tools

# Copy configuration reload script
COPY reload-config.sh /usr/local/bin/reload-config.sh
RUN chmod +x /usr/local/bin/reload-config.sh

EXPOSE 80 8080

# Start nginx and configuration watcher
CMD ["/bin/sh", "-c", "/usr/local/bin/reload-config.sh & nginx -g 'daemon off;'"]
