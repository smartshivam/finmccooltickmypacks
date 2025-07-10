#!/bin/bash

# Quick start script for Ubuntu deployment
# This script provides multiple deployment options

set -e

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

print_header() {
    echo -e "${GREEN}"
    echo "=================================="
    echo "  MyTours API - Ubuntu Deployment"
    echo "=================================="
    echo -e "${NC}"
}

print_option() {
    echo -e "${YELLOW}$1${NC} $2"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_header

echo "Choose your deployment method:"
echo ""
print_option "1)" "Docker Compose (Recommended - Easiest)"
print_option "2)" "Systemd Service (Production Ready)"
print_option "3)" "Manual Background Process (Simple)"
print_option "4)" "Exit"
echo ""

read -p "Enter your choice (1-4): " choice

case $choice in
    1)
        echo ""
        echo "🐳 Docker Compose Deployment"
        echo "=============================="
        
        # Check if Docker is installed
        if ! command -v docker &> /dev/null; then
            echo "Installing Docker..."
            curl -fsSL https://get.docker.com -o get-docker.sh
            sudo sh get-docker.sh
            sudo usermod -aG docker $USER
        fi
        
        # Check if Docker Compose is installed
        if ! command -v docker-compose &> /dev/null; then
            echo "Installing Docker Compose..."
            sudo curl -L "https://github.com/docker/compose/releases/download/v2.20.0/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
            sudo chmod +x /usr/local/bin/docker-compose
        fi
        
        echo "Building and starting containers..."
        docker-compose up -d --build
        
        print_success "Application deployed successfully!"
        echo "🌐 API URL: http://localhost"
        echo "📖 Swagger: http://localhost/swagger"
        echo ""
        echo "Useful commands:"
        echo "  View logs: docker-compose logs -f"
        echo "  Stop: docker-compose down"
        echo "  Restart: docker-compose restart"
        ;;
        
    2)
        echo ""
        echo "⚙️  Systemd Service Deployment"
        echo "==============================="
        
        # Install .NET 8.0 Runtime
        echo "Installing .NET 8.0 Runtime..."
        wget -q https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb
        sudo dpkg -i packages-microsoft-prod.deb
        sudo apt update
        sudo apt install -y aspnetcore-runtime-8.0
        
        # Install MySQL
        echo "Installing MySQL..."
        sudo apt install -y mysql-server
        
        # Setup database
        echo "Setting up database..."
        sudo mysql -e "CREATE DATABASE IF NOT EXISTS mytoursdb;"
        sudo mysql -e "CREATE USER IF NOT EXISTS 'finnmcooltours'@'localhost' IDENTIFIED BY 'finnmcooltours';"
        sudo mysql -e "GRANT ALL PRIVILEGES ON mytoursdb.* TO 'finnmcooltours'@'localhost';"
        sudo mysql -e "FLUSH PRIVILEGES;"
        
        # Create user and directories
        echo "Creating application user and directories..."
        sudo useradd -r -s /bin/false mytoursapi || true
        sudo mkdir -p /opt/mytoursapi
        
        # Copy files (assuming they're already published)
        if [ -d "./publish" ]; then
            sudo cp -r ./publish/* /opt/mytoursapi/
            sudo chown -R mytoursapi:mytoursapi /opt/mytoursapi
        else
            print_error "Please run 'dotnet publish -c Release -o ./publish' first"
            exit 1
        fi
        
        # Install systemd service
        sudo cp mytoursapi.service /etc/systemd/system/
        sudo systemctl daemon-reload
        sudo systemctl enable mytoursapi
        sudo systemctl start mytoursapi
        
        print_success "Service deployed successfully!"
        echo "🌐 API URL: http://localhost:5000"
        echo "📖 Swagger: http://localhost:5000/swagger"
        echo ""
        echo "Useful commands:"
        echo "  Status: sudo systemctl status mytoursapi"
        echo "  Logs: sudo journalctl -u mytoursapi -f"
        echo "  Restart: sudo systemctl restart mytoursapi"
        ;;
        
    3)
        echo ""
        echo "🔧 Manual Background Process"
        echo "============================"
        
        # Install dependencies
        echo "Installing dependencies..."
        sudo apt update
        sudo apt install -y aspnetcore-runtime-8.0 mysql-server
        
        # Setup database
        sudo mysql -e "CREATE DATABASE IF NOT EXISTS mytoursdb;"
        sudo mysql -e "CREATE USER IF NOT EXISTS 'finnmcooltours'@'localhost' IDENTIFIED BY 'finnmcooltours';"
        sudo mysql -e "GRANT ALL PRIVILEGES ON mytoursdb.* TO 'finnmcooltours'@'localhost';"
        sudo mysql -e "FLUSH PRIVILEGES;"
        
        # Check if published
        if [ ! -f "./publish/MyToursApi.dll" ]; then
            print_error "Please run 'dotnet publish -c Release -o ./publish' first"
            exit 1
        fi
        
        # Kill existing process if running
        pkill -f MyToursApi || true
        
        # Start in background
        cd ./publish
        nohup dotnet MyToursApi.dll --urls="http://0.0.0.0:5000" > ../mytoursapi.log 2>&1 &
        
        # Get PID
        sleep 2
        PID=$(pgrep -f MyToursApi)
        
        if [ ! -z "$PID" ]; then
            print_success "Application started successfully! PID: $PID"
            echo "🌐 API URL: http://localhost:5000"
            echo "📖 Swagger: http://localhost:5000/swagger"
            echo "📝 Logs: tail -f mytoursapi.log"
            echo ""
            echo "To stop: kill $PID"
        else
            print_error "Failed to start application. Check mytoursapi.log for errors."
        fi
        ;;
        
    4)
        echo "Goodbye!"
        exit 0
        ;;
        
    *)
        print_error "Invalid choice. Please run the script again."
        exit 1
        ;;
esac
