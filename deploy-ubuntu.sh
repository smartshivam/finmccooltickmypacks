#!/bin/bash

# MyToursApi Ubuntu Deployment Script
# Run this script on your Ubuntu server

echo "🚀 MyToursApi Ubuntu Deployment Script"
echo "======================================="

# Variables (modify these as needed)
APP_NAME="mytoursapi"
APP_USER="mytoursapi"
APP_DIR="/opt/mytoursapi"
SERVICE_FILE="/etc/systemd/system/mytoursapi.service"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if running as root
if [ "$EUID" -ne 0 ]; then
    print_error "Please run this script as root or with sudo"
    exit 1
fi

# Update system packages
print_status "Updating system packages..."
apt update && apt upgrade -y

# Install .NET 8.0 Runtime
print_status "Installing .NET 8.0 Runtime..."
wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
dpkg -i packages-microsoft-prod.deb
apt update
apt install -y aspnetcore-runtime-8.0

# Install MySQL Server
print_status "Installing MySQL Server..."
apt install -y mysql-server

# Configure MySQL
print_status "Configuring MySQL..."
mysql -e "CREATE DATABASE IF NOT EXISTS mytoursdb;"
mysql -e "CREATE USER IF NOT EXISTS 'finnmcooltours'@'localhost' IDENTIFIED BY 'finnmcooltours';"
mysql -e "GRANT ALL PRIVILEGES ON mytoursdb.* TO 'finnmcooltours'@'localhost';"
mysql -e "FLUSH PRIVILEGES;"

# Create application user
print_status "Creating application user..."
if ! id "$APP_USER" &>/dev/null; then
    useradd -r -s /bin/false $APP_USER
    print_status "User $APP_USER created"
else
    print_warning "User $APP_USER already exists"
fi

# Create application directory
print_status "Creating application directory..."
mkdir -p $APP_DIR
chown $APP_USER:$APP_USER $APP_DIR

# Copy application files (you need to upload your published files to the server first)
print_warning "Please copy your published application files to $APP_DIR"
print_warning "You can use: scp -r ./publish/* user@server:$APP_DIR/"

# Create systemd service file
print_status "Creating systemd service file..."
cat > $SERVICE_FILE << EOF
[Unit]
Description=MyTours API
After=network.target

[Service]
Type=notify
User=$APP_USER
Group=$APP_USER
WorkingDirectory=$APP_DIR
ExecStart=/usr/bin/dotnet $APP_DIR/MyToursApi.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=mytoursapi
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://0.0.0.0:5000

[Install]
WantedBy=multi-user.target
EOF

# Set proper permissions for service file
chmod 644 $SERVICE_FILE

# Configure firewall (if ufw is installed)
if command -v ufw &> /dev/null; then
    print_status "Configuring firewall..."
    ufw allow 5000/tcp
    ufw allow 22/tcp
fi

# Reload systemd and enable service
print_status "Configuring systemd service..."
systemctl daemon-reload
systemctl enable $APP_NAME

print_status "Deployment script completed!"
echo ""
echo "📋 Next Steps:"
echo "1. Upload your application files to $APP_DIR"
echo "2. Set proper ownership: sudo chown -R $APP_USER:$APP_USER $APP_DIR"
echo "3. Start the service: sudo systemctl start $APP_NAME"
echo "4. Check status: sudo systemctl status $APP_NAME"
echo "5. View logs: sudo journalctl -u $APP_NAME -f"
echo ""
echo "🌐 Your API will be available at: http://your-server-ip:5000"
echo "📖 Swagger UI: http://your-server-ip:5000/swagger"
