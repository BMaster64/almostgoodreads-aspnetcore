document.addEventListener('DOMContentLoaded', function () {
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[title]'))
var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl)
    });
});

function toggleImageInputs() {
    const urlInput = document.getElementById('coverImageUrl');
    const fileInput = document.getElementById('coverImageFile');

    if (urlInput.value.trim() !== '') {
        fileInput.disabled = true;
        fileInput.value = '';  // Clear the file input
    } else if (fileInput.files.length > 0) {
        urlInput.disabled = true;
        urlInput.value = '';  // Clear the URL input
    } else {
        // If both are empty, enable both
        urlInput.disabled = false;
        fileInput.disabled = false;
    }
}
// Add functions for MyBooks feature
function addToMyBooks(bookId, status) {
    fetch('/api/MyBooks/update', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
        },
        body: JSON.stringify({ bookId: bookId, status: status })
    })
        .then(response => {
            if (response.ok) {
                window.location.reload();
            } else {
                alert('Failed to add book to your collection. Please try again.');
            }
        });
}

function removeFromMyBooks(bookId) {
    if (confirm('Are you sure you want to remove this book from your collection?')) {
        fetch('/api/MyBooks/remove', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            },
            body: JSON.stringify({ bookId: bookId })
        })
            .then(response => {
                if (response.ok) {
                    window.location.reload();
                } else {
                    alert('Failed to remove book from your collection. Please try again.');
                }
            });
    }
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', function () {
    toggleImageInputs();
});
