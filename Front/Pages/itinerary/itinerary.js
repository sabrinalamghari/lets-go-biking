const qs = (s, r = document) => r.querySelector(s);

const els = {
    startAc: qs('#start'),
    endAc: qs('#end'),
    form: qs('#routeForm'),
    swapBtn: qs('#swapBtn'),
    sumStart: qs('#sum-start'),
    sumEnd: qs('#sum-end'),
    steps: qs('#steps'),
    tabBtnMap: qs('#tab-btn-map'),
    tabBtnDetails: qs('#tab-btn-details'),
    tabMap: qs('#tab-map'),
    tabDetails: qs('#tab-details'),
};

const API_BASE = "http://localhost:9002";

// -------- MAP (ton code inchangé, juste regroupé) ----------
function bboxFromTwoPoints(a, b, paddingDeg = 0.01) {
    const aLon = a.lon ?? a.lng;
    const bLon = b.lon ?? b.lng;
    const left = Math.min(aLon, bLon) - paddingDeg;
    const right = Math.max(aLon, bLon) + paddingDeg;
    const bottom = Math.min(a.lat, b.lat) - paddingDeg;
    const top = Math.max(a.lat, b.lat) + paddingDeg;
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
    const pLon = p.lon ?? p.lng;

    if (p && !end) {
        const pad = 0.01;
        const left = pLon - pad, right = pLon + pad, bottom = p.lat - pad, top = p.lat + pad;
        frame.src = `https://www.openstreetmap.org/export/embed.html?bbox=${left},${bottom},${right},${top}&layer=mapnik&marker=${p.lat},${pLon}`;
        return;
    }

    const bb = bboxFromTwoPoints(start, end, 0.02);
    const startLon = start.lon ?? start.lng;
    frame.src = `https://www.openstreetmap.org/export/embed.html?bbox=${bb.left},${bb.bottom},${bb.right},${bb.top}&layer=mapnik&marker=${start.lat},${startLon}`;
}


// -------- UTILS selection ----------
function currentSelections() {
    const sSel = els.startAc?.selection || null;
    const eSel = els.endAc?.selection || null;
    return { sSel, eSel };
}

function currentPoints() {
    const s = els.startAc?.selection?.coords || null; // {lat, lon}
    const e = els.endAc?.selection?.coords || null;
    return { s, e };
}

function selectionLabel(sel) {
    return sel?.label || sel?.display_name || sel?.text || sel?.value || "—";
}

function coordsToQuery(coords) {
    if (!coords) return "";
    const lng = (coords.lon !== undefined && coords.lon !== null)
        ? coords.lon
        : coords.lng;
    return `${coords.lat},${lng}`;
}


// -------- AFFICHAGE ----------
function formatMeters(m) {
    if (m == null) return "—";
    if (m < 1000) return `${Math.round(m)} m`;
    return `${(m / 1000).toFixed(1)} km`;
}

function formatDuration(sec) {
    if (sec == null) return "—";
    sec = Math.round(sec);
    const h = Math.floor(sec / 3600);
    const min = Math.floor((sec % 3600) / 60);
    if (h > 0) return `${h} h ${min} min`;
    return `${min} min`;
}

function clearSteps() {
    els.steps.innerHTML = "";
}

function addStep(text, cls = "") {
    const li = document.createElement("li");
    li.textContent = text;
    if (cls) li.className = cls;
    els.steps.appendChild(li);
}

function renderRoute(route) {
    clearSteps();

    if (!route || route.mode === "error") {
        addStep(route?.note || "Erreur inconnue", "step-error");
        return;
    }

    // entête récap
    addStep(`Mode : ${route.mode}`, "step-header");
    addStep(`Distance totale : ${formatMeters(route.totalDistanceMeters)}`);
    addStep(`Durée totale : ${formatDuration(route.totalDurationSeconds)}`);
    if (route.note) addStep(route.note);

    addStep("", "step-sep");

    // legs détaillés
    route.legs.forEach((leg, i) => {
        const emoji = leg.type === "bike" ? "🚲" : "🚶";
        addStep(`${emoji} Étape ${i + 1} (${leg.type}) — ${formatMeters(leg.distanceMeters)} · ${formatDuration(leg.durationSeconds)}`, "step-leg");

        (leg.instructions || []).forEach(rawInstr => {
            const t = translateInstruction(rawInstr);
            const isAction = !!translations[rawInstr];

            const icon = isAction ? iconForAction(t) : "•";

            addStep(`${icon} ${t}`, isAction ? "step-action" : "step-street");
        });


        addStep(" ", "step-gap");
    });
}

// -------- API CALL ----------
async function fetchRoute() {
    const { sSel, eSel } = currentSelections();
    const { s, e } = currentPoints();

    // ✅ Texte fiable : sélection si dispo, sinon ce qui est tapé
    const fromText = (sSel ? selectionLabel(sSel) : (els.startAc?.value || "")).trim();
    const toText = (eSel ? selectionLabel(eSel) : (els.endAc?.value || "")).trim();

    if (!fromText || !toText) {
        clearSteps();
        addStep("Choisis un départ et une arrivée 😅", "step-error");
        return;
    }

    // récap visuel
    els.sumStart.textContent = fromText;
    els.sumEnd.textContent = toText;

    const fromQ = encodeURIComponent(fromText);
    const toQ = encodeURIComponent(toText);

    clearSteps();
    addStep("Calcul en cours…", "step-header");

    try {
        const res = await fetch(`${API_BASE}/route?from=${fromQ}&to=${toQ}`);
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        const data = await res.json();

        renderRoute(data);
        showTab("details");
    } catch (err) {
        clearSteps();
        addStep("Impossible de récupérer l’itinéraire.", "step-error");
        addStep(err.message);
        console.error(err);
    }
}



// -------- TABS ----------
function showTab(name) {
    const isMap = name === "map";
    els.tabBtnMap.classList.toggle("active", isMap);
    els.tabBtnDetails.classList.toggle("active", !isMap);
    els.tabMap.classList.toggle("hidden", !isMap);
    els.tabDetails.classList.toggle("hidden", isMap);
}

els.tabBtnMap?.addEventListener("click", () => showTab("map"));
els.tabBtnDetails?.addEventListener("click", () => showTab("details"));

// -------- EVENTS ----------
els.startAc?.addEventListener('address-selected', () => {
    const { s, e } = currentPoints();
    setOsmIframeForPoints(s, e);
});

els.endAc?.addEventListener('address-selected', () => {
    const { s, e } = currentPoints();
    setOsmIframeForPoints(s, e);
});

els.swapBtn?.addEventListener('click', () => {
    const sSel = els.startAc.selection;
    const eSel = els.endAc.selection;
    els.startAc.setSelection(eSel || null);
    els.endAc.setSelection(sSel || null);

    const { s, e } = currentPoints();
    setOsmIframeForPoints(s, e);
});

els.form?.addEventListener('submit', (ev) => {
    ev.preventDefault();

    // Récupération sélection et coords
    const { sSel, eSel } = currentSelections();
    const { s, e } = currentPoints();

    // ❗ MET LA CARTE À JOUR (coordonnées suffisent)
    setOsmIframeForPoints(s, e);

    // Juste pour debug visuel
    console.log("FROM:", sSel ? sSel.label : els.startAc.value);
    console.log("TO:", eSel ? eSel.label : els.endAc.value);

    fetchRoute(); // ❤️ appelle la version corrigée
});


window.addEventListener('DOMContentLoaded', () => {
    const { s, e } = currentPoints();
    setOsmIframeForPoints(s, e);
    showTab("map");
});



const translations = {
    "depart": "Départ",
    "turn": "Tourner",
    "new name": "Continuer",
    "end of road": "Fin de la route",
    "arrive": "Arrivée",

    "fork": "Prendre la bifurcation",
    "straight": "Continuer tout droit",
    "merge": "S’insérer",
    "roundabout": "Prendre le rond-point",
    "exit roundabout": "Sortir du rond-point",
    "rotary": "Rond-point",
    "on ramp": "Prendre la bretelle",
    "off ramp": "Quitter la bretelle",
    "use lane": "Suivre la voie",
    "continue": "Continuer"
};

function translateInstruction(instr) {
    return translations[instr] || instr;
}

function iconForAction(instr) {
    switch (instr) {
        case "Tourner": return "↪";
        case "Fin de la route": return "⤓";
        case "Prendre la bifurcation": return "⤨";
        default: return "•";
    }
}



