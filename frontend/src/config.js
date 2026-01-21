// API Base URL - use environment variable or detect from current location
// For GitHub Codespaces: converts 3000 port number in domain to 5000
// For localhost: converts localhost:3000 to localhost:5000
const getAPIUrl = () => {
  if (import.meta.env.VITE_API_URL) {
    return import.meta.env.VITE_API_URL;
  }
  
  const origin = window.location.origin;
  
  // Handle GitHub Codespaces format: https://name-3000.app.github.dev/ -> https://name-5000.app.github.dev/
  if (origin.includes('-3000.')) {
    const url = origin.replace('-3000.', '-5000.');
    console.log('Using Codespaces API URL:', url);
    return url;
  }
  
  // Handle localhost format: http://localhost:3000 -> http://localhost:5000
  const url = origin.replace(':3000', ':5000');
  console.log('Using localhost API URL:', url);
  return url;
};

export const API_BASE_URL = getAPIUrl();
