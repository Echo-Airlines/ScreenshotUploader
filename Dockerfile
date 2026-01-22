# Dockerfile for ClickOnce hosting (optional - can use docker-compose with nginx:alpine directly)
FROM nginx:alpine

# Copy nginx configuration
COPY clickonce-nginx.conf /etc/nginx/conf.d/default.conf

# Create directory for ClickOnce files
RUN mkdir -p /usr/share/nginx/html

# Expose port 80
EXPOSE 80

# Start nginx
CMD ["nginx", "-g", "daemon off;"]
