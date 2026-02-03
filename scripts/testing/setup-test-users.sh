#!/bin/bash

# Setup Test Users for E2E Testing
# This script creates the required test users in Keycloak for role-based E2E testing
# Run this AFTER setup-keycloak.sh has been executed

set -e

echo "🧪 Setting up E2E test users..."

# Configuration (override via environment variables for deployed Keycloak)
KEYCLOAK_URL="${KEYCLOAK_URL:-http://localhost:8080}"
# Aspire dev default master admin username is typically 'admin'.
ADMIN_USER="${KEYCLOAK_ADMIN_USER:-admin}"
REALM_NAME="${KEYCLOAK_REALM:-student-registrar}"

# Function to check if command succeeded (copied from setup-keycloak.sh)
check_api_response() {
    local response="$1"
    local description="$2"
    
    if echo "$response" | grep -q "error"; then
        echo "❌ Failed to $description"
        echo "Error: $response"
        return 1
    fi
    return 0
}

# Prompt for admin password (same as setup-keycloak.sh)
if [ -n "${KEYCLOAK_ADMIN_PASSWORD:-}" ]; then
    ADMIN_PASSWORD="$KEYCLOAK_ADMIN_PASSWORD"
    echo "🔑 Using KEYCLOAK_ADMIN_PASSWORD from environment"
else
    echo "📋 Enter your Keycloak admin password:"
    echo "   (The same password you used for setup-keycloak.sh)"
    echo ""
    read -s -p "Enter Keycloak admin password: " ADMIN_PASSWORD
    echo ""
fi

# Get admin token (using the same method as setup-keycloak.sh)
echo "🔑 Getting admin access token..."
TOKEN_RESPONSE=$(curl -s -X POST "${KEYCLOAK_URL}/realms/master/protocol/openid-connect/token" \
    -H "Content-Type: application/x-www-form-urlencoded" \
    --data-urlencode "username=${ADMIN_USER}" \
    --data-urlencode "password=${ADMIN_PASSWORD}" \
    -d "grant_type=password" \
    -d "client_id=admin-cli")

TOKEN=$(echo "$TOKEN_RESPONSE" | jq -r '.access_token')

if [ "$TOKEN" == "null" ] || [ -z "$TOKEN" ]; then
    echo "❌ Failed to get admin token. Please check your password and try again."
    exit 1
fi

echo "✅ Admin token obtained successfully"

# Create test users (following setup-keycloak.sh pattern)
echo "👥 Creating test users..."

# Create admin1 test user
ADMIN1_USERNAME="admin1"
ADMIN1_PASSWORD="AdminPass123!"

echo "👤 Checking if user $ADMIN1_USERNAME exists..."
USER_EXISTS=$(curl -s -X GET "${KEYCLOAK_URL}/admin/realms/${REALM_NAME}/users?username=${ADMIN1_USERNAME}" \
    -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json" | jq 'length')

if [ "$USER_EXISTS" -eq 0 ]; then
    echo "👤 Creating user: $ADMIN1_USERNAME in realm: $REALM_NAME"
    
    USER_RESPONSE=$(curl -s -X POST "${KEYCLOAK_URL}/admin/realms/${REALM_NAME}/users" \
        -H "Authorization: Bearer $TOKEN" \
        -H "Content-Type: application/json" \
        -d "{
            \"username\": \"$ADMIN1_USERNAME\",
            \"enabled\": true,
            \"emailVerified\": true,
            \"firstName\": \"Admin\",
            \"lastName\": \"Test\",
            \"email\": \"admin.test@example.com\"
        }")
    
    if ! check_api_response "$USER_RESPONSE" "create user"; then
        echo "❌ Failed to create user $ADMIN1_USERNAME"
        exit 1
    fi
    
    # Get user ID and set password + role
    USER_ID=$(curl -s -X GET "${KEYCLOAK_URL}/admin/realms/${REALM_NAME}/users?username=${ADMIN1_USERNAME}" \
        -H "Authorization: Bearer $TOKEN" | jq -r '.[0].id')
    
    if [ "$USER_ID" != "null" ] && [ -n "$USER_ID" ]; then
        # Set password
        PASSWORD_RESPONSE=$(curl -s -X PUT "${KEYCLOAK_URL}/admin/realms/${REALM_NAME}/users/${USER_ID}/reset-password" \
            -H "Authorization: Bearer $TOKEN" \
            -H "Content-Type: application/json" \
            -d "{
                \"type\": \"password\",
                \"value\": \"$ADMIN1_PASSWORD\",
                \"temporary\": false
            }")
        
        if ! check_api_response "$PASSWORD_RESPONSE" "set password"; then
            echo "⚠️  User created but failed to set password"
        fi
        
        # Assign Administrator role
        ADMIN_ROLE_ID=$(curl -s -X GET "${KEYCLOAK_URL}/admin/realms/${REALM_NAME}/roles/Administrator" \
            -H "Authorization: Bearer $TOKEN" | jq -r '.id')
        
        if [ "$ADMIN_ROLE_ID" != "null" ] && [ -n "$ADMIN_ROLE_ID" ]; then
            ROLE_RESPONSE=$(curl -s -X POST "${KEYCLOAK_URL}/admin/realms/${REALM_NAME}/users/${USER_ID}/role-mappings/realm" \
                -H "Authorization: Bearer $TOKEN" \
                -H "Content-Type: application/json" \
                -d "[{
                    \"id\": \"$ADMIN_ROLE_ID\",
                    \"name\": \"Administrator\"
                }]")
            
            if ! check_api_response "$ROLE_RESPONSE" "assign Administrator role"; then
                echo "⚠️  User created but failed to assign Administrator role"
            fi
            
            echo "✅ User $ADMIN1_USERNAME created with Administrator role."
        fi
    fi
else
    echo "ℹ️ User $ADMIN1_USERNAME already exists in realm $REALM_NAME."
fi

# Create educator1 test user
EDUCATOR1_USERNAME="educator1"
EDUCATOR1_PASSWORD="EducatorPass123!"

echo "👤 Checking if user $EDUCATOR1_USERNAME exists..."
USER_EXISTS=$(curl -s -X GET "${KEYCLOAK_URL}/admin/realms/${REALM_NAME}/users?username=${EDUCATOR1_USERNAME}" \
    -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json" | jq 'length')

if [ "$USER_EXISTS" -eq 0 ]; then
    echo "� Creating user: $EDUCATOR1_USERNAME in realm: $REALM_NAME"
    
    USER_RESPONSE=$(curl -s -X POST "${KEYCLOAK_URL}/admin/realms/${REALM_NAME}/users" \
        -H "Authorization: Bearer $TOKEN" \
        -H "Content-Type: application/json" \
        -d "{
            \"username\": \"$EDUCATOR1_USERNAME\",
            \"enabled\": true,
            \"emailVerified\": true,
            \"firstName\": \"Emily\",
            \"lastName\": \"Educator\",
            \"email\": \"emily.educator@example.com\"
        }")
    
    if ! check_api_response "$USER_RESPONSE" "create user"; then
        echo "❌ Failed to create user $EDUCATOR1_USERNAME"
        exit 1
    fi
    
    # Get user ID and set password + role
    USER_ID=$(curl -s -X GET "${KEYCLOAK_URL}/admin/realms/${REALM_NAME}/users?username=${EDUCATOR1_USERNAME}" \
        -H "Authorization: Bearer $TOKEN" | jq -r '.[0].id')
    
    if [ "$USER_ID" != "null" ] && [ -n "$USER_ID" ]; then
        # Set password
        PASSWORD_RESPONSE=$(curl -s -X PUT "${KEYCLOAK_URL}/admin/realms/${REALM_NAME}/users/${USER_ID}/reset-password" \
            -H "Authorization: Bearer $TOKEN" \
            -H "Content-Type: application/json" \
            -d "{
                \"type\": \"password\",
                \"value\": \"$EDUCATOR1_PASSWORD\",
                \"temporary\": false
            }")
        
        if ! check_api_response "$PASSWORD_RESPONSE" "set password"; then
            echo "⚠️  User created but failed to set password"
        fi
        
        # Assign Educator role
        EDUCATOR_ROLE_ID=$(curl -s -X GET "${KEYCLOAK_URL}/admin/realms/${REALM_NAME}/roles/Educator" \
            -H "Authorization: Bearer $TOKEN" | jq -r '.id')
        
        if [ "$EDUCATOR_ROLE_ID" != "null" ] && [ -n "$EDUCATOR_ROLE_ID" ]; then
            ROLE_RESPONSE=$(curl -s -X POST "${KEYCLOAK_URL}/admin/realms/${REALM_NAME}/users/${USER_ID}/role-mappings/realm" \
                -H "Authorization: Bearer $TOKEN" \
                -H "Content-Type: application/json" \
                -d "[{
                    \"id\": \"$EDUCATOR_ROLE_ID\",
                    \"name\": \"Educator\"
                }]")
            
            if ! check_api_response "$ROLE_RESPONSE" "assign Educator role"; then
                echo "⚠️  User created but failed to assign Educator role"
            fi
            
            echo "✅ User $EDUCATOR1_USERNAME created with Educator role."
        fi
    fi
else
    echo "ℹ️ User $EDUCATOR1_USERNAME already exists in realm $REALM_NAME."
fi

# Create member1 test user
MEMBER1_USERNAME="member1"
MEMBER1_PASSWORD="MemberPass123!"

echo "👤 Checking if user $MEMBER1_USERNAME exists..."
USER_EXISTS=$(curl -s -X GET "${KEYCLOAK_URL}/admin/realms/${REALM_NAME}/users?username=${MEMBER1_USERNAME}" \
    -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json" | jq 'length')

if [ "$USER_EXISTS" -eq 0 ]; then
    echo "👤 Creating user: $MEMBER1_USERNAME in realm: $REALM_NAME"
    
    USER_RESPONSE=$(curl -s -X POST "${KEYCLOAK_URL}/admin/realms/${REALM_NAME}/users" \
        -H "Authorization: Bearer $TOKEN" \
        -H "Content-Type: application/json" \
        -d "{
            \"username\": \"$MEMBER1_USERNAME\",
            \"enabled\": true,
            \"emailVerified\": true,
            \"firstName\": \"Mark\",
            \"lastName\": \"Member\",
            \"email\": \"mark.member@example.com\"
        }")
    
    if ! check_api_response "$USER_RESPONSE" "create user"; then
        echo "❌ Failed to create user $MEMBER1_USERNAME"
        exit 1
    fi
    
    # Get user ID and set password + role
    USER_ID=$(curl -s -X GET "${KEYCLOAK_URL}/admin/realms/${REALM_NAME}/users?username=${MEMBER1_USERNAME}" \
        -H "Authorization: Bearer $TOKEN" | jq -r '.[0].id')
    
    if [ "$USER_ID" != "null" ] && [ -n "$USER_ID" ]; then
        # Set password
        PASSWORD_RESPONSE=$(curl -s -X PUT "${KEYCLOAK_URL}/admin/realms/${REALM_NAME}/users/${USER_ID}/reset-password" \
            -H "Authorization: Bearer $TOKEN" \
            -H "Content-Type: application/json" \
            -d "{
                \"type\": \"password\",
                \"value\": \"$MEMBER1_PASSWORD\",
                \"temporary\": false
            }")
        
        if ! check_api_response "$PASSWORD_RESPONSE" "set password"; then
            echo "⚠️  User created but failed to set password"
        fi
        
        # Assign Member role
        MEMBER_ROLE_ID=$(curl -s -X GET "${KEYCLOAK_URL}/admin/realms/${REALM_NAME}/roles/Member" \
            -H "Authorization: Bearer $TOKEN" | jq -r '.id')
        
        if [ "$MEMBER_ROLE_ID" != "null" ] && [ -n "$MEMBER_ROLE_ID" ]; then
            ROLE_RESPONSE=$(curl -s -X POST "${KEYCLOAK_URL}/admin/realms/${REALM_NAME}/users/${USER_ID}/role-mappings/realm" \
                -H "Authorization: Bearer $TOKEN" \
                -H "Content-Type: application/json" \
                -d "[{
                    \"id\": \"$MEMBER_ROLE_ID\",
                    \"name\": \"Member\"
                }]")
            
            if ! check_api_response "$ROLE_RESPONSE" "assign Member role"; then
                echo "⚠️  User created but failed to assign Member role"
            fi
            
            echo "✅ User $MEMBER1_USERNAME created with Member role."
        else
            echo "❌ Member role not found in Keycloak realm"
        fi
    fi
else
    echo "ℹ️ User $MEMBER1_USERNAME already exists in realm $REALM_NAME."
fi

echo "✅ Test user setup complete!"
echo ""
echo "📋 Created test users:"
echo "  👨‍💼 admin1 (AdminPass123!) - Role: Administrator [TEST ONLY]"
echo "  👨‍🏫 educator1 (EducatorPass123!) - Role: Educator [TEST ONLY]"
echo "  👤 member1 (MemberPass123!) - Role: Member [TEST ONLY]"
echo ""
echo "🔧 System admin account (separate from tests):"
echo "  👨‍💼 scoopadmin (changethis123!) - Role: Administrator [SYSTEM ACCOUNT]"
echo ""
echo "⚠️  SECURITY NOTE: admin1/educator1/member1 are for E2E testing only!"
echo "   Use scoopadmin for actual system administration and data seeding."
