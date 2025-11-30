// ================= ACTIVE MQ / STOMP =================
let stompClient = null;
let stompConnected = false;
let stompSubscription = null;

// Doit matcher la queue définie en C# : TP_LetsGo
const DESTINATION = "/queue/TP_LetsGo_Weather";

// ================== LEAFLET MAP ==================
let map = null;
let routeLayer = null;

function initMap() {
    // ⚠️ protège contre les doubles appels
    if (map) return;

    map = L.map('map').setView([45.75, 4.85], 13); // Lyon par défaut

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '&copy; OpenStreetMap'
    }).addTo(map);

    routeLayer = L.layerGroup().addTo(map);
}

function setMapMarkers(start, end) {
    if (!routeLayer) {
        console.warn("[MAP] routeLayer pas encore initialisé, setMapMarkers ignoré");
        return;
    }

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
    if (!routeLayer) {
        console.warn("[MAP] routeLayer pas encore init dans drawRouteOnMap");
        return;
    }

    routeLayer.clearLayers();

    const { s, e } = currentPoints();
    setMapMarkers(s, e);

    if (!route || !route.legs) return;

    route.legs.forEach(leg => {
        if (!Array.isArray(leg.geometry)) return;

        const latlngs = leg.geometry.map(pt => [pt[0], pt[1]]);

        const color =
            leg.type === "bike"
                ? "#0050ff"
                : "#66b3ff";

        L.polyline(latlngs, {
            color,
            weight: 5,
            opacity: 0.9
        }).addTo(routeLayer);
    });

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
    notifList: qs('#notif-list'),
    chkMeteo: qs('#chk-meteo'),
    chkPollution: qs('#chk-pollution'),
    chkBikes: qs('#chk-bikes')
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

    // Moins d'une minute
    if (sec < 60) return "< 1 min";

    const minTotal = Math.floor(sec / 60);
    const hTotal = Math.floor(sec / 3600);
    const days = Math.floor(sec / 86400); // 24h * 3600 sec

    const h = hTotal % 24;
    const min = minTotal % 60;

    const minStr = min < 10 ? "0" + min : min; // 05, 07…

    // 🔹 Cas < 1h
    if (days === 0 && h === 0) {
        return `${min} min`;
    }

    // 🔹 Cas < 1 jour (format heures/minutes)
    if (days === 0) {
        return `${h} h ${minStr} min`;
    }

    // 🔹 Cas >= 1 jour
    // Exemples :
    // 1 jour 3h 5”
    // 2 jours 0h 40”
    let result = `${days} j`;

    if (h > 0) {
        result += ` ${h} h`;
    }

    if (min > 0) {
        result += ` ${minStr} min`;
    }

    return result;
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

    addStep(`Mode : ${formatMode(route.mode)}`, "step-header");
    addStep(`Distance totale : ${formatMeters(route.totalDistanceMeters)}`);
    addStep(`Durée totale : ${formatDuration(route.totalDurationSeconds)}`);
    if (route.note) addStep(route.note);
    addStep("", "step-sep");

    route.legs.forEach((leg, i) => {
        const emoji = leg.type === "bike" ? "🚲" : "🚶";
        const typeLabel = formatLegType(leg.type);

        addStep(
            `${emoji} Étape ${i + 1} — ${typeLabel} · ${formatMeters(leg.distanceMeters)} · ${formatDuration(leg.durationSeconds)}`,
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
    connectToActiveMq();
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

function connectToActiveMq() {
    if (stompConnected) return;

    const wsUrl = "ws://localhost:61614"; // ActiveMQ WebSocket STOMP

    // Laisse la lib créer le WebSocket elle-même
    stompClient = Stomp.client(wsUrl);
    stompClient.debug = null; // désactiver les logs verbeux

    stompClient.connect(
        {}, // headers (login/pass si tu en as)
        (frame) => {
            console.log("[STOMP] Connecté à ActiveMQ :", frame);
            stompConnected = true;
            subscribeToQueue();
        },
        (err) => {
            console.error("[STOMP] ERREUR de connexion :", err);
        }
    );
}

function subscribeToQueue() {
    if (!stompConnected || !stompClient) return;

    if (stompSubscription) {
        stompSubscription.unsubscribe();
    }

    stompSubscription = stompClient.subscribe(DESTINATION, (message) => {
        console.log("[STOMP] Reçu : ", message.body);

        try {
            let body = message.body;
            body = body.replace(/\u0000/g, "").trim();

            const alert = JSON.parse(body);
            handleAlert(alert);
        } catch (e) {
            console.error("Message non-JSON ou erreur dans handleAlert :", message.body);
            console.error("Erreur :", e);
        }
    });



    console.log("[STOMP] Abonné à " + DESTINATION);
}


function handleAlert(alert) {
    // Si on n'est pas sur une page avec la zone d'alertes, on ne fait rien
    if (!els.notifList) return;

    const t = alert.type || "other";

    // on vérifie que les checkbox existent avant de lire .checked
    if (t === "meteo" && els.chkMeteo && !els.chkMeteo.checked) return;
    if (t === "pollution" && els.chkPollution && !els.chkPollution.checked) return;
    if (t === "bike" && els.chkBikes && !els.chkBikes.checked) return;

    const li = document.createElement("li");
    li.className = `notif-item ${alert.severity || "info"}`;

    li.textContent = alert.message || "(alerte)";

    els.notifList.prepend(li);

    if (t === "bike" && alert.severity === "critical" && typeof fetchRoute === "function") {
        console.log("[ALERT] Recalcul d'itinéraire suite à une alerte vélo critique");
        fetchRoute();
    }
}

function formatMode(mode) {
    switch (mode) {
        case "walk_only":
            return "À pied uniquement";
        case "bike+walk":
            return "Vélo + marche";
        case "bike+walk+bike":
            return "Vélo + marche + vélo";
        case "error":
            return "Erreur";
        default:
            return mode || "—";
    }
}

function formatLegType(type) {
    switch (type) {
        case "walk":
            return "à pied";
        case "bike":
            return "à vélo";
        default:
            return type || "";
    }
}




