const qs  = (s, r=document) => r.querySelector(s);

const els = {
  startAc: qs('#start'),             
  endAc:   qs('#end'),
  form:    qs('#routeForm'),
  swapBtn: qs('#swapBtn'),
  sumStart: qs('#sum-start'),
  sumEnd:   qs('#sum-end'),
  steps:     qs('#steps'),
  tabBtnMap:     qs('#tab-btn-map'),     
  tabBtnDetails: qs('#tab-btn-details'),
  tabMap:        qs('#tab-map'),
  tabDetails:    qs('#tab-details'),
};

function bboxFromTwoPoints(a, b, paddingDeg = 0.01) {
  const left   = Math.min(a.lon, b.lon) - paddingDeg;
  const right  = Math.max(a.lon, b.lon) + paddingDeg;
  const bottom = Math.min(a.lat, b.lat) - paddingDeg;
  const top    = Math.max(a.lat, b.lat) + paddingDeg;
  return { left, bottom, right, top };
}

function setOsmIframeForPoints(start, end) {
  const frame = document.getElementById('osmFrame');
  if (!frame) return;

  if (!start && !end) {
    frame.src = `https://www.openstreetmap.org/export/embed.html?bbox=2.20,48.80,2.45,48.92&layer=mapnik`;
    return;
  }

  const p = start || end;
  if (p && !end) {
    const pad = 0.01; // ~1km
    const left = p.lon - pad, right = p.lon + pad, bottom = p.lat - pad, top = p.lat + pad;
    frame.src = `https://www.openstreetmap.org/export/embed.html?bbox=${left},${bottom},${right},${top}&layer=mapnik&marker=${p.lat},${p.lon}`;
    return;
  }

  const bb = bboxFromTwoPoints(start, end, 0.02);
  frame.src = `https://www.openstreetmap.org/export/embed.html?bbox=${bb.left},${bb.bottom},${bb.right},${bb.top}&layer=mapnik&marker=${start.lat},${start.lon}`;
}


function currentPoints() {
  const s = els.startAc?.selection?.coords || null;
  const e = els.endAc  ?.selection?.coords || null;
  return { s, e };
}


els.startAc?.addEventListener('address-selected', ()=>{
  const { s, e } = currentPoints();
  setOsmIframeForPoints(s, e);
});
els.endAc?.addEventListener('address-selected', ()=>{
  const { s, e } = currentPoints();
  setOsmIframeForPoints(s, e);
});

els.swapBtn?.addEventListener('click', ()=>{
  const sSel = els.startAc.selection;
  const eSel = els.endAc.selection;
  els.startAc.setSelection(eSel || null);
  els.endAc.setSelection(sSel || null);
  const { s, e } = currentPoints();
  setOsmIframeForPoints(s, e);
});

els.form?.addEventListener('submit', (e)=>{
  e.preventDefault();
  const { s, e: e2 } = currentPoints();
  setOsmIframeForPoints(s, e2);
});

window.addEventListener('DOMContentLoaded', ()=>{
  const { s, e } = currentPoints();
  setOsmIframeForPoints(s, e);
});


