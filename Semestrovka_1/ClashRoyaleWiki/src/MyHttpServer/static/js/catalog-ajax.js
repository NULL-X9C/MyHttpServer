document.addEventListener('DOMContentLoaded', () => {
    const cardsGrid = document.getElementById('cards-grid');
    if (!cardsGrid) return;

    const desktopForm = document.getElementById('catalog-filter-form');
    const searchInput = document.getElementById('search-input');
    const raritySelect = document.getElementById('rarity-select');
    const formSelect = document.getElementById('form-select');
    const elixirSelect = document.getElementById('elixir-select');
    const resetBtn = document.getElementById('reset-filters-btn');
    const autocompleteBox = document.getElementById('search-autocomplete');

    const drawer = document.getElementById('mobile-filter-drawer');
    const openDrawerBtns = document.querySelectorAll('.js-open-filters');
    const closeDrawerBtns = document.querySelectorAll('.js-close-filters');
    const mobileForm = document.getElementById('mobile-filter-form');
    const mobileSearch = document.getElementById('mobile-search-input');
    const mobileRarity = document.getElementById('mobile-rarity-select');
    const mobileCardForm = document.getElementById('mobile-form-select');
    const mobileElixir = document.getElementById('mobile-elixir-select');
    const mobileResetBtn = drawer ? drawer.querySelector('.filter-drawer__footer a') : null;

    let debounceTimer = null;
    let activeController = null;

    function escapeHtml(value)
    {
        return String(value ?? '')
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    function syncDesktopToMobile() {
        if (mobileSearch && searchInput) mobileSearch.value = searchInput.value;
        if (mobileRarity && raritySelect) mobileRarity.value = raritySelect.value;
        if (mobileCardForm && formSelect) mobileCardForm.value = formSelect.value;
        if (mobileElixir && elixirSelect) mobileElixir.value = elixirSelect.value;
    }

    function syncMobileToDesktop() {
        if (searchInput && mobileSearch) searchInput.value = mobileSearch.value;
        if (raritySelect && mobileRarity) raritySelect.value = mobileRarity.value;
        if (formSelect && mobileCardForm) formSelect.value = mobileCardForm.value;
        if (elixirSelect && mobileElixir) elixirSelect.value = mobileElixir.value;
    }

    function openDrawer() {
        if (!drawer) return;
        syncDesktopToMobile();
        drawer.classList.add('filter-drawer--open');
        drawer.setAttribute('aria-hidden', 'false');
    }

    function closeDrawer() {
        if (!drawer) return;
        drawer.classList.remove('filter-drawer--open');
        drawer.setAttribute('aria-hidden', 'true');
    }

    function hideAutocomplete() {
        if (!autocompleteBox) return;
        autocompleteBox.style.display = 'none';
        autocompleteBox.innerHTML = '';
    }

    function renderAutocomplete(cards) {
        if (!autocompleteBox || !searchInput) return;

        const query = searchInput.value.trim();
        if (!query || cards.length === 0) {
            hideAutocomplete();
            return;
        }

        const suggestions = cards.slice(0, 6);
        autocompleteBox.innerHTML = suggestions.map(card => `
            <button type="button" class="autocomplete-item" data-name="${escapeHtml(card.name)}">
                <img class="autocomplete-item__thumb" src="${escapeHtml(card.iconUrl)}" alt="">
                <span class="autocomplete-item__name">${escapeHtml(card.name)}</span>
                <span class="elixir-badge">${escapeHtml(card.elixirCost)}</span>
            </button>
        `).join('');

        autocompleteBox.style.display = 'block';

        autocompleteBox.querySelectorAll('.autocomplete-item').forEach(btn => {
            btn.addEventListener('click', () => {
                searchInput.value = btn.dataset.name || '';
                syncDesktopToMobile();
                hideAutocomplete();
                fetchAndRenderCards(false);
            });
        });
    }

    function buildFormsMarkup(card) {
        if (!card.hasSpecialForms) return '';

        const heroPill = card.hasHero ? `
            <span class="form-pill form-pill--hero" title="Hero">
                <span class="form-pill__thumb">
                    <img class="form-pill__icon" src="${escapeHtml(card.heroIconUrl)}" alt="" aria-hidden="true" loading="lazy">
                </span>
                Hero
            </span>
        ` : '';

        const evoPill = card.hasEvolution ? `
            <span class="form-pill form-pill--evo" title="Evo">
                <span class="form-pill__thumb">
                    <img class="form-pill__icon" src="${escapeHtml(card.evolutionIconUrl)}" alt="" aria-hidden="true" loading="lazy">
                </span>
                Evo
            </span>
        ` : '';

        return `<div class="card-item__forms">${heroPill}${evoPill}</div>`;
    }

    function renderCards(cards) {
        if (!Array.isArray(cards) || cards.length === 0) {
            cardsGrid.innerHTML = `
                <div class="cards-grid__empty">
                    <p class="cards-grid__empty-title">Карты не найдены</p>
                    <p class="cards-grid__empty-text">Попробуйте изменить параметры поиска или сбросить фильтры.</p>
                </div>
            `;
            return;
        }

        cardsGrid.innerHTML = cards.map(card => `
            <a class="card-item" href="/cards/details?slug=${encodeURIComponent(card.slug)}">
                <div class="card-item__inner">
                    <div class="card-item__face card-item__face--front">
                        <div class="card-item__image-wrap">
                            <img class="card-item__image" src="${escapeHtml(card.iconUrl)}" alt="${escapeHtml(card.name)}" loading="lazy">
                        </div>
                        <div class="card-item__info">
                            <h3 class="card-item__name">${escapeHtml(card.name)}</h3>
                            ${buildFormsMarkup(card)}
                            <div class="card-item__meta">
                                <span class="rarity-badge rarity-badge--${escapeHtml(card.rarityClass)}">${escapeHtml(card.rarity)}</span>
                                <span class="elixir-badge">${escapeHtml(card.elixirCost)}</span>
                            </div>
                        </div>
                    </div>
                    <div class="card-item__face card-item__face--back">
                        <svg class="card-item__back-icon" fill="currentColor" viewBox="0 0 20 20" aria-hidden="true">
                            <path d="M10 12a2 2 0 100-4 2 2 0 000 4z"></path>
                            <path fill-rule="evenodd" d="M.458 10C1.732 5.943 5.522 3 10 3s8.268 2.943 9.542 7c-1.274 4.057-5.064 7-9.542 7S1.732 14.057.458 10zM14 10a4 4 0 11-8 0 4 4 0 018 0z" clip-rule="evenodd"></path>
                        </svg>
                        <span class="card-item__back-text">Подробнее</span>
                    </div>
                </div>
            </a>
        `).join('');
    }

    async function fetchAndRenderCards(showSuggestions = false) {
        if (activeController) {
            activeController.abort();
        }
        activeController = new AbortController();

        const params = new URLSearchParams();
        const search = searchInput ? searchInput.value.trim() : '';
        const rarity = raritySelect ? raritySelect.value : 'all';
        const form = formSelect ? formSelect.value : 'all';
        const elixir = elixirSelect ? elixirSelect.value : 'all';

        if (search) params.set('search', search);
        if (rarity && rarity !== 'all') params.set('rarity', rarity);
        if (form && form !== 'all') params.set('form', form);
        if (elixir && elixir !== 'all') params.set('elixir', elixir);

        const queryString = params.toString();
        const url = queryString ? `/api/cards?${queryString}` : '/api/cards';

        try {
            const response = await fetch(url, {
                method: 'GET',
                headers: { 'Accept': 'application/json' },
                signal: activeController.signal
            });

            if (!response.ok) {
                throw new Error(`HTTP ${response.status}`);
            }

            const cards = await response.json();
            renderCards(cards);

            if (showSuggestions) {
                renderAutocomplete(cards);
            } else {
                hideAutocomplete();
            }
        } catch (err) {
            if (err.name === 'AbortError') return;
            cardsGrid.innerHTML = `
                <div class="cards-grid__empty">
                    <p class="cards-grid__empty-title">Не удалось загрузить карты</p>
                    <p class="cards-grid__empty-text">Проверьте соединение с сервером и попробуйте ещё раз.</p>
                </div>
            `;
        }
    }

    function resetAllFilters(event) {
        if (event) event.preventDefault();
        if (searchInput) searchInput.value = '';
        if (raritySelect) raritySelect.value = 'all';
        if (formSelect) formSelect.value = 'all';
        if (elixirSelect) elixirSelect.value = 'all';
        syncDesktopToMobile();
        hideAutocomplete();
        closeDrawer();
        fetchAndRenderCards(false);
    }

    // Десктопные события
    if (desktopForm) {
        desktopForm.addEventListener('submit', (e) => {
            e.preventDefault();
            hideAutocomplete();
            fetchAndRenderCards(false);
        });
    }

    if (searchInput) {
        searchInput.addEventListener('input', () => {
            syncDesktopToMobile();
            clearTimeout(debounceTimer);
            debounceTimer = setTimeout(() => fetchAndRenderCards(true), 300);
        });
    }

    [raritySelect, formSelect, elixirSelect].forEach(select => {
        if (!select) return;
        select.addEventListener('change', () => {
            syncDesktopToMobile();
            fetchAndRenderCards(false);
        });
    });

    if (resetBtn) {
        resetBtn.addEventListener('click', resetAllFilters);
    }

    // Закрытие автодополнения 
    document.addEventListener('click', (e) => {
        if (!autocompleteBox || !searchInput) return;
        if (!autocompleteBox.contains(e.target) && e.target !== searchInput) {
            hideAutocomplete();
        }
    });

    // шторка
    openDrawerBtns.forEach(btn => btn.addEventListener('click', openDrawer));
    closeDrawerBtns.forEach(btn => btn.addEventListener('click', closeDrawer));

    if (mobileForm) {
        mobileForm.addEventListener('submit', (e) => {
            e.preventDefault();
            syncMobileToDesktop();
            closeDrawer();
            fetchAndRenderCards(false);
        });
    }

    if (mobileResetBtn) {
        mobileResetBtn.addEventListener('click', resetAllFilters);
    }
});