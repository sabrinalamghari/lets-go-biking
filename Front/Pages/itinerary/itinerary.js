// ================== LEAFLET MAP ==================
let map, routeLayer;

function initMap() {
    map = L.map('map').setView([45.75, 4.85], 13); // Lyon par défaut

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '&copy; OpenStreetMap'
    }).addTo(map);

    routeLayer = L.layerGroup().addTo(map);
}

function setMapMarkers(start, end) {
    routeLayer.clearLayers();

    if (start) {
        const lon = start.lon ?? start.lng;
        L.marker([start.lat, lon]).addTo(routeLayer);
    }
    if (end) {
        const lon = end.lon ?? end.lng;
        L.marker([end.lat, lon]).addTo(routeLayer);
    }

    if (start && end) {
        const b = L.latLngBounds(
            [start.lat, start.lon ?? start.lng],
            [end.lat, end.lon ?? end.lng]
        );
        map.fitBounds(b.pad(0.2));
    } else if (start || end) {
        const p = start || end;
        map.setView([p.lat, p.lon ?? p.lng], 15);
    }
}

function drawRouteOnMap(route) {
    routeLayer.clearLayers();

    const { s, e } = currentPoints();
    setMapMarkers(s, e);

    if (!route || !route.legs) return;

    route.legs.forEach(leg => {
        if (!Array.isArray(leg.geometry)) return;

        const latlngs = leg.geometry.map(pt => [pt[0], pt[1]]);

        const color =
            leg.type === "bike"
                ? "#0050ff"   // vélo → bleu foncé
                : "#66b3ff";  // marche → bleu clair

        L.polyline(latlngs, {
            color,
            weight: 5,
            opacity: 0.9
        }).addTo(routeLayer);
    });

    // zoom global
    const all = route.legs.flatMap(l => l.geometry.map(pt => [pt[0], pt[1]]));
    if (all.length > 0) {
        const bounds = L.latLngBounds(all);
        map.fitBounds(bounds.pad(0.2));
    }
}


// ================== DOM / UTILS ==================
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


// ================== AFFICHAGE TEXTE ==================
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

    addStep(`Mode : ${route.mode}`, "step-header");
    addStep(`Distance totale : ${formatMeters(route.totalDistanceMeters)}`);
    addStep(`Durée totale : ${formatDuration(route.totalDurationSeconds)}`);
    if (route.note) addStep(route.note);
    addStep("", "step-sep");

    route.legs.forEach((leg, i) => {
        const emoji = leg.type === "bike" ? "🚲" : "🚶";
        addStep(
            `${emoji} Étape ${i + 1} (${leg.type}) — ${formatMeters(leg.distanceMeters)} · ${formatDuration(leg.durationSeconds)}`,
            "step-leg"
        );

        (leg.instructions || []).forEach(rawInstr => {
            const t = translateInstruction(rawInstr);
            const isAction = !!translations[rawInstr];
            const icon = isAction ? iconForAction(t) : "•";
            addStep(`${icon} ${t}`, isAction ? "step-action" : "step-street");
        });

        addStep(" ", "step-gap");
    });

    drawRouteOnMap(route);
    showTab("map"); // si tu veux switcher automatiquement sur la carte
}


// ================== API CALL ==================
async function fetchRoute() {
    const { sSel, eSel } = currentSelections();
    const { s, e } = currentPoints();

    const fromText = (sSel ? selectionLabel(sSel) : (els.startAc?.value || "")).trim();
    const toText = (eSel ? selectionLabel(eSel) : (els.endAc?.value || "")).trim();

    if (!fromText || !toText) {
        clearSteps();
        addStep("Choisis un départ et une arrivée 😅", "step-error");
        return;
    }

    els.sumStart.textContent = fromText;
    els.sumEnd.textContent = toText;

    // marqueurs au moment du calcul
    setMapMarkers(s, e);

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


// ================== TABS ==================
function showTab(name) {
    const isMap = name === "map";
    els.tabBtnMap.classList.toggle("active", isMap);
    els.tabBtnDetails.classList.toggle("active", !isMap);
    els.tabMap.classList.toggle("hidden", !isMap);
    els.tabDetails.classList.toggle("hidden", isMap);
}

els.tabBtnMap?.addEventListener("click", () => showTab("map"));
els.tabBtnDetails?.addEventListener("click", () => showTab("details"));


// ================== EVENTS ==================
els.startAc?.addEventListener('address-selected', () => {
    const { s, e } = currentPoints();
    setMapMarkers(s, e);
});

els.endAc?.addEventListener('address-selected', () => {
    const { s, e } = currentPoints();
    setMapMarkers(s, e);
});

els.swapBtn?.addEventListener('click', () => {
    const sSel = els.startAc.selection;
    const eSel = els.endAc.selection;
    els.startAc.setSelection(eSel || null);
    els.endAc.setSelection(sSel || null);

    const { s, e } = currentPoints();
    setMapMarkers(s, e);
});

els.form?.addEventListener('submit', (ev) => {
    ev.preventDefault();
    fetchRoute();
});

window.addEventListener('DOMContentLoaded', () => {
    initMap();
    showTab("map");
});


// ================== INSTRUCTIONS / TRAD ==================
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
