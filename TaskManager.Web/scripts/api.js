// API İşlemleri için Merkezi Yönetim
const API_BASE_URL = 'http://localhost:5022/api';

class ApiService {
    // Generic fetch metodu
    static async request(endpoint, method = 'GET', body = null, isAuthenticated = false) {
        const headers = {
            'Content-Type': 'application/json'
        };

        // Token gerektiren istekler için
        if (isAuthenticated) {
            const token = localStorage.getItem('token');
            if (!token) {
                throw new Error('Kimlik doğrulama hatası');
            }
            headers['Authorization'] = `Bearer ${token}`;
        }

        const config = {
            method,
            headers
        };

        // Body varsa ekle
        if (body) {
            config.body = JSON.stringify(body);
        }

        try {
            const response = await fetch(`${API_BASE_URL}${endpoint}`, config);
            
            // Response'un içeriğini kontrol et
            const contentType = response.headers.get('content-type');
            const data = contentType && contentType.includes('application/json') 
                ? await response.json() 
                : await response.text();

            if (!response.ok) {
                // Hata mesajını parse et
                const errorMessage = typeof data === 'object' 
                    ? (data.message || 'Bir hata oluştu') 
                    : data || 'Bir hata oluştu';
                throw new Error(errorMessage);
            }

            return data;
        } catch (error) {
            console.error('API Error:', error);
            throw error;
        }
    }

    // Giriş metodu
    static async login(username, password) {
        return this.request('/Auth/login', 'POST', { username, password });
    }

    // Kayıt metodu
    static async register(username, email, password, confirmPassword) {
        return this.request('/Auth/register', 'POST', { 
            username, 
            email, 
            password, 
            confirmPassword 
        });
    }
}

// Global hata yakalama
window.addEventListener('unhandledrejection', event => {
    console.error('Unhandled Promise Rejection:', event.reason);
});