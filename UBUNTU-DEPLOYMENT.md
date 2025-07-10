# MyTours API - Ubuntu Deployment Guide

## Quick Deployment Steps

### 1. Prepare Your Local Build
```bash
# Build the application for deployment
dotnet publish -c Release -o ./publish

# Create deployment package
tar -czf mytoursapi-deploy.tar.gz -C ./publish .
```

### 2. Upload to Ubuntu Server
```bash
# Upload deployment package
scp mytoursapi-deploy.tar.gz user@your-server:/tmp/
scp deploy-ubuntu.sh user@your-server:/tmp/
```

### 3. Run Deployment Script on Ubuntu Server
```bash
# SSH to your server
ssh user@your-server

# Make script executable and run
chmod +x /tmp/deploy-ubuntu.sh
sudo /tmp/deploy-ubuntu.sh
```

### 4. Deploy Application Files
```bash
# Extract application files
sudo mkdir -p /opt/mytoursapi
cd /opt/mytoursapi
sudo tar -xzf /tmp/mytoursapi-deploy.tar.gz
sudo chown -R mytoursapi:mytoursapi /opt/mytoursapi
```

### 5. Start the Service
```bash
# Start and enable the service
sudo systemctl start mytoursapi
sudo systemctl enable mytoursapi

# Check status
sudo systemctl status mytoursapi
```

## Alternative Methods

### Method 1: Using Docker (Recommended for Production)

```bash
# On your Ubuntu server, create docker-compose.yml
version: '3.8'
services:
  db:
    image: mysql:8.0
    restart: always
    environment:
      MYSQL_ROOT_PASSWORD: root
      MYSQL_DATABASE: mytoursdb
      MYSQL_USER: finnmcooltours
      MYSQL_PASSWORD: finnmcooltours
    volumes:
      - mysql_data:/var/lib/mysql
    ports:
      - "3306:3306"

  api:
    image: mytoursapi:latest
    restart: always
    ports:
      - "80:8080"
      - "443:8081"
    depends_on:
      - db
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=server=db;port=3306;database=mytoursdb;user=finnmcooltours;password=finnmcooltours;

volumes:
  mysql_data:

# Build and run
docker build -t mytoursapi .
docker-compose up -d
```

### Method 2: Manual Background Process

```bash
# Install .NET 8.0 Runtime
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt update
sudo apt install -y aspnetcore-runtime-8.0

# Install MySQL
sudo apt install -y mysql-server
sudo mysql_secure_installation

# Setup database
sudo mysql -e "CREATE DATABASE mytoursdb;"
sudo mysql -e "CREATE USER 'finnmcooltours'@'localhost' IDENTIFIED BY 'finnmcooltours';"
sudo mysql -e "GRANT ALL PRIVILEGES ON mytoursdb.* TO 'finnmcooltours'@'localhost';"

# Run application in background
nohup dotnet MyToursApi.dll --urls="http://0.0.0.0:5000" > app.log 2>&1 &

# Or using screen/tmux
screen -S mytoursapi
dotnet MyToursApi.dll --urls="http://0.0.0.0:5000"
# Press Ctrl+A, then D to detach
```

## Useful Commands

### Service Management
```bash
# Start service
sudo systemctl start mytoursapi

# Stop service
sudo systemctl stop mytoursapi

# Restart service
sudo systemctl restart mytoursapi

# Check status
sudo systemctl status mytoursapi

# View logs
sudo journalctl -u mytoursapi -f

# View recent logs
sudo journalctl -u mytoursapi --since "1 hour ago"
```

### Process Management (if running manually)
```bash
# Find the process
ps aux | grep MyToursApi

# Kill the process
sudo pkill -f MyToursApi

# Check if port is in use
sudo netstat -tulpn | grep :5000
```

### Health Checks
```bash
# Test API health
curl -I http://localhost:5000/swagger

# Check database connection
mysql -u finnmcooltours -p mytoursdb -e "SHOW TABLES;"
```

## Configuration for Production

### Update appsettings.json for production:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "server=localhost;port=3306;database=mytoursdb;user=finnmcooltours;password=your-secure-password;"
  },
  "JwtSettings": {
    "SecretKey": "YourProductionSecretKey-MakeSureItsLongAndSecure123!"
  },
  "AdminCredentials": {
    "Email": "admin@yourdomain.com",
    "Password": "SecureAdminPassword123!"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "your-domain.com"
}
```

### Nginx Reverse Proxy (Optional)
```nginx
server {
    listen 80;
    server_name your-domain.com;
    
    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

## Security Considerations

1. **Database Security**: Change default passwords
2. **Firewall**: Configure UFW to only allow necessary ports
3. **SSL/TLS**: Use Nginx with Let's Encrypt for HTTPS
4. **User Permissions**: Run service with limited user privileges
5. **Environment Variables**: Store secrets in environment variables
6. **Regular Updates**: Keep system and packages updated
