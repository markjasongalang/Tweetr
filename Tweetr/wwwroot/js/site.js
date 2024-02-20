
// Change the height of the textarea dynamically (based on the text input of the user)
const postTextAreas = document.querySelectorAll('.post-text-area');
for (let i = 0; i < postTextAreas.length; i++) {
    postTextAreas[i].addEventListener('input', autoResize, false);
}

function autoResize() {
    this.style.height = 'auto';
    this.style.height = `${this.scrollHeight}px`;
}

