class AddressAutocomplete extends HTMLElement {
  constructor() {
    super();
    this.attachShadow({ mode: 'open' });

    this._items = [];
    this._highlight = -1;
    this._selection = null;

    let t;
    this._debouncedFetch = (q) => { clearTimeout(t); t = setTimeout(() => this._search(q), 350); };
    this._onOutsideClick = (e) => { if (!this.contains(e.target)) this._close(); };
  }

  async connectedCallback() {
    const [html, css] = await Promise.all([
      fetch(new URL('./address-autocomplete.html', import.meta.url)).then(r => r.text()),
      fetch(new URL('./address-autocomplete.css',  import.meta.url)).then(r => r.text()),
    ]);

    const style = document.createElement('style');
    style.textContent = css;
    this.shadowRoot.innerHTML = html;
    this.shadowRoot.prepend(style);

    this.$input = this.shadowRoot.querySelector('.input');
    this.$list  = this.shadowRoot.querySelector('.list');

    if (this.hasAttribute('placeholder')) this.$input.placeholder = this.getAttribute('placeholder');
    if (this.hasAttribute('value')) this.$input.value = this.getAttribute('value');

    this.$input.setAttribute('aria-controls', this._listboxId());

    this.$input.addEventListener('input', (e) => this._onType(e));
    this.$input.addEventListener('keydown', (e) => this._onKey(e));
    this.$list.addEventListener('click', (e) => this._onClick(e));
    document.addEventListener('click', this._onOutsideClick);
  }

  disconnectedCallback() {
    document.removeEventListener('click', this._onOutsideClick);
  }

  set value(v){ if (this.$input) this.$input.value = v || ''; }
  get value(){ return this.$input ? (this.$input.value || '') : ''; }
  setSelection(sel){ this._selection = sel; this.value = sel?.label || ''; }

  _onType(e) {
    const q = e.target.value.trim();
    this._selection = null; 
    if (q.length < 3) { this._close(); return; }
    this._renderLoading();
    this._debouncedFetch(q);
  }

  _onClick(e) {
    const li = e.target.closest('.item');
    if (!li) return;
    const idx = Number(li.dataset.i);
    this._choose(idx);
  }

  _onKey(e) {
    const max = this._items.length - 1;
    if (e.key === 'ArrowDown') {
      e.preventDefault();
      this._highlight = Math.min(max, this._highlight + 1);
      this._updateHighlight();
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      this._highlight = Math.max(0, this._highlight - 1);
      this._updateHighlight();
    } else if (e.key === 'Enter') {
      if (this._items[this._highlight]) {
        e.preventDefault();
        this._choose(this._highlight);
      }
    } else if (e.key === 'Escape') {
      this._close();
    }
  }
  get selection(){ return this._selection; }

  async _search(q) {
    try {
      const url = `https://api-adresse.data.gouv.fr/search/?q=${encodeURIComponent(q)}&limit=5`;
      const res = await fetch(url);
      const data = await res.json();
      this._items = Array.isArray(data.features) ? data.features : [];
      if (!this._items.length) this._renderEmpty();
      else this._renderList();
    } catch (err) {
      console.error('[address-autocomplete] fetch error', err);
      this._renderError();
    }
  }

  _renderLoading(){ this.$list.innerHTML = `<li class="loading">Chargement…</li>`; this._open(); }
  _renderEmpty(){ this.$list.innerHTML = `<li class="empty">Aucun résultat</li>`; this._open(); }
  _renderError(){ this.$list.innerHTML = `<li class="error">Erreur réseau</li>`; this._open(); }

  _renderList() {
    this._highlight = -1;
    this.$list.innerHTML = this._items.map((f,i)=>
      `<li class="item" role="option" data-i="${i}" id="${this._optionId(i)}">${f.properties.label}</li>`
    ).join('');
    this._open();
  }

  _updateHighlight() {
    const lis = [...this.$list.querySelectorAll('.item')];
    lis.forEach((li, i) => li.setAttribute('aria-selected', String(i === this._highlight)));
    const current = lis[this._highlight];
    if (current) current.scrollIntoView({ block: 'nearest' });
  }

_choose(i) {
  const feature = this._items[i];
  if (!feature) return;
  const [lon, lat] = feature.geometry.coordinates;
  const detail = { label: feature.properties.label, coords: { lat, lon }, feature };
  this._selection = detail;
  this.value = detail.label;
  this._close();
  this.dispatchEvent(new CustomEvent('address-selected', {
    detail, bubbles: true, composed: true
  }));
}

  

  _open(){ this.$list.hidden = false; this.$input.setAttribute('aria-expanded', 'true'); }
  _close(){ this.$list.hidden = true;  this.$input.setAttribute('aria-expanded', 'false'); }
  _listboxId(){ return this._idOrSet(this.$list, 'listbox'); }
  _optionId(i){ return `${this._listboxId()}-opt-${i}`; }
  _idOrSet(el, base){ if (!el.id) el.id = `${base}-${Math.random().toString(36).slice(2,8)}`; return el.id; }
}

customElements.define('address-autocomplete', AddressAutocomplete);
