(function (css, icon) {
    if (document.getElementById('mowods')) return;

    const title = 'M\u00f2w\u00f3ds';

    // cohtml skips style text set after append
    const style = document.createElement('style');
    style.textContent = css;
    document.head.appendChild(style);

    const root = el('div', 'content root-content hidden');
    root.id = 'mowods';
    root.innerHTML = '<div class="list-filter">'
        + '<h1><img class="mowods-melon" src="' + icon + '">' + title + '</h1>'
        + '<div class="mowods-search">'
        + '<div class="mowods-search-icon"></div>'
        + '<div class="mowods-hint">Search</div>'
        + '<input id="mowods-search" class="inp_search" type="text" data-clickable data-placeholder="Search" data-input-title="Search Mods" data-max-length="64">'
        + '</div>'
        + '<div class="scroll-content" data-scroll><div class="filter-content"></div></div>'
        + '</div>'
        + '<div class="list-content" data-scroll><div class="settings-categorie active"></div></div>';
    const container = document.getElementById('root-page-container');
    container.insertBefore(root, container.firstChild);

    const pageList = root.querySelector('.filter-content');
    const body = root.querySelector('.settings-categorie');
    const searchBox = root.querySelector('.mowods-search');
    const searchInput = root.querySelector('#mowods-search');
    const searchHint = root.querySelector('.mowods-hint');

    const keyboardTargets = {};
    let pages = [];
    let current = null;
    let rows = {};
    let groups = [];
    let openDropdown = null;

    function el(tag, className, text) {
        const e = document.createElement(tag);
        if (className) e.className = className;
        if (text !== undefined) e.textContent = text;
        return e;
    }

    function clickable(tag, className, text) {
        const e = el(tag, className, text);
        e.setAttribute('data-clickable', '');
        return e;
    }

    function img(src) {
        const e = document.createElement('img');
        e.src = src;
        return e;
    }

    function setClass(e, name, on) {
        if (on) e.classList.add(name);
        else e.classList.remove(name);
    }

    function hide(e, hidden) {
        setClass(e, 'mowods-hidden', hidden);
    }

    function escapeHtml(text) {
        const e = document.createElement('div');
        e.textContent = text;
        return e.innerHTML;
    }

    function decimalsOf(step) {
        const parts = String(step).split('.');
        return parts.length > 1 ? parts[1].length : 0;
    }

    function onceMouseUp(handler) {
        function up(ev) {
            document.removeEventListener('mouseup', up);
            handler(ev);
        }
        document.addEventListener('mouseup', up);
    }

    function closeDropdown() {
        if (openDropdown) openDropdown.classList.remove('open');
        openDropdown = null;
    }

    document.addEventListener('mousedown', closeDropdown);

    engine.on('updateInputWithKeyboardText', (text, target) => {
        if (keyboardTargets[target]) keyboardTargets[target](text);
    });

    engine.on('MowodsValue', (id, json, isDefault) => {
        const row = rows[id];
        if (!row) return;
        row.control.update(JSON.parse(json));
        row.isDefault = isDefault;
        hide(row.reset, isDefault);
        refreshGroupResets();
    });

    window.addEventListener('uiConfirm', () => {
        if (window.uiConfirm.id === 'mowods-reset' && window.uiConfirm.value === 'true')
            engine.call('MowodsReset', parseInt(window.uiConfirm.data));
    });

    const controls = {
        toggle(item, send) {
            const e = clickable('div', 'inp_toggle no-scroll');
            let value;
            e.addEventListener('mousedown', () => send(!value));
            return {
                element: e,
                update(v) {
                    value = v;
                    setClass(e, 'checked', v);
                }
            };
        },

        slider(item, send) {
            const wrapper = el('div', 'inp_slider_wrapper');
            const left = clickable('div', 'inp_slider_arrow inp_slider_arrow_left button');
            const right = clickable('div', 'inp_slider_arrow inp_slider_arrow_right button');
            const e = clickable('div', 'inp_slider no-scroll');
            const back = el('div', 'valueLabel background');
            const bar = el('div', 'valueBar');
            const front = el('div', 'valueLabel foreground');
            left.appendChild(img('gfx/slider-arrow-left.svg'));
            right.appendChild(img('gfx/slider-arrow-right.svg'));
            bar.appendChild(front);
            e.appendChild(back);
            e.appendChild(bar);
            wrapper.appendChild(left);
            wrapper.appendChild(e);
            wrapper.appendChild(right);

            const decimals = decimalsOf(item.step);
            const range = item.max - item.min;
            let value;

            function snap(v) {
                const clamped = Math.min(Math.max(v, item.min), item.max);
                const steps = Math.round((clamped - item.min) / item.step);
                return parseFloat((item.min + steps * item.step).toFixed(decimals));
            }

            function show(v) {
                const percent = Math.min(Math.max((v - item.min) / range, 0), 1);
                bar.style.width = (percent * 100) + '%';
                // Front label inside bar. Stretched to full width so labels lineup.
                front.style.width = percent > 0 ? (100 / percent) + '%' : '0';
                back.textContent = front.textContent = v.toFixed(decimals);
                setClass(left, 'disabled', v <= item.min);
                setClass(right, 'disabled', v >= item.max);
            }

            function drag(ev) {
                const rect = e.getBoundingClientRect();
                value = snap(item.min + (ev.clientX - rect.left) / rect.width * range);
                show(value);
            }

            e.addEventListener('mousedown', ev => {
                drag(ev);
                document.addEventListener('mousemove', drag);
                onceMouseUp(ev => {
                    document.removeEventListener('mousemove', drag);
                    drag(ev);
                    send(value);
                });
            });

            // hold arrow = repeat, save on release
            function hold(direction) {
                function step() {
                    value = snap(value + direction * item.step);
                    show(value);
                }
                step();
                let repeat = null;
                const delay = setTimeout(() => { repeat = setInterval(step, 80); }, 400);
                onceMouseUp(() => {
                    clearTimeout(delay);
                    clearInterval(repeat);
                    send(value);
                });
            }

            left.addEventListener('mousedown', () => hold(-1));
            right.addEventListener('mousedown', () => hold(1));

            return {
                element: wrapper,
                update(v) {
                    value = v;
                    show(v);
                }
            };
        },

        number(item, send) {
            const e = clickable('div', 'inp_number');
            let value;
            const numpadTarget = {
                getMode: () => item.integer ? 'int' : 'float',
                getValue: () => value,
                updateValue: send
            };
            e.addEventListener('mousedown', () => displayNumpad(numpadTarget));
            return {
                element: e,
                update(v) {
                    value = v;
                    e.textContent = item.integer ? v : parseFloat(v.toFixed(4));
                }
            };
        },

        text(item, send) {
            const e = clickable('input', 'inp_search mowods-text');
            e.type = 'text';
            e.id = 'mowods-text-' + item.id;
            e.setAttribute('data-placeholder', item.name);
            e.setAttribute('data-input-title', item.name);
            e.setAttribute('data-max-length', '256');
            e.addEventListener('click', () => displayKeyboard(e));
            keyboardTargets[e.id] = send;
            return {
                element: e,
                update(v) {
                    e.value = v;
                }
            };
        },

        choice(item, send) {
            return item.buttons ? segmented(item, send) : dropdown(item, send);
        },

        button(item) {
            const e = clickable('div', 'content-btn button mowods-action', item.label);
            e.addEventListener('click', () => engine.call('MowodsClick', item.id));
            return { element: e };
        }
    };

    function segmented(item, send) {
        const e = el('div', 'inp_segmented_control no-scroll');
        const options = item.options.map((label, i) => {
            if (i > 0) e.appendChild(el('div', 'segmented-option-divider'));
            const option = clickable('div', 'segmented-option', label);
            option.addEventListener('mousedown', () => send(i));
            e.appendChild(option);
            return option;
        });
        return {
            element: e,
            update(v) {
                options.forEach((option, i) => setClass(option, 'active', v === i));
            }
        };
    }

    function dropdown(item, send) {
        const e = el('div', 'inp_dropdown no-scroll');
        const shown = clickable('div', 'dropdown-value');
        const list = el('div', 'valueList list-content');
        item.options.forEach((label, i) => {
            if (i > 0) list.appendChild(el('div', 'row-divider'));
            const option = clickable('div', 'listValue', label);
            option.addEventListener('mousedown', ev => {
                ev.stopPropagation();
                closeDropdown();
                send(i);
            });
            list.appendChild(option);
        });
        e.appendChild(shown);
        e.appendChild(el('div', 'dropdown-arrow'));
        e.appendChild(list);

        shown.addEventListener('mousedown', ev => {
            ev.stopPropagation();
            const wasOpen = openDropdown === e;
            closeDropdown();
            if (wasOpen) return;

            e.classList.remove('up');
            e.classList.add('open');
            openDropdown = e;
            // list past page bottom? open up
            afterLayout(() => {
                const view = root.querySelector('.list-content').getBoundingClientRect();
                const rect = e.getBoundingClientRect();
                const roomBelow = view.bottom - rect.bottom;
                const roomAbove = rect.top - view.top;
                if (list.getBoundingClientRect().bottom > view.bottom && roomAbove > roomBelow) e.classList.add('up');
            });
        });

        return {
            element: e,
            update(v) {
                shown.textContent = v >= 0 ? item.options[v] : '';
            }
        };
    }

    function defaultText(item) {
        const d = item.default;
        if (d === null) return null;
        switch (item.kind) {
            case 'toggle': return d ? 'On' : 'Off';
            case 'choice': return item.options[d];
            case 'slider': return d.toFixed(decimalsOf(item.step));
            default: return String(d);
        }
    }

    function tooltipOf(item) {
        const fallback = defaultText(item);
        const lines = [item.tooltip, fallback === null ? null : 'Default: ' + fallback].filter(Boolean);
        return escapeHtml(lines.join('\n')).replace(/\n/g, '<br>');
    }

    function addRow(card, item) {
        const caption = el('div', 'option-caption', item.name);
        const tooltip = tooltipOf(item);
        if (tooltip) caption.setAttribute('data-tooltip', tooltip);

        const input = el('div', 'option-input');
        const row = el('div', 'row-wrapper');
        row.appendChild(caption);
        row.appendChild(input);
        card.appendChild(row);

        const control = controls[item.kind](item, value => engine.call('MowodsSet', item.id, JSON.stringify(value)));
        input.appendChild(control.element);
        if (item.kind === 'button') return;

        const reset = clickable('div', 'inp_slider_reset button mowods-reset');
        reset.setAttribute('data-tooltip', 'Reset to default');
        reset.appendChild(img('gfx/slider-reset.png'));
        reset.addEventListener('click', () => engine.call('MowodsReset', item.id));
        row.appendChild(reset);

        rows[item.id] = { control, reset, isDefault: item.isDefault };
        control.update(item.value);
        hide(reset, item.isDefault);
    }

    function addRows(card, items) {
        items.forEach((item, i) => {
            if (i > 0) card.appendChild(el('div', 'row-divider'));
            addRow(card, item);
        });
    }

    function settingIds(items) {
        return items.filter(item => item.kind !== 'button').map(item => item.id);
    }

    function groupReset(id, name, ids) {
        const button = clickable('div', 'button mowods-pill', 'Reset');
        button.addEventListener('click', () => uiConfirmShow('Reset ' + name, 'Reset every setting in ' + name + ' to its default?', 'mowods-reset', String(id)));
        groups.push({ button, ids });
        return button;
    }

    function refreshGroupResets() {
        groups.forEach(group => hide(group.button, group.ids.every(id => rows[id].isDefault)));
    }

    function newCard() {
        const card = el('div', 'settings-background');
        body.appendChild(card);
        return card;
    }

    function colorDot(colors) {
        const dot = el('div', 'mowods-dot');
        colors.forEach(color => {
            const half = el('div');
            half.style.backgroundColor = color;
            dot.appendChild(half);
        });
        return dot;
    }

    function addHeader(card, name, lines, dot, buttons) {
        const header = el('div', 'settings-subcategory');
        const top = el('div', 'subcategory-row-wrapper mowods-subheader');
        if (dot) top.appendChild(dot);
        top.appendChild(el('div', 'subcategory-name', name));
        buttons.forEach(button => top.appendChild(button));
        header.appendChild(top);
        lines.filter(Boolean).forEach(line => header.appendChild(el('div', 'subcategory-description', line)));
        card.appendChild(header);
    }

    function clear() {
        closeDropdown();
        body.innerHTML = '';
        rows = {};
        groups = [];
    }

    function renderPage(page) {
        clear();
        const buttons = [];
        if (page.hasLink) {
            const link = clickable('div', 'button mowods-pill', 'Open Repo');
            link.addEventListener('click', () => engine.call('MowodsOpenLink', page.id));
            buttons.push(link);
        }
        const ids = page.sections.reduce((all, section) => all.concat(settingIds(section.items)), []);
        buttons.push(groupReset(page.id, page.name, ids));

        // one section = page card holds its rows
        const lone = page.sections.length === 1;
        let card = newCard();
        addHeader(card, page.name, [page.info, lone ? page.sections[0].description : null], colorDot(page.colors), buttons);

        page.sections.forEach(section => {
            if (!lone) {
                card = newCard();
                if (section.name) addHeader(card, section.name, [section.description], null, [groupReset(section.id, section.name, settingIds(section.items))]);
            }
            addRows(card, section.items);
        });

        refreshGroupResets();
    }

    function renderSearch(query, result, message) {
        pages.forEach(page => {
            const count = result.counts[page.id] || 0;
            page.count.textContent = count;
            hide(page.count, false);
            page.button.classList.remove('active');
            setClass(page.button, 'mowods-dim', count === 0);
        });

        clear();
        const total = result.sections.reduce((sum, section) => sum + section.items.length, 0);
        const summary = message || (total === 0
            ? 'We looked everywhere, but could not find what you were looking for!'
            : total + (total === 1 ? ' setting' : ' settings'));
        addHeader(newCard(), 'Results for "' + query + '"', [summary], null, []);
        result.sections.forEach(section => {
            const card = newCard();
            addHeader(card, section.name, [section.description], null, []);
            addRows(card, section.items);
        });
    }

    function endSearch() {
        searchInput.value = '';
        hide(searchHint, false);
        pages.forEach(page => {
            hide(page.count, true);
            page.button.classList.remove('mowods-dim');
        });
    }

    function showPage(page) {
        current = page;
        endSearch();
        pages.forEach(other => setClass(other.button, 'active', other === page));
        engine.call('MowodsPage', page.id).then(json => renderPage(JSON.parse(json)));
    }

    function showCurrent() {
        if (current) return showPage(current);
        clear();
        addHeader(newCard(), title, ['No mod has settings to show.'], null, []);
    }

    function search(text) {
        const query = text.trim();
        if (!query) return showCurrent();

        hide(searchHint, true);
        
        // short query match everything. big dom = cohtml lag
        if (query.length < 3) return renderSearch(query, { sections: [], counts: {} }, "That's a little too broad, don't you think?");

        engine.call('MowodsSearch', query).then(json => renderSearch(query, JSON.parse(json)));
    }

    keyboardTargets[searchInput.id] = search;
    searchInput.addEventListener('click', () => displayKeyboard(searchInput));

    engine.call('MowodsPages').then(json => {
        pages = JSON.parse(json);
        pages.forEach(page => {
            page.button = clickable('div', 'filter-option button mowods-page');
            page.count = el('div', 'result-count mowods-hidden');
            page.button.appendChild(colorDot(page.colors));
            page.button.appendChild(el('div', 'mowods-page-name', page.name));
            page.button.appendChild(page.count);
            page.button.addEventListener('click', () => showPage(page));
            pageList.appendChild(page.button);
        });
        current = pages[0] || null;
    });

    // hidden spacer in home actions row (same slot UI Expansion Kit used)
    const homeButton = document.querySelector('#empty-home-button-block .action-btn');
    homeButton.removeAttribute('style');
    homeButton.innerHTML = '';
    homeButton.appendChild(img(icon));
    homeButton.appendChild(document.createTextNode(title));
    homeButton.addEventListener('click', () => {
        changeTab('mowods', homeButton);
        homeButton.classList.remove('active');
        // search box as wide as page buttons. scrollbar stay outside
        afterLayout(() => { searchBox.style.width = pageList.offsetWidth + 'px'; });
        showCurrent();
    });
})