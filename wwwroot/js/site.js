
document.addEventListener('DOMContentLoaded', function () {
    initializeNotifications();

    initializeMessages();

    initializeUIComponents();

    initializeBookmarks();
});

function initializeNotifications() {
    const notificationBell = document.getElementById('listingNotificationBell');
    const notificationDropdown = document.getElementById('listingNotificationDropdown');

    if (!notificationBell || !notificationDropdown) return;

    loadNotifications();

    setInterval(loadNotifications, 30000);

    notificationBell.addEventListener('click', function (e) {
        e.stopPropagation();
        toggleDropdown(notificationDropdown);
    });

    document.addEventListener('click', function (event) {
        if (!notificationDropdown.contains(event.target) && event.target !== notificationBell) {
            notificationDropdown.style.display = 'none';
        }
    });
}

function loadNotifications() {
    fetch('/Notifications/GetNotifications')
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                updateNotificationUI(data.notifications, data.unreadCount);
            }
        })
        .catch(error => console.error('Error loading notifications:', error));
}

function updateNotificationUI(notifications, unreadCount) {
    const notificationBell = document.getElementById('listingNotificationBell');
    const notificationDropdown = document.getElementById('listingNotificationDropdown');

    if (!notificationBell || !notificationDropdown) return;

    const badge = document.getElementById('notification-badge') || createBadge();
    if (unreadCount > 0) {
        badge.textContent = unreadCount;
        badge.style.display = 'block';
    } else {
        badge.style.display = 'none';
    }

    notificationDropdown.innerHTML = '';

    if (notifications.length === 0) {
        const emptyNotice = document.createElement('p');
        emptyNotice.className = 'notification-item empty';
        emptyNotice.textContent = 'No notifications';
        notificationDropdown.appendChild(emptyNotice);
    } else {
        notifications.forEach(notification => {
            const notificationItem = document.createElement('div');
            notificationItem.className = `notification-item ${notification.isRead ? 'read' : 'unread'}`;

            notificationItem.innerHTML = `
                <p>${notification.message}</p>
                <span class="notification-time">${formatTimeAgo(new Date(notification.createdAt))}</span>
                <div class="notification-actions">
                    <button class="mark-read-btn" data-id="${notification.id}" title="Mark as read">
                        ${notification.isRead ? '✓' : 'Mark read'}
                    </button>
                </div>
            `;

            notificationDropdown.appendChild(notificationItem);

            const markReadBtn = notificationItem.querySelector('.mark-read-btn');
            markReadBtn.addEventListener('click', function (e) {
                e.stopPropagation();
                markNotificationAsRead(notification.id);
            });

            notificationItem.addEventListener('click', function () {
                handleNotificationClick(notification);
            });
        });

        const viewAllLink = document.createElement('a');
        viewAllLink.href = '/Notifications/Index';
        viewAllLink.className = 'view-all-link';
        viewAllLink.textContent = 'View All Notifications';
        notificationDropdown.appendChild(viewAllLink);
    }

    function createBadge() {
        const badge = document.createElement('span');
        badge.id = 'notification-badge';
        badge.className = 'notification-badge';
        badge.style.cssText = `
            position: absolute;
            top: -5px;
            right: -5px;
            background-color: red;
            color: white;
            border-radius: 50%;
            width: 18px;
            height: 18px;
            font-size: 12px;
            display: flex;
            align-items: center;
            justify-content: center;
        `;
        notificationBell.style.position = 'relative';
        notificationBell.appendChild(badge);
        return badge;
    }
}

function markNotificationAsRead(notificationId) {
    fetch('/Notifications/MarkAsRead', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
        },
        body: `notificationId=${notificationId}`
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                loadNotifications(); 
            }
        })
        .catch(error => console.error('Error marking notification as read:', error));
}

function handleNotificationClick(notification) {
    markNotificationAsRead(notification.id);

    switch (notification.type) {
        case 'ListingApproved':
        case 'ListingRejected':
            window.location.href = `/Listing/Details/${notification.relatedEntityId}`;
            break;
        case 'NewMessage':
            window.location.href = `/User/LiveChat?listingId=${notification.relatedEntityId}`;
            break;
        case 'NewOffer':
        case 'OfferAccepted':
        case 'OfferRejected':
            window.location.href = `/Listing/Offers/${notification.relatedEntityId}`;
            break;
        case 'VerificationApproved':
        case 'VerificationRejected':
            window.location.href = `/User/MyProfilePage`;
            break;
        case 'PurchaseCompleted':
            window.location.href = `/Payment/PurchaseComplete?purchaseId=${notification.relatedEntityId}`;
            break;
        default:
            window.location.href = '/Notifications/Index';
    }
}

function initializeMessages() {
    const messageIcon = document.getElementById('messageNotificationBell');
    const messageDropdown = document.getElementById('messageNotificationDropdown');

    if (!messageIcon || !messageDropdown) return;

    loadUnreadMessageCount();

    setInterval(loadUnreadMessageCount, 30000);

    messageIcon.addEventListener('click', function (e) {
        e.stopPropagation();
        toggleDropdown(messageDropdown);
    });

    document.addEventListener('click', function (event) {
        if (!messageDropdown.contains(event.target) && event.target !== messageIcon) {
            messageDropdown.style.display = 'none';
        }
    });

    const chatForm = document.getElementById('chatForm');
    if (chatForm) {
        chatForm.addEventListener('submit', function (e) {
            e.preventDefault();
            sendMessage();
        });
    }
}

function loadUnreadMessageCount() {
    fetch('/Message/GetUnreadCount')
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                updateMessageBadge(data.unreadCount);
            }
        })
        .catch(error => console.error('Error loading unread messages count:', error));
}

function updateMessageBadge(unreadCount) {
    const messageIcon = document.getElementById('messageNotificationBell');
    if (!messageIcon) return;

    let badge = document.getElementById('message-badge');

    if (!badge && unreadCount > 0) {
        badge = document.createElement('span');
        badge.id = 'message-badge';
        badge.className = 'notification-badge';
        badge.style.cssText = `
            position: absolute;
            top: -5px;
            right: -5px;
            background-color: red;
            color: white;
            border-radius: 50%;
            width: 18px;
            height: 18px;
            font-size: 12px;
            display: flex;
            align-items: center;
            justify-content: center;
        `;
        messageIcon.style.position = 'relative';
        messageIcon.appendChild(badge);
    }

    if (badge) {
        if (unreadCount > 0) {
            badge.textContent = unreadCount;
            badge.style.display = 'flex';
        } else {
            badge.style.display = 'none';
        }
    }
}

function sendMessage() {
    const messageInput = document.getElementById('messageInput');
    const listingId = document.getElementById('listingId').value;
    const receiverId = document.getElementById('receiverId').value;

    if (!messageInput || !messageInput.value.trim()) return;

    const message = {
        receiverId: receiverId,
        listingId: listingId,
        content: messageInput.value.trim()
    };

    fetch('/Message/SendMessageAjax', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify(message)
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                appendMessage(data.message);

                messageInput.value = '';
            }
        })
        .catch(error => console.error('Error sending message:', error));
}

function appendMessage(message) {
    const chatMessages = document.getElementById('chatMessages');
    if (!chatMessages) return;

    const messageDiv = document.createElement('div');
    messageDiv.className = `message ${message.sentByMe ? 'you' : ''}`;

    const currentUserImage = document.getElementById('currentUserImage').value || '/images/placeholder.png';

    messageDiv.innerHTML = `
        <img src="${currentUserImage}" alt="You" style="width: 40px; height: 40px; border-radius: 50%;">
        <div class="message-content">
            ${message.content}
            <div class="message-time">${formatTimeAgo(new Date(message.timestamp))}</div>
        </div>
    `;

    chatMessages.appendChild(messageDiv);

    chatMessages.scrollTop = chatMessages.scrollHeight;
}

function initializeUIComponents() {
    const dropdownToggles = document.querySelectorAll('[data-toggle="dropdown"]');
    dropdownToggles.forEach(toggle => {
        const targetId = toggle.getAttribute('data-target');
        const target = document.getElementById(targetId);

        if (toggle && target) {
            toggle.addEventListener('click', function (e) {
                e.stopPropagation();
                toggleDropdown(target);
            });
        }
    });

    initializeImageGallery();

    initializeFormValidations();
}

function toggleDropdown(dropdown) {
    if (dropdown.style.display === 'block') {
        dropdown.style.display = 'none';
    } else {
        document.querySelectorAll('.dropdown-menu').forEach(menu => {
            if (menu !== dropdown) {
                menu.style.display = 'none';
            }
        });
        dropdown.style.display = 'block';
    }
}

function initializeImageGallery() {
    const mainImage = document.querySelector('.main-image');
    const thumbnails = document.querySelectorAll('.thumbnail-container img');

    if (!mainImage || thumbnails.length === 0) return;

    thumbnails.forEach(thumbnail => {
        thumbnail.addEventListener('click', function () {
            mainImage.src = this.src.replace(/\/\d+\/\d+$/, '/800/600');

            thumbnails.forEach(thumb => thumb.classList.remove('active'));
            this.classList.add('active');
        });
    });
}

function initializeFormValidations() {
    const listingForm = document.getElementById('listingForm');
    if (listingForm) {
        listingForm.addEventListener('submit', function (e) {
            if (!validateListingForm()) {
                e.preventDefault();
            }
        });
    }

    const paymentForm = document.getElementById('paymentForm');
    if (paymentForm) {
        paymentForm.addEventListener('submit', function (e) {
            if (!validatePaymentForm()) {
                e.preventDefault();
            }
        });
    }
}

function validateListingForm() {
    const title = document.getElementById('title');
    const price = document.getElementById('price');
    const description = document.getElementById('description');
    const location = document.getElementById('location');
    const images = document.getElementById('images');

    let isValid = true;

    if (!title || !title.value.trim()) {
        showError(title, 'Title is required');
        isValid = false;
    }

    if (!price || isNaN(parseFloat(price.value)) || parseFloat(price.value) <= 0) {
        showError(price, 'Please enter a valid price');
        isValid = false;
    }

    if (!description || description.value.trim().length < 20) {
        showError(description, 'Please enter a detailed description (at least 20 characters)');
        isValid = false;
    }

    if (!location || location.value === '') {
        showError(location, 'Please select a location');
        isValid = false;
    }

    if (images && images.files.length === 0 && !document.querySelector('.edit-listing')) {
        showError(images, 'Please upload at least one image');
        isValid = false;
    }

    return isValid;
}

function validatePaymentForm() {
    const cardholderName = document.getElementById('fullname');
    const cardNumber = document.getElementById('cardnumber');
    const expiry = document.getElementById('expiry');
    const cvv = document.getElementById('cvv');

    let isValid = true;

    if (!cardholderName || !cardholderName.value.trim()) {
        showError(cardholderName, 'Cardholder name is required');
        isValid = false;
    }

    if (!cardNumber || !cardNumber.value.trim() || !isValidCardNumber(cardNumber.value)) {
        showError(cardNumber, 'Please enter a valid card number');
        isValid = false;
    }

    if (!expiry || !expiry.value.trim() || !isValidExpiry(expiry.value)) {
        showError(expiry, 'Please enter a valid expiry date (MM/YY)');
        isValid = false;
    }

    if (!cvv || !cvv.value.trim() || !isValidCVV(cvv.value)) {
        showError(cvv, 'Please enter a valid CVV');
        isValid = false;
    }

    return isValid;
}

function showError(element, message) {
    const errorElement = element.nextElementSibling?.classList.contains('error-message')
        ? element.nextElementSibling
        : document.createElement('div');

    if (!errorElement.classList.contains('error-message')) {
        errorElement.className = 'error-message';
        errorElement.style.cssText = 'color: red; font-size: 12px; margin-top: 5px;';
        element.parentNode.insertBefore(errorElement, element.nextSibling);
    }

    errorElement.textContent = message;
    element.style.borderColor = 'red';

    element.addEventListener('input', function () {
        this.style.borderColor = '';
        errorElement.textContent = '';
    }, { once: true });
}

function formatTimeAgo(date) {
    const seconds = Math.floor((new Date() - date) / 1000);

    let interval = Math.floor(seconds / 31536000);
    if (interval >= 1) {
        return interval === 1 ? '1 year ago' : `${interval} years ago`;
    }

    interval = Math.floor(seconds / 2592000);
    if (interval >= 1) {
        return interval === 1 ? '1 month ago' : `${interval} months ago`;
    }

    interval = Math.floor(seconds / 86400);
    if (interval >= 1) {
        return interval === 1 ? '1 day ago' : `${interval} days ago`;
    }

    interval = Math.floor(seconds / 3600);
    if (interval >= 1) {
        return interval === 1 ? '1 hour ago' : `${interval} hours ago`;
    }

    interval = Math.floor(seconds / 60);
    if (interval >= 1) {
        return interval === 1 ? '1 minute ago' : `${interval} minutes ago`;
    }

    return seconds < 10 ? 'just now' : `${Math.floor(seconds)} seconds ago`;
}

function isValidCardNumber(number) {
    const cleanNumber = number.replace(/\D/g, '');
    return cleanNumber.length >= 13 && cleanNumber.length <= 19;
}

function isValidExpiry(expiry) {
    const regex = /^(0[1-9]|1[0-2])\/\d{2}$/;
    if (!regex.test(expiry)) return false;

    const [month, year] = expiry.split('/');
    const currentYear = new Date().getFullYear() % 100;
    const currentMonth = new Date().getMonth() + 1;

    const expiryYear = parseInt(year, 10);
    const expiryMonth = parseInt(month, 10);

    return (expiryYear > currentYear) || (expiryYear === currentYear && expiryMonth >= currentMonth);
}

function isValidCVV(cvv) {
    const cleanCVV = cvv.replace(/\D/g, '');
    return cleanCVV.length >= 3 && cleanCVV.length <= 4;
}

function initializeBookmarks() {
    const bookmarkBtns = document.querySelectorAll('.btn-bookmark');
    bookmarkBtns.forEach(btn => {
        btn.addEventListener('click', function () {
            const listingId = this.getAttribute('data-id');
            toggleBookmark(listingId, this);
        });
    });

    const addToCartBtns = document.querySelectorAll('.btn-add-to-cart');
    addToCartBtns.forEach(btn => {
        btn.addEventListener('click', function () {
            const listingId = this.getAttribute('data-id');
            addToCart(listingId);
        });
    });

    const removeFromCartBtns = document.querySelectorAll('.btn-remove-from-cart');
    removeFromCartBtns.forEach(btn => {
        btn.addEventListener('click', function () {
            const bookmarkId = this.getAttribute('data-id');
            removeFromCart(bookmarkId, this.closest('.shopping-item-card'));
        });
    });
}

function toggleBookmark(listingId, button) {
    fetch('/Listing/ToggleBookmark', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
        },
        body: `listingId=${listingId}`
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                if (data.isBookmarked) {
                    button.classList.add('bookmarked');
                    button.innerHTML = '<i class="fas fa-bookmark"></i> Bookmarked';
                } else {
                    button.classList.remove('bookmarked');
                    button.innerHTML = '<i class="far fa-bookmark"></i> Bookmark';
                }
            }
        })
        .catch(error => console.error('Error toggling bookmark:', error));
}

function addToCart(listingId) {
    fetch('/Payment/AddToCart', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
        },
        body: `listingId=${listingId}`
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                showMessage('Item added to cart successfully!', false);
            } else {
                showMessage(data.message, true);
            }
        })
        .catch(error => {
            console.error('Error adding to cart:', error);
            showMessage('Error adding to cart. Please try again.', true);
        });
}

function removeFromCart(bookmarkId, cartItem) {
    fetch('/Payment/RemoveFromCart', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
        },
        body: `bookmarkId=${bookmarkId}`
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                if (cartItem) {
                    cartItem.remove();
                }

                updateCartTotal();

                showMessage('Item removed from cart successfully!', false);
            } else {
                showMessage(data.message, true);
            }
        })
        .catch(error => {
            console.error('Error removing from cart:', error);
            showMessage('Error removing from cart. Please try again.', true);
        });
}

function updateCartTotal() {
    const cartItems = document.querySelectorAll('.shopping-item-card');
    let total = 0;

    cartItems.forEach(item => {
        const priceText = item.querySelector('.item-price').textContent;
        const price = parseFloat(priceText.replace(/[^\d.]/g, ''));
        if (!isNaN(price)) {
            total += price;
        }
    });

    const totalElement = document.getElementById('cart-total');
    if (totalElement) {
        totalElement.textContent = `Total: ${total.toFixed(2)} TL`;
    }

    const buyAllBtn = document.querySelector('.buyall-btn');
    if (buyAllBtn) {
        buyAllBtn.style.display = cartItems.length > 0 ? 'block' : 'none';
    }
}

function showMessage(message, isError) {
    const messageContainer = document.getElementById('message-container') || createMessageContainer();

    messageContainer.textContent = message;
    messageContainer.className = isError ? 'error-message' : 'success-message';
    messageContainer.style.display = 'block';

    setTimeout(() => {
        messageContainer.style.display = 'none';
    }, 3000);
}

function createMessageContainer() {
    const container = document.createElement('div');
    container.id = 'message-container';
    container.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        padding: 10px 20px;
        border-radius: 5px;
        z-index: 1000;
        display: none;
    `;
    document.body.appendChild(container);
    return container;
}