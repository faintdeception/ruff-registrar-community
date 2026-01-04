#!/bin/bash

# setup-keycloak.sh - Set up Keycloak realm and roles for Student Registrar

set -e

KEYCLOAK_URL="http://localhost:8080"
# Aspire dev default master admin username is typically 'admin'.
ADMIN_USER="admin"
REALM_NAME="student-registrar"
CLIENT_ID="student-registrar"

echo "🔐 Setting up Keycloak for Student Registrar"
echo "============================================="
echo ""

# Check for required tools
if ! command -v jq &> /dev/null; then
    echo "❌ jq is required but not installed. Please install jq first."
    exit 1
fi

if ! command -v curl &> /dev/null; then
    echo "❌ curl is required but not installed. Please install curl first."
    exit 1
fi

# Function to check if command succeeded
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

# Function to get admin access token
get_admin_token() {
    local admin_password=$1
    curl -s -X POST "${KEYCLOAK_URL}/realms/master/protocol/openid-connect/token" \
        -H "Content-Type: application/x-www-form-urlencoded" \
        --data-urlencode "username=${ADMIN_USER}" \
        --data-urlencode "password=${admin_password}" \
        -d "grant_type=password" \
        -d "client_id=admin-cli" | jq -r '.access_token'
}

# Prompt for admin password
echo "📋 First, get your Keycloak admin password:"
echo "   1. Start your application: dotnet run --project src/StudentRegistrar.AppHost"
echo "   2. Open Aspire Dashboard: http://localhost:15888"
echo "   3. Go to Resources tab and find the Keycloak admin password"
echo ""

# Test Keycloak connectivity first
echo "🔍 Testing Keycloak connectivity..."
echo "   Trying to reach: ${KEYCLOAK_URL}/realms/master"
KEYCLOAK_STATUS=$(curl -s -o /dev/null -w "%{http_code}" --max-time 10 "${KEYCLOAK_URL}/realms/master")
CURL_EXIT_CODE=$?

if [ $CURL_EXIT_CODE -ne 0 ]; then
    echo "❌ Failed to connect to Keycloak (curl exit code: $CURL_EXIT_CODE)"
    case $CURL_EXIT_CODE in
        7) echo "   Error: Failed to connect to host" ;;
        28) echo "   Error: Connection timeout" ;;
        *) echo "   Error: Unknown curl error" ;;
    esac
    echo ""
    echo "💡 Troubleshooting steps:"
    echo "   1. Check if your application is running: dotnet run --project src/StudentRegistrar.AppHost"
    echo "   2. Check if Keycloak is accessible in browser: ${KEYCLOAK_URL}"
    echo "   3. Verify the port in Aspire Dashboard: http://localhost:15888"
    exit 1
fi

if [ "$KEYCLOAK_STATUS" != "200" ]; then
    echo "❌ Keycloak returned HTTP status: $KEYCLOAK_STATUS"
    echo "   URL: ${KEYCLOAK_URL}/realms/master"
    echo "   Please ensure Keycloak is running and accessible."
    exit 1
fi
echo "✅ Keycloak is accessible"

if [ -n "${KEYCLOAK_ADMIN_PASSWORD:-}" ]; then
    ADMIN_PASSWORD="$KEYCLOAK_ADMIN_PASSWORD"
    echo "🔑 Using KEYCLOAK_ADMIN_PASSWORD from environment"
else
    read -s -p "Enter Keycloak admin password: " ADMIN_PASSWORD
    echo ""
fi

# Get admin token
echo "🔑 Getting admin access token..."
TOKEN_RESPONSE=$(curl -s -X POST "${KEYCLOAK_URL}/realms/master/protocol/openid-connect/token" \
    -H "Content-Type: application/x-www-form-urlencoded" \
    --data-urlencode "username=${ADMIN_USER}" \
    --data-urlencode "password=${ADMIN_PASSWORD}" \
    -d "grant_type=password" \
    -d "client_id=admin-cli")

echo "Debug - Token response: $TOKEN_RESPONSE"

TOKEN=$(echo "$TOKEN_RESPONSE" | jq -r '.access_token')

if [ "$TOKEN" == "null" ] || [ -z "$TOKEN" ]; then
    echo "❌ Failed to get admin token. Please check your password and try again."
    echo "Full response: $TOKEN_RESPONSE"
    exit 1
fi

echo "✅ Admin token obtained successfully"

# Create realm
echo "🏗️  Creating realm: $REALM_NAME"
curl -s -X POST "${KEYCLOAK_URL}/admin/realms" \
    -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json" \
    -d "{
        \"realm\": \"$REALM_NAME\",
        \"enabled\": true,
        \"displayName\": \"Student Registrar\",
        \"loginWithEmailAllowed\": true,
        \"registrationAllowed\": false,
        \"rememberMe\": true,
        \"verifyEmail\": false,
        \"resetPasswordAllowed\": true
    }" || echo "Realm may already exist"

# Create roles
echo "👥 Creating user roles..."
ROLES=("Administrator" "Educator" "Member")

for role in "${ROLES[@]}"; do
    echo "   Creating role: $role"
    curl -s -X POST "${KEYCLOAK_URL}/admin/realms/$REALM_NAME/roles" \
        -H "Authorization: Bearer $TOKEN" \
        -H "Content-Type: application/json" \
        -d "{
            \"name\": \"$role\",
            \"description\": \"$role role for Student Registrar\"
        }" || echo "   Role $role may already exist"
done

# Create client
echo "🔗 Creating client: $CLIENT_ID"
curl -s -X POST "${KEYCLOAK_URL}/admin/realms/$REALM_NAME/clients" \
    -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json" \
    -d "{
        \"clientId\": \"$CLIENT_ID\",
        \"enabled\": true,
        \"publicClient\": false,
        \"bearerOnly\": false,
        \"standardFlowEnabled\": true,
        \"directAccessGrantsEnabled\": true,
        \"serviceAccountsEnabled\": true,
        \"redirectUris\": [\"http://localhost:3000/*\", \"http://localhost:3001/*\"],
        \"webOrigins\": [\"http://localhost:3000\", \"http://localhost:3001\"],
        \"attributes\": {
            \"saml.assertion.signature\": \"false\",
            \"saml.force.post.binding\": \"false\",
            \"saml.multivalued.roles\": \"false\",
            \"saml.encrypt\": \"false\",
            \"saml.server.signature\": \"false\",
            \"saml.server.signature.keyinfo.ext\": \"false\",
            \"exclude.session.state.from.auth.response\": \"false\",
            \"saml_force_name_id_format\": \"false\",
            \"saml.client.signature\": \"false\",
            \"tls.client.certificate.bound.access.tokens\": \"false\",
            \"saml.authnstatement\": \"false\",
            \"display.on.consent.screen\": \"false\",
            \"saml.onetimeuse.condition\": \"false\"
        }
    }" || echo "Client may already exist"

# Get client secret
echo "🔐 Retrieving client secret..."
CLIENT_UUID=$(curl -s -X GET "${KEYCLOAK_URL}/admin/realms/$REALM_NAME/clients?clientId=$CLIENT_ID" \
    -H "Authorization: Bearer $TOKEN" | jq -r '.[0].id')

if [ "$CLIENT_UUID" != "null" ] && [ -n "$CLIENT_UUID" ]; then
    CLIENT_SECRET=$(curl -s -X GET "${KEYCLOAK_URL}/admin/realms/$REALM_NAME/clients/$CLIENT_UUID/client-secret" \
        -H "Authorization: Bearer $TOKEN" | jq -r '.value')
    
    # Grant service account permissions for user management
    echo "🔧 Configuring service account permissions..."
    
    # Get the service account user ID
    SERVICE_ACCOUNT_USER=$(curl -s -X GET "${KEYCLOAK_URL}/admin/realms/$REALM_NAME/clients/$CLIENT_UUID/service-account-user" \
        -H "Authorization: Bearer $TOKEN")
    
    SERVICE_ACCOUNT_USER_ID=$(echo "$SERVICE_ACCOUNT_USER" | jq -r '.id')
    
    if [ "$SERVICE_ACCOUNT_USER_ID" != "null" ] && [ -n "$SERVICE_ACCOUNT_USER_ID" ]; then
        # Get the realm-management client ID
        REALM_MANAGEMENT_CLIENT=$(curl -s -X GET "${KEYCLOAK_URL}/admin/realms/$REALM_NAME/clients?clientId=realm-management" \
            -H "Authorization: Bearer $TOKEN" | jq -r '.[0].id')
        
        if [ "$REALM_MANAGEMENT_CLIENT" != "null" ] && [ -n "$REALM_MANAGEMENT_CLIENT" ]; then
            # Get the manage-users role
            MANAGE_USERS_ROLE=$(curl -s -X GET "${KEYCLOAK_URL}/admin/realms/$REALM_NAME/clients/$REALM_MANAGEMENT_CLIENT/roles/manage-users" \
                -H "Authorization: Bearer $TOKEN")
            
            MANAGE_USERS_ROLE_ID=$(echo "$MANAGE_USERS_ROLE" | jq -r '.id')
            
            if [ "$MANAGE_USERS_ROLE_ID" != "null" ] && [ -n "$MANAGE_USERS_ROLE_ID" ]; then
                # Grant the manage-users role to the service account
                GRANT_RESPONSE=$(curl -s -X POST "${KEYCLOAK_URL}/admin/realms/$REALM_NAME/users/$SERVICE_ACCOUNT_USER_ID/role-mappings/clients/$REALM_MANAGEMENT_CLIENT" \
                    -H "Authorization: Bearer $TOKEN" \
                    -H "Content-Type: application/json" \
                    -d "[{
                        \"id\": \"$MANAGE_USERS_ROLE_ID\",
                        \"name\": \"manage-users\"
                    }]")
                
                if [ $? -eq 0 ]; then
                    echo "✅ Service account granted manage-users permission"
                else
                    echo "⚠️  Failed to grant manage-users permission to service account"
                fi
            else
                echo "⚠️  Could not find manage-users role"
            fi
        else
            echo "⚠️  Could not find realm-management client"
        fi
    else
        echo "⚠️  Could not find service account user"
    fi
    
    # Create scoopadmin user if not exists
    SCOOPADMIN_USERNAME="scoopadmin"
    SCOOPADMIN_PASSWORD="changethis123!"

    # Check if user exists
    USER_EXISTS=$(curl -s -X GET "${KEYCLOAK_URL}/admin/realms/${REALM_NAME}/users?username=${SCOOPADMIN_USERNAME}" \
        -H "Authorization: Bearer $TOKEN" \
        -H "Content-Type: application/json" | jq 'length')

    if [ "$USER_EXISTS" -eq 0 ]; then
        echo "👤 Creating user: $SCOOPADMIN_USERNAME in realm: $REALM_NAME"
        
        # Create user
        USER_RESPONSE=$(curl -s -X POST "${KEYCLOAK_URL}/admin/realms/${REALM_NAME}/users" \
            -H "Authorization: Bearer $TOKEN" \
            -H "Content-Type: application/json" \
            -d "{
                \"username\": \"$SCOOPADMIN_USERNAME\",
                \"enabled\": true,
                \"emailVerified\": true,
                \"firstName\": \"Scoop\",
                \"lastName\": \"Admin\",
                \"email\": \"scoopadmin@example.com\"
            }")
        
        if ! check_api_response "$USER_RESPONSE" "create user"; then
            echo "❌ Failed to create user $SCOOPADMIN_USERNAME"
            exit 1
        fi
        
        # Get user ID by querying for the user
        USER_ID=$(curl -s -X GET "${KEYCLOAK_URL}/admin/realms/${REALM_NAME}/users?username=${SCOOPADMIN_USERNAME}" \
            -H "Authorization: Bearer $TOKEN" | jq -r '.[0].id')
        
        if [ "$USER_ID" != "null" ] && [ -n "$USER_ID" ]; then
            # Set password
            PASSWORD_RESPONSE=$(curl -s -X PUT "${KEYCLOAK_URL}/admin/realms/${REALM_NAME}/users/${USER_ID}/reset-password" \
                -H "Authorization: Bearer $TOKEN" \
                -H "Content-Type: application/json" \
                -d "{
                    \"type\": \"password\",
                    \"value\": \"$SCOOPADMIN_PASSWORD\",
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
                
                echo "✅ User $SCOOPADMIN_USERNAME created with Administrator role."
            else
                echo "⚠️  User created but failed to get Administrator role ID"
            fi
        else
            echo "❌ Failed to get user ID after creation"
        fi
    else
        echo "ℹ️ User $SCOOPADMIN_USERNAME already exists in realm $REALM_NAME."
    fi
    
    # Create scoopmember user
    SCOOPMEMBER_USERNAME="scoopmember"
    SCOOPMEMBER_PASSWORD="changethis123"
    
    echo "👤 Checking if user $SCOOPMEMBER_USERNAME exists..."
    MEMBER_USER_EXISTS=$(curl -s -X GET "${KEYCLOAK_URL}/admin/realms/${REALM_NAME}/users?username=${SCOOPMEMBER_USERNAME}" \
        -H "Authorization: Bearer $TOKEN" \
        -H "Content-Type: application/json" | jq 'length')

    if [ "$MEMBER_USER_EXISTS" -eq 0 ]; then
        echo "👤 Creating user: $SCOOPMEMBER_USERNAME in realm: $REALM_NAME"
        
        # Create user
        USER_RESPONSE=$(curl -s -X POST "${KEYCLOAK_URL}/admin/realms/${REALM_NAME}/users" \
            -H "Authorization: Bearer $TOKEN" \
            -H "Content-Type: application/json" \
            -d "{
                \"username\": \"$SCOOPMEMBER_USERNAME\",
                \"enabled\": true,
                \"emailVerified\": true,
                \"firstName\": \"Scoop\",
                \"lastName\": \"Member\",
                \"email\": \"scoopmember@example.com\"
            }")
        
        if ! check_api_response "$USER_RESPONSE" "create user"; then
            echo "❌ Failed to create user $SCOOPMEMBER_USERNAME"
            exit 1
        fi
        
        # Get user ID by querying for the user
        USER_ID=$(curl -s -X GET "${KEYCLOAK_URL}/admin/realms/${REALM_NAME}/users?username=${SCOOPMEMBER_USERNAME}" \
            -H "Authorization: Bearer $TOKEN" | jq -r '.[0].id')
        
        if [ "$USER_ID" != "null" ] && [ -n "$USER_ID" ]; then
            # Set password
            PASSWORD_RESPONSE=$(curl -s -X PUT "${KEYCLOAK_URL}/admin/realms/${REALM_NAME}/users/${USER_ID}/reset-password" \
                -H "Authorization: Bearer $TOKEN" \
                -H "Content-Type: application/json" \
                -d "{
                    \"type\": \"password\",
                    \"value\": \"$SCOOPMEMBER_PASSWORD\",
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
                
                echo "✅ User $SCOOPMEMBER_USERNAME created with Member role."
            else
                echo "⚠️  User created but failed to get Member role ID"
            fi
        else
            echo "❌ Failed to get user ID after creation"
        fi
    else
        echo "ℹ️ User $SCOOPMEMBER_USERNAME already exists in realm $REALM_NAME."
    fi
    
    # Create scoopinstructor user
    SCOOPINSTRUCTOR_USERNAME="scoopinstructor"
    SCOOPINSTRUCTOR_PASSWORD="changethis123"
    
    echo "👤 Checking if user $SCOOPINSTRUCTOR_USERNAME exists..."
    INSTRUCTOR_USER_EXISTS=$(curl -s -X GET "${KEYCLOAK_URL}/admin/realms/${REALM_NAME}/users?username=${SCOOPINSTRUCTOR_USERNAME}" \
        -H "Authorization: Bearer $TOKEN" \
        -H "Content-Type: application/json" | jq 'length')

    if [ "$INSTRUCTOR_USER_EXISTS" -eq 0 ]; then
        echo "👤 Creating user: $SCOOPINSTRUCTOR_USERNAME in realm: $REALM_NAME"
        
        # Create user
        USER_RESPONSE=$(curl -s -X POST "${KEYCLOAK_URL}/admin/realms/${REALM_NAME}/users" \
            -H "Authorization: Bearer $TOKEN" \
            -H "Content-Type: application/json" \
            -d "{
                \"username\": \"$SCOOPINSTRUCTOR_USERNAME\",
                \"enabled\": true,
                \"emailVerified\": true,
                \"firstName\": \"Scoop\",
                \"lastName\": \"Instructor\",
                \"email\": \"scoopinstructor@example.com\"
            }")
        
        if ! check_api_response "$USER_RESPONSE" "create user"; then
            echo "❌ Failed to create user $SCOOPINSTRUCTOR_USERNAME"
            exit 1
        fi
        
        # Get user ID by querying for the user
        USER_ID=$(curl -s -X GET "${KEYCLOAK_URL}/admin/realms/${REALM_NAME}/users?username=${SCOOPINSTRUCTOR_USERNAME}" \
            -H "Authorization: Bearer $TOKEN" | jq -r '.[0].id')
        
        if [ "$USER_ID" != "null" ] && [ -n "$USER_ID" ]; then
            # Set password
            PASSWORD_RESPONSE=$(curl -s -X PUT "${KEYCLOAK_URL}/admin/realms/${REALM_NAME}/users/${USER_ID}/reset-password" \
                -H "Authorization: Bearer $TOKEN" \
                -H "Content-Type: application/json" \
                -d "{
                    \"type\": \"password\",
                    \"value\": \"$SCOOPINSTRUCTOR_PASSWORD\",
                    \"temporary\": false
                }")
            
            if ! check_api_response "$PASSWORD_RESPONSE" "set password"; then
                echo "⚠️  User created but failed to set password"
            fi
            
            # Assign Instructor role
            INSTRUCTOR_ROLE_ID=$(curl -s -X GET "${KEYCLOAK_URL}/admin/realms/${REALM_NAME}/roles/Instructor" \
                -H "Authorization: Bearer $TOKEN" | jq -r '.id')
            
            if [ "$INSTRUCTOR_ROLE_ID" != "null" ] && [ -n "$INSTRUCTOR_ROLE_ID" ]; then
                ROLE_RESPONSE=$(curl -s -X POST "${KEYCLOAK_URL}/admin/realms/${REALM_NAME}/users/${USER_ID}/role-mappings/realm" \
                    -H "Authorization: Bearer $TOKEN" \
                    -H "Content-Type: application/json" \
                    -d "[{
                        \"id\": \"$INSTRUCTOR_ROLE_ID\",
                        \"name\": \"Instructor\"
                    }]")
                
                if ! check_api_response "$ROLE_RESPONSE" "assign Instructor role"; then
                    echo "⚠️  User created but failed to assign Instructor role"
                fi
                
                echo "✅ User $SCOOPINSTRUCTOR_USERNAME created with Instructor role."
            else
                echo "⚠️  User created but failed to get Instructor role ID"
            fi
        else
            echo "❌ Failed to get user ID after creation"
        fi
    else
        echo "ℹ️ User $SCOOPINSTRUCTOR_USERNAME already exists in realm $REALM_NAME."
    fi
    
    echo "✅ Setup complete!"
    echo ""
    echo "📋 Configuration Summary:"
    echo "========================="
    echo "Realm: $REALM_NAME"
    echo "Client ID: $CLIENT_ID"
    echo "Client Secret: $CLIENT_SECRET"
    echo "Keycloak URL: $KEYCLOAK_URL"
    echo ""
    echo "👤 Test Users Created:"
    echo "====================="
    echo "Username: $SCOOPADMIN_USERNAME"
    echo "Password: $SCOOPADMIN_PASSWORD"
    echo "Email: scoopadmin@example.com"
    echo "Role: Administrator"
    echo ""
    echo "Username: $SCOOPMEMBER_USERNAME"
    echo "Password: $SCOOPMEMBER_PASSWORD"
    echo "Email: scoopmember@example.com"
    echo "Role: Member"
    echo ""
    echo "Username: $SCOOPINSTRUCTOR_USERNAME"
    echo "Password: $SCOOPINSTRUCTOR_PASSWORD"
    echo "Email: scoopinstructor@example.com"
    echo "Role: Instructor"
    echo ""
    echo "🔧 Add this to your API configuration:"
    echo "====================================="
    echo "{"
    echo "  \"Keycloak\": {"
    echo "    \"Realm\": \"$REALM_NAME\","
    echo "    \"ClientId\": \"$CLIENT_ID\","
    echo "    \"ClientSecret\": \"$CLIENT_SECRET\""
    echo "  }"
    echo "}"
    echo ""
    echo "💡 Or set as user secrets:"
    echo "=========================="
    echo "dotnet user-secrets set \"Keycloak:Realm\" \"$REALM_NAME\" --project src/StudentRegistrar.AppHost"
    echo "dotnet user-secrets set \"Keycloak:ClientId\" \"$CLIENT_ID\" --project src/StudentRegistrar.AppHost"
    echo "dotnet user-secrets set \"Keycloak:ClientSecret\" \"$CLIENT_SECRET\" --project src/StudentRegistrar.AppHost"
    echo ""
else
    echo "❌ Failed to retrieve client information"
fi

echo "🎉 Keycloak setup complete! Your configuration will now persist across restarts."
echo "   You can now create users via the API or Keycloak Admin Console."
