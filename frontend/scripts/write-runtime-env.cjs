const fs = require('fs');
const path = require('path');

const packageJsonPath = path.join(__dirname, '..', 'package.json');
const publicEnvPath = path.join(__dirname, '..', 'public', 'env.js');

const packageJson = JSON.parse(fs.readFileSync(packageJsonPath, 'utf8'));

const runtimeEnv = {
  NEXT_PUBLIC_KEYCLOAK_URL: process.env.NEXT_PUBLIC_KEYCLOAK_URL || '',
  NEXT_PUBLIC_KEYCLOAK_REALM: process.env.NEXT_PUBLIC_KEYCLOAK_REALM || '',
  NEXT_PUBLIC_KEYCLOAK_CLIENT_ID: process.env.NEXT_PUBLIC_KEYCLOAK_CLIENT_ID || '',
  NEXT_PUBLIC_API_URL: process.env.NEXT_PUBLIC_API_URL || process.env.API_BASE_URL || '',
  NEXT_PUBLIC_PORTAL_BASE_URL: process.env.NEXT_PUBLIC_PORTAL_BASE_URL || '',
  NEXT_PUBLIC_APP_VERSION: process.env.NEXT_PUBLIC_APP_VERSION || packageJson.version || 'unknown',
};

const fileContents = `window.__ENV__ = ${JSON.stringify(runtimeEnv, null, 2)};\n`;

fs.writeFileSync(publicEnvPath, fileContents, 'utf8');
console.log(`Wrote runtime env to ${publicEnvPath}`);