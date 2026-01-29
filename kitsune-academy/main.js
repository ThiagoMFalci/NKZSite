const reveals = document.querySelectorAll('.reveal');

const observer = new IntersectionObserver(
    (entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('active');
            }
        });
    },
    { threshold: 0.15 }
);

reveals.forEach(el => observer.observe(el));

// ========== EFEITOS LEGAIS NOS BOTÕES ==========
const buttons = document.querySelectorAll('.cta-primary, .cta-secondary, .btn-primary, .btn-outline, .quick-action-btn');

buttons.forEach(btn => {
    btn.addEventListener('click', function(e) {
        // Efeito ripple
        const ripple = document.createElement('span');
        ripple.style.cssText = `
            position: absolute;
            top: 50%;
            left: 50%;
            width: 0;
            height: 0;
            background: radial-gradient(circle, rgba(255,255,255,0.5), transparent);
            border-radius: 50%;
            transform: translate(-50%, -50%);
            pointer-events: none;
            animation: ripple 0.6s ease-out;
        `;
        
        // Adicionar estilo de animação se não existir
        if (!document.querySelector('style[data-ripple]')) {
            const style = document.createElement('style');
            style.setAttribute('data-ripple', 'true');
            style.textContent = `
                @keyframes ripple {
                    0% {
                        width: 0;
                        height: 0;
                        opacity: 1;
                    }
                    100% {
                        width: 400px;
                        height: 400px;
                        opacity: 0;
                    }
                }
            `;
            document.head.appendChild(style);
        }
        
        this.appendChild(ripple);
        setTimeout(() => ripple.remove(), 600);
        
        // Efeito de desmanchar: o botão em si desaparece em padrão de piso
        const tileSize = 14;
        const tilesPerRow = Math.ceil(rect.width / tileSize);
        const tilesPerCol = Math.ceil(rect.height / tileSize);
        
        // Criar um overlay que cobre o botão
        const overlay = document.createElement('div');
        overlay.style.position = 'fixed';
        overlay.style.left = rect.left + 'px';
        overlay.style.top = rect.top + 'px';
        overlay.style.width = rect.width + 'px';
        overlay.style.height = rect.height + 'px';
        overlay.style.pointerEvents = 'none';
        overlay.style.zIndex = '9999';
        
        // Criar o padrão de piso que desaparece
        const colors = ['#9c27b0', '#5c6cff', '#7c3aed', '#4f46e5'];
        
        for (let row = 0; row < tilesPerCol; row++) {
            for (let col = 0; col < tilesPerRow; col++) {
                const tile = document.createElement('div');
                
                tile.style.position = 'absolute';
                tile.style.width = tileSize + 'px';
                tile.style.height = tileSize + 'px';
                tile.style.left = (col * tileSize) + 'px';
                tile.style.top = (row * tileSize) + 'px';
                tile.style.borderRadius = '0px';
                tile.style.border = '1px solid rgba(255, 255, 255, 0.3)';
                
                tile.style.background = colors[Math.floor(Math.random() * colors.length)];
                tile.style.boxShadow = `inset 0 0 4px ${tile.style.background}`;
                
                // Delay progressivo: esquerda para direita e cima para baixo
                const delayCol = (col / tilesPerRow) * 0.5;
                const delayRow = (row / tilesPerCol) * 0.3;
                const delay = delayCol + delayRow;
                
                tile.style.animation = `tile-disappear 0.8s ease-in-out ${delay}s forwards`;
                
                overlay.appendChild(tile);
            }
        }
        
        document.body.appendChild(overlay);
        setTimeout(() => overlay.remove(), 2000);
        
        // Efeito de clique (escala)
        this.style.transform = 'scale(0.95)';
        setTimeout(() => {
            this.style.transform = '';
        }, 100);
        
        // Efeito de luz/flash
        const flash = document.createElement('div');
        flash.style.cssText = `
            position: fixed;
            top: ${centerY}px;
            left: ${centerX}px;
            width: 30px;
            height: 30px;
            background: radial-gradient(circle, rgba(156,39,176,0.8), transparent);
            border-radius: 50%;
            pointer-events: none;
            z-index: 9998;
            animation: flash-out 0.4s ease-out forwards;
        `;
        
        if (!document.querySelector('style[data-flash]')) {
            const style = document.createElement('style');
            style.setAttribute('data-flash', 'true');
            style.textContent = `
                @keyframes flash-out {
                    0% {
                        width: 30px;
                        height: 30px;
                        opacity: 1;
                        box-shadow: 0 0 20px rgba(156,39,176,0.8);
                    }
                    100% {
                        width: 200px;
                        height: 200px;
                        opacity: 0;
                        box-shadow: 0 0 50px rgba(156,39,176,0);
                    }
                }
            `;
            document.head.appendChild(style);
        }
        
        document.body.appendChild(flash);
        setTimeout(() => flash.remove(), 400);
    });
    
    // Efeito hover com glow
    btn.addEventListener('mouseenter', function() {
        this.style.textShadow = '0 0 20px rgba(156,39,176,0.6)';
    });
    
    btn.addEventListener('mouseleave', function() {
        this.style.textShadow = '';
    });
});
