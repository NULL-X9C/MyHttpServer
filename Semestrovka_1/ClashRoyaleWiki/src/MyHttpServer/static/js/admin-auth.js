document.addEventListener('DOMContentLoaded', () => {
    const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/;
    const SLUG_REGEX = /^[a-z0-9]+(?:-[a-z0-9]+)*$/;
    const ALLOWED_EXTS = ['.png', '.webp', '.jpg', '.jpeg'];
    const MAX_FILE_BYTES = 5 * 1024 * 1024; // 5 МБ

    function showClientError(box, messages) {
        if (!box) return;
        box.textContent = Array.isArray(messages) ? messages.join(' ') : messages;
        box.classList.remove('alert--hidden');
        box.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }

    function clearClientError(form, box) {
        if (box) {
            box.textContent = '';
            box.classList.add('alert--hidden');
        }
        if (form) {
            form.querySelectorAll('.filter-panel__input--invalid')
                .forEach(el => el.classList.remove('filter-panel__input--invalid'));
        }
    }

    function markInvalid(input) {
        if (input) input.classList.add('filter-panel__input--invalid');
    }

    // Подтверждение удаления (Admin/Index.html и EditCard.html)
    document.querySelectorAll('.js-delete-form').forEach(form => {
        form.addEventListener('submit', (e) => {
            if (!window.confirm('Удалить выбранную запись? Это действие нельзя отменить.')) {
                e.preventDefault();
            }
        });
    });

    // Страница входа: Regex №1 для Email (Views/Auth/Login.html)
    const loginForm = document.getElementById('login-form');
    if (loginForm) {
        const emailInput = document.getElementById('login-email');
        const passwordInput = document.getElementById('login-password');
        const errorBox = document.getElementById('client-error-box');

        loginForm.addEventListener('submit', (e) => {
            clearClientError(loginForm, errorBox);
            const errors = [];

            const email = emailInput ? emailInput.value.trim() : '';
            if (!EMAIL_REGEX.test(email)) {
                errors.push('Введите корректный Email (например, admin@clash.ru).');
                markInvalid(emailInput);
            }

            const password = passwordInput ? passwordInput.value : '';
            if (password.length < 4) {
                errors.push('Пароль должен содержать не менее 4 символов.');
                markInvalid(passwordInput);
            }

            if (errors.length > 0) {
                e.preventDefault();
                showClientError(errorBox, errors);
            }
        });
    }

    // Страница создания, редактирования карты (Views/Admin/EditCard.html)
    const saveCardForm = document.getElementById('save-card-form');
    if (!saveCardForm) return;

    const cardErrorBox = document.getElementById('client-error-box');
    const nameInput = document.getElementById('card-name');
    const slugInput = document.getElementById('card-slug');
    const elixirInput = document.getElementById('card-elixir');
    const yearInput = document.getElementById('card-year');
    const iconUrlInput = document.getElementById('card-icon-url');
    const descInput = document.getElementById('card-description');
    const hitSpeedInput = document.getElementById('stat-hit-speed');

    const fileInput = document.getElementById('card-image-file');
    const previewImg = document.getElementById('card-image-preview');
    const base64Input = document.getElementById('image-base64-input');
    const extInput = document.getElementById('image-ext-input');

    // Автовыбор текущих enum-значений в <select>
    saveCardForm.querySelectorAll('.js-enum-select').forEach(select => {
        const selectedValue = select.dataset.selected;
        if (selectedValue && selectedValue !== '0') {
            select.value = selectedValue;
        }
    });

    // Превью картинки и чтение файла в Base64
    function updatePreviewVisibility() {
        if (!previewImg) return;
        const src = previewImg.getAttribute('src');
        previewImg.style.display = src && src.trim() !== '' ? 'block' : 'none';
    }

    updatePreviewVisibility();

    if (iconUrlInput && previewImg) {
        iconUrlInput.addEventListener('input', () => {
            const url = iconUrlInput.value.trim();
            if (!base64Input || !base64Input.value) {
                previewImg.src = url;
                updatePreviewVisibility();
            }
        });
    }

    if (fileInput) {
        fileInput.addEventListener('change', () => {
            clearClientError(saveCardForm, cardErrorBox);

            const file = fileInput.files && fileInput.files[0];
            if (!file) {
                if (base64Input) base64Input.value = '';
                if (extInput) extInput.value = '';
                return;
            }

            const dotIndex = file.name.lastIndexOf('.');
            const ext = dotIndex >= 0 ? file.name.slice(dotIndex).toLowerCase() : '';
            if (!ALLOWED_EXTS.includes(ext)) {
                showClientError(cardErrorBox, 'Допустимые форматы изображений: .png, .webp, .jpg, .jpeg.');
                fileInput.value = '';
                return;
            }

            if (file.size > MAX_FILE_BYTES) {
                showClientError(cardErrorBox, 'Размер изображения не должен превышать 5 МБ.');
                fileInput.value = '';
                return;
            }

            const reader = new FileReader();
            reader.onload = () => {
                const dataUrl = String(reader.result || '');
                if (base64Input) base64Input.value = dataUrl;
                if (extInput) extInput.value = ext;
                if (previewImg) {
                    previewImg.src = dataUrl;
                    updatePreviewVisibility();
                }
            };
            reader.readAsDataURL(file);
        });
    }

    // Инициализация WYSIWYG-редактора Quill.js
    const editorContainer = document.getElementById('strategy-editor');
    const strategyHiddenInput = document.getElementById('strategy-html-input');
    let quill = null;

    if (editorContainer && typeof Quill !== 'undefined') {
        quill = new Quill(editorContainer, {
            theme: 'snow',
            placeholder: 'Напишите тактический разбор, синергии и контрмеры...',
            modules: {
                toolbar: [
                    [{ header: [2, 3, false] }],
                    ['bold', 'italic', 'underline'],
                    [{ list: 'ordered' }, { list: 'bullet' }],
                    ['clean']
                ]
            }
        });
    }

    // Валидация перед отправкой формы 
    saveCardForm.addEventListener('submit', (e) => {
        clearClientError(saveCardForm, cardErrorBox);
        const errors = [];

        if (quill && strategyHiddenInput) {
            const plainText = quill.getText().trim();
            strategyHiddenInput.value = plainText.length > 0 ? quill.root.innerHTML : '';
        }

        const name = nameInput ? nameInput.value.trim() : '';
        if (name.length < 2 || name.length > 100) {
            errors.push('Название карты должно содержать от 2 до 100 символов.');
            markInvalid(nameInput);
        }

        const slug = slugInput ? slugInput.value.trim().toLowerCase() : '';
        if (slugInput) slugInput.value = slug;
        if (!SLUG_REGEX.test(slug)) {
            errors.push('Slug должен содержать только строчные латинские буквы, цифры и дефисы (например, hog-rider).');
            markInvalid(slugInput);
        }

        const elixir = elixirInput ? Number(elixirInput.value) : NaN;
        if (!Number.isInteger(elixir) || elixir < 1 || elixir > 10) {
            errors.push('Стоимость эликсира должна быть целым числом от 1 до 10.');
            markInvalid(elixirInput);
        }

        const year = yearInput ? Number(yearInput.value) : NaN;
        if (!Number.isInteger(year) || year < 2016 || year > 2030) {
            errors.push('Год выхода должен быть в диапазоне от 2016 до 2030.');
            markInvalid(yearInput);
        }

        const hasIconUrl = iconUrlInput && iconUrlInput.value.trim().length > 0;
        const hasUploadedImage = base64Input && base64Input.value.trim().length > 0;
        if (!hasIconUrl && !hasUploadedImage) {
            errors.push('Укажите путь к картинке карты или загрузите файл с ПК.');
            markInvalid(iconUrlInput);
        }

        const desc = descInput ? descInput.value.trim() : '';
        if (desc.length === 0) {
            errors.push('Описание карты не может быть пустым.');
            markInvalid(descInput);
        }

        const hitSpeed = hitSpeedInput ? hitSpeedInput.value.trim() : '';
        if (hitSpeed.length === 0) {
            errors.push('Укажите скорость атаки (например, 1.2s или -).');
            markInvalid(hitSpeedInput);
        }

        if (errors.length > 0) {
            e.preventDefault();
            showClientError(cardErrorBox, errors);
        }
    });
});