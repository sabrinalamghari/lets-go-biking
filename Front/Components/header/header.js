class AppHeader extends HTMLElement {
  constructor(){ super(); this.attachShadow({mode:'open'}); }
  async connectedCallback(){
    console.log('[app-header] connected');
    try {
      const htmlURL = new URL('./header.html', import.meta.url);
      const cssURL  = new URL('./header.css',  import.meta.url);
      console.log('[app-header] fetch', htmlURL.href, cssURL.href);

      const [html, css] = await Promise.all([
        fetch(htmlURL).then(r => { if(!r.ok) throw new Error('HTTP '+r.status); return r.text(); }),
        fetch(cssURL).then(r  => { if(!r.ok) throw new Error('HTTP '+r.status); return r.text(); }),
      ]);

      const style = document.createElement('style');
      style.textContent = css;
      this.shadowRoot.innerHTML = html;
      this.shadowRoot.prepend(style);

      const ensure = document.createElement('style');
      ensure.textContent = `:host{display:block} header{display:block}`;
      this.shadowRoot.appendChild(ensure);

      console.log('[app-header] injected OK');
    } catch (e) {
      console.error('[app-header] load error:', e);
      this.shadowRoot.innerHTML = `
        <div style="padding:8px;border:1px dashed #c00;color:#c00;background:#ffecec;border-radius:6px">
          Impossible de charger <code>header.html</code>/<code>header.css</code> (voir console / onglet Network).
        </div>`;
    }
  }
}
customElements.define('app-header', AppHeader);
