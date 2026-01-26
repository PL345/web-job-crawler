// API Base URL - defaults to same origin with /api path
// This works because nginx proxy serves both frontend and backend on same port
const getAPIUrl = () => {
  if (import.meta.env.VITE_API_URL) {
    return import.meta.env.VITE_API_URL;
  }
  
  // Use same origin (handled by nginx proxy)
  // Frontend and API are both served from the same origin
  const origin = window.location.origin;
  return `${origin}/api`;
};

export const API_BASE_URL = getAPIUrl();
