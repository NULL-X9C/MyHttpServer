document.addEventListener('DOMContentLoaded', () => {
    // Переключение вкладок Базовая Герой Эволюция»
    const tabButtons = document.querySelectorAll('.js-form-tab-btn');
    const tabPanels = document.querySelectorAll('.js-form-panel');

    if (tabButtons.length > 0 && tabPanels.length > 0) {
        function activateTab(targetId) {
            tabButtons.forEach(btn => {
                const isTarget = btn.dataset.target === targetId;
                btn.classList.toggle('forms-showcase__tab--active', isTarget);
            });

            tabPanels.forEach(panel => {
                const isTarget = panel.id === targetId;
                panel.classList.toggle('forms-showcase__panel--hidden', !isTarget);
            });
        }

        activateTab(tabButtons[0].dataset.target);

        tabButtons.forEach(btn => {
            btn.addEventListener('click', () => activateTab(btn.dataset.target));
        });
    }

    //  счётчик символов и валидация формы добавления формы
    const addForm = document.getElementById('add-card-form');
    if (!addForm) return;

    const titleInput = document.getElementById('form-title-input');
    const cyclesInput = document.getElementById('cycles-input');
    const iconInput = document.getElementById('form-icon-input');
    const descInput = document.getElementById('ability-desc-input');
    const charCounter = document.getElementById('char-counter');
    const maxLen = 300;

    // Контейнер для вывода ошибок валидации прямо внутри формы
    const errorBox = document.createElement('div');
    errorBox.className = 'alert alert--error alert--hidden';
    addForm.prepend(errorBox);

    function updateCounter() {
        if (!descInput || !charCounter) return;
        const len = descInput.value.length;
        charCounter.textContent = `${len} / ${maxLen}`;
        charCounter.classList.toggle('filter-panel__counter--warn', len >= 240 && len < maxLen);
        charCounter.classList.toggle('filter-panel__counter--limit', len >= maxLen);
    }

    if (descInput) {
        updateCounter();
        descInput.addEventListener('input', updateCounter);
    }

    function markInvalid(input) {
        if (input) input.classList.add('filter-panel__input--invalid');
    }

    addForm.addEventListener('submit', (e) => {
        const errors = [];
        addForm.querySelectorAll('.filter-panel__input--invalid')
            .forEach(el => el.classList.remove('filter-panel__input--invalid'));

        const title = titleInput ? titleInput.value.trim() : '';
        if (title.length < 2 || title.length > 100) {
            errors.push('Название формы должно содержать от 2 до 100 символов.');
            markInvalid(titleInput);
        }

        const cycles = cyclesInput ? Number(cyclesInput.value) : NaN;
        if (!Number.isInteger(cycles) || cycles < 1 || cycles > 10) {
            errors.push('Поле «Циклы / Эликсир» должно быть целым числом от 1 до 10.');
            markInvalid(cyclesInput);
        }

        const iconUrl = iconInput ? iconInput.value.trim() : '';
        if (!iconUrl) {
            errors.push('Укажите путь к картинке формы.');
            markInvalid(iconInput);
        }

        const desc = descInput ? descInput.value.trim() : '';
        if (desc.length === 0 || desc.length > maxLen) {
            errors.push(`Описание способности обязательно и не должно превышать ${maxLen} символов.`);
            markInvalid(descInput);
        }

        if (errors.length > 0) {
            e.preventDefault();
            errorBox.textContent = errors.join(' ');
            errorBox.classList.remove('alert--hidden');
            errorBox.scrollIntoView({ behavior: 'smooth', block: 'center' });
        } else {
            errorBox.classList.add('alert--hidden');
        }
    });
});