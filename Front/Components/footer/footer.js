class AppFooter extends HTMLElement {
  constructor(){ super(); this.attachShadow({mode:'open'}); }
  async connectedCallback(){
    console.log('[app-footer] connected');
    try {
      const htmlURL = new URL('./footer.html', import.meta.url);
      const cssURL  = new URL('./footer.css',  import.meta.url);
      console.log('[app-footer] fetch', htmlURL.href, cssURL.href);

      const [html, css] = await Promise.all([
        fetch(htmlURL).then(r => { if(!r.ok) throw new Error('HTTP '+r.status); return r.text(); }),
        fetch(cssURL).then(r  => { if(!r.ok) throw new Error('HTTP '+r.status); return r.text(); }),
      ]);

      const style = document.createElement('style');
      style.textContent = css;
      this.shadowRoot.innerHTML = html;
      this.shadowRoot.prepend(style);

      const y = this.shadowRoot.querySelector('#y');
      if (y) y.textContent = new Date().getFullYear();

      const ensure = document.createElement('style');
      ensure.textContent = `:host{display:block} footer{display:block}`;
      this.shadowRoot.appendChild(ensure);

      console.log('[app-footer] injected OK');
    } catch (e) {
      console.error('[app-footer] load error:', e);
      this.shadowRoot.innerHTML = `
        <div style="padding:8px;border:1px dashed #c00;color:#c00;background:#ffecec;border-radius:6px">
          Impossible de charger <code>footer.html</code>/<code>footer.css</code>.
        </div>`;
    }
  }
}
customElements.define('app-footer', AppFooter);
