document.addEventListener('DOMContentLoaded', () => {
    // Tab değişikliği
    const loginTab = document.getElementById('loginTab');
    const registerTab = document.getElementById('registerTab');
    const loginForm = document.getElementById('loginForm');
    const registerForm = document.getElementById('registerForm');

    loginTab.addEventListener('click', () => {
        loginTab.classList.add('active');
        registerTab.classList.remove('active');
        loginForm.style.display = 'block';
        registerForm.style.display = 'none';
    });

    registerTab.addEventListener('click', () => {
        registerTab.classList.add('active');
        loginTab.classList.remove('active');
        registerForm.style.display = 'block';
        loginForm.style.display = 'none';
    });

    // Giriş Formu
    loginForm.addEventListener('submit', async (e) => {
        e.preventDefault();
        const loginErrorDiv = document.getElementById('loginError');
        loginErrorDiv.textContent = '';

        const username = document.getElementById('loginUsername').value;
        const password = document.getElementById('loginPassword').value;

        try {
            const result = await ApiService.login(username, password);

            // Token ve kullanıcı bilgilerini kaydet
            localStorage.setItem('token', result.token);
            localStorage.setItem('userId', result.userId);
            localStorage.setItem('username', result.username);

            // Başarılı giriş sonrası dashboard'a yönlendir
            window.location.href = 'pages/dashboard.html';
        } catch (error) {
            loginErrorDiv.textContent = error.message || 'Giriş başarısız';
        }
    });

    // Kayıt Formu
    registerForm.addEventListener('submit', async (e) => {
        e.preventDefault();
        const registerErrorDiv = document.getElementById('registerError');
        registerErrorDiv.textContent = '';

        const username = document.getElementById('registerUsername').value;
        const email = document.getElementById('registerEmail').value;
        const password = document.getElementById('registerPassword').value;
        const confirmPassword = document.getElementById('registerConfirmPassword').value;

        // Şifre kontrolleri
        if (password !== confirmPassword) {
            registerErrorDiv.textContent = 'Şifreler eşleşmiyor';
            return;
        }

        if (password.length < 6) {
            registerErrorDiv.textContent = 'Şifre en az 6 karakter olmalıdır';
            return;
        }

        try {
            await ApiService.register(username, email, password, confirmPassword);
            
            // Başarılı kayıt sonrası login tabına geç
            loginTab.click();
            alert('Kayıt başarılı. Lütfen giriş yapın.');
        } catch (error) {
            registerErrorDiv.textContent = error.message || 'Kayıt başarısız';
        }
    });

    // Token kontrolü
    function checkAuthentication() {
        const token = localStorage.getItem('token');
        const currentPath = window.location.pathname;

        // Eğer token varsa ve giriş sayfasındaysa dashboard'a yönlendir
        if (token && (currentPath.includes('index.html') || currentPath === '/')) {
            window.location.href = 'pages/dashboard.html';
        }
        
        // Eğer token yoksa ve korumalı sayfadaysa giriş sayfasına yönlendir
        if (!token && (currentPath.includes('dashboard.html') || currentPath.includes('tasks.html'))) {
            window.location.href = '../index.html';
        }
    }

    // Sayfa yüklendiğinde authentication kontrolünü yap
    checkAuthentication();
});