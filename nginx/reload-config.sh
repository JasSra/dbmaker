#!/bin/sh

# Watch for changes in the dynamic configuration
while inotifywait -e modify /etc/nginx/conf.d/dynamic-upstreams.conf; do
    echo "Configuration changed, reloading nginx..."
    nginx -t && nginx -s reload
done
