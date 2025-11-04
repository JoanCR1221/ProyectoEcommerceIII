// wwwroot/js/productDetailsPublic.js
// Funcionalidades para la vista pública de detalles de producto

class ProductDetailsPublic {
    constructor(productId, categoryId) {
        this.productId = productId;
        this.categoryId = categoryId;
        this.init();
    }

    init() {
        this.setupAddToCart();
        this.loadRelatedProducts();
    }

    setupAddToCart() {
        const addToCartForms = document.querySelectorAll('.add-to-cart-form');

        addToCartForms.forEach(form => {
            form.addEventListener('submit', function (e) {
                const button = this.querySelector('.add-to-cart-btn');
                const buttonText = button.querySelector('.button-text');
                const spinner = button.querySelector('.spinner-border');

                // Mostrar loading inmediatamente - el servidor se encargará del resto
                button.disabled = true;
                buttonText.innerHTML = '<i class="fas fa-spinner fa-spin me-1"></i>Procesando...';
                spinner.classList.remove('d-none');

                // Permitir que el formulario se envíe normalmente
                // El servidor redirigirá al login si es necesario
            });
        });
    }

    addToCart(productId) {
        // Lógica para agregar al carrito
        console.log('Agregando producto al carrito:', productId);

        // Mostrar feedback al usuario
        const btn = document.querySelector('.add-to-cart');
        const originalText = btn.innerHTML;

        btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Agregando...';
        btn.disabled = true;

        // Simular llamada a API (reemplazar con lógica real)
        setTimeout(() => {
            btn.innerHTML = '<i class="fas fa-check"></i> ¡Agregado!';
            btn.classList.remove('btn-primary');
            btn.classList.add('btn-success');

            // Mostrar notificación
            this.showNotification('Producto agregado al carrito', 'success');

            // Restaurar después de 2 segundos
            setTimeout(() => {
                btn.innerHTML = originalText;
                btn.disabled = false;
                btn.classList.remove('btn-success');
                btn.classList.add('btn-primary');
            }, 2000);
        }, 1000);
    }

    async loadRelatedProducts() {
        try {
            const response = await fetch(`/Products/GetRelatedProducts?categoryId=${this.categoryId}&currentProductId=${this.productId}`);

            if (!response.ok) {
                throw new Error('Error al cargar productos relacionados');
            }

            const products = await response.json();
            this.renderRelatedProducts(products);
        } catch (error) {
            console.error('Error cargando productos relacionados:', error);
            this.showRelatedProductsError();
        }
    }

    renderRelatedProducts(products) {
        const container = document.getElementById('relatedProducts');

        if (!products || products.length === 0) {
            container.innerHTML = '<p class="text-muted text-center">No hay productos relacionados disponibles</p>';
            return;
        }

        container.innerHTML = `
            <div class="row">
                ${products.map(product => `
                    <div class="col-md-3 mb-4">
                        <div class="card h-100 product-card">
                            <img src="${product.imageUrl || '/images/no-image.png'}" 
                                 class="card-img-top" 
                                 alt="${product.name}"
                                 style="height: 200px; object-fit: cover;"
                                 onerror="this.src='/images/no-image.png'">
                            <div class="card-body d-flex flex-column">
                                <h6 class="card-title">${product.name}</h6>
                                <p class="card-text text-primary fw-bold mt-auto">₡${product.price.toFixed(2)}</p>
                                <a href="/Products/DetailsPublic/${product.id}" 
                                   class="btn btn-sm btn-outline-primary w-100 mt-2">
                                    Ver Producto
                                </a>
                            </div>
                        </div>
                    </div>
                `).join('')}
            </div>
        `;
    }

    showRelatedProductsError() {
        const container = document.getElementById('relatedProducts');
        container.innerHTML = `
            <div class="alert alert-warning text-center">
                <i class="fas fa-exclamation-triangle"></i>
                No se pudieron cargar los productos relacionados
            </div>
        `;
    }

    showNotification(message, type = 'info') {
        // Crear notificación toast
        const toast = document.createElement('div');
        toast.className = `alert alert-${type} alert-dismissible fade show position-fixed`;
        toast.style.cssText = `
            top: 20px; 
            right: 20px; 
            z-index: 1050; 
            min-width: 300px;
        `;
        toast.innerHTML = `
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `;

        document.body.appendChild(toast);

        // Auto-remover después de 5 segundos
        setTimeout(() => {
            if (toast.parentNode) {
                toast.remove();
            }
        }, 5000);
    }
}

// Inicializar cuando el DOM esté listo
document.addEventListener('DOMContentLoaded', function () {
    // Obtener IDs del data attributes o de variables globales
    const productId = document.querySelector('[data-product-id]')?.getAttribute('data-product-id');
    const categoryId = document.querySelector('[data-category-id]')?.getAttribute('data-category-id');

    if (productId && categoryId) {
        window.productDetails = new ProductDetailsPublic(productId, categoryId);
    }
});



// Spinner para agregar al carrito con delay
document.addEventListener('DOMContentLoaded', function () {
    const addToCartForms = document.querySelectorAll('.add-to-cart-form');

    addToCartForms.forEach(form => {
        form.addEventListener('submit', async function (e) {
            e.preventDefault(); // Prevenir envío inmediato

            const button = this.querySelector('.add-to-cart-btn');
            const buttonText = button.querySelector('.button-text');
            const spinner = button.querySelector('.spinner-border');

            // Mostrar spinner y deshabilitar botón
            button.disabled = true;
            buttonText.innerHTML = '<i class="fas fa-spinner fa-spin me-1"></i>Agregando...';
            spinner.classList.remove('d-none');

            try {
                // Delay artificial de 1.5 segundos
                await new Promise(resolve => setTimeout(resolve, 1500));

                // Crear un formulario temporal para enviar los datos
                const formData = new FormData(this);
                const response = await fetch(this.action, {
                    method: 'POST',
                    body: formData
                });

                if (response.ok) {
                    // Éxito - mostrar confirmación
                    buttonText.innerHTML = '<i class="fas fa-check me-1"></i>¡Agregado!';
                    spinner.classList.add('d-none');

                    // Recargar después de 1 segundo más
                    setTimeout(() => {
                        window.location.reload();
                    }, 1000);

                } else {
                    throw new Error('Error al agregar al carrito');
                }

            } catch (error) {
                console.error('Error:', error);
                buttonText.innerHTML = '<i class="fas fa-exclamation-triangle me-1"></i>Error';
                spinner.classList.add('d-none');

                // Restaurar después de 2 segundos
                setTimeout(() => {
                    resetAddToCartButton(button);
                }, 2000);
            }
        });
    });

    function resetAddToCartButton(button) {
        const buttonText = button.querySelector('.button-text');
        const spinner = button.querySelector('.spinner-border');

        button.disabled = false;
        buttonText.innerHTML = '<i class="fas fa-cart-plus me-1"></i>Agregar al Carrito';
        spinner.classList.add('d-none');
    }

    
});


                // JavaScript para botones "Agregar al Carrito" en favoritos
document.addEventListener('DOMContentLoaded', function () {
    const addToCartButtons = document.querySelectorAll('.add-to-cart');

    addToCartButtons.forEach(button => {
        button.addEventListener('click', async function (e) {
            e.preventDefault();

            const productId = this.getAttribute('data-product-id');
            const buttonText = this.querySelector('.button-text');
            const spinner = this.querySelector('.spinner-border');

            // Verificar que existan los elementos
            if (!buttonText || !spinner) {
                console.error('No se encontraron los elementos del spinner');
                return;
            }

            // Mostrar spinner y deshabilitar botón
            this.disabled = true;
            buttonText.innerHTML = '<i class="fas fa-spinner fa-spin me-1"></i>Agregando...';
            spinner.classList.remove('d-none');

            try {
                // Delay de 1.5 segundos
                await new Promise(resolve => setTimeout(resolve, 1500));

                // Obtener el token anti-forgery
                const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

                const response = await fetch('/ShoppingCarts/AddToCart', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': token
                    },
                    body: JSON.stringify({
                        productId: parseInt(productId),
                        quantity: 1
                    })
                });

                if (response.ok) {
                    buttonText.innerHTML = '<i class="fas fa-check me-1"></i>¡Agregado!';
                    spinner.classList.add('d-none');
                    
                    setTimeout(() => {
                        window.location.reload();
                    }, 1000);
                } else {
                    throw new Error('Error en la respuesta del servidor');
                }

            } catch (error) {
                console.error('Error:', error);
                buttonText.innerHTML = '<i class="fas fa-exclamation-triangle me-1"></i>Error';
                spinner.classList.add('d-none');
                
                setTimeout(() => {
                    this.disabled = false;
                    buttonText.innerHTML = '<i class="fas fa-cart-plus me-1"></i>Agregar al Carrito';
                }, 2000);
            }
        });
    });
});