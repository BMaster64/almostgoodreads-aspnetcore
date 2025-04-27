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

// Initialize on page load
document.addEventListener('DOMContentLoaded', function () {
    toggleImageInputs();
});