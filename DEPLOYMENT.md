# Deployment Setup Guide

This guide explains how to set up CI/CD for the Flea Market application using GitHub Actions with your self-hosted runners.

## GitHub Repository Secrets

You need to configure the following secrets in your GitHub repository (Settings → Secrets and variables → Actions):

### Required Secrets

- `PROD_CONNECTION_STRING` - SQL Server connection string for production database
- `REGISTRATION_PASSWORD` - Shared password for user registration
- `GOOGLE_CLIENT_ID` - Google OAuth Client ID for production
- `GOOGLE_CLIENT_SECRET` - Google OAuth Client Secret for production
- `FACEBOOK_APP_ID` - Facebook App ID for production
- `FACEBOOK_APP_SECRET` - Facebook App Secret for production

### Example Values

```
PROD_CONNECTION_STRING=Server=localhost;Database=FleaMarketProd;User Id=fleamarket_user;Password=your_secure_password;TrustServerCertificate=true;
REGISTRATION_PASSWORD=your_secure_registration_password
GOOGLE_CLIENT_ID=123456789012-abcdefghijklmnopqrstuvwxyz.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=GOCSPX-AbCdEfGhIjKlMnOpQrStUvWxYz
FACEBOOK_APP_ID=1234567890123456
FACEBOOK_APP_SECRET=abcdef1234567890abcdef1234567890
```

## Self-Hosted Runner Setup

### Windows NUC Runner (nuc.hospitalit.co.uk)

Your Windows runner should have:
- .NET 8.0 SDK installed
- GitHub Actions runner registered with labels: `[self-hosted, windows, nuc.hospitalit.co.uk]`

### Ubuntu Server Runner (ubuntubox-)

Your Ubuntu runner should have:
- .NET 8.0 runtime installed
- GitHub Actions runner registered with labels: `[self-hosted, linux, ubuntubox-]`
- NGINX configured as reverse proxy
- Systemd for service management

## NGINX Configuration

Create or update `/etc/nginx/sites-available/fleamarket.beloved-websites.co.uk`:

```nginx
server {
    listen 80;
    server_name fleamarket.beloved-websites.co.uk;
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name fleamarket.beloved-websites.co.uk;

    # SSL Configuration (LetsEncrypt)
    ssl_certificate /etc/letsencrypt/live/fleamarket.beloved-websites.co.uk/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/fleamarket.beloved-websites.co.uk/privkey.pem;
    include /etc/letsencrypt/options-ssl-nginx.conf;
    ssl_dhparam /etc/letsencrypt/ssl-dhparams.pem;

    # Security headers
    add_header X-Frame-Options DENY;
    add_header X-Content-Type-Options nosniff;
    add_header X-XSS-Protection "1; mode=block";
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;

    # File upload size limit
    client_max_body_size 10M;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;

        # Timeouts
        proxy_connect_timeout 60s;
        proxy_send_timeout 60s;
        proxy_read_timeout 60s;
    }

    # Static files caching
    location ~* \.(css|js|png|jpg|jpeg|gif|ico|svg|woff|woff2|ttf|eot)$ {
        proxy_pass http://localhost:5000;
        proxy_set_header Host $host;
        expires 1y;
        add_header Cache-Control "public, immutable";
    }
}
```

Enable the site:
```bash
sudo ln -sf /etc/nginx/sites-available/fleamarket.beloved-websites.co.uk /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

## Database Setup on Ubuntu Server

1. Install SQL Server on Ubuntu (if not already installed):
```bash
# Add Microsoft repository
curl -fsSL https://packages.microsoft.com/keys/microsoft.asc | sudo gpg --dearmor -o /usr/share/keyrings/microsoft-prod.gpg
echo "deb [arch=amd64,arm64,armhf signed-by=/usr/share/keyrings/microsoft-prod.gpg] https://packages.microsoft.com/ubuntu/$(lsb_release -rs)/prod $(lsb_release -cs) main" | sudo tee /etc/apt/sources.list.d/mssql-release.list

# Install SQL Server
sudo apt update
sudo apt install -y mssql-server
sudo /opt/mssql/bin/mssql-conf setup

# Install SQL Server command-line tools
sudo apt install -y mssql-tools unixodbc-dev
echo 'export PATH="$PATH:/opt/mssql-tools/bin"' >> ~/.bashrc
```

2. Create database and user:
```sql
-- Connect as SA user
sqlcmd -S localhost -U SA

-- Create database
CREATE DATABASE FleaMarketProd;
GO

-- Create dedicated user
USE FleaMarketProd;
GO
CREATE LOGIN fleamarket_user WITH PASSWORD = 'your_secure_password';
GO
CREATE USER fleamarket_user FOR LOGIN fleamarket_user;
GO
ALTER ROLE db_owner ADD MEMBER fleamarket_user;
GO
```

## Initial Deployment

1. Push your code to the master branch
2. The GitHub Action will automatically:
   - Build the application on the Windows NUC
   - Deploy to the Ubuntu server
   - Create the systemd service
   - Start the application

3. Run database migrations manually on first deployment:
```bash
cd /var/www/fleamarket
sudo -u www-data dotnet ef database update --no-build
```

## OAuth Configuration for Production

Update your OAuth app settings to include the production redirect URIs:

### Google OAuth
- Redirect URI: `https://fleamarket.beloved-websites.co.uk/signin-google`

### Facebook OAuth
- Redirect URI: `https://fleamarket.beloved-websites.co.uk/signin-facebook`

## Monitoring and Logs

View application logs:
```bash
sudo journalctl -u fleamarket -f
```

Check application status:
```bash
sudo systemctl status fleamarket
```

View NGINX logs:
```bash
sudo tail -f /var/log/nginx/access.log
sudo tail -f /var/log/nginx/error.log
```