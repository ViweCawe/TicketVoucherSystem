document.querySelectorAll('dialog[open]').forEach((dialog) => {
    dialog.close();
    dialog.showModal();
});

document.querySelectorAll('[data-close-dialog]').forEach((button) => {
    button.addEventListener('click', () => button.closest('dialog')?.close());
});
